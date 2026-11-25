using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version19Tests
    {
        /// <summary>
        /// 执行Version19Tests中的All验证；使用隔离测试数据，不修改用户的真实记录。
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
        /// 执行Version19Tests中的Yes验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static void Yes(Button button)
        {
            using (var timer = new Timer
            {
                Interval = 60
            }

            )
            {
                timer.Tick += delegate
                {
                    var prompt = Application.OpenForms.Cast<Form>().FirstOrDefault(f => f.Modal);
                    if (prompt == null)
                        return;
                    var yes = All(prompt).OfType<Button>().FirstOrDefault(b => b.Text == "确认");
                    if (yes != null)
                    {
                        timer.Stop();
                        yes.PerformClick();
                    }
                };
                timer.Start();
                button.PerformClick();
            }
        }

        /// <summary>
        /// 执行Version19Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            foreach (var area in new[]
            {
                new Rectangle(0, 0, 800, 600),
                new Rectangle(-1280, 0, 1280, 720)
            }

            )
            {
                WindowPlacement.Target = () => area;
                using (var dialog = new CourseImportDialog(new State()))
                {
                    dialog.Show();
                    Application.DoEvents();
                    var tabs = All(dialog).OfType<TabControl>().Single();
                    foreach (TabPage tab in tabs.TabPages)
                    {
                        tabs.SelectedTab = tab;
                        Application.DoEvents();
                        check(area.Contains(dialog.Bounds), "course window fits monitor: " + area + " / " + tab.Text);
                        foreach (var b in All(tab).OfType<Button>().Where(b => b.Visible))
                            check(dialog.RectangleToScreen(dialog.ClientRectangle).Contains(b.RectangleToScreen(b.ClientRectangle)), "course action remains on screen: " + b.Text);
                        using (var bitmap = new Bitmap(dialog.Width, dialog.Height))
                        {
                            dialog.DrawToBitmap(bitmap, new Rectangle(Point.Empty, dialog.Size));
                            bitmap.Save(Path.Combine(directory, "course-" + area.Width + "-" + tabs.SelectedIndex + ".png"));
                        }
                    }

                    tabs.SelectedIndex = 0;
                    var add = All(dialog).OfType<Button>().First(b => b.Text == "＋ 手动添加");
                    add.PerformClick();
                    dialog.PreviewGrid.EndEdit();
                    check(dialog.PreviewGrid.Rows.Count == 1 && dialog.PreviewGrid.Columns[8] is DataGridViewButtonColumn, "manual course exposes per-row delete button");
                    dialog.PreviewGrid.ClearSelection();
                    dialog.PreviewGrid.Rows[0].Selected = true;
                    All(dialog).OfType<Button>().First(b => b.Text == "删除选中行").PerformClick();
                    check(dialog.PreviewGrid.Rows.Count == 0, "manual preview course can be explicitly deleted");
                    All(dialog).OfType<Button>().First(b => b.Text == "查看示例").PerformClick();
                    Application.DoEvents();
                    var example = Application.OpenForms.OfType<CourseExampleDialog>().Single();
                    check(All(example).OfType<DataGridView>().Single().Rows.Count == 6 && dialog.PreviewGrid.Rows.Count == 0, "one click opens real workbook example without importing it");
                    example.Close();
                    dialog.Close();
                }
            }

            WindowPlacement.Target = null;
            var state = new State();
            state.Todos.Add(new Todo { Title = "删除这条" });
            state.Todos.Add(new Todo { Title = "保留这条" });
            state.Todos.Add(new Todo { Title = "删除历史", Date = "2020-01-01", Done = true });
            using (var history = new TodoHistory(() => state, id =>
            {
            }, ids =>
            {
                foreach (var id in ids)
                    state.DeleteTodo(id);
                Data.Write(state);
                return true;
            }))
            {
                history.Show();
                Application.DoEvents();
                history.Items.SetItemChecked(0, true);
                history.Items.SetItemChecked(2, true);
                Application.DoEvents();
                Yes(history.DeleteButton);
                check(state.Todos.Count == 1 && state.Todos[0].Title == "保留这条" && Data.Read().Todos.Count == 1, "history multi-select delete persists only selected todo removals");
                history.Close();
            }

            using (var detail = new TodoDetails(new Todo { Title = "详情可删除" }))
            {
                detail.Show();
                Yes(All(detail).OfType<Button>().First(b => b.Text == "删除待办"));
                check(detail.Deleted && detail.DialogResult == DialogResult.OK, "todo detail has explicit deletion action and confirmation");
            }

            state = new State
            {
                Countries = new List<string>
                {
                    "CN",
                    "KR"
                }
            };
            string status;
            var november = HolidayUpdates.Load(state, new DateTime(2026, 11, 1), out status);
            check(status.Contains("中国：本月无已公布假日") && november.Any(m => m.Source.StartsWith("CN")), "November zero holidays is distinguished from missing Chinese calendar");
            HolidayUpdates.Load(state, new DateTime(2027, 1, 1), out status);
            check(status.Contains("放假调休待公告更新") && status.Contains("韩国"), "missing China year is explicit alongside loaded Korean calendar");
            var october = HolidayUpdates.Load(state, new DateTime(2026, 10, 1), out status);
            check(october.Any(m => m.Date == "2026-10-01" && m.Source.StartsWith("CN")) && october.Any(m => m.Date == "2026-10-03" && m.Source.StartsWith("KR")), "Chinese and Korean holidays coexist in October");
            string testRoot = Data.Root, legacy = Path.Combine(directory, "old-records"), fresh = Path.Combine(directory, "new-records");
            Data.Root = legacy;
            state.Todos.Add(new Todo { Title = "旧记录" });
            state.Events.Add(new Entry { Title = "旧日程", Start = DateTime.Today, End = DateTime.Today.AddHours(1) });
            state.Skin = "sage";
            Data.Write(state);
            try
            {
                Data.StartFreshVersion19(legacy, fresh);
                var loaded = Data.Read();
                check(loaded.Todos.Count == 0 && loaded.Events.Count == 0 && loaded.Skin == "sage" && loaded.Countries.SequenceEqual(new[] { "CN", "KR" }), "new version starts empty while retaining appearance and country settings");
                loaded.Todos.Add(new Todo { Title = "新记录" });
                Data.Write(loaded);
                Data.StartFreshVersion19(legacy, fresh);
                check(Data.Read().Todos.Count == 1 && File.ReadAllText(Path.Combine(legacy, "calendar.json")).Contains("旧记录"), "subsequent launch preserves new records and old data remains untouched");
            }
            finally
            {
                Data.Root = testRoot;
            }
        }
    }
#endif

}