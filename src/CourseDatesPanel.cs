using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    /// <summary>
    /// 小月历：有课的日子标深色，点击日期增减课次。
    /// </summary>
    public class LessonCalendar : Control
    {
        const int Columns = 7, Rows = 7;
        public DateTime Month = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public Func<DateTime, bool> HasLesson = d => false;
        public Action<DateTime> DayClicked;

        /// <summary>初始化自绘样式。</summary>
        public LessonCalendar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        /// <summary>取得格子对应的日期，月外的格子返回空。</summary>
        DateTime? DayAt(int column, int row)
        {
            int offset = ((int)Month.DayOfWeek + 6) % 7, day = (row - 1) * Columns + column - offset + 1;
            return row < 1 || day < 1 || day > DateTime.DaysInMonth(Month.Year, Month.Month) ? (DateTime?)null : new DateTime(Month.Year, Month.Month, day);
        }

        /// <summary>绘制星期和日期。</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.Clear(Palette.Paper);
            float w = Width / (float)Columns, h = Height / (float)Rows;
            for (int c = 0; c < Columns; c++)
                TextRenderer.DrawText(g, L.ShortDay(c), Font, Rectangle.Round(new RectangleF(c * w, 0, w, h)), Palette.Muted);
            for (int r = 1; r < Rows; r++)
                for (int c = 0; c < Columns; c++)
                {
                    var day = DayAt(c, r);
                    if (day == null)
                        continue;
                    var cell = new RectangleF(c * w, r * h, w, h);
                    float side = Math.Min(w, h) - 4;
                    var mark = new RectangleF(cell.X + (w - side) / 2, cell.Y + (h - side) / 2, side, side);
                    bool lesson = HasLesson(day.Value);
                    if (lesson)
                        using (var fill = new SolidBrush(Skins.Accent))
                            g.FillEllipse(fill, mark);
                    else if (day.Value == DateTime.Today)
                        using (var pen = new Pen(Skins.Accent))
                            g.DrawEllipse(pen, mark);
                    TextRenderer.DrawText(g, day.Value.Day.ToString(), Font, Rectangle.Round(cell), lesson ? Skins.OnAccent : Palette.Ink);
                }
        }

        /// <summary>点击日期。</summary>
        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var day = DayAt((int)(e.X / (Width / (float)Columns)), (int)(e.Y / (Height / (float)Rows)));
            if (day != null && DayClicked != null)
                DayClicked(day.Value);
        }
    }

    public partial class CourseImportDialog
    {
        /// <summary>
        /// 日期面板：改首课日期、重复周数，或在月历上点选日子。
        /// </summary>
        internal void ShowDateMenu(int row, Point at)
        {
            var working = slots[row].Lessons.ToList();
            var panel = new Panel { Size = new Size(286, 330), BackColor = Palette.Paper };
            panel.Controls.Add(new Label { Text = L.T("首课"), Bounds = new Rectangle(6, 11, 40, 24) });
            var first = new TextBox { Bounds = new Rectangle(48, 8, 104, 26), Text = working[0].Start.ToString("yyyy-MM-dd") };
            panel.Controls.Add(first);
            panel.Controls.Add(new Label { Text = L.T("重复"), Bounds = new Rectangle(162, 11, 38, 24) });
            var weeks = new NumericUpDown { Bounds = new Rectangle(200, 8, 52, 26), Minimum = 1, Maximum = 52, Value = Math.Min(52, working.Count) };
            panel.Controls.Add(weeks);
            panel.Controls.Add(new Label { Text = L.T("周"), Bounds = new Rectangle(256, 11, 24, 24) });
            var title = new Label { Bounds = new Rectangle(44, 42, 198, 30), TextAlign = ContentAlignment.MiddleCenter };
            panel.Controls.Add(title);
            var calendar = new LessonCalendar { Bounds = new Rectangle(6, 74, 274, 250), Font = book.Font, Month = new DateTime(working[0].Start.Year, working[0].Start.Month, 1) };
            calendar.HasLesson = d => working.Any(e => e.Start.Date == d);
            panel.Controls.Add(calendar);
            bool updating = false;
            Action show = delegate
            {
                updating = true;
                title.Text = L.Date(calendar.Month, "yyyy 年 M 月", "MMMM yyyy");
                if (working.Count > 0)
                {
                    first.Text = working.Min(e => e.Start).ToString("yyyy-MM-dd");
                    weeks.Value = Math.Min(52, working.Count);
                }

                calendar.Invalidate();
                updating = false;
            };
            Action<int> turn = delegate (int months)
            {
                calendar.Month = calendar.Month.AddMonths(months);
                show();
            };
            var previous = new SoftButton { Text = "‹", Bounds = new Rectangle(6, 42, 34, 30), BackColor = Skins.Button, ForeColor = Palette.Ink };
            previous.Click += delegate { turn(-1); };
            var next = new SoftButton { Text = "›", Bounds = new Rectangle(246, 42, 34, 30), BackColor = Skins.Button, ForeColor = Palette.Ink };
            next.Click += delegate { turn(1); };
            panel.Controls.Add(previous);
            panel.Controls.Add(next);
            calendar.DayClicked = delegate (DateTime day)
            {
                Change(state => CourseBook.ToggleDate(state, working, day));
                if (working.Count == 0)
                    bookMenu.Close();
                else
                    show();
            };
            weeks.ValueChanged += delegate
            {
                if (updating)
                    return;
                Change(state => CourseBook.SetWeeks(state, working, (int)weeks.Value));
                show();
            };
            Action moveFirst = delegate
            {
                DateTime day;
                if (updating || !DateTime.TryParseExact(first.Text.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out day))
                {
                    show();
                    return;
                }

                if (day != working.Min(e => e.Start).Date)
                {
                    Change(state => CourseBook.MoveFirst(working, day));
                    calendar.Month = new DateTime(day.Year, day.Month, 1);
                }

                show();
            };
            first.Leave += delegate { moveFirst(); };
            first.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    moveFirst();
                }
            };
            show();
            ShowBookMenu(panel, at);
        }
    }
}

