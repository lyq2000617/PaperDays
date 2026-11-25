using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version25Tests
    {
        /// <summary>
        /// 执行Version25Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string dir)
        {
            using (var dialog = new CourseImportDialog(new State()))
            {
                Tests.ShowHost(dialog);
                dialog.PreviewGrid.Rows.Add("周一", "08:00", "09:00", "三班", "数学", "", "", "2026-11-09");
                var choose = typeof(CourseImportDialog).GetMethod("ChoiceClick", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                for (int round = 0; round < 3; round++)
                    foreach (int column in new[]
                    {
                        9,
                        6
                    }

                    )
                    {
                        choose.Invoke(dialog, new object[] { dialog.PreviewGrid, new DataGridViewCellEventArgs(column, 0) });
                        var menu = dialog.ChoiceMenu;
                        check(menu != null && !menu.IsDisposed, "course picker opens " + round + " " + column);
                        var panel = (FlowLayoutPanel)((ToolStripControlHost)menu.Items[0]).Control;
                        var button = panel.Controls.OfType<Button>().Skip(1).First();
                        button.PerformClick();
                        check(!menu.IsDisposed, "course menu is not disposed inside its close callback");
                        var until = DateTime.UtcNow.AddMilliseconds(80);
                        while (DateTime.UtcNow < until)
                        {
                            Application.DoEvents();
                            System.Threading.Thread.Sleep(2);
                        }

                        check(menu.IsDisposed && Convert.ToString(dialog.PreviewGrid.Rows[0].Cells[column].Value) == (column == 9 ? Palette.Hex[1] : CourseIcons.Emoji[0]), "picker selection applied and cleanup completes after close " + round + " " + column);
                    }

                choose.Invoke(dialog, new object[] { dialog.PreviewGrid, new DataGridViewCellEventArgs(9, 0) });
                var dismissed = dialog.ChoiceMenu;
                dismissed.Close(ToolStripDropDownCloseReason.Keyboard);
                var stop = DateTime.UtcNow.AddMilliseconds(80);
                while (DateTime.UtcNow < stop)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(2);
                }

                check(dismissed.IsDisposed, "cancelled course picker releases safely");
                dialog.Close();
            }

            TimeSpan chinese, english;
            check(Editor.TryClock(" 09：07 ", out chinese) && Editor.TryClock("09:07", out english) && chinese == english && !Editor.TryClock("25：99", out chinese), "Chinese and English colon inputs are equivalent with validation");
            var today = new DateTime(2026, 12, 31);
            var state = new State();
            var pending = new Todo
            {
                Title = "整理课程\r\n核对学生反馈",
                Date = "2026-12-30"
            };
            var done = new Todo
            {
                Title = "完成备课",
                Date = "2026-12-29",
                Done = true,
                CompletedAt = new DateTime(2026, 12, 30)
            };
            var future = new Todo
            {
                Title = "下周工作",
                Date = "2027-01-05"
            };
            state.Todos.AddRange(new[] { pending, done, future });
            check(PlanReview.Items(state, "day", today).Select(t => t.Id).SequenceEqual(new[] { pending.Id }), "overdue unfinished plan automatically appears today without completed or future tasks");
            check(PlanReview.Items(state, "day", today.AddDays(3)).Count == 1 && pending.Date == "2026-12-30" && state.Todos.Count == 3, "rollover across year and several offline days preserves one original record");
            pending.Done = true;
            pending.CompletedAt = today.AddHours(10);
            check(PlanReview.Items(state, "day", today).Contains(pending) && !PlanReview.Items(state, "day", today.AddDays(1)).Contains(pending), "carried plan stays visible when completed today and stops carrying tomorrow");
            pending.Done = false;
            pending.CompletedAt = null;
            Data.Write(state);
            var restored = Data.Read();
            check(PlanReview.Items(restored, "day", today).Count == 1 && restored.Todos.Count == 3, "rollover and original records survive restart");
            check(TodoExport.Filter(state, new DateTime(2026, 12, 30), new DateTime(2026, 12, 30), false, 0).Single().Id == pending.Id, "inclusive plan-date boundaries select exact day");
            check(TodoExport.Filter(state, new DateTime(2026, 12, 30), new DateTime(2026, 12, 30), true, 0).Single().Id == done.Id, "completion-date filter excludes unknown and unfinished dates");
            check(TodoExport.Filter(state, today, today.AddDays(-1), false, 0).Count == 0, "reversed export range produces no records");
            using (var history = new TodoHistory(() => state, id =>
            {
            }))
            {
                Tests.ShowHost(history);
                history.SetAll(true);
                check(history.SelectedRows().Length == 3, "history checkbox select-all includes visible records");
                history.Items.SetItemChecked(0, false);
                Application.DoEvents();
                string output = history.ExportText();
                check(history.SelectedRows().Length == 2 && !output.Contains("下周工作") && output.Contains("整理课程") && output.Contains("核对学生反馈") && output.Contains("未完成") && output.Contains("不得编造"), "AI export contains only checked records with complete multiline content and fact rules");
                File.WriteAllText(Path.Combine(dir, "AI-summary-example.txt"), output);
                history.UseRange.Checked = true;
                history.From.Value = new DateTime(2026, 12, 30);
                history.To.Value = new DateTime(2026, 12, 30);
                history.RefreshRows();
                check(history.Items.Items.Count == 1 && history.SelectedRows().Length == 1, "filter preserves visible checked items and excludes hidden choices from export");
                using (var bmp = new Bitmap(history.Width, history.Height))
                {
                    history.DrawToBitmap(bmp, new Rectangle(Point.Empty, history.Size));
                    bmp.Save(Path.Combine(dir, "history-export.png"));
                }

                history.Close();
            }

            TimeSpan time;
            check(Editor.TryClock("8:07", out time) && time == new TimeSpan(8, 7, 0) && !Editor.TryClock("24:60", out time), "time editor accepts typed arbitrary minutes and rejects invalid time");
            using (var editor = new Editor(new State(), null, today))
            {
                Tests.ShowHost(editor);
                editor.TitleInput.Text = "韩语课";
                editor.StartTime.Text = "18：07";
                editor.EndTime.Text = "20:10";
                check(editor.StartTime.DropDownStyle == ComboBoxStyle.DropDown && editor.StartTime.Items.Contains("18:15") && editor.TitleInput.Font.Size <= 12, "compact title and editable time dropdown");
                editor.AllDay.Checked = true;
                check(!editor.StartTime.Visible && !editor.EndTime.Visible, "all-day hides time inputs");
                editor.AllDay.Checked = false;
                check(editor.StartTime.Visible && editor.EndTime.Visible, "timed mode restores time inputs");
                using (var bmp = new Bitmap(editor.Width, editor.Height))
                {
                    editor.DrawToBitmap(bmp, new Rectangle(Point.Empty, editor.Size));
                    bmp.Save(Path.Combine(dir, "editor25.png"));
                }

                check(editor.TrySave() && editor.Result.Start.Minute == 7 && editor.Result.End.Minute == 10, "new time controls preserve typed minutes on save");
            }
        }
    }
#endif

}