using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    public interface IResponsiveDialog
    {
    }

    /// <summary>
    /// 自带布局的窗口实现此接口后，可以拿到比例并在比例变化时重排。
    /// </summary>
    public interface IScaledDialog
    {
        /// <summary>接收窗口比例对象。</summary>
        void Attach(ResponsiveScale scale);
    }

    public static class CourseUI
    {
        /// <summary>
        /// 处理课表界面中的 Bar 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static FlowLayoutPanel Bar(Control parent, DockStyle dock)
        {
            var bar = new FlowLayoutPanel
            {
                Dock = dock,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = true,
                Padding = new Padding(4),
                BackColor = Palette.Paper
            };
            parent.Controls.Add(bar);
            return bar;
        }

        /// <summary>
        /// 绘制或创建可操作按钮，保留选中反馈与点击区域。
        /// </summary>
        public static Button Button(Control parent, string text, int width, Action action)
        {
            var b = UI.Button(parent, text, 0, 0, width, delegate
            {
                action();
            });
            b.Height = 36;
            b.Margin = new Padding(4);
            return b;
        }

        /// <summary>
        /// 处理课表界面中的 Table 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static DataGridView Table()
        {
            var g = new SmoothCourseGrid
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Palette.Paper,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = true,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };
            g.DefaultCellStyle.Padding = new Padding(5);
            g.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            g.EditingControlShowing += delegate (object sender, DataGridViewEditingControlShowingEventArgs e)
            {
                var text = e.Control as TextBox;
                if (text != null)
                    text.TextAlign = HorizontalAlignment.Center;
            };
            g.DefaultCellStyle.SelectionBackColor = Skins.Line;
            g.DefaultCellStyle.SelectionForeColor = Palette.Ink;
            g.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            g.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            g.ColumnHeadersDefaultCellStyle.BackColor = Palette.Cream;
            g.RowTemplate.Height = 36;
            g.GridColor = Skins.Line;
            return g;
        }

        /// <summary>
        /// 处理课表界面中的 Note 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static Label Note(Control parent, string text, int height)
        {
            var l = new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = height,
                Padding = new Padding(10),
                ForeColor = Palette.Muted
            };
            parent.Controls.Add(l);
            return l;
        }

        /// <summary>
        /// 保存课表界面中的 SaveExample 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static void SaveExample(Form owner)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "课表导入示例.xlsx");
            using (var file = new SaveFileDialog
            {
                Filter = L.T("Excel 工作簿 (*.xlsx)|*.xlsx"),
                FileName = "课表导入示例.xlsx"
            }

            )
                if (WindowPlacement.Show(file, owner) == DialogResult.OK)
                    try
                    {
                        if (!String.Equals(Path.GetFullPath(path), Path.GetFullPath(file.FileName), StringComparison.OrdinalIgnoreCase))
                            File.Copy(path, file.FileName, true);
                    }
                    catch (Exception ex)
                    {
                        Notice.Show(owner, ex.Message, "无法保存示例");
                    }
        }
    }

    public partial class CourseImportDialog
    {
        TabControl managerTabs;
        ComboBox importReminder;
        CheckBox teacherMode;
        /// <summary>
        /// 构建课表界面中的 BuildManager 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void BuildManager()
        {
            start = new DateTimePicker
            {
                Value = DateTime.Today,
                Checked = true,
                Visible = false
            };
            weeks = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 52,
                Value = 1,
                Width = 64,
                Margin = new Padding(4, 9, 12, 4)
            };
            emoji = new CheckBox
            {
                Text = L.T("课程配色 + emoji"),
                Checked = true,
                AutoSize = true,
                Margin = new Padding(12, 10, 4, 4)
            };
            var root = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14),
                BackColor = Palette.Paper
            };
            Controls.Add(root);
            var footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52
            };
            root.Controls.Add(footer);
            var commit = CourseUI.Button(footer, L.T("确认导入"), 130, ConfirmImport);
            commit.Location = new Point(footer.ClientSize.Width - 234, 6);
            commit.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            applyButton = CourseUI.Button(footer, L.T("应用"), 130, ApplyBook);
            applyButton.Location = commit.Location;
            applyButton.Anchor = commit.Anchor;
            applyButton.Visible = false;
            var cancel = CourseUI.Button(footer, L.T("关闭"), 90, () => Close());
            cancel.Location = new Point(footer.ClientSize.Width - 94, 6);
            cancel.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            CancelButton = cancel;
            status = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 75,
                Padding = new Padding(10),
                ForeColor = Palette.Muted,
                Text = L.T("选择 Excel 或手动添加课程，再核对预览。")
            };
            root.Controls.Add(status);
            managerTabs = new TabControl
            {
                Dock = DockStyle.Fill
            };
            root.Controls.Add(managerTabs);
            managerTabs.BringToFront();
            root.Controls.SetChildIndex(footer, root.Controls.Count - 1);
            var import = new TabPage(L.T("课程预览"))
            {
                BackColor = Palette.Paper,
                Padding = new Padding(8)
            };
            var saved = new TabPage(L.T("已导入课程"))
            {
                BackColor = Palette.Paper,
                Padding = new Padding(8)
            };
            managerTabs.TabPages.AddRange(new[] { import, saved });
            var toolbar = CourseUI.Bar(import, DockStyle.Top);
            CourseUI.Button(toolbar, L.T("选择 Excel"), 130, LoadFile);
            CourseUI.Button(toolbar, L.T("查看示例"), 115, ShowExample);
            CourseUI.Button(toolbar, L.T("＋ 手动添加"), 140, AddManual);
            CourseUI.Button(toolbar, L.T("删除选中行"), 130, DeletePreview);
            var options = CourseUI.Bar(import, DockStyle.Top);
            options.Controls.Add(new Label { Text = L.T("工作表"), AutoSize = true, Margin = new Padding(6, 12, 4, 4) });
            sheetList = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 210,
                Margin = new Padding(4, 8, 12, 4)
            };
            options.Controls.Add(sheetList);
            options.Controls.Add(new Label { Text = L.T("重复周数"), AutoSize = true, Margin = new Padding(4, 12, 4, 4) });
            options.Controls.Add(weeks);
            options.Controls.Add(emoji);
            teacherMode = new CheckBox
            {
                Text = L.T("教师模式（班级优先）"),
                AutoSize = true,
                Margin = new Padding(4, 10, 4, 4)
            };
            options.Controls.Add(teacherMode);
            options.Controls.Add(new Label { Text = L.T("提醒"), AutoSize = true, Margin = new Padding(12, 12, 4, 4) });
            importReminder = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 130,
                Margin = new Padding(4, 8, 4, 4)
            };
            importReminder.Items.AddRange(CourseBook.MinuteNames.Cast<object>().ToArray());
            importReminder.SelectedIndex = Array.IndexOf(CourseBook.Minutes, 15);
            options.Controls.Add(importReminder);
            teacherMode.CheckedChanged += delegate
            {
                UpdateStatus();
            };
            CourseUI.Note(import, "星期下拉选择；时间可下拉或输入；点“样式”可设颜色、高亮和 emoji。首课日期随星期调整。", 48);
            grid = CourseUI.Table();
            string[] names =
            {
                L.T("星期"),
                L.T("开始"),
                L.T("结束"),
                L.T("班级"),
                L.T("课程"),
                L.T("教室"),
                "emoji",
                L.T("首课日期")
            };
            foreach (var name in names)
                grid.Columns.Add(name, name);
            grid.Columns[7].ReadOnly = false;
            grid.CellDoubleClick += ChooseCourseValue;
            grid.Columns[0].FillWeight = 60;
            grid.Columns[6].FillWeight = 55;
            grid.Columns[4].FillWeight = 160;
            grid.Columns[7].FillWeight = 110;
            var del = new DataGridViewButtonColumn
            {
                HeaderText = L.T("操作"),
                Text = L.T("删除"),
                UseColumnTextForButtonValue = true,
                FillWeight = 65,
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(del);
            float[] weights =
            {
                60,
                70,
                70,
                95,
                140,
                90,
                60,
                95,
                65
            };
            for (int i = 0; i < weights.Length; i++)
                grid.Columns[i].FillWeight = weights[i];
            foreach (DataGridViewColumn col in grid.Columns)
                col.MinimumWidth = 55;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            InstallCourseChoices();
            import.Controls.Add(grid);
            grid.BringToFront();
            import.Controls.SetChildIndex(toolbar, 3);
            import.Controls.SetChildIndex(options, 2);
            grid.CellContentClick += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (e.ColumnIndex == 8 && e.RowIndex >= 0)
                    grid.Rows.RemoveAt(e.RowIndex);
            };
            grid.CellEndEdit += CourseCellEdited;
            grid.RowsRemoved += delegate
            {
                if (!updating)
                    UpdateStatus();
            };
            sheetList.SelectedIndexChanged += delegate
            {
                LoadSheet();
            };
            weeks.ValueChanged += delegate
            {
                UpdateStatus();
            };
            periodGrid = CourseUI.Table();
            periodGrid.Columns.Add("period", L.T("节次"));
            periodGrid.Columns.Add("start", L.T("开始"));
            periodGrid.Columns.Add("end", L.T("结束"));
            FormClosed += delegate
            {
                periodGrid.Dispose();
            };
            BuildBook(saved);
            managerTabs.SelectedIndexChanged += delegate
            {
                if (managerTabs.SelectedTab == saved)
                    RefreshBook();
                commit.Visible = managerTabs.SelectedTab == import;
                applyButton.Visible = !commit.Visible;
                status.Visible = managerTabs.SelectedTab == import;
            };
            RefreshBook();
        }

        /// <summary>
        /// 添加课表界面中的 AddManual 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void AddManual()
        {
            int n = grid.Rows.Add(L.T("周") + "一二三四五六日"[((int)DateTime.Today.DayOfWeek + 6) % 7], "08:00", "08:45", "", "", "", "", DateTime.Today.ToString("yyyy-MM-dd"));
            grid.CurrentCell = grid.Rows[n].Cells[4];
            grid.FirstDisplayedScrollingRowIndex = n;
            grid.BeginEdit(true);
            UpdateStatus();
        }

        /// <summary>
        /// 删除课表界面中的 DeletePreview 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        internal void DeletePreview()
        {
            grid.EndEdit();
            foreach (var row in grid.SelectedRows.Cast<DataGridViewRow>().OrderByDescending(r => r.Index).ToList())
                grid.Rows.Remove(row);
            UpdateStatus();
        }

        /// <summary>
        /// 显示课表界面中的 ShowExample 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        internal async void ShowExample()
        {
            try
            {
                using (var f = new CourseExampleDialog())
                    await IndependentWindows.Show(f);
            }
            catch (Exception ex)
            {
                Notice.Show(this, ex.Message, "示例未打开");
            }
        }
    }

    public class CourseExampleDialog : Form, IResponsiveDialog
    {
        /// <summary>
        /// 初始化课表界面的CourseExampleDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public CourseExampleDialog()
        {
            UI.Style(this, "课表示例 · 点击即可查看", 880, 470);
            var root = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18)
            };
            Controls.Add(root);
            CourseUI.Note(root, "参考格式：星期和起止时间需有效；课程名或班级填写一项即可，首课日期可留空。", 62);
            var actions = CourseUI.Bar(root, DockStyle.Bottom);
            CourseUI.Button(actions, L.T("保存 Excel 副本"), 170, () => CourseUI.SaveExample(this));
            CourseUI.Button(actions, L.T("关闭"), 100, () => Close());
            var table = CourseUI.Table();
            table.ReadOnly = true;
            table.AllowUserToDeleteRows = false;
            foreach (var name in new[]
            {
                L.T("星期"),
                L.T("开始"),
                L.T("结束"),
                L.T("课程"),
                L.T("班级"),
                L.T("教室")
            }

            )
                table.Columns.Add(name, name);
            table.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            foreach (var c in SmartCourses.Parse(XlsxReader.Read(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "课表导入示例.xlsx"))[0]))
                table.Rows.Add(L.T("周") + "一二三四五六日"[c.Day - 1], c.Start.ToString(@"hh\:mm"), c.End.ToString(@"hh\:mm"), c.Title, c.Class, c.Room);
            root.Controls.Add(table);
            table.BringToFront();
        }
    }
}