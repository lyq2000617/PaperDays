using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version14Tests
    {
        /// <summary>
        /// 执行Version14Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            var a = new Rectangle(-1920, 0, 1920, 1080);
            var b = new Rectangle(0, 0, 2560, 1440);
            var icons = new[]
            {
                new Rectangle(-1900, 20, 80, 90),
                new Rectangle(-1800, 20, 80, 90),
                new Rectangle(25, 20, 110, 120)
            };
            check(DesktopIcons.Reserve(a, icons, 1) == 216, "auto reserve measures left columns on negative monitor coordinates");
            check(DesktopIcons.Reserve(b, icons, 1.5f) == 159, "auto reserve separates another monitor and DPI padding");
            check(DesktopIcons.Reserve(new Rectangle(2560, -420, 1080, 1920), icons, 1) == 0, "screen without icons has no automatic reserve");
            check(DesktopIcons.Reserve(a, new[] { new Rectangle(-950, 20, 100, 90) }, 1) == 0, "scattered middle icons cannot consume calendar width");
            var scene = new Scene
            {
                State = new State(),
                ReservedLeft = 100
            };
            using (var host = new Form
            {
                ClientSize = new Size(1100, 700)
            }

            )
            {
                host.Show();
                using (var surface = new DesktopSurface(scene, host.Handle))
                {
                    surface.Attach(host.RectangleToScreen(host.ClientRectangle));
                    IntPtr handle = surface.Handle;
                    scene.ReservedLeft = 300;
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    surface.UpdateRegion();
                    surface.Invalidate();
                    surface.Update();
                    clock.Stop();
                    check(surface.Handle == handle && !surface.Region.IsVisible(200, 100) && surface.Region.IsVisible(350, 100), "reserve changes update native region without reconnecting");
                    File.WriteAllText(Path.Combine(directory, "reserve-timing.txt"), "In-place region update and render: " + clock.ElapsedMilliseconds + " ms (test host, not Explorer)");
                    scene.ReservedLeft = 0;
                    surface.UpdateRegion();
                    check(surface.Region == null && surface.Handle == handle, "removing reserve restores full mouse region without new window");
                }

                host.Close();
            }

            check(DesktopIcons.Reserve(new Rectangle(0, 0, 1920, 1080), new[] { new Rectangle(-4, 20, 90, 90) }, 1) == 102, "partially clipped first-column icon still reserves space");
            var state = new State
            {
                StartInTray = true,
                IconColumns = 3
            };
            Data.Write(state);
            check(Data.Read().StartInTray && Data.Read().IconColumns == 3, "startup tray preference and manual columns persist");
            using (var controller = new Controller(state, false, true))
            {
                Application.DoEvents();
                check(state.IconColumns == -1, "launch resets previous manual setting to automatic");
                var type = typeof(Controller);
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                type.GetMethod("Tick", flags).Invoke(controller, null);
                check((bool)type.GetField("hidden", flags).GetValue(controller), "background timer preserves tray-only state");
                var list = (System.Collections.Generic.List<DesktopSurface>)type.GetField("surfaces", flags).GetValue(controller);
                check(list.Count == 0, "starting in tray never attaches a desktop surface");
                check(((Timer)type.GetField("timer", flags).GetValue(controller)).Enabled, "reminder timer continues while calendar is in tray");
                var controllerScene = (Scene)type.GetField("scene", flags).GetValue(controller);
                controllerScene.Month = new DateTime(2028, 5, 1);
                using (var host = new Form
                {
                    ClientSize = new Size(1000, 700)
                }

                )
                {
                    host.Show();
                    var surface = new DesktopSurface(controllerScene, host.Handle);
                    surface.Attach(host.RectangleToScreen(host.ClientRectangle));
                    list.Add(surface);
                    type.GetField("hidden", flags).SetValue(controller, false);
                    type.GetMethod("HideCalendar", flags).Invoke(controller, null);
                    check(surface.IsDisposed && list.Count == 0 && (bool)type.GetField("hidden", flags).GetValue(controller), "hide action disposes visible surface and enters tray state");
                    check(controllerScene.Month == new DateTime(2028, 5, 1) && ((Timer)type.GetField("timer", flags).GetValue(controller)).Enabled, "hide preserves browsed month and reminder timer");
                    type.GetMethod("Connect", flags).Invoke(controller, null);
                    check(list.Count == 0, "monitor reconnect cannot unhide a tray-only calendar");
                    host.Close();
                }

                type.GetMethod("Stop", flags).Invoke(controller, null);
            }

            var automatic = new Scene
            {
                State = new State
                {
                    IconColumns = -1
                }
            };
            var iconPositions = new[]
            {
                new Rectangle(20, 20, 80, 90)
            };
            automatic.ReservedLeft = DesktopIcons.Reserve(new Rectangle(0, 0, 1920, 1080), iconPositions, 1);
            using (var bitmap = automatic.Render(new Size(1920, 1080)))
            {
            }

            check(automatic.Layout.Sidebar.IsEmpty && automatic.Hits.Any(h => h.Action == "widgetzodiac"), "automatic icon monitor uses reserved layout");
            automatic.ReservedLeft = DesktopIcons.Reserve(new Rectangle(1920, 0, 1920, 1080), iconPositions, 1);
            using (var bitmap = automatic.Render(new Size(1920, 1080)))
            {
            }

            check(automatic.ReservedLeft == 0 && !automatic.Layout.Sidebar.IsEmpty && automatic.Hits.Any(h => h.Action == "leftzodiac") && !automatic.Hits.Any(h => h.Action == "widgetzodiac"), "switching to icon-free monitor restores full sidebar in auto mode");
            automatic.ReservedLeft = DesktopIcons.Reserve(new Rectangle(0, 0, 1920, 1080), iconPositions, 1);
            using (var bitmap = automatic.Render(new Size(1920, 1080)))
            {
            }

            check(automatic.ReservedLeft > 0 && automatic.Layout.Sidebar.IsEmpty, "switching back restores reserved layout without changing auto setting");
            DateTime today = DateTime.Today;
            var item = new CompleteHoroscope
            {
                title = "天秤座",
                name = "libra",
                type = "今日运势",
                time = today.ToString("M月d日"),
                shortcomment = "测试",
                luckycolor = "绿",
                luckynumber = "12",
                fortune = new System.Collections.Generic.Dictionary<string, int>(),
                fortunetext = new System.Collections.Generic.Dictionary<string, string>()
            };
            foreach (string key in new[]
            {
                "all",
                "work",
                "love",
                "money",
                "health"
            }

            )
            {
                item.fortune[key] = 3;
                item.fortunetext[key] = "测试分类 " + key;
            }

            string json = Data.Json().Serialize(new CompleteHoroscopeResponse { code = 200, data = item });
            var reading = CompleteHoroscopes.Parse(json, "天秤座", today, DateTime.UtcNow);
            check(reading.IsOnline && reading.Number == 12 && !String.IsNullOrEmpty(reading.HealthText), "complete source keeps health and multi-digit lucky number");
            foreach (string invalid in new[]
            {
                json.Replace("libra", "aries"),
                json.Replace(today.ToString("M月d日"), today.AddDays(-1).ToString("M月d日")),
                json.Replace("测试分类 health", "")
            }

            )
            {
                bool rejected = false;
                try
                {
                    CompleteHoroscopes.Parse(invalid, "天秤座", today, DateTime.UtcNow);
                }
                catch
                {
                    rejected = true;
                }

                check(rejected, "reject wrong sign stale date or missing health from complete source");
            }
        }
    }
#endif

}