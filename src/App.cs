using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace PaperDays
{
    public sealed partial class Controller : ApplicationContext
    {
        readonly SerialShellWorker shellWorker = new SerialShellWorker();
        int backgroundRevision;
        string displayBackgroundKey="";
        IntPtr attemptedDesktop;
        MonitorBackground background = new MonitorBackground(Data.Root, delegate
        {
            return new WallpaperBackend();
        });
        State state;
        Scene scene;
        Control dispatcher = new Control();
        NotifyIcon tray;
        List<DesktopSurface> surfaces = new List<DesktopSurface>();
        string screenLayout = "";
        string desktopError = "";
        System.Windows.Forms.Timer timer, refreshTimer, startupTimer;
        Rectangle screen;
        DateTime lastDay = DateTime.Today, nextOnlineRefresh = DateTime.MinValue;
        bool stopping, syncBusy, syncPending, hidden, iconBusy;
        System.Windows.Forms.Timer iconTimer;
        IconSnapshot iconSnapshot;
        Form fallback;
        string iconStatus = "自动识别尚未完成；当前留白不是检测结果。";
        /// <summary>
        /// 初始化桌面控制与应用入口的Controller，准备状态、布局与必要的事件绑定。
        /// </summary>
        public Controller(State state, bool startup = false, bool testHost = false, bool prepared = false)
        {
            isolatedWindows=testHost;
            state.IconColumns = -1;
            this.state = state;
            L.Language = state.Language;
            WindowPlacement.Target = delegate
            {
                var m = DisplayOption.Resolve(this.state.ScreenDevice, DisplayOption.Current());
                return m.WorkArea.IsEmpty ? m.Bounds : m.WorkArea;
            };
            try
            {
                if (!testHost && Wallpaper.Startup())
                    Wallpaper.SetStartup(true);
            }
            catch (Exception ex)
            {
                Notice.Show("已有自启动入口未能更新到当前版本：" + ex.Message);
            }

            this.state = state;
            L.Language = state.Language;
            if (!testHost && !prepared)
            {
                DailyArchive.Enable(state, DateTime.Today);
                Data.Write(state);
            }

            StartupTiming.Mark("Archive ready");
            Skins.Apply(state);
            scene = new Scene
            {
                State = state
            };
            scene.ClearFortune();
            screen = Screen.PrimaryScreen.Bounds;
            IntPtr handle = dispatcher.Handle;
            if(!testHost)StartDisplayWatch();
            tray = new NotifyIcon
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath),
                Visible = true,
                Text = L.T("桌面日历 ") + Program.Version + L.T(" · 壁纸日历")
            };
            var menu = new ThemedMenu();
            var autostart = new ToolStripMenuItem(L.T("开机自启动"));
            menu.Opening += delegate {
                L.RefreshMenu(menu.Items);
                try { autostart.Checked = Wallpaper.Startup(); } catch { autostart.Checked = false; }
            };
            AddComponentPreferences(menu);
            menu.Items.Add(L.T("显示屏幕"), null, delegate { ChooseScreen(); });
            autostart.Click += delegate { ToggleStartup(false); };
            menu.Items.Add(autostart);
            AddLanguageMenu(menu);
            menu.Items.Add(L.T("退出"), null, delegate { Stop(); });
            tray.ContextMenuStrip = menu;
            refreshTimer = new System.Windows.Forms.Timer
            {
                Interval = 15
            };
            refreshTimer.Tick += delegate
            {
                refreshTimer.Stop();
                RefreshSurface();
            };
            timer = new System.Windows.Forms.Timer
            {
                Interval = 15000
            };
            timer.Tick += delegate
            {
                Tick();
            };
            timer.Start();
            iconTimer = new System.Windows.Forms.Timer
            {
                Interval = 3000
            };
            iconTimer.Tick += delegate
            {
                RefreshIcons();
            };
            iconTimer.Start();
            hidden = state.StartInTray || testHost;
            dispatcher.BeginInvoke((Action)delegate
            {
                ReloadCalendars();
                if (hidden)
                    return;
                if (startup)
                    WaitForDesktop();
                else
                    Connect();
            });
        }

        /// <summary>
        /// 等待 Explorer 就绪并安排重试；超时显示可操作的备用窗口。
        /// </summary>
        void WaitForDesktop()
        {
            if (DesktopSurface.FindDesktop() != IntPtr.Zero)
            {
                Connect();
                return;
            }

            StartupTiming.Mark("Waiting for Explorer");
            int attempts = 0;
            startupTimer = new System.Windows.Forms.Timer
            {
                Interval = 500
            };
            startupTimer.Tick += delegate
            {
                if (stopping)
                {
                    startupTimer.Stop();
                    return;
                }

                attempts++;
                if (DesktopSurface.FindDesktop() != IntPtr.Zero || attempts >= 30)
                {
                    startupTimer.Stop();
                    Connect();
                }
            };
            startupTimer.Start();
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 ToggleStartup 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void ToggleStartup(bool announce = true)
        {
            try
            {
                bool enabled = !Wallpaper.Startup();
                Wallpaper.SetStartup(enabled);
                if (announce)
                    Notice.Show(enabled ? "已开启开机自启动：登录 Windows 后会自动打开日历。请将程序保留在当前文件夹；也可在 Windows 启动应用中检查是否被禁用。" : L.T("已关闭开机自启动。"), L.T("开机自启动"));
            }
            catch (Exception ex)
            {
                Notice.Show(L.T("自启动设置未保存：") + ex.Message);
            }
        }

        /// <summary>
        /// 读取本地假日并启动异步更新，立即保留缓存显示。
        /// </summary>
        void ReloadCalendars()
        {
            L.Language = state.Language;
            Skins.Apply(state);
            string status;
            scene.Holidays = Calendars.Load(state, scene.Month, out status);
            scene.State = state;
            scene.Status = status;
            SyncOnline();
        }

        /// <summary>
        /// 后台更新假日与星座数据，合并重复请求后刷新界面。
        /// </summary>
        async void SyncOnline()
        {
            if (stopping)
                return;
            if (syncBusy)
            {
                syncPending = true;
                return;
            }

            syncBusy = true;
            nextOnlineRefresh = DateTime.UtcNow.AddMinutes(30);
            DateTime month = scene.Month;
            string[] codes = state.Countries.ToArray();
            string sign = state.Zodiac;
            try
            {
                var holidays = HolidayUpdates.UpdateAsync(codes, month);
                try
                {
                    await Horoscopes.UpdateAsync(sign, DateTime.Today);
                }
                catch
                {
                }

                string errors = await holidays;
                if (stopping)
                    return;
                string status;
                scene.Holidays = Calendars.Load(state, scene.Month, out status);
                scene.Status = status + (String.IsNullOrEmpty(errors) ? "" : " · 部分数据未更新，点此查看");
                RequestRender();
            }
            catch (Exception ex)
            {
                if (!stopping)
                {
                    scene.Status = L.T("更新未完成：") + ex.Message;
                    RequestRender();
                }
            }
            finally
            {
                syncBusy = false;
                if (syncPending && !stopping)
                {
                    syncPending = false;
                    SyncOnline();
                }
            }
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 InteractivePanel 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void InteractivePanel()
        {
            var form = new Form
            {
                Text = L.T("桌面日历 · 交互面板"),
                ClientSize = new Size(1440, 900),
                StartPosition = FormStartPosition.CenterScreen,
                MinimumSize = new Size(960, 600)
            };
            fallback = form;
            var panel = new SceneView(scene)
            {
                Dock = DockStyle.Fill
            };
            panel.SaveInlineTodo = SaveInlineTodo;
            panel.HitAction = h => Dispatch(h, false);
            form.Controls.Add(panel);
            panel.Clicked = delegate (Point point, bool dbl)
            {
                Hit hit = scene.At(point, panel.ClientSize);
                if (hit != null)
                {
                    Dispatch(hit, dbl);
                    panel.Invalidate();
                }
            };
            WindowPlacement.Prepare(form);
            form.Show();
            scene.ScreenDpi = UiDpi.Of(form);
            form.Activate();
            StartupTiming.Mark("Fallback calendar displayed");
        }

        /// <summary>
        /// 合并短时间内的刷新请求，避免重复绘制。
        /// </summary>
        void RequestRender()
        {
            refreshTimer.Stop();
            refreshTimer.Start();
        }

        /// <summary>
        /// 刷新当前桌面与备用视图的内容。
        /// </summary>
        void SyncDisplayBackground() {
            if(isolatedWindows||stopping||!wallpaperReady)return;
            var monitor=DisplayOption.Resolve(state.ScreenDevice,DisplayOption.Current());
            bool solid=!hidden&&DesktopAppearance.NeedsSolidBackground(state);
            string key=solid?monitor.Device+monitor.Bounds+Palette.Cream.ToArgb():"original";
            if(key==displayBackgroundKey)return;displayBackgroundKey=key;
            QueueBackground(monitor.Bounds,Palette.Cream,solid,true);
        }
        void RefreshSurface()
        {
            scene.State = state;
            SyncDisplayBackground();

            foreach (var surface in surfaces)
                if (surface.Connected)
                {
                    surface.SynchronizeLayout();
                    surface.Invalidate();
                    // Let the message loop coalesce repeated paint requests.
                }

            foreach (Form f in Application.OpenForms)
                foreach (Control c in f.Controls)
                    if (c is SceneView)
                        c.Invalidate();
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 ScreenLayout 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static string ScreenLayout()
        {
            return String.Join(";", Screen.AllScreens.Select(x => x.DeviceName + ":" + x.Bounds + ":" + x.WorkingArea + ":" + IconArea.Dpi(x.Bounds)));
        }

        /// <summary>
        /// 连接当前选择的桌面窗口，先绘制日历，再排队更新系统壁纸。
        /// </summary>
        bool connecting, wallpaperReady;
        async void Connect()
        {
            if (hidden || stopping || connecting) return;
            connecting=true;
            try { if(!wallpaperReady) { await shellWorker.Run(delegate { background.Restore(); Wallpaper.Restore(); }); wallpaperReady=true; } }
            catch(Exception ex) { desktopError=ex.Message; }
            finally { connecting=false; }
            if(hidden || stopping) return;
            if (fallback != null)
            {
                fallback.Dispose();
                fallback = null;
            }

            if (startupTimer != null)
                startupTimer.Stop();
            foreach (var old in surfaces)
                old.Dispose();
            surfaces.Clear();
            screenLayout = ScreenLayout();
            if(!state.ShowCalendar && !state.ShowWidget && !state.ShowPlans) {
                scene.MonitorKey=DisplayOption.Resolve(state.ScreenDevice,DisplayOption.Current()).Device;
                scene.Hits.Clear();desktopError="";attemptedDesktop=DesktopSurface.FindDesktop();WriteDiagnostics();return;
            }
            ReloadCalendars();
            var errors = new List<string>();
            StartupTiming.Mark("Connecting desktop");
            IntPtr parent = DesktopSurface.FindDesktop();
            attemptedDesktop = parent;
            var options = DisplayOption.Current();
            var monitor = DisplayOption.Resolve(state.ScreenDevice, options);
            scene.ReservedLeft = state.IconColumns < 0 && iconSnapshot != null && iconSnapshot.Success ? DesktopIcons.Reserve(monitor.Bounds, iconSnapshot.Bounds, IconArea.Dpi(monitor.Bounds)) : IconArea.For(monitor, options.Length, state.IconColumns, parent);
            DesktopSurface surface = null;
            try
            {
                scene.ScreenDpi = (int)(96 * IconArea.Dpi(monitor.Bounds));
                scene.MonitorKey = monitor.Device;
                surface = new DesktopSurface(scene, parent);
                var view = surface;
                view.SaveInlineTodo = SaveInlineTodo;
                view.HitAction = h => Dispatch(h, false);
                view.LayoutSaved = () => { Change(delegate { state.WidgetLayouts=scene.State.WidgetLayouts; }); };
                view.Clicked = delegate (Point point, bool dbl)
                {
                    if (stopping)
                        return;
                    Hit hit = scene.At(point, view.ClientSize);
                    if (hit != null)
                        Dispatch(hit, dbl);
                };
                view.Attach(monitor.WorkArea.IsEmpty ? monitor.Bounds : monitor.WorkArea);
                surfaces.Add(view);
                StartupTiming.Mark("Calendar first paint completed");
                if(!isolatedWindows && state.GuideVersion<2)dispatcher.BeginInvoke((Action)OpenGuide);

            }
            catch (Exception ex)
            {
                if (surface != null)
                    surface.Dispose();
                errors.Add(monitor.Device + "：" + ex.Message + (ex is System.ComponentModel.Win32Exception ? "（Windows 错误 " + ((System.ComponentModel.Win32Exception)ex).NativeErrorCode + "）" : ""));
            }

            desktopError = String.Join("\r\n", errors);
            RefreshSurface();
            WriteDiagnostics();
            if (surfaces.Count != 1)
            {
                Notice.Show("未能连接桌面日历。\n" + desktopError + "\n普通交互窗口仅供临时使用，不代表桌面模式成功。", L.T("桌面连接结果"));
                InteractivePanel();
            }
            else if (errors.Count > 0)
                Notice.Show(desktopError, L.T("背景状态"));
            RefreshIcons();
        }

        // All shell wallpaper operations share one STA queue; stale applies cannot overtake restore.
        /// <summary>
        /// 按版本号串行提交壁纸操作，跳过过期换色请求，避免恢复与换色竞争。
        /// </summary>
        async void QueueBackground(Rectangle bounds, Color color, bool apply, bool restore)
        {
            if(isolatedWindows)return;
            int revision = System.Threading.Interlocked.Increment(ref backgroundRevision);
            try
            {
                await shellWorker.Run(delegate
                {
                    if (!restore && revision != System.Threading.Volatile.Read(ref backgroundRevision))
                        return;
                    StartupTiming.Mark("Wallpaper operation started");
                    if (restore)
                    {
                        background.Restore();
                        Wallpaper.Restore();
                    }

                    if (apply && revision == System.Threading.Volatile.Read(ref backgroundRevision))
                        background.Apply(bounds, color);
                    StartupTiming.Mark("Wallpaper operation completed");
                });
                if (!stopping) foreach(var surface in surfaces) surface.RefreshGlassWallpaper();
            }
            catch (Exception ex)
            {
                StartupTiming.Mark("Wallpaper operation failed: " + ex.GetType().Name);
                if (!stopping && !hidden)
                {
                    desktopError = "图标区背景未能更新，日历仍可使用：" + ex.Message;
                    WriteDiagnostics();
                }
            }
        }

        /// <summary>
        /// 提交输入后收起日历，异步恢复原壁纸。
        /// </summary>
        async void HideCalendar()
        {
            if (hidden)
                return;
            foreach (var surface in surfaces)
                if (!surface.FinishInline())
                    return;
            hidden = true;
            if (startupTimer != null)
                startupTimer.Stop();
            System.Threading.Interlocked.Increment(ref backgroundRevision);
            var errors = ShutdownFlow.Run(() =>
            {
                foreach (var surface in surfaces)
                    surface.Dispose();
                surfaces.Clear();
                if (fallback != null)
                {
                    fallback.Dispose();
                    fallback = null;
                }
            });
            errors.AddRange(await shellWorker.Run(() => ShutdownFlow.Run(() => background.Restore(), () => Wallpaper.Restore())));
            if (errors.Count > 0)
                Notice.Show("日历已收起，背景恢复未完成：" + String.Join("；", errors));
            WriteDiagnostics();
        }

        /// <summary>
        /// 从托盘恢复日历并连接桌面。
        /// </summary>
        void ShowCalendar()
        {
            if (stopping)
                return;
            if (!state.ShowCalendar) {
                Change(delegate { state.ShowCalendar = true; });
                RefreshSurface();
            }
            if (!hidden && surfaces.Count > 0) return;
            hidden = false;
            Connect();
        }

        /// <summary>
        /// 刷新桌面控制与应用入口中的 RefreshIcons 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void RefreshIcons()
        {
            if (hidden || stopping || state.IconColumns >= 0 || iconBusy)
                return;
            iconBusy = true;
            try
            {
                var result = await DesktopIcons.ReadAsync();
                if (stopping)
                    return;
                if (result.Success)
                {
                    iconSnapshot = result;
                    iconStatus = L.T("检测成功 · ") + result.Bounds.Count + L.T(" 个图标 · ") + result.Method;
                    ApplyIconReserve();
                }
                else
                    iconStatus = "自动识别失败，当前保留原留白；请手动设置。\r\n" + result.Error;
                WriteDiagnostics();
            }
            finally
            {
                iconBusy = false;
            }
        }

        /// <summary>
        /// 按图标实际占用更新留白区域，必要时排队换壁纸。
        /// </summary>
        void ApplyIconReserve()
        {
            if (hidden || stopping)
                return;
            var options = DisplayOption.Current();
            var monitor = DisplayOption.Resolve(state.ScreenDevice, options);
            int width = state.IconColumns < 0 && iconSnapshot != null && iconSnapshot.Success ? DesktopIcons.Reserve(monitor.Bounds, iconSnapshot.Bounds, IconArea.Dpi(monitor.Bounds)) : IconArea.For(monitor, options.Length, state.IconColumns, DesktopSurface.FindDesktop());
            if (width == scene.ReservedLeft)
                return;
            int previous = scene.ReservedLeft;
            scene.ReservedLeft = width;
            foreach (var surface in surfaces)
                surface.UpdateRegion();
            RefreshSurface();
            SyncDisplayBackground();
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 IconReport 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        string IconReport()
        {
            var options = DisplayOption.Current();
            var selected = DisplayOption.Resolve(state.ScreenDevice, options);
            return "PaperDays 0.37 · " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + iconStatus + Environment.NewLine + L.T("模式：") + (state.IconColumns < 0 ? L.T("自动") : state.IconColumns + " 列（手动）") + Environment.NewLine + L.T("所选屏幕：") + selected.Device + Environment.NewLine + L.T("当前留白：") + scene.ReservedLeft + L.T(" 像素") + Environment.NewLine + String.Join(Environment.NewLine, options.Select(m => m.Device + " " + m.Bounds + L.T(" 缩放 ") + IconArea.Dpi(m.Bounds))) + Environment.NewLine + "上次成功检测位置：" + Environment.NewLine + (iconSnapshot == null ? L.T("无") : String.Join(Environment.NewLine, iconSnapshot.Bounds.Select(b => b.ToString())));
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 DiagnosticReport 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        string DiagnosticReport()
        {
            return L.T("版本：") + Program.Version + "\r\n屏幕布局：" + screenLayout + "\r\n已连接屏幕：" + surfaces.Count + " / 1" + "\r\n" + String.Join("\r\n", surfaces.Select(x => x.Describe())) + "\r\n国家日历：" + String.Join(", ", state.Countries) + "\r\n" + scene.Status + "\r\n" + desktopError + "\r\n" + iconStatus + "\r\n收起到托盘：" + hidden;
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 WriteDiagnostics 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void WriteDiagnostics()
        {
            try
            {
                Directory.CreateDirectory(Data.Root);
                File.WriteAllText(Path.Combine(Data.Root, "desktop-status.txt"), DiagnosticReport());
            }
            catch
            {
            }
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 Dispatch 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void Dispatch(Hit hit, bool dbl)
        {
            if (hit.Action == "timeslot")
            {
                scene.Selected = hit.Date;
                if (dbl)
                    Edit(null, hit.Date, null, null, true);
                RequestRender();
                return;
            }

            if (hit.Action == "date")
            {
                scene.Selected = hit.Date;
                if (dbl)
                    Edit(null, hit.Date);
                RequestRender();
                return;
            }

            if (hit.Action == "todo")
            {
                Change(delegate
                {
                    state.ToggleTodo(hit.Id);
                });
                return;
            }

            if (dbl)
                return;
            if (DispatchComponent(hit)) return;
            switch (hit.Action)
            {
                case "prev":
                    if (scene.Month.Year > 1901)
                    {
                        scene.Navigate(-1);
                        ReloadCalendars();
                        RequestRender();
                    }

                    break;
                case "next":
                    if (scene.Month.Year < 2099)
                    {
                        scene.Navigate(1);
                        ReloadCalendars();
                        RequestRender();
                    }

                    break;
                case "viewmonth":
                    Change(delegate
                    {
                        state.CalendarView = "month";
                        scene.Month = scene.Selected;
                    });
                    ReloadCalendars();
                    break;
                case "viewweek":
                    Change(delegate
                    {
                        state.CalendarView = "week";
                    });
                    ReloadCalendars();
                    break;
                case "weekequal":
                    Change(delegate
                    {
                        state.WeekEqualHours = !state.WeekEqualHours;
                    });
                    break;
                case "today":
                    scene.Month = scene.Selected = DateTime.Today;
                    ReloadCalendars();
                    RequestRender();
                    break;
                case "new":
                    Edit(null, scene.Selected);
                    break;
                case "event":
                    var item = state.Events.FirstOrDefault(e => e.Id == hit.Id);
                    if (item != null)
                        Edit(item, item.Start.Date);
                    break;
                case "day":
                    DayDetails(hit.Date);
                    break;
                case "settings":
                    Settings();
                    break;
                case "tools":
                    ToolsMenu();
                    break;
                case "leftzodiac":
                    Change(delegate
                    {
                        state.Widget = "星座日程";
                    });
                    break;
                case "leftfortune":
                    if(state.Widget!="占卜")scene.ClearFortune();
                    Change(delegate
                    {
                        state.Widget = "占卜";
                    });
                    break;
                case "screen":
                    ChooseScreen();
                    break;
                case "widgetzodiac":
                    ShowZodiac();
                    break;
                case "widgetfortune":
                    Divine(true);
                    break;
                case "tododelete":
                    Change(delegate
                    {
                        state.DeleteTodo(hit.Id);
                    });
                    break;
                case "zodiacchoose":
                    await ChooseZodiac();
                    break;
                case "reading":
                    ReadingDetails();
                    break;
                case "holidaystatus":
                case "sources":
                    CalendarSummary();
                    break;
                case "plandaily":
                    SetPlan("day");
                    break;
                case "planweek":
                    SetPlan("week");
                    break;
                case "planmonth":
                    SetPlan("month");
                    break;
                case "planprev":
                    ShiftPlan(-1);
                    break;
                case "plannext":
                    ShiftPlan(1);
                    break;
                case "planexport":
                    ExportPlan();
                    break;
                case "todo":
                    Change(delegate
                    {
                        var todo = state.Todos.FirstOrDefault(t => t.Id == hit.Id);
                        if (todo != null)
                            todo.Done = !todo.Done;
                    });
                    break;
                case "addtodo":
                    AddTodo();
                    break;
                case "todofull":
                    TodoFull(hit.Id);
                    break;
                case "todohistory":
                    TodoRecords();
                    break;
                case "todoprev":
                    Change(delegate
                    {
                        state.TodoPage = Math.Max(0, state.TodoPage - 1);
                    });
                    break;
                case "todonext":
                    Change(delegate
                    {
                        int count = scene.PlanItems().Count;
                        state.TodoPage = Math.Min(Math.Max(0, scene.PlanMode == "day" ? scene.TodoPageCount - 1 : (count - 1) / 8), state.TodoPage + 1);
                    });
                    break;
                case "drawfortune":
                    Divine(false);
                    break;
                case "fortunecat0":
                case "fortunecat1":
                case "fortunecat2":
                case "fortunecat3":
                case "fortunecat4":
                    Change(delegate
                    {
                        state.FortuneCategory = hit.Id;
                        if (state.Divination != null)
                            state.Divination.Category = hit.Id;
                    });
                    break;
            }
        }

        /// <summary>
        /// 保存行内待办文字，通过统一事务入口维护历史归档。
        /// </summary>
        bool SaveInlineTodo(string id, string text)
        {
            return Change(delegate
            {
                var todo = state.Todos.FirstOrDefault(t => t.Id == id);
                if (todo == null)
                    state.Todos.Add(new Todo { Title = text });
                else
                    todo.Title = text;
            });
        }

        /// <summary>
        /// 保存变更前快照，更新归档并落盘；失败时恢复原状态。
        /// </summary>
        bool Change(Action action)
        {
            string old = Data.Json().Serialize(state);
            try
            {
                DailyArchive.Sync(state, DateTime.Today);
                action();
                DailyArchive.Sync(state, DateTime.Today);
                Data.Write(state);
                scene.State = state;
                RequestRender();
                return true;
            }
            catch (Exception ex)
            {
                state = Data.Json().Deserialize<State>(old);
                scene.State = state;
                Skins.Apply(state);
                Notice.Show(L.T("保存失败，已保留原数据：") + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 Modal 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        System.Threading.Tasks.Task<DialogResult> Modal(Form form)
        {
            return IndependentWindows.Show(form);
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 Edit 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void Edit(Entry entry, DateTime date, string suggested = null, string emoji = null, bool timeSlot = false)
        {
            using (var editor = new Editor(state, entry, date, timeSlot))
            {
                if (suggested != null)
                {
                    editor.TitleInput.Text = suggested;
                    editor.EmojiInput.Text = emoji;
                    editor.EmojiMode.Checked = true;
                }

                editor.Commit=candidate=>Change(delegate {
                    if(entry!=null)state.Events.RemoveAll(e=>e.Id==entry.Id);
                    state.Events.Add(candidate);
                    if(entry!=null&&editor.SameCourse!=null&&editor.SameCourse.Checked)
                        foreach(var lesson in state.Events.Where(e=>e.CalendarId==entry.CalendarId&&e.Id.StartsWith("course-")))lesson.CopyStyle(candidate);
                });
                if(await Modal(editor)==DialogResult.OK&&editor.Deleted&&entry!=null)
                    Change(delegate{state.Events.RemoveAll(e=>e.Id==entry.Id);});
            }
        }

        /// <summary>
        /// 添加桌面控制与应用入口中的 AddTodo 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void AddTodo()
        {
            using (var dialog = new TodoDetails())
            {
                if (await Modal(dialog) != DialogResult.OK || stopping)
                    return;
                Change(delegate
                {
                    state.Todos.Add(new Todo { Title = dialog.Result });
                    state.TodoPage = (state.Todos.Count(t => t.Date == DateTime.Today.ToString("yyyy-MM-dd")) - 1) / 8;
                });
            }
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 TodoFull 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void TodoFull(string id)
        {
            var archived = state.TodoArchive.FirstOrDefault(t => t.Id == id);
            if (archived != null)
            {
                using (var detail = DailyArchive.View(archived))
                    await Modal(detail);
                return;
            }

            var todo = state.Todos.FirstOrDefault(t => t.Id == id);
            if (todo == null)
                return;
            using (var dialog = new TodoDetails(todo))
            {
                if (await Modal(dialog) != DialogResult.OK || stopping)
                    return;
                Change(delegate
                {
                    var current = state.Todos.FirstOrDefault(t => t.Id == id);
                    if (current != null)
                    {
                        if (dialog.Deleted)
                            state.DeleteTodo(id);
                        else
                            current.Title = dialog.Result;
                    }
                });
            }
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 TodoRecords 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void TodoRecords()
        {
            using (var dialog = new TodoHistory(() => state, TodoFull, ids => Change(delegate
            {
                foreach (string id in ids)
                    state.DeleteTodo(id);
            })))
                await Modal(dialog);
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 DayDetails 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void DayDetails(DateTime date)
        {
            using (var form = new Form())
            {
                UI.Style(form, L.Date(date, "yyyy 年 M 月 d 日", "MMM d, yyyy"), 550, 520);
                var panel = new FlowLayoutPanel
                {
                    Dock = DockStyle.Fill,
                    Padding = new Padding(20),
                    FlowDirection = FlowDirection.TopDown,
                    WrapContents = false,
                    AutoScroll = true
                };
                form.Controls.Add(panel);
                foreach (var m in state.Marks.Concat(scene.Holidays).Where(x => x.Date == date.ToString("yyyy-MM-dd")))
                    panel.Controls.Add(new Label { Text = HolidayStyle.Label(m) + "\n" + m.Source, AutoSize = true, MaximumSize = new Size(490, 0), Margin = new Padding(5, 4, 5, 18) });
                Entry chosen = null;
                bool add = false;
                foreach (var e in state.VisibleEvents().Where(x => x.On(date)).OrderBy(x => x.Start))
                {
                    var ev = e;
                    var b = new SoftButton
                    {
                        Text = (e.UseEmoji ? e.Emoji : "●") + " " + e.Title + " · " + (e.AllDay ? L.T("全天") : e.Start.ToString("HH:mm")),
                        Width = 485,
                        Height = 40,
                        ForeColor = ColorTranslator.FromHtml(e.Color),
                        FlatStyle = FlatStyle.Flat
                    };
                    b.Click += delegate
                    {
                        chosen = ev;
                        form.Close();
                    };
                    panel.Controls.Add(b);
                }

                var plus = new SoftButton
                {
                    Text = L.T("＋ 添加日程"),
                    Width = 485,
                    Height = 38
                };
                plus.Click += delegate
                {
                    add = true;
                    form.Close();
                };
                panel.Controls.Add(plus);
                await Modal(form);
                if (chosen != null)
                    Edit(chosen, date);
                else if (add)
                    Edit(null, date);
            }
        }

        /// <summary>
        /// 编辑设置并保留原有日程、待办及课程资料。
        /// </summary>
        async void Settings()
        {
            bool wasStartup = false;
            string startupError = "";
            try
            {
                wasStartup = Wallpaper.Startup();
            }
            catch (Exception ex)
            {
                startupError = ex.Message;
            }

            using (var settings = new SettingsDialog(state, wasStartup, scene.Month, startupError))
            {
                if (await Modal(settings) != DialogResult.OK)
                    return;
                string previousScreen = state.ScreenDevice;
                State next = settings.Result;
                next.Events = state.Events.Where(e=>!settings.DeletedCalendarIds.Contains(e.CalendarId)).ToList();
                next.Events.AddRange(settings.AddedEntries.Where(e => !settings.DeletedCalendarIds.Contains(e.CalendarId) && !next.Events.Any(x => x.Id == e.Id)));
                next.Todos = state.Todos.ToList();
                next.Marks = state.Marks.ToList();
                foreach (var mark in settings.AddedMarks)
                {
                    next.Marks.RemoveAll(x => x.Date == mark.Date && x.Source == mark.Source && x.Label == mark.Label);
                    next.Marks.Add(mark);
                }

                next.Divination = state.Divination;
                next.Skin = state.Skin;
                next.IconColumns = state.IconColumns;
                next.ShowCalendar=state.ShowCalendar; next.ShowPlans=state.ShowPlans; next.ShowWidget=state.ShowWidget; next.WidgetLayouts=state.WidgetLayouts;
                next.LanguageConfirmed=state.LanguageConfirmed;next.GuideVersion=state.GuideVersion; next.SkinDisplayModes=state.SkinDisplayModes; next.FillScreen = state.FillScreen; next.GlassRelief = state.GlassRelief; next.GlassPreferences=state.GlassPreferences;
                next.GlassBaseSkin = state.GlassBaseSkin;
                next.TransparentDesktop = state.TransparentDesktop;
                next.PanelOpacity = state.PanelOpacity;
                next.Language = state.Language;
                foreach (var cal in state.Calendars.Where(c => c.Id.StartsWith("course-type-") || c.Id.StartsWith("class-")))
                    if (!settings.DeletedCalendarIds.Contains(cal.Id) && !next.Calendars.Any(c => c.Id == cal.Id))
                        next.Calendars.Add(cal);
                if (Change(delegate
                {
                    state = next;
                }))
                {
                    ReloadCalendars();
                    if (previousScreen != state.ScreenDevice)
                        Connect();
                    RefreshSurface();
                    if (String.IsNullOrEmpty(startupError) && (settings.Startup || settings.Startup != wasStartup))
                        try
                        {
                            Wallpaper.SetStartup(settings.Startup);
                        }
                        catch (Exception ex)
                        {
                            Notice.Show("日历和偏好已保存，但开机启动未能修改：\n" + ex.Message);
                        }
                }
            }
        }

        /// <summary>
        /// 切换计划查看范围并重置分页。
        /// </summary>
        void SetPlan(string mode)
        {
            scene.PlanMode = mode;
            scene.PlanAnchor = DateTime.Today;
            state.TodoPage = 0;
            RefreshSurface();
        }

        /// <summary>
        /// 前后移动计划日期范围。
        /// </summary>
        void ShiftPlan(int direction)
        {
            scene.PlanAnchor = scene.PlanMode == "month" ? scene.PlanAnchor.AddMonths(direction) : scene.PlanAnchor.AddDays(direction * 7);
            state.TodoPage = 0;
            RefreshSurface();
        }

        /// <summary>
        /// 导出本地周报或月报材料及详细提示词，不自动发送到外部服务。
        /// </summary>
        void ExportPlan()
        {
            using (var save = new SaveFileDialog
            {
                Filter = L.T("纯文本 (*.txt)|*.txt"),
                FileName = (scene.PlanMode == "month" ? "AI月报素材-" : "AI周报素材-") + PlanReview.Start(scene.PlanMode, scene.PlanAnchor).ToString("yyyyMMdd") + ".txt"
            }

            )
                if (WindowPlacement.Show(save) == DialogResult.OK)
                    try
                    {
                        File.WriteAllText(save.FileName, PlanReview.Export(state, scene.PlanMode, scene.PlanAnchor), System.Text.Encoding.UTF8);
                        Notice.Show("已导出 TXT，包含全部完成记录和 AI 周报／月报提示词。把文件上传给你使用的 AI，或复制全文，即可生成汇报草稿；本程序不会自动发送给 AI。", L.T("导出完成"));
                    }
                    catch (Exception ex)
                    {
                        Notice.Show(L.T("导出失败：") + ex.Message);
                    }
        }

        /// <summary>
        /// 显示工具操作菜单，关闭后延迟释放以避免点击回调访问已释放对象。
        /// </summary>
        void ToolsMenu() { ShowOrganizedTools(); }

        /// <summary>
        /// 选择桌面控制与应用入口中的 ChooseIconArea 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void ChooseIconArea()
        {
            using (var f = new IconAreaDialog(state.IconColumns, iconStatus, delegate
            {
                return IconReport();
            }))
                if (await Modal(f) == DialogResult.OK && Change(delegate
                {
                    state.IconColumns = f.Columns;
                }))
                {
                    ApplyIconReserve();
                    RefreshIcons();
                }
        }

        /// <summary>
        /// 选择桌面控制与应用入口中的 ChooseSkin 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void ChooseSkin() { OpenGlassSettings(); }

        /// <summary>
        /// 导入桌面控制与应用入口中的 ImportCourses 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void ImportCourses()
        {
            using (var dialog = new CourseImportDialog(state, () => state, edit => Change(delegate
            {
                edit();
                ReloadCalendars();
            })))
            {
                if (await Modal(dialog) != DialogResult.OK)
                    return;
                int added = 0;
                if (Change(delegate
                {
                    added = CourseImporter.Commit(state, dialog.Courses, dialog.StartDate, dialog.Weeks, dialog.UseEmoji);
                }))
                {
                    ReloadCalendars();
                    RefreshSurface();
                    Notice.Show(L.T("已导入 ") + added + L.T(" 条课程；重复课程自动跳过。"), L.T("课表导入完成"));
                }
            }
        }

        /// <summary>
        /// 选择桌面控制与应用入口中的 ChooseScreen 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void ChooseScreen()
        {
            using (var selector = new MonitorDialog(state.ScreenDevice))
            {
                if (await Modal(selector) != DialogResult.OK)
                    return;
                if (Change(delegate
                {
                    state.ScreenDevice = selector.SelectedDevice;
                }))
                    Connect();
            }
        }

        /// <summary>
        /// 显示桌面控制与应用入口中的 ShowZodiac 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void ShowZodiac()
        {
            using (var f = new ReadingDialog(Reading.Daily(state.Zodiac, DateTime.Today), state.Zodiac + L.T(" · 今日运势")))
            {
                if (await Modal(f) == DialogResult.Retry)
                {
                    await ChooseZodiac();
                    ShowZodiac();
                }
            }
        }

        /// <summary>
        /// 选择桌面控制与应用入口中的 ChooseZodiac 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async System.Threading.Tasks.Task ChooseZodiac()
        {
            using (var f = new ZodiacDialog(state.Zodiac))
            {
                if (await Modal(f) == DialogResult.OK && Change(delegate
                {
                    state.Zodiac = f.Selected;
                }))
                {
                    RefreshSurface();
                    SyncOnline();
                }
            }
        }

        /// <summary>
        /// 处理桌面控制与应用入口中的 Divine 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void Divine(bool existing)
        {
            if (!existing)
            {
                var record = scene.ReadFortune();
                if (record == null)
                    RequestRender();
                else
                    Change(delegate
                    {
                        state.Divination = record;
                        state.Widget = "占卜";
                    });
                return;
            }

            scene.ClearFortune();
            Change(delegate {state.Divination=null;});
            using (var f = new DivinationDialog(null))
            {
                await Modal(f);
                if (f.Changed && !stopping)
                {
                    Change(delegate
                    {
                        state.Divination = f.Result;
                        state.Widget = "占卜";
                    });
                    scene.LoadFortune();
                }
            }
        }

        /// <summary>
        /// 读取桌面控制与应用入口中的 ReadingDetails 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async void ReadingDetails()
        {
            if (state.Widget == "占卜" || state.Widget == "抽签卡片")
            {
                if(state.Divination==null)return;
                using(var detail=new DivinationResultDialog(state.Divination)) await Modal(detail);
                return;
            }

            ShowZodiac();
        }

        /// <summary>
        /// 处理跨日归档、连接检查和到期提醒。
        /// </summary>
        void Tick()
        {
            if (stopping || (startupTimer != null && startupTimer.Enabled))
                return;
            if (!syncBusy && DateTime.UtcNow >= nextOnlineRefresh)
                SyncOnline();
            if (lastDay != DateTime.Today)
            {
                lastDay = DateTime.Today;
                Change(delegate
                {
                    DailyArchive.Sync(state, lastDay);
                });
                scene.UpdateDay(lastDay);
                state.TodoPage = 0;
                if (scene.PlanMode == "day")
                    scene.PlanAnchor = lastDay;
                ReloadCalendars();
                RequestRender();
            }

            if (!hidden && (screenLayout != ScreenLayout() || surfaces.Any(v => !v.Connected) || ((state.ShowCalendar || state.ShowWidget || state.ShowPlans) && surfaces.Count == 0 && DesktopSurface.FindDesktop() != IntPtr.Zero && DesktopSurface.FindDesktop() != attemptedDesktop)))
                Connect();
            foreach (var surface in surfaces)
            {
                if (surface.Connected)
                    { surface.KeepVisible(); if (Skins.IsGlass(state.Skin)) surface.Invalidate(); }
                else
                    desktopError = "桌面窗口已重建，请从托盘重新连接交互桌面。";
            }

            WriteDiagnostics();
            var due = state.Events.Where(e => Data.Due(e, DateTime.UtcNow)).ToList();
            if (due.Count > 0)
            {
                string text = String.Join("\n", due.Take(5).Select(e => e.Title + " · " + e.Start.ToString("MM-dd HH:mm")));
                tray.ShowBalloonTip(10000, L.T("日程提醒"), text, ToolTipIcon.Info);
                Change(delegate
                {
                    foreach (var e in due)
                        e.Notified = true;
                });
                var popup = new ReminderDialog(due);
                popup.TopMost = true;
                popup.Show();
            }
        }

        /// <summary>
        /// 展示所选国家日历的来源、年份与加载状态。
        /// </summary>
        void CalendarSummary() { Settings(); }

        /// <summary>
        /// 导出桌面控制与应用入口中的 Export 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void Export()
        {
            using (var f = new SaveFileDialog
            {
                Filter = L.T("备份 (*.json)|*.json"),
                FileName = L.T("桌面日历备份-") + DateTime.Today.ToString("yyyyMMdd") + ".json"
            }

            )
                if (WindowPlacement.Show(f) == DialogResult.OK)
                    File.WriteAllText(f.FileName, Data.Json().Serialize(state));
        }

        /// <summary>
        /// 依据所有权记录恢复原状态，避免覆盖其他程序的修改。
        /// </summary>
        void Restore()
        {
            using (var f = new OpenFileDialog
            {
                Filter = L.T("备份 (*.json)|*.json")
            }

            )
                if (WindowPlacement.Show(f) == DialogResult.OK)
                    try
                    {
                        State next = Data.Json().Deserialize<State>(File.ReadAllText(f.FileName));
                        Data.Normalize(next);
                        Data.Validate(next);
                        if (Notice.Show(L.T("替换当前日程、待办和日历设置？"), L.T("恢复备份"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                        {
                            Change(delegate
                            {
                                state = next;
                            });
                            ReloadCalendars();
                        }
                    }
                    catch (Exception ex)
                    {
                        Notice.Show(ex.Message);
                    }
        }

        /// <summary>
        /// 保存未提交输入并串行恢复壁纸，收尾后退出消息循环。
        /// </summary>
        async void Stop()
        {
            if (stopping)
                return;
            foreach (var surface in surfaces)
                if (!surface.FinishInline())
                    return;
            if (fallback != null)
                foreach (var view in fallback.Controls.OfType<SceneView>())
                    if (!view.FinishInline())
                        return;
            stopping = true;
            StopDisplayWatch();
            if(guide!=null&&!guide.IsDisposed)guide.Close();
            if (startupTimer != null)
                startupTimer.Stop();
            timer.Stop();
            iconTimer.Stop();
            iconTimer.Dispose();
            refreshTimer.Stop();
            var watch = System.Diagnostics.Stopwatch.StartNew();
            long restoreMs = 0;
            System.Threading.Interlocked.Increment(ref backgroundRevision);
            var errors = await shellWorker.Run(() => ShutdownFlow.Run(() => background.Restore(), () => Wallpaper.Restore()));
            restoreMs = watch.ElapsedMilliseconds;
            errors.AddRange(ShutdownFlow.Run(() => IndependentWindows.CloseAll(), () =>
            {
                foreach (var surface in surfaces)
                    surface.Dispose();
                surfaces.Clear();
                if (fallback != null)
                {
                    fallback.Dispose();
                    fallback = null;
                }
            }, () => Data.Write(state)));
            try
            {
                File.WriteAllText(Path.Combine(Data.Root, "exit-timing.txt"), "壁纸恢复阶段：" + restoreMs + " ms\r\n退出处理总计：" + watch.ElapsedMilliseconds + " ms\r\n" + String.Join("；", errors));
            }
            catch
            {
            }

            if (errors.Count > 0)
                MessageBox.Show("退出时部分操作未完成，恢复记录已保留：\n" + String.Join("\n", errors), L.T("退出提示"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            shellWorker.Dispose();
            tray.Dispose();
            dispatcher.Dispose();
            ExitThread();
        }
    }

    public partial class SceneView : Control
    {
        Scene scene;
        ToolTip tip = new ToolTip
        {
            InitialDelay = 350,
            AutoPopDelay = 10000
        };
        string tipText = "";
        bool leftDown, suppressUp;
        public int ClickCount, DoubleClickCount;
        public Action<Point, bool> Clicked;
        /// <summary>
        /// 初始化桌面控制与应用入口的SceneView，准备状态、布局与必要的事件绑定。
        /// </summary>
        public SceneView(Scene scene)
        {
            this.scene = scene;
            DoubleBuffered = true;
            SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
        }

        /// <summary>
        /// 绘制控件当前状态，保持主题配色和内容可见。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            scene.Paint(e.Graphics, ClientSize);
            PlaceInlineEditor();
        }

        /// <summary>
        /// 处理当前控件需要的 Windows 消息，其余消息交给基类。
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            int msg = m.Msg;
            Point point = new Point(unchecked((short)(m.LParam.ToInt64() & 65535)), unchecked((short)((m.LParam.ToInt64() >> 16) & 65535)));
            if (ScrollMessage(msg,point)) return;
            if (LayoutMessage(msg,point)) return;
            if (msg == 0x201)
            {
                pressedHit = scene.At(point, ClientSize);
                leftDown = true;
                suppressUp = false;
            }

            if (msg == 0x203)
            {
                suppressUp = true;
                leftDown = false;
                base.WndProc(ref m);
                Capture = false;
                DoubleClickCount++;
                if (Clicked != null)
                    Clicked(point, true);
                return;
            }

            if (msg == 0x202)
            {
                bool route = leftDown && !suppressUp && ClientRectangle.Contains(point);
                leftDown = false;
                suppressUp = false;
                base.WndProc(ref m);
                Capture = false;
                if (route)
                {
                    ClickCount++;
                    if (!HandleInlineClick(point) && Clicked != null)
                        Clicked(point, false);
                }

                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// 响应MouseMove事件，同步桌面控制与应用入口状态并保留基类行为。
        /// </summary>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var hit = scene.At(e.Location, ClientSize);
            Cursor = LayoutCursor(hit);
            string hover = hit == null ? null : hit.Action;
            if (scene.HoverAction != hover)
            {
                scene.HoverAction = hover;
                Invalidate();
            }

            string text = "";
            if(hit!=null && hit.Action.StartsWith("hide"))text=L.T("从桌面隐藏");
            if(hit!=null && hit.Action=="layoutmove")text=L.T("拖动以移动");
            if (hit != null && hit.Action == "tools")
                text = L.T("工具：切换屏幕、导入课表、皮肤、自启动");
            if (hit != null && hit.Action == "planexport")
                text = L.T("导出 TXT 给 AI，生成周报或月报；不自动发送");
            if (hit != null && hit.Action == "widgetzodiac")
                text = L.T("星座 · 今日运势");
            if (hit != null && hit.Action == "widgetfortune")
                text = L.T("占卜 · 数字小六壬");
            if (hit != null && hit.Action == "event")
            {
                var item = scene.State.Events.FirstOrDefault(x => x.Id == hit.Id);
                if (item != null)
                    text = item.Title + "\n" + item.Start.ToString("MM-dd HH:mm") + " — " + item.End.ToString("MM-dd HH:mm") + "\n" + item.TimeZone + "\n" + item.Notes;
            }

            if (text != tipText)
            {
                tipText = text;
                tip.SetToolTip(this, text);
            }
        }

        /// <summary>
        /// 释放控件、计时器、图形和系统资源，并调用基类清理。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                FinishInline();
                tip.Dispose();
            }

            base.Dispose(disposing);
        }

        /// <summary>
        /// 响应MouseLeave事件，同步桌面控制与应用入口状态并保留基类行为。
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            scene.HoverAction = null;
            Invalidate();
        }

        /// <summary>
        /// 响应Resize事件，同步桌面控制与应用入口状态并保留基类行为。
        /// </summary>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            Invalidate();
        }
    }

    public static class Program
    {
        public const string Version = "0.58";

        /// <summary>
        /// 配置 DPI 与消息循环，并启动程序；异常时明确提示错误。
        /// </summary>
        [STAThread]
        public static int Main(string[] args)
        {
#if !TESTS
            if (args.Any(a => a != "--startup")) return 2;
            StartJitProfile();
#endif
            Native.Dpi();
#if TESTS
            if (args.Length > 0 && args[0] == "--test")
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            if (args.Length > 0 && args[0] == "--test")
                OnlineData.Enabled = false;
#endif
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
#if TESTS
                if (args.Length > 0 && args[0] == "--test")
                    return Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test57") return Version57Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test56") return Version56Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--bench56") return PopupBench56.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test55") return Version55Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--quick54") return Version54Tests.Run(args[1],true);
                if (args.Length > 1 && args[0] == "--core54") return Core54Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test54") return Version54Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--probe54") return Probe54.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test53") return Version53Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--desktop51") return Native51Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--ui52") return Version52Tests.Run(args[1],true);
                if (args.Length > 1 && args[0] == "--test52") return Version52Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--ui51") return Version51Tests.Run(args[1],true);
                if (args.Length > 1 && args[0] == "--test51") return Version51Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test50") return Version50Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--route50") return Native50Tests.Run(args[1],true);
                if (args.Length > 1 && args[0] == "--native50") return Native50Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test49") return Version49Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test48") return Version48Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test47") return Version47Tests.Run(args[1]);
                if (args.Length > 1 && args[0] == "--test46") return Version46Tests.Run(args[1]);
#endif
                bool newVersion;
                using(var versionMarker=new Mutex(false,"Local\\PaperDaysVersion058",out newVersion)) {
                bool fresh;
                using (var mutex = new Mutex(true, "Local\\PaperDaysCalendar", out fresh))
                {
                    if (!fresh)
                    {
                        Notice.Show(L.T(newVersion?"旧版仍在运行，请先从托盘退出旧版，再打开新版。":"PaperDays 已在运行，可从右下角托盘打开。"));
                        return 0;
                    }

                    StartupTiming.Start();
                    Application.Run(new StartupContext(args.Contains("--startup")));
                }}

                return 0;
            }
            catch (Exception ex)
            {
#if TESTS
                if (args.Length > 1)
                {
                    Directory.CreateDirectory(args[1]);
                    File.WriteAllText(Path.Combine(args[1], "error.txt"), ex.ToString());
                }
                else
#endif
                    Notice.Show(ex.Message, "桌面日历未能启动（原数据未覆盖）");
                return 1;
            }
        }

        /// <summary>
        /// 记录启动时用到的方法，下次启动由后台线程提前编译，缩短开机和首次打开窗口的等待。
        /// </summary>
        static void StartJitProfile()
        {
            try
            {
                string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaperDays-v0.19");
                Directory.CreateDirectory(directory);
                System.Runtime.ProfileOptimization.SetProfileRoot(directory);
                System.Runtime.ProfileOptimization.StartProfile("startup-" + Version + ".jit");
            }
            catch (Exception)
            {
            }
        }

#if TESTS

        /// <summary>
        /// 处理桌面控制与应用入口中的 Demo 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static State Demo()
        {
            var state = new State();
            DateTime day = DateTime.Today;
            state.Events.Add(new Entry { Title = "和朋友喝咖啡", Start = new DateTime(day.Year, day.Month, 12, 15, 0, 0), End = new DateTime(day.Year, day.Month, 12, 16, 0, 0), Color = Palette.Hex[4], ShowTime = false });
            state.Events.Add(new Entry { Title = "项目提案", Start = new DateTime(day.Year, day.Month, 20, 10, 0, 0), End = new DateTime(day.Year, day.Month, 20, 11, 0, 0), Color = Palette.Hex[5], Important = true });
            state.Events.Add(new Entry { Title = "短途旅行", Start = new DateTime(day.Year, day.Month, 23), End = new DateTime(day.Year, day.Month, 25, 23, 59, 0), Color = Palette.Hex[0], AllDay = true, UseEmoji = true, Emoji = "✈️" });
            state.Events.Add(new Entry { Title = "读书时间", Start = day.AddHours(18), End = day.AddHours(19), Color = Palette.Hex[2], UseEmoji = true, Emoji = "📚" });
            state.Events.Add(new Entry { Title = "整理下周计划", Start = day.AddHours(20), End = day.AddHours(21), Color = Palette.Hex[3], ShowTime = false });
            state.Todos.Add(new Todo { Title = "喝一杯温水", Done = true });
            state.Todos.Add(new Todo { Title = "完成今天的提案" });
            state.Todos.Add(new Todo { Title = "散步 20 分钟" });
            state.Todos.Add(new Todo { Title = "睡前读十页书" });
            return state;
        }
#endif


    }
}
