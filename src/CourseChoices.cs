using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    public class TimeCell : DataGridViewTextBoxCell
    {
        public override Type EditType
        {
            /// <summary>
            /// 读取EditType属性，供CourseChoices使用。
            /// </summary>
            get
            {
                return typeof(TimeEdit);
            }
        }

        /// <summary>
        /// 初始化课程表格单元格的编辑控件。
        /// </summary>
        public override void InitializeEditingControl(int row, object value, DataGridViewCellStyle style)
        {
            base.InitializeEditingControl(row, value, style);
            ((TimeEdit)DataGridView.EditingControl).Text = Convert.ToString(Value);
        }
    }

    public class TimeEdit : TimeField, IDataGridViewEditingControl
    {
        /// <summary>初始化与日程共用的时间选择器，并通知表格提交编辑值。</summary>
        public TimeEdit()
        {
            Input.TextAlign = HorizontalAlignment.Center;
            Items.Clear();
            for (int m = 0; m < 1440; m += 5)
                Items.Add(TimeSpan.FromMinutes(m).ToString(@"hh\:mm"));
            TextChanged += delegate
            {
                EditingControlValueChanged = true;
                if (EditingControlDataGridView != null)
                    EditingControlDataGridView.NotifyCurrentCellDirty(true);
            };
        }

        public DataGridView EditingControlDataGridView { 
            /// <summary>
            /// 读取EditingControlDataGridView属性，供CourseChoices使用。
            /// </summary>
  get; 
            /// <summary>
            /// 更新EditingControlDataGridView属性，供CourseChoices使用。
            /// </summary>
  set; }
        public int EditingControlRowIndex { 
            /// <summary>
            /// 读取EditingControlRowIndex属性，供CourseChoices使用。
            /// </summary>
  get; 
            /// <summary>
            /// 更新EditingControlRowIndex属性，供CourseChoices使用。
            /// </summary>
  set; }
        public bool EditingControlValueChanged { 
            /// <summary>
            /// 读取EditingControlValueChanged属性，供CourseChoices使用。
            /// </summary>
  get; 
            /// <summary>
            /// 更新EditingControlValueChanged属性，供CourseChoices使用。
            /// </summary>
  set; }

        public Cursor EditingPanelCursor
        {
            /// <summary>
            /// 读取EditingPanelCursor属性，供CourseChoices使用。
            /// </summary>
            get
            {
                return Cursors.Default;
            }
        }

        public bool RepositionEditingControlOnValueChange
        {
            /// <summary>
            /// 读取RepositionEditingControlOnValueChange属性，供CourseChoices使用。
            /// </summary>
            get
            {
                return false;
            }
        }

        public object EditingControlFormattedValue
        {
            /// <summary>
            /// 读取EditingControlFormattedValue属性，供CourseChoices使用。
            /// </summary>
            get
            {
                return Text;
            }

            /// <summary>
            /// 更新EditingControlFormattedValue属性，供CourseChoices使用。
            /// </summary>
            set
            {
                Text = Convert.ToString(value);
            }
        }

        /// <summary>
        /// 读取CourseChoices中的 GetEditingControlFormattedValue 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context)
        {
            return Text;
        }

        /// <summary>
        /// 应用CourseChoices中的 ApplyCellStyleToEditingControl 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public void ApplyCellStyleToEditingControl(DataGridViewCellStyle style)
        {
            Font = style.Font;
            ForeColor = style.ForeColor;
            BackColor = style.BackColor;
        }

        /// <summary>
        /// 处理CourseChoices中的 EditingControlWantsInputKey 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public bool EditingControlWantsInputKey(Keys key, bool wanted)
        {
            var code = key & Keys.KeyCode;
            return code != Keys.Tab && code != Keys.Enter && code != Keys.Escape;
        }

        /// <summary>
        /// 处理CourseChoices中的 PrepareEditingControlForEdit 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public void PrepareEditingControlForEdit(bool select)
        {
            if (select)
                Input.SelectAll();
            Input.Focus();
        }
    }

    public partial class CourseImportDialog
    {
        bool choosing;
        internal ToolStripDropDown ChoiceMenu;
        /// <summary>
        /// 处理CourseChoices中的 InstallCourseChoices 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        internal void InstallCourseChoices()
        {
            grid.EditMode = DataGridViewEditMode.EditOnEnter;
            grid.Columns.RemoveAt(0);
            var days = new DataGridViewComboBoxColumn
            {
                Name = L.T("星期"),
                HeaderText = L.T("星期"),
                FlatStyle = FlatStyle.Flat,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
            };
            days.Items.AddRange(new object[] { L.T("周一"), L.T("周二"), L.T("周三"), L.T("周四"), L.T("周五"), L.T("周六"), L.T("周日") });
            grid.Columns.Insert(0, days);
            for (int i = 1; i <= 2; i++)
            {
                grid.Columns[i].CellTemplate = new TimeCell();
            }

            grid.Columns[6].ReadOnly = true;
            grid.Columns.Add(L.T("颜色"), L.T("颜色"));
            grid.Columns[9].ReadOnly = true;
            grid.Columns[9].FillWeight = 55;
            grid.Columns[9].MinimumWidth = 38;
            float[] weights =
            {
                65,
                80,
                80,
                90,
                140,
                70,
                55,
                125,
                65,
                50
            };
            int[] minimums =
            {
                58,
                65,
                65,
                50,
                70,
                45,
                40,
                100,
                55,
                55
            };
            for (int i = 0; i < 10; i++)
            {
                grid.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                grid.Columns[i].FillWeight = weights[i];
                grid.Columns[i].MinimumWidth = minimums[i];
            }

            grid.Columns[9].DisplayIndex = 8;
            var style = new DataGridViewButtonColumn
            {
                HeaderText = L.T("样式"),
                Text = L.T("设置"),
                UseColumnTextForButtonValue = true,
                FillWeight = 55,
                MinimumWidth = 55,
                FlatStyle = FlatStyle.Flat
            };
            grid.Columns.Add(style);
            style.DisplayIndex = 9;
            grid.CellContentClick += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (e.ColumnIndex == 10 && e.RowIndex >= 0)
                    ChooseStyle(grid.Rows[e.RowIndex]);
            };
            grid.CurrentCellDirtyStateChanged += delegate
            {
                if (grid.CurrentCell is DataGridViewComboBoxCell && grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            grid.CellValueChanged += delegate (object sender, DataGridViewCellEventArgs e)
            {
                if (!updating && !choosing && e.RowIndex >= 0 && e.ColumnIndex == 0)
                    AdjustFirstDate(grid.Rows[e.RowIndex]);
            };
            grid.CellClick += ChoiceClick;
            grid.CellPainting += delegate (object sender, DataGridViewCellPaintingEventArgs e)
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 6)
                {
                    PaintCourseIcon(e);
                    return;
                }

                if (e.RowIndex < 0 || e.ColumnIndex != 9)
                    return;
                e.PaintBackground(e.ClipBounds, true);
                string hex = Convert.ToString(grid.Rows[e.RowIndex].Cells[9].Value);
                if (String.IsNullOrEmpty(hex))
                    hex = Convert.ToString(grid.Rows[e.RowIndex].Cells[9].Tag);
                Color color = Palette.Hex.Length > 0 ? ColorTranslator.FromHtml(Palette.Hex[0]) : Color.Gray;
                try
                {
                    if (!String.IsNullOrEmpty(hex))
                        color = ColorTranslator.FromHtml(hex);
                }
                catch
                {
                }

                using (var brush = new SolidBrush(color))
                    e.Graphics.FillEllipse(brush, e.CellBounds.X + e.CellBounds.Width / 2 - 7, e.CellBounds.Y + e.CellBounds.Height / 2 - 7, 14, 14);
                e.Handled = true;
            };
        }

        /// <summary>
        /// 处理CourseChoices中的 AdjustFirstDate 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        internal void AdjustFirstDate(DataGridViewRow row)
        {
            if (choosing)
                return;
            choosing = true;
            try
            {
                int day = CourseImporter.Day(Convert.ToString(row.Cells[0].Value));
                row.Cells[7].Value = DateTime.Today.AddDays((day - ((int)DateTime.Today.DayOfWeek + 6) % 7 - 1 + 7) % 7).ToString("yyyy-MM-dd");
            }
            finally
            {
                choosing = false;
            }

            UpdateStatus();
        }

        /// <summary>
        /// 处理CourseChoices中的 CourseCellEdited 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void CourseCellEdited(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 7)
                try
                {
                    var date = CourseDates.Optional(Convert.ToString(grid.Rows[e.RowIndex].Cells[7].Value));
                    if (date.HasValue)
                    {
                        choosing = true;
                        grid.Rows[e.RowIndex].Cells[0].Value = L.T("周") + "一二三四五六日"[((int)date.Value.DayOfWeek + 6) % 7];
                    }
                }
                catch
                {
                }
                finally
                {
                    choosing = false;
                }

            UpdateStatus();
        }

        /// <summary>
        /// 处理CourseChoices中的 ChoiceClick 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void ChoiceClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || (e.ColumnIndex != 6 && e.ColumnIndex != 9))
                return;
            grid.EndEdit();
            var row = grid.Rows[e.RowIndex];
            if (ChoiceMenu != null && !ChoiceMenu.IsDisposed)
                ChoiceMenu.Close();
            var menu = new ToolStripDropDown
            {
                Padding = new Padding(8),
                BackColor = Palette.Paper
            };
            ChoiceMenu = menu;
            var panel = new FlowLayoutPanel
            {
                Size = new Size(264, 100),
                BackColor = Palette.Paper,
                Padding = new Padding(4)
            };
            if (e.ColumnIndex == 9)
            {
                foreach (string hex in Palette.Hex)
                {
                    string chosen = hex;
                    var dot = new ColorDot
                    {
                        Size = new Size(36, 36),
                        DotColor = ColorTranslator.FromHtml(hex),
                        AccessibleName = L.T("课程颜色")
                    };
                    dot.Click += delegate
                    {
                        ApplyMark(row, 9, chosen);
                        if (!menu.IsDisposed)
                            menu.Close();
                    };
                    panel.Controls.Add(dot);
                }
            }
            else
            {
                panel.Size = new Size(306, 184);
                foreach (string symbol in new[] { "无" }.Concat(CourseIcons.Emoji))
                {
                    string chosen = symbol;
                    Button b = symbol == "无" ? (Button)new SoftButton { Text = symbol, BackColor = Palette.Cream, ForeColor = Palette.Ink } : new EmojiButton { Emoji = symbol, Original = false };
                    b.Size = new Size(36, 36);
                    b.Click += delegate
                    {
                        ApplyMark(row, 6, chosen);
                        if (!menu.IsDisposed)
                            menu.Close();
                    };
                    panel.Controls.Add(b);
                }
            }

            var host = new ToolStripControlHost(panel)
            {
                AutoSize = false,
                Size = panel.Size,
                Margin = Padding.Empty
            };
            menu.Items.Add(host);
            menu.Closed += delegate
            {
                if (ChoiceMenu == menu)
                    ChoiceMenu = null;
                var cleanup = new Timer
                {
                    Interval = 1
                };
                cleanup.Tick += delegate
                {
                    cleanup.Stop();
                    cleanup.Dispose();
                    if (!menu.IsDisposed)
                        menu.Dispose();
                };
                cleanup.Start();
            };
            var rect = grid.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, true);
            menu.Show(grid, new Point(rect.Left, rect.Bottom));
        }

        /// <summary>
        /// 在预览表格里按当前样式画课程图标，和桌面显示一致。
        /// </summary>
        void PaintCourseIcon(DataGridViewCellPaintingEventArgs e)
        {
            string emoji = Convert.ToString(e.Value);
            if (String.IsNullOrEmpty(emoji) || emoji == "无")
                return;
            e.PaintBackground(e.ClipBounds, true);
            var row = grid.Rows[e.RowIndex];
            var style = StyleOf(row);
            string color = Convert.ToString(row.Cells[9].Value);
            if (String.IsNullOrEmpty(color))
                color = Convert.ToString(row.Cells[9].Tag);
            string ink = Skins.EmojiInk(new Entry { Color = String.IsNullOrEmpty(color) ? Palette.Hex[0] : color, EmojiTint = style != null && style.EmojiTint });
            int side = Math.Min(22, e.CellBounds.Height - 8);
            EmojiArt.Draw(e.Graphics, emoji, new RectangleF(e.CellBounds.X + (e.CellBounds.Width - side) / 2f, e.CellBounds.Y + (e.CellBounds.Height - side) / 2f, side, side), style != null && style.EmojiOriginal, ink);
            e.Handled = true;
        }

        readonly Dictionary<string, Entry> styles = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 取得行所属课程的名称，同名课程共用样式。
        /// </summary>
        string CourseName(DataGridViewRow row)
        {
            return Convert.ToString(row.Cells[teacherMode.Checked ? 3 : 4].Value).Trim();
        }

        /// <summary>
        /// 取得这一行课程已设置的样式。
        /// </summary>
        Entry StyleOf(DataGridViewRow row)
        {
            Entry style;
            return styles.TryGetValue(CourseName(row), out style) ? style : null;
        }

        /// <summary>
        /// 打开和日程相同的颜色、高亮、emoji 设置，结果应用到这门课的所有行。
        /// </summary>
        void ChooseStyle(DataGridViewRow row)
        {
            grid.EndEdit();
            using (var editor = StyleEditor(row))
                if (editor.ShowDialog(this) == DialogResult.OK)
                    ApplyStyle(row, editor.Result);
        }

        /// <summary>
        /// 用这一行的当前样式创建样式窗口。
        /// </summary>
        internal Editor StyleEditor(DataGridViewRow row)
        {
            string color = Convert.ToString(row.Cells[9].Value), emoji = Convert.ToString(row.Cells[6].Value);
            if (String.IsNullOrEmpty(color))
                color = Convert.ToString(row.Cells[9].Tag);
            var current = new Entry { Title = CourseName(row).Length > 0 ? CourseName(row) : L.T("课程"), Start = DateTime.Today.AddHours(9), End = DateTime.Today.AddHours(10), Color = String.IsNullOrEmpty(color) ? Palette.Hex[0] : color, HighlightSlot = 0 };
            current.UseEmoji = emoji != "无";
            if (emoji.Length > 0 && emoji != "无")
                current.Emoji = emoji;
            var saved = StyleOf(row);
            if (saved != null)
            {
                current.EmojiOriginal = saved.EmojiOriginal;
                current.EmojiTint = saved.EmojiTint;
                current.Highlight = saved.Highlight;
                current.HighlightColor = saved.HighlightColor;
                current.HighlightSlot = saved.HighlightSlot;
            }

            return new Editor(existing, current, DateTime.Today, false, true);
        }

        /// <summary>
        /// 保存样式并更新这门课所有行的颜色和 emoji。
        /// </summary>
        internal void ApplyStyle(DataGridViewRow row, Entry style)
        {
            styles[CourseName(row)] = style;
            ApplyMark(row, 6, style.UseEmoji ? style.Emoji : "无");
            ApplyMark(row, 9, style.Color);
        }

        /// <summary>
        /// 应用CourseChoices中的 ApplyMark 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        internal void ApplyMark(DataGridViewRow row, int column, string value)
        {
            string key = Convert.ToString(row.Cells[teacherMode.Checked ? 3 : 4].Value).Trim();
            foreach (DataGridViewRow r in grid.Rows)
                if (r == row || key.Length > 0 && String.Equals(Convert.ToString(r.Cells[teacherMode.Checked ? 3 : 4].Value).Trim(), key, StringComparison.OrdinalIgnoreCase))
                {
                    r.Cells[column].Value = value;
                    if (column == 6)
                        r.Cells[6].Tag = null;
                }
            UpdateStatus();
            grid.Invalidate();
        }
    }
}