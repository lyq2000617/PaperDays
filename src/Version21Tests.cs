using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version21Tests
    {
        /// <summary>
        /// 执行Version21Tests中的All验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static System.Collections.Generic.IEnumerable<Control> All(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var d in All(c))
                    yield return d;
            }
        }

        /// <summary>
        /// 执行Version21Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            using (var dialog = new CourseImportDialog(new State()))
            {
                dialog.Show();
                Application.DoEvents();
                check(!All(dialog).OfType<TabPage>().Any(t => t.Text == "节次时间"), "period-time tab removed from course manager");
                All(dialog).OfType<Button>().First(b => b.Text == "＋ 手动添加").PerformClick();
                dialog.PreviewGrid.EndEdit();
                var grid = dialog.PreviewGrid;
                var row = grid.Rows[0];
                check(Convert.ToString(row.Cells[7].Value) == DateTime.Today.ToString("yyyy-MM-dd"), "new manual course starts with today's date");
                check(grid.Columns[0] is DataGridViewComboBoxColumn && ((DataGridViewComboBoxColumn)grid.Columns[0]).Items.Count == 7, "weekday dropdown exposes seven days");
                row.Cells[0].Value = "周五";
                var expected = DateTime.Today.AddDays((5 - ((int)DateTime.Today.DayOfWeek + 6) % 7 - 1 + 7) % 7);
                check(Convert.ToString(row.Cells[7].Value) == expected.ToString("yyyy-MM-dd"), "weekday change adjusts first date to nearest occurrence from today");
                grid.CurrentCell = row.Cells[1];
                grid.BeginEdit(true);
                var time = grid.EditingControl as TimeEdit;
                check(time != null && time.DropDownStyle == ComboBoxStyle.DropDown && time.Items.Contains("08:30"), "time cell supports editable dropdown");
                time.Text = "08:32";
                grid.EndEdit();
                check(Convert.ToString(row.Cells[1].Value) == "08:32", "arbitrary valid minute typed and retained");
                row.Cells[2].Value = "09:15";
                row.Cells[4].Value = "数学";
                dialog.ApplyMark(row, 6, "★");
                dialog.ApplyMark(row, 9, "#789888");
                using (var bmp = new Bitmap(dialog.Width, dialog.Height))
                {
                    dialog.DrawToBitmap(bmp, new Rectangle(Point.Empty, dialog.Size));
                    bmp.Save(Path.Combine(directory, "course-choices.png"));
                }

                dialog.ConfirmImport();
                var state = new State();
                CourseImporter.Commit(state, dialog.Courses, DateTime.Today, 1, true);
                check(state.Events.Single().Emoji == "★" && state.Events.Single().Color == "#789888" && state.Events.Single().ColorSlot == -1, "selected emoji and custom color persist in calendar entries");
                Skins.Apply(state);
                check(state.Events.Single().Color == "#789888", "selected course color is not overwritten on repaint");
            }

            var todos = new State();
            var scene = new Scene
            {
                State = todos
            };
            using (var host = new Form
            {
                ClientSize = new Size(1280, 800)
            }

            )
            using (var view = new SceneView(scene)
            {
                Dock = DockStyle.Fill
            }

            )
            {
                host.Controls.Add(view);
                host.Show();
                host.Activate();
                Application.DoEvents();
                view.SaveInlineTodo = (id, text) =>
                {
                    var item = todos.Todos.FirstOrDefault(t => t.Id == id);
                    if (item == null)
                        todos.Todos.Add(new Todo { Title = text });
                    else
                        item.Title = text;
                    Data.Write(todos);
                    return true;
                };
                int forms = Application.OpenForms.Count;
                view.BeginInline();
                Application.DoEvents();
                check(view.InlineInput != null && view.InlineInput.Multiline && view.InlineInput.AcceptsReturn && Application.OpenForms.Count == forms, "inline todo begins with native multiline editor and no popup");
                view.InlineInput.Text = "准备课程\r\n整理课堂反馈";
                Application.DoEvents();
                check(todos.Todos.Count == 0, "draft is not submitted by entering multiline text");
                view.FinishInline();
                check(Data.Read().Todos.Single().Title == "准备课程\r\n整理课堂反馈" && view.InlineInput == null, "inline commit persists line breaks and removes editor");
                view.BeginInline();
                view.InlineInput.Text = "  \r\n ";
                view.FinishInline();
                check(todos.Todos.Count == 1, "empty inline draft creates no todo");
                view.BeginInline(todos.Todos[0].Id);
                view.InlineInput.Text = "修改后的待办";
                var target = new Button
                {
                    Text = "桌面其他区域",
                    Location = new Point(10, 10)
                };
                host.Controls.Add(target);
                target.BringToFront();
                target.Focus();
                Application.DoEvents();
                check(view.InlineInput == null && Data.Read().Todos.Single().Title == "修改后的待办", "leaving inline input automatically saves edits");
                view.BeginInline();
                view.InlineInput.Text = String.Concat(Enumerable.Repeat("很长的待办内容，支持自动换行。", 50));
                Application.DoEvents();
                view.Update();
                check(scene.InlineHeight <= 200 && view.InlineInput.ScrollBars == RichTextBoxScrollBars.Vertical, "long inline draft grows to bounded height then scrolls");
                using (var image = new Bitmap(view.Width, view.Height))
                {
                    view.DrawToBitmap(image, new Rectangle(Point.Empty, view.Size));
                    image.Save(Path.Combine(directory, "inline-todo.png"));
                }

                view.SaveInlineTodo = (id, text) => false;
                check(!view.FinishInline() && view.InlineInput.TextLength > 100, "failed autosave keeps draft available");
                view.SaveInlineTodo = (id, text) => true;
                view.FinishInline();
                host.Close();
            }
        }
    }
#endif

}