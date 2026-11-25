using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version18Tests
    {
        /// <summary>
        /// 执行Version18Tests中的SendMessage验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hwnd, uint message, IntPtr w, IntPtr l);
        /// <summary>
        /// 执行Version18Tests中的All验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static IEnumerable<Control> All(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var sub in All(c))
                    yield return sub;
            }
        }

        /// <summary>
        /// 执行Version18Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            var matrix = new SheetData
            {
                Name = "一班",
                Rows = new List<List<string>>
                {
                    new List<string>
                    {
                        "节次",
                        "周一\n11月2日",
                        "周二"
                    },
                    new List<string>
                    {
                        "第一节",
                        "数学",
                        "英语"
                    },
                    new List<string>
                    {
                        "第二节",
                        "",
                        "语文"
                    }
                },
                Merges = new List<string>
                {
                    "B2:B3"
                }
            };
            var rows = SmartCourses.Parse(matrix);
            check(rows.Count == 3 && rows[0].Period == "1" && rows[0].EndPeriod == "2", "period labels and merged course endpoints are retained, weekday with date recognized");
            var times = new SheetData
            {
                Name = "作息",
                Rows = new List<List<string>>
                {
                    new List<string>
                    {
                        "节次",
                        "开始时间",
                        "结束时间"
                    },
                    new List<string>
                    {
                        "1",
                        "08：00",
                        "08:45"
                    },
                    new List<string>
                    {
                        "2",
                        "08:55",
                        "09:40"
                    }
                }
            };
            var map = SmartCourses.ReadPeriods(new[] { times });
            check(map["1"][0] == TimeSpan.FromHours(8) && map["2"][1] == new TimeSpan(9, 40, 0), "workbook period mapping reads fullwidth colon and clock times");
            var different = new SheetData
            {
                Name = "另一个作息",
                Rows = new List<List<string>>
                {
                    new List<string>
                    {
                        "节次",
                        "开始时间",
                        "结束时间"
                    },
                    new List<string>
                    {
                        "1",
                        "09:00",
                        "09:45"
                    }
                }
            };
            check(!SmartCourses.ReadPeriods(new[] { times, different }).ContainsKey("1"), "conflicting school times remain unresolved, never silently guessed");
            var separate = new SheetData
            {
                Name = "分列时间",
                Rows = new List<List<string>>
                {
                    new List<string>
                    {
                        "开始时间",
                        "结束时间",
                        "周一",
                        "周二"
                    },
                    new List<string>
                    {
                        "0.33333333333333",
                        "0.36458333333333",
                        "数学",
                        "英语"
                    }
                }
            };
            check(SmartCourses.Parse(separate).All(c => c.Start == TimeSpan.FromHours(8) && c.End == new TimeSpan(8, 45, 0)), "weekly matrix supports separate Excel numeric start and end columns");
            using (var dialog = new CourseImportDialog(new State()))
            {
                dialog.Show();
                Application.DoEvents();
                foreach (var c in rows)
                {
                    int n = dialog.PreviewGrid.Rows.Add("周" + "一二三四五六日"[c.Day - 1], "", "", c.Class, c.Title, c.Room, c.Emoji, "");
                    dialog.PreviewGrid.Rows[n].Tag = c;
                }

                dialog.LoadPeriods(rows);
                foreach (DataGridViewRow row in dialog.PeriodGrid.Rows)
                {
                    var t = map[Convert.ToString(row.Cells[0].Value)];
                    row.Cells[1].Value = t[0].ToString(@"hh\:mm");
                    row.Cells[2].Value = t[1].ToString(@"hh\:mm");
                }

                dialog.ApplyPeriods();
                check(Convert.ToString(dialog.PreviewGrid.Rows[0].Cells[2].Value) == "09:40" && Convert.ToString(dialog.PreviewGrid.Rows[1].Cells[1].Value) == "08:00", "one period mapping applies to all courses and merged lessons");
                check(All(dialog).OfType<TabPage>().Any(t => t.Text.Contains("已导入")), "course import and deletion live in same window");
                All(dialog).OfType<Button>().First(b => b.Text == "＋ 手动添加").PerformClick();
                dialog.PreviewGrid.EndEdit();
                var manual = dialog.PreviewGrid.Rows[3];
                manual.Cells[1].Value = "14:00";
                manual.Cells[2].Value = "14:45";
                manual.Cells[4].Value = "手动课程";
                dialog.ConfirmImport();
                check(dialog.Courses.Count == 4 && dialog.Courses.Last().Title == "手动课程", "manual course can be added and committed without an Excel file");
            }

            string example = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "课表导入示例.xlsx");
            check(File.Exists(example), "release includes reference Excel workbook");
            var sample = SmartCourses.Parse(XlsxReader.Read(example)[0]);
            check(sample.Count == 6 && sample[0].Start == TimeSpan.FromHours(8), "delivered sample imports all six courses and actual Excel times");
            var state = new State();
            CourseImporter.Commit(state, sample, DateTime.Today, 2, true);
            check(state.Events.Count == 12 && state.Events.Where(e => e.Title == "数学").Select(e => e.Color).Distinct().Count() == 1, "sample repeated courses commit with consistent subject colors");
            string longText = String.Join("\r\n", Enumerable.Repeat("这是一条需要保留完整内容的长待办，包含进度和后续安排。", 120));
            var old = new Todo
            {
                Title = longText,
                Date = "2020-01-01"
            };
            state.Todos.Add(old);
            Data.Write(state);
            check(Data.Read().Todos.Single().Title == longText, "old multiline todo persists without truncation");
            var scene = new Scene
            {
                State = state
            };
            scene.UpdateDay(DateTime.Today.AddDays(1));
            check(state.Todos.Single().Title == longText, "day change never deletes historical todo content");
            using (var detail = new TodoDetails(old))
            {
                detail.Show();
                Application.DoEvents();
                check(detail.Content.Text == longText && detail.Content.Multiline && detail.Content.ScrollBars == ScrollBars.Vertical, "todo detail displays full multiline content with scrolling");
                detail.Close();
            }

            state = new State();
            state.Todos.Add(new Todo { Title = "点文字查看长待办" });
            scene = new Scene
            {
                State = state
            };
            foreach (var size in new[]
            {
                new Size(1920, 1080),
                new Size(1080, 1920)
            }

            )
                using (var bitmap = scene.Render(size))
                {
                    var toggle = scene.Hits.First(h => h.Action == "todo");
                    var full = scene.Hits.First(h => h.Action == "todofull");
                    check(!toggle.Box.IntersectsWith(full.Box) && scene.Hits.Any(h => h.Action == "todohistory"), "todo detail and checkbox use separate targets, history available: " + size);
                }

            using (var host = new Form
            {
                ClientSize = new Size(1100, 750)
            }

            )
            using (var dialog = new TodoDetails())
            using (var chooser = new CourseImportDialog(state))
            {
                host.Show();
                using (var surface = new DesktopSurface(scene, host.Handle))
                {
                    surface.Attach(host.RectangleToScreen(host.ClientRectangle));
                    var pending = IndependentWindows.Show(dialog);
                    dialog.WindowState = FormWindowState.Minimized;
                    var other = IndependentWindows.Show(chooser);
                    Application.DoEvents();
                    int clicks = 0;
                    surface.Clicked = delegate (Point point, bool dbl)
                    {
                        var hit = scene.At(point, surface.ClientSize);
                        if (hit != null && hit.Action == "todo")
                        {
                            state.ToggleTodo(hit.Id);
                            clicks++;
                        }
                    };
                    surface.Invalidate();
                    surface.Update();
                    var h = scene.Hits.First(target => target.Action == "todo");
                    int x = (int)(h.Box.X + h.Box.Width / 2), y = (int)(h.Box.Y + h.Box.Height / 2);
                    var pos = new IntPtr((y << 16) | (x & 65535));
                    SendMessage(surface.Handle, 0x201, new IntPtr(1), pos);
                    SendMessage(surface.Handle, 0x202, IntPtr.Zero, pos);
                    Application.DoEvents();
                    check(!pending.IsCompleted && !dialog.Modal && host.Enabled && clicks == 1 && state.Todos[0].Done, "minimized independent dialog leaves native calendar mouse dispatch enabled");
                    check(chooser.Visible && !other.IsCompleted, "another feature window opens while todo window is minimized");
                    chooser.Close();
                    dialog.Close();
                    check(pending.IsCompleted && other.IsCompleted, "closing modeless windows completes their save/cancel tasks");
                }

                host.Close();
            }

            using (var form = new TodoDetails())
            {
                var pending = IndependentWindows.Show(form);
                form.Content.Text = longText;
                All(form).OfType<Button>().First(b => b.Text == "保存").PerformClick();
                Application.DoEvents();
                check(pending.IsCompleted && pending.Result == DialogResult.OK && form.Result == longText, "modeless todo editor returns full saved content to its caller");
            }

            var live = new State
            {
                StartInTray = true
            };
            using (var controller = new Controller(live, false, true))
            {
                var flags = BindingFlags.NonPublic | BindingFlags.Instance;
                typeof(Controller).GetMethod("Settings", flags).Invoke(controller, null);
                Application.DoEvents();
                var settings = Application.OpenForms.OfType<SettingsDialog>().Single();
                settings.WindowState = FormWindowState.Minimized;
                typeof(Controller).GetMethod("AddTodo", flags).Invoke(controller, null);
                Application.DoEvents();
                var todo = Application.OpenForms.OfType<TodoDetails>().Single();
                todo.Content.Text = "设置窗口缩小时新增的待办";
                All(todo).OfType<Button>().First(b => b.Text == "保存").PerformClick();
                Application.DoEvents();
                settings.AddedEntries.Add(new Entry { Title = "设置里导入的日程", Start = DateTime.Today, End = DateTime.Today.AddHours(1), CalendarId = "personal" });
                settings.WindowState = FormWindowState.Normal;
                All(settings).OfType<Button>().First(b => b.Text == "应用").PerformClick();
                Application.DoEvents();
                var persisted = Data.Read();
                check(persisted.Todos.Any(t => t.Title == "设置窗口缩小时新增的待办") && persisted.Events.Any(e => e.Title == "设置里导入的日程"), "controller saves new todo while settings minimized, settings apply preserves concurrent todo and imports");
                typeof(Controller).GetMethod("Stop", flags).Invoke(controller, null);
            }
        }
    }
#endif

}