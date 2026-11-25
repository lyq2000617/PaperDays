using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Tests
    {
        /// <summary>
        /// 执行Tests中的ShowWindow验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr h, int command);
        /// <summary>
        /// 执行Tests中的ShowHost验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void ShowHost(Form form)
        {
            form.Show();
            ShowWindow(form.Handle, 5);
            Application.DoEvents();
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MouseInput
        {
            public int X, Y;
            public uint Data, Flags, Time;
            public UIntPtr Extra;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Input
        {
            public uint Type;
            public MouseInput Mouse;
        }

        /// <summary>
        /// 执行Tests中的SendInput验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint count, Input[] inputs, int size);
        /// <summary>
        /// 执行Tests中的Inject验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static uint Inject()
        {
            uint count = SendInput(1, new[] { new Input { Mouse = new MouseInput { Flags = 2 } } }, Marshal.SizeOf(typeof(Input)));
            Pump(55);
            count += SendInput(1, new[] { new Input { Mouse = new MouseInput { Flags = 4 } } }, Marshal.SizeOf(typeof(Input)));
            return count;
        }

        /// <summary>
        /// 执行Tests中的WindowFromPoint验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr WindowFromPoint(Point point);
        /// <summary>
        /// 执行Tests中的GetAncestor验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetAncestor(IntPtr window, uint flags);
        /// <summary>
        /// 执行Tests中的Pump验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static void Pump(int ms)
        {
            var until = DateTime.UtcNow.AddMilliseconds(ms);
            while (DateTime.UtcNow < until)
            {
                Application.DoEvents();
                Thread.Sleep(5);
            }
        }

        /// <summary>
        /// 执行Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static int Run(string directory)
        {
            Directory.CreateDirectory(directory);
            Data.Root = directory;
            var lines = new List<string>();
            Action<bool, string> check = (value, name) =>
            {
                if (!value)
                    throw new Exception("FAIL: " + name);
                lines.Add("PASS: " + name);
                File.WriteAllLines(Path.Combine(directory, "test-results.txt"), lines);
            };
            var state = new State();
            var entry = new Entry
            {
                Title = "测试日程 🌿",
                Start = new DateTime(2026, 10, 30, 9, 0, 0),
                End = new DateTime(2026, 10, 31, 10, 0, 0),
                TimeZone = "Korea Standard Time"
            };
            state.Events.Add(entry);
            state.Todos.Add(new Todo { Title = "测试待办", Done = true });
            Data.Write(state);
            State loaded = Data.Read();
            check(loaded.Events[0].Title == entry.Title && loaded.Todos[0].Done, "events and completed todos persist");
            check(entry.UtcStart() == new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc), "timezone conversion");
            check(!Data.Due(entry, entry.UtcStart().AddMinutes(-16)) && Data.Due(entry, entry.UtcStart().AddMinutes(-15)), "reminder boundary");
            entry.Notified = true;
            check(!Data.Due(entry, entry.UtcStart().AddHours(2)), "reminder deduplication");
            check(entry.On(new DateTime(2026, 10, 31)) && !entry.On(new DateTime(2026, 11, 1)), "multi-day event range");
            check(Data.Lunar(new DateTime(2026, 2, 17)) == "正月", "lunar new year");
            Data.Write(state);
            check(File.Exists(Path.Combine(directory, "calendar.json.bak")), "atomic backup");
            string valid = File.ReadAllText(Path.Combine(directory, "calendar.json"));
            File.WriteAllText(Path.Combine(directory, "calendar.json"), "broken");
            bool rejected = false;
            try
            {
                Data.Read();
            }
            catch
            {
                rejected = true;
            }

            check(rejected, "corrupted data is not overwritten");
            File.WriteAllText(Path.Combine(directory, "calendar.json"), valid);
            check(Calendars.China().Count(x => x.Label != "补班") == 33 && Calendars.China().Count(x => x.Label == "补班") == 6, "2026 official Chinese holiday dates");
            state.Countries = new List<string>
            {
                "KR",
                "JP",
                "US"
            };
            string status;
            var marks = Calendars.Load(state, new DateTime(2026, 10, 1), out status);
            check(marks.Any(x => x.Source.StartsWith("KR")) && marks.Any(x => x.Source.StartsWith("JP")) && marks.Any(x => x.Source.StartsWith("US")), "multiple foreign calendars load offline");
            string ics = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nUID:test\r\nSUMMARY:旅行\r\nDTSTART;VALUE=DATE:20261023\r\nDTEND;VALUE=DATE:20261026\r\nEND:VEVENT\r\nEND:VCALENDAR";
            var imported = Calendars.ImportIcs(ics, "trip", Palette.Hex[1]);
            check(imported.Count == 1 && imported[0].On(new DateTime(2026, 10, 25)) && !imported[0].On(new DateTime(2026, 10, 26)), "ICS exclusive end date");
            rejected = false;
            try
            {
                Calendars.ImportIcs(ics.Replace("UID:test", "RRULE:FREQ=DAILY\r\nUID:test"), "trip", Palette.Hex[1]);
            }
            catch
            {
                rejected = true;
            }

            check(rejected, "unsupported recurrence is rejected explicitly");
            using (var editor = new Editor(state, null, new DateTime(2026, 10, 18)))
            {
                editor.Show();
                Pump(80);
                editor.TitleInput.Text = "真实控件保存测试";
                editor.StartTime.Text = "14:35";
                editor.EndTime.Text = "16:10";
                editor.ColorButtons[2].PerformClick();
                editor.ShowTime.Checked = false;
                editor.EmojiMode.Checked = true;
                editor.EmojiInput.Text = "🎂";
                editor.SaveButton.PerformClick();
                check(editor.Result != null && editor.Result.Start.Hour == 14 && editor.Result.Start.Minute == 35 && editor.Result.End.Hour == 16 && editor.Result.End.Minute == 10, "time controls save hours and minutes");
                check(editor.Result.Color == Palette.Hex[2] && !editor.Result.ShowTime && editor.Result.UseEmoji && editor.Result.Emoji == "🎂", "palette and marker selections save");
                state.Events.Add(editor.Result);
                Data.Write(state);
                check(Data.Read().Events.Last().Color == Palette.Hex[2], "edited colors survive reload");
            }

            var scene = new Scene
            {
                State = state,
                Month = new DateTime(2026, 10, 1),
                Holidays = marks
            };
            using (var image = scene.Render(new Size(1600, 1000)))
                image.Save(Path.Combine(directory, "calendar-test.png"));
            check(scene.Hits.Count(x => x.Action == "date") == 42, "42 date hit targets");
            check(scene.Hits.Any(x => x.Action == "addtodo") && scene.Hits.Any(x => x.Action == "todo"), "todo add and toggle targets");
            foreach (var size in new[]
            {
                new Size(1920, 1080),
                new Size(2560, 1440),
                new Size(1920, 1200)
            }

            )
            {
                var target = scene.Hits.First(x => x.Action == "date" && x.Date == new DateTime(2026, 10, 15));
                using (var rendered = scene.Render(size))
                {
                }

                target = scene.Hits.First(x => x.Action == "date" && x.Date == new DateTime(2026, 10, 15));
                var point = new Point((int)(target.Box.X + target.Box.Width * .3f), (int)(target.Box.Y + target.Box.Height * .12f));
                var got = scene.At(point, size);
                check(got != null && got.Action == "date" && got.Date == target.Date, "coordinate alignment " + size.Width + "x" + size.Height);
            }

            var tracker = new DoubleClickTracker();
            check(!tracker.Push(new Point(100, 100), 1000, 500, new Size(4, 4)) && tracker.Push(new Point(101, 100), 1100, 500, new Size(4, 4)) && !tracker.Push(new Point(101, 100), 1200, 500, new Size(4, 4)), "double click does not repeat on third click");
            string id = state.Todos[0].Id;
            bool before = state.Todos[0].Done;
            state.ToggleTodo(id);
            check(state.Todos[0].Done != before, "todo click toggles completion");
            state.ToggleTodo(id);
            check(state.Todos[0].Done == before, "second todo click restores state");
            state.Calendars[0].Enabled = false;
            check(!state.VisibleEvents().Any(), "unchecking calendar hides its events");
            state.Calendars[0].Enabled = true;
            state.Widget = "抽签卡片";
            state.Fortune = "整理一个小角落";
            Data.Write(state);
            State again = Data.Read();
            check(again.Widget == "抽签卡片" && again.Fortune == state.Fortune, "widget selection and fortune persist");
            var nextMonth = new Scene
            {
                State = state,
                Month = new DateTime(2026, 12, 1).AddMonths(1)
            };
            using (var image = nextMonth.Render(new Size(1600, 1000)))
            {
            }

            var nextDate = nextMonth.Hits.First(h => h.Action == "date" && h.Date == new DateTime(2027, 1, 15));
            using (var editor = new Editor(state, null, nextDate.Date))
            {
                editor.TitleInput.Text = "下月日程";
                check(editor.TrySave() && editor.Result.Start.Date == nextDate.Date, "next-month date opens editor with correct year and day");
                state.Events.Add(editor.Result);
                Data.Write(state);
                check(Data.Read().Events.Any(e => e.Title == "下月日程" && e.Start.Date == nextDate.Date), "future-month event persists");
                check(editor.ColorButtons.All(b => b is ColorDot && b.Text == ""), "event color choices are unlabeled circular swatches");
            }

            string deleteId = state.Todos[0].Id;
            check(state.DeleteTodo(deleteId), "todo delete removes selected id");
            Data.Write(state);
            check(!Data.Read().Todos.Any(t => t.Id == deleteId), "deleted todo stays deleted after reload");
            foreach (string sign in Reading.Signs)
            {
                var reading = Reading.Daily(sign, new DateTime(2026, 10, 30));
                check(reading.Detail() == Reading.Daily(sign, new DateTime(2026, 10, 30)).Detail() && !reading.IsOnline && reading.Number == 0 && !String.IsNullOrEmpty(reading.Caution), "daily reading stable and complete: " + sign);
            }

            state.ScreenDevice = "test-screen";
            state.FortuneIndex = 5;
            state.FortuneDay = DateTime.Today.ToString("yyyy-MM-dd");
            Data.Write(state);
            check(Data.Read().ScreenDevice == "test-screen" && Data.Read().FortuneIndex == 5, "selected display and drawn fortune survive restart");
            check(Reading.Lots.All(r => !String.IsNullOrEmpty(r.Grade) && !String.IsNullOrEmpty(r.Verse) && !String.IsNullOrEmpty(r.Interpretation)), "every fortune has grade verse and interpretation");
            var history = new State();
            history.Todos.Add(new Todo { Title = "跨年完成", Date = "2025-12-20", Done = true, CompletedAt = new DateTime(2026, 1, 1, 20, 0, 0) });
            history.Todos.Add(new Todo { Title = "下一周", Date = "2026-01-05", Done = true, CompletedAt = new DateTime(2026, 1, 5) });
            history.Todos.Add(new Todo { Title = "未完成", Date = "2026-01-01" });
            history.Todos.Add(new Todo { Title = "二月完成", Date = "2026-01-30", Done = true, CompletedAt = new DateTime(2026, 2, 1) });
            history.Todos.Add(new Todo { Title = "历史记录", Date = "2026-01-02", Done = true });
            var weekly = PlanReview.Items(history, "week", new DateTime(2026, 1, 1));
            check(weekly.Count == 2 && weekly.Any(t => t.Title == "跨年完成") && !weekly.Any(t => t.Title == "下一周"), "weekly review uses Monday boundaries and completion date across year");
            check(PlanReview.Items(history, "month", new DateTime(2026, 1, 20)).Count == 3, "monthly review excludes next month and incomplete tasks");
            for (int i = 0; i < 17; i++)
                history.Todos.Add(new Todo { Title = "导出事项" + i, Date = "2026-01-03", Done = true, CompletedAt = new DateTime(2026, 1, 3) });
            string note = PlanReview.Export(history, "month", new DateTime(2026, 1, 1));
            check(note.Contains("20 项") && note.Contains("导出事项16") && !note.Contains("未完成") && note.Contains("旧记录"), "export includes all pages and labels legacy dates");
            File.WriteAllText(Path.Combine(directory, "summary-example.md"), note);
            var completed = new Todo
            {
                Title = "记录完成时间"
            };
            history.Todos.Add(completed);
            history.ToggleTodo(completed.Id);
            check(completed.Done && completed.CompletedAt.HasValue, "completion records timestamp");
            history.ToggleTodo(completed.Id);
            check(!completed.Done && !completed.CompletedAt.HasValue, "unchecking removes completion timestamp");
            foreach (string sign in Reading.Signs)
            {
                using (var selector = new ZodiacDialog("天秤座"))
                {
                    selector.Show();
                    selector.SignButtons[Array.IndexOf(Reading.Signs, sign)].PerformClick();
                    check(selector.DialogResult == DialogResult.OK && selector.Selected == sign, "direct zodiac selection: " + sign);
                }
            }

            var displays = new[]
            {
                new DisplayOption
                {
                    Device = "test-primary",
                    Bounds = new Rectangle(0, 0, 1920, 1080),
                    Primary = true
                },
                new DisplayOption
                {
                    Device = "test-left",
                    Bounds = new Rectangle(-2560, 0, 2560, 1440)
                }
            };
            using (var chooser = new MonitorDialog("test-primary", displays))
            {
                chooser.Show();
                chooser.ScreenButtons[1].PerformClick();
                check(chooser.DialogResult == DialogResult.OK && chooser.SelectedDevice == "test-left", "display card selects exact device id including negative-position monitor");
                state.ScreenDevice = chooser.SelectedDevice;
                Data.Write(state);
                check(Data.Read().ScreenDevice == "test-left", "display choice is persisted by device id");
                check(DisplayOption.Resolve(Data.Read().ScreenDevice, displays).Bounds.X == -2560, "desktop target resolution uses selected secondary screen bounds");
                check(DisplayOption.Resolve("disconnected", displays).Primary, "disconnected screen falls back to primary");
            }

            var clarityScene = new Scene
            {
                State = state,
                ReservedLeft = 208
            };
            using (var preview = clarityScene.Render(new Size(1600, 1000)))
            {
            }

            check(clarityScene.Hits.Any(h => h.Action == "tools") && clarityScene.Hits.Any(h => h.Action == "widgetzodiac") && clarityScene.Hits.Any(h => h.Action == "widgetfortune"), "desktop has direct monitor choice and shared fortune tabs");
            state.Widget = "抽签卡片";
            state.FortuneIndex = 0;
            state.FortuneDay = DateTime.Today.ToString("yyyy-MM-dd");
            using (var preview = clarityScene.Render(new Size(1600, 1000)))
            {
            }

            check(clarityScene.Hits.Any(h => h.Action == "widgetfortune") && !clarityScene.Hits.Any(h => h.Action == "drawfortune"), "fortune and zodiac are compact footer entries, not persistent cards");
            FeatureTests.Run(check, directory);
            var weekState = new State
            {
                CalendarView = "week"
            };
            DateTime monday = new DateTime(2026, 11, 2);
            weekState.Events.Add(new Entry { Title = "一小时", Start = monday.AddHours(9), End = monday.AddHours(10) });
            weekState.Events.Add(new Entry { Title = "两小时", Start = monday.AddHours(11), End = monday.AddHours(13) });
            weekState.Events.Add(new Entry { Title = "重叠", Start = monday.AddHours(11.5), End = monday.AddHours(12) });
            weekState.Events.Add(new Entry { Title = "跨午夜", Start = monday.AddHours(23), End = monday.AddDays(1).AddHours(1) });
            var blocks = WeekLayout.ForDay(weekState.Events, monday);
            var one = WeekLayout.Box(blocks.First(b => b.Entry.Title == "一小时"), monday, 0, 120, new DayScale(0, 600, true), 15);
            var two = WeekLayout.Box(blocks.First(b => b.Entry.Title == "两小时"), monday, 0, 120, new DayScale(0, 600, true), 15);
            check(Math.Abs(two.Height - one.Height * 2) < .01, "week event height is proportional to duration");
            check(blocks.First(b => b.Entry.Title == "两小时").Columns == 2 && blocks.First(b => b.Entry.Title == "重叠").Lane != blocks.First(b => b.Entry.Title == "两小时").Lane, "overlapping week events use separate columns");
            check(WeekLayout.ForDay(weekState.Events, monday.AddDays(1)).First().End == monday.AddDays(1).AddHours(1), "cross-midnight event appears on next day");
            var weekScene = new Scene
            {
                State = weekState,
                Selected = monday,
                Month = monday
            };
            using (var image = weekScene.Render(new Size(1600, 900)))
                image.Save(Path.Combine(directory, "week-test.png"));
            check(weekScene.Hits.Count(h => h.Action == "timeslot") == 294 && weekScene.Hits.Any(h => h.Action == "timeslot" && h.Date == monday.AddHours(9.5)), "week has half-hour click targets with exact time");
            weekScene.Navigate(1);
            check(weekScene.Selected == monday.AddDays(7), "week navigation moves seven days");
            weekState.Todos.Add(new Todo { Title = "可删除的完成事项", Date = monday.ToString("yyyy-MM-dd"), CompletedAt = monday, Done = true });
            weekScene.PlanMode = "week";
            weekScene.PlanAnchor = monday;
            using (var image = weekScene.Render(new Size(1600, 900)))
            {
            }

            check(weekScene.Hits.Any(h => h.Action == "tododelete"), "weekly completed list exposes delete target");
            weekScene.PlanMode = "month";
            using (var image = weekScene.Render(new Size(1600, 900)))
            {
            }

            check(weekScene.Hits.Any(h => h.Action == "tododelete"), "monthly completed list exposes delete target");
            weekState.DeleteTodo(weekState.Todos[0].Id);
            check(!PlanReview.Export(weekState, "month", monday).Contains("可删除的完成事项"), "deleted completion is absent from TXT summary");
            check(EmojiArt.Get("📚") != null, "bundled emoji can render original color offline");
            DivinationTests.Run(check);
            using (var slotEditor = new Editor(new State(), null, new DateTime(2026, 11, 2, 23, 30, 0), true))
            {
                slotEditor.TitleInput.Text = "夜间日程";
                check(slotEditor.TrySave() && slotEditor.Result.Start == new DateTime(2026, 11, 2, 23, 30, 0) && slotEditor.Result.End == new DateTime(2026, 11, 3, 0, 30, 0), "week time-slot editor saves selected time and handles midnight end");
            }

            LayoutTests.Run(check, directory);
            Version12Tests.Run(check, directory);
            Version13Tests.Run(check, directory);
            Version14Tests.Run(check, directory);
            Version17Tests.Run(check, directory);
            Version18Tests.Run(check, directory);
            Version19Tests.Run(check, directory);
            Version20Tests.Run(check, directory);
            Version21Tests.Run(check, directory);
            Version22Tests.Run(check, directory);
            Version25Tests.Run(check, directory);
            Version27Tests.Run(check, directory);
            Version38Tests.Run(check, directory);
            NativeSurfaceTest(check, lines);
            File.WriteAllLines(Path.Combine(directory, "test-results.txt"), lines);
            return 0;
        }

        /// <summary>
        /// 执行Tests中的SendMessage验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
        /// <summary>
        /// 执行Tests中的NativeSurfaceTest验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static void NativeSurfaceTest(Action<bool, string> check, List<string> lines)
        {
            var state = new State();
            state.Todos.Add(new Todo { Title = "原生点击测试" });
            var scene = new Scene
            {
                State = state
            };
            using (var parent = new Form
            {
                ClientSize = new Size(1000, 650),
                StartPosition = FormStartPosition.Manual,
                Location = new Point(20, 20),
                Text = "栖日 · 原生桌面窗口验证"
            }

            )
            {
                parent.Show();
                Pump(80);
                using (var surface = new DesktopSurface(scene, parent.Handle))
                {
                    surface.Attach(parent.RectangleToScreen(parent.ClientRectangle));
                    Pump(80);
                    check(surface.Connected, "native child is attached to test host");
                    check(surface.Region == null && surface.ClientSize == parent.ClientSize, "desktop surface covers full selected screen without cutouts");
                    int additions = 0, doubles = 0;
                    Editor editor = null;
                    surface.Clicked = delegate (Point point, bool dbl)
                    {
                        Hit hit = scene.At(point, surface.ClientSize);
                        if (hit == null)
                            return;
                        if (hit.Action == "addtodo")
                        {
                            additions++;
                            state.Todos.Add(new Todo { Title = "新增测试" });
                            Data.Write(state);
                        }

                        if (hit.Action == "todo")
                        {
                            state.ToggleTodo(hit.Id);
                            Data.Write(state);
                        }

                        if (hit.Action == "date" && dbl)
                        {
                            doubles++;
                            editor = new Editor(state, null, hit.Date);
                            editor.Show();
                        }
                    };
                    Action<Hit, bool> click = delegate (Hit h, bool dbl)
                    {
                        int x = (int)(h.Box.X + h.Box.Width * .25f), y = (int)(h.Box.Y + h.Box.Height * .15f);
                        IntPtr pos = new IntPtr((y << 16) | (x & 65535));
                        SendMessage(surface.Handle, 0x201, new IntPtr(1), pos);
                        SendMessage(surface.Handle, 0x202, IntPtr.Zero, pos);
                        if (dbl)
                        {
                            SendMessage(surface.Handle, 0x203, new IntPtr(1), pos);
                            SendMessage(surface.Handle, 0x202, IntPtr.Zero, pos);
                        }

                        Pump(60);
                    };
                    surface.SaveInlineTodo = (id, text) =>
                    {
                        additions++;
                        state.Todos.Add(new Todo { Title = text });
                        Data.Write(state);
                        return true;
                    };
                    click(scene.Hits.First(h => h.Action == "addtodo"), false);
                    check(surface.InlineInput != null, "add button opens inline textbox without dialog");
                    surface.InlineInput.Text = "新增测试";
                    click(scene.Hits.First(h => h.Action == "today"), false);
                    check(additions == 1 && Data.Read().Todos.Count == 2, "WM mouse click on native surface adds and persists todo");
                    click(scene.Hits.First(h => h.Action == "todo"), false);
                    check(state.Todos[0].Done && Data.Read().Todos[0].Done, "WM mouse click on native surface toggles and persists todo");
                    click(scene.Hits.First(h => h.Action == "todo"), false);
                    check(!state.Todos[0].Done, "second native click unchecks todo");
                    click(scene.Hits.First(h => h.Action == "date"), true);
                    check(doubles == 1 && editor != null && editor.Visible, "WM double click on date opens real editor once");
                    if (editor != null)
                        editor.Dispose();
                    state.Countries = new List<string>
                    {
                        "KR",
                        "JP",
                        "US"
                    };
                    string status;
                    scene.Holidays = Calendars.Load(state, new DateTime(2026, 10, 1), out status);
                    scene.Status = status;
                    scene.Month = new DateTime(2026, 10, 1);
                    surface.Invalidate();
                    surface.Update();
                    check(status.Contains("韩国") && status.Contains("日本") && status.Contains("美国") && scene.Holidays.Any(m => m.Date == "2026-10-03" && m.Source.StartsWith("KR")), "applying countries repaints native surface with October holidays and counts");
                    using (var image = new Bitmap(surface.Width, surface.Height))
                    {
                        surface.DrawToBitmap(image, new Rectangle(Point.Empty, surface.Size));
                        image.Save(Path.Combine(Data.Root, "native-surface.png"));
                    }
                }

                using (var left = new DesktopSurface(scene, parent.Handle))
                using (var right = new DesktopSurface(scene, parent.Handle))
                {
                    Rectangle leftBounds = parent.RectangleToScreen(new Rectangle(0, 0, 500, 650)), rightBounds = parent.RectangleToScreen(new Rectangle(500, 0, 500, 650));
                    left.Attach(leftBounds);
                    right.Attach(rightBounds);
                    check(left.PointToScreen(Point.Empty) == leftBounds.Location && right.PointToScreen(Point.Empty) == rightBounds.Location, "two surfaces retain independent screen positions");
                    check(left.Connected && right.Connected && left.Visible && right.Visible, "two monitor-like surfaces are visible together");
                }

                parent.Close();
            }

            // A separate check against Explorer. A denied attachment is a limitation, not a pass.
            try
            {
                using (var desktop = new DesktopSurface(scene, DesktopSurface.FindDesktop()))
                {
                    desktop.Attach(Screen.PrimaryScreen.Bounds);
                    Pump(80);
                    check(desktop.Connected, "Explorer desktop child attachment");
                    Point p = desktop.PointToScreen(new Point(desktop.Width / 2, desktop.Height / 2));
                    check(WindowFromPoint(p) == desktop.Handle, "desktop calendar really owns its mouse hit target");
                }
            }
            catch (Exception ex)
            {
                lines.Add("LIMITATION: Explorer attachment not verified in this environment: " + ex.Message + (ex is System.ComponentModel.Win32Exception ? "; Windows error=" + ((System.ComponentModel.Win32Exception)ex).NativeErrorCode : ""));
            }

            lines.Add("LIMITATION: WM mouse messages validate native event dispatch; physical user clicks, Win+D and Explorer restart still require Windows desktop acceptance testing.");
        }
    }
#endif

}