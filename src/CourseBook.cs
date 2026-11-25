using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PaperDays
{
    /// <summary>
    /// 课程的一个固定时段：班级、星期、时间、教室和提醒都相同的一组课次。
    /// </summary>
    public sealed class CourseSlot
    {
        public string Title, Class, Room;
        public int Day, Reminder;
        public TimeSpan Start, End;
        public List<Entry> Lessons;
    }

    /// <summary>
    /// 已导入课程的查询和批量修改，不依赖界面。
    /// </summary>
    public static class CourseBook
    {
        public static readonly int[] Minutes = { -1, 0, 5, 15, 30, 60, 1440 };
        public static string[] MinuteNames { get { return new[] { L.T("不提醒"), L.T("准时"), L.T("提前 5 分钟"), L.T("提前 15 分钟"), L.T("提前 30 分钟"), L.T("提前 1 小时"), L.T("提前 1 天") }; } }
        public static string[] Days { get { return new[] { L.T("周一"), L.T("周二"), L.T("周三"), L.T("周四"), L.T("周五"), L.T("周六"), L.T("周日") }; } }
        static readonly Regex RoomLine = new Regex(@"教室：([^\r\n]*)");

        /// <summary>从备注里读取教室。</summary>
        public static string Room(Entry e)
        {
            var match = RoomLine.Match(e.Notes ?? "");
            return match.Success ? match.Groups[1].Value.Trim() : "";
        }

        /// <summary>把教室写回备注，其他备注内容不变。</summary>
        public static void SetRoom(Entry e, string room)
        {
            string notes = e.Notes ?? "", line = String.IsNullOrWhiteSpace(room) ? "" : "教室：" + room.Trim();
            if (RoomLine.IsMatch(notes))
                e.Notes = RoomLine.Replace(notes, line, 1).Trim();
            else if (line.Length > 0)
                e.Notes = (notes.Length > 0 ? notes + "\r\n" : "") + line;
        }

        /// <summary>
        /// 列出课程时段：按课程名拼音排序，同一门课内按星期和时间排序。
        /// </summary>
        public static List<CourseSlot> Slots(State state, string filter = "")
        {
            string word = (filter ?? "").Trim();
            var pinyin = StringComparer.Create(CultureInfo.GetCultureInfo("zh-CN"), false);
            return state.Events.Where(e => e.Id.StartsWith("course-") && (word.Length == 0 || e.Title.IndexOf(word, StringComparison.OrdinalIgnoreCase) >= 0))
                .GroupBy(e => new { e.Title, Class = e.ClassName ?? "", Day = ((int)e.Start.DayOfWeek + 6) % 7 + 1, Start = e.Start.TimeOfDay, End = e.End.TimeOfDay, Room = Room(e), e.Reminder })
                .Select(g => new CourseSlot { Title = g.Key.Title, Class = g.Key.Class, Day = g.Key.Day, Start = g.Key.Start, End = g.Key.End, Room = g.Key.Room, Reminder = g.Key.Reminder, Lessons = g.OrderBy(e => e.Start).ToList() })
                .OrderBy(s => s.Title, pinyin).ThenBy(s => s.Day).ThenBy(s => s.Start).ThenBy(s => s.Class).ToList();
        }

        /// <summary>
        /// 取得要修改的课次：默认只动今天及以后，已上过的课保持原样。
        /// </summary>
        public static List<Entry> Targets(IEnumerable<CourseSlot> slots, bool all, DateTime today)
        {
            return slots.SelectMany(s => s.Lessons).Where(e => all || e.Start.Date >= today.Date).ToList();
        }

        /// <summary>解析 24 小时制时间。</summary>
        public static TimeSpan Time(string text)
        {
            DateTime parsed;
            if (!DateTime.TryParseExact((text ?? "").Trim(), new[] { "H:mm", "HH:mm" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                throw new FormatException(L.T("时间请填写 24 小时制，例如 08:30"));
            return parsed.TimeOfDay;
        }

        /// <summary>修改开始时间；不能晚于结束时间。</summary>
        public static int SetStart(List<Entry> lessons, TimeSpan start)
        {
            if (lessons.Any(e => start >= e.End.TimeOfDay))
                throw new ArgumentException(L.T("开始时间要早于结束时间"));
            foreach (var e in lessons)
            {
                e.Start = e.Start.Date + start;
                e.Notified = false;
            }

            return lessons.Count;
        }

        /// <summary>修改结束时间；不能早于开始时间。</summary>
        public static int SetEnd(List<Entry> lessons, TimeSpan end)
        {
            if (lessons.Any(e => end <= e.Start.TimeOfDay))
                throw new ArgumentException(L.T("结束时间要晚于开始时间"));
            foreach (var e in lessons)
                e.End = e.End.Date + end;
            return lessons.Count;
        }

        /// <summary>把课次移到同一周的另一天。</summary>
        public static int SetDay(List<Entry> lessons, int day)
        {
            foreach (var e in lessons)
            {
                int shift = day - (((int)e.Start.DayOfWeek + 6) % 7 + 1);
                e.Start = e.Start.AddDays(shift);
                e.End = e.End.AddDays(shift);
                e.Notified = false;
            }

            return lessons.Count;
        }

        /// <summary>修改提前提醒的分钟数。</summary>
        public static int SetReminder(List<Entry> lessons, int minutes)
        {
            foreach (var e in lessons)
            {
                e.Reminder = minutes;
                e.Notified = false;
            }

            return lessons.Count;
        }

        /// <summary>修改课程名，这门课的全部课次一起改。</summary>
        public static int Rename(State state, string title, string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0)
                throw new ArgumentException(L.T("课程名不能为空"));
            var lessons = Lessons(state, title);
            foreach (var e in lessons)
                e.Title = name;
            return lessons.Count;
        }

        /// <summary>取得一门课的全部课次。</summary>
        public static List<Entry> Lessons(State state, string title)
        {
            return state.Events.Where(e => e.Id.StartsWith("course-") && e.Title == title).ToList();
        }

        /// <summary>删除课次，并清理不再使用的课程日历。</summary>
        public static int Delete(State state, List<Entry> lessons)
        {
            var ids = new HashSet<string>(lessons.Select(e => e.Id));
            int removed = state.Events.RemoveAll(e => ids.Contains(e.Id));
            state.Calendars.RemoveAll(c => (c.Id.StartsWith("course-type-") || c.Id.StartsWith("class-")) && !state.Events.Any(e => e.CalendarId == c.Id));
            return removed;
        }

        /// <summary>保存日程和日历的副本，用于撤销。直接复制对象，时间不经过序列化。</summary>
        public static State Snapshot(State state)
        {
            return new State { Events = state.Events.Select(e => e.Copy()).ToList(), Calendars = state.Calendars.ToList() };
        }

        /// <summary>恢复保存的日程和日历。</summary>
        public static void Restore(State state, State snapshot)
        {
            state.Events = snapshot.Events.Select(e => e.Copy()).ToList();
            state.Calendars = snapshot.Calendars.ToList();
        }

        /// <summary>按模板在指定日期加一节课。</summary>
        static Entry AddLesson(State state, List<Entry> lessons, Entry sample, DateTime day)
        {
            var lesson = sample.Copy();
            lesson.Id = "course-" + Guid.NewGuid().ToString("N");
            lesson.End = day.Date + (sample.End - sample.Start.Date);
            lesson.Start = day.Date + sample.Start.TimeOfDay;
            lesson.Notified = false;
            state.Events.Add(lesson);
            lessons.Add(lesson);
            return lesson;
        }

        /// <summary>点选日期：这天有课就去掉，没课就加一节。</summary>
        public static int ToggleDate(State state, List<Entry> lessons, DateTime day)
        {
            var found = lessons.FirstOrDefault(e => e.Start.Date == day.Date);
            if (found == null)
                AddLesson(state, lessons, lessons[0], day);
            else
            {
                lessons.Remove(found);
                Delete(state, new List<Entry> { found });
            }

            return 1;
        }

        /// <summary>修改重复周数：多了往后每周加一节，少了去掉最后几节。</summary>
        public static int SetWeeks(State state, List<Entry> lessons, int count)
        {
            lessons.Sort((a, b) => a.Start.CompareTo(b.Start));
            int changed = Math.Abs(count - lessons.Count);
            while (lessons.Count < count)
                AddLesson(state, lessons, lessons[lessons.Count - 1], lessons[lessons.Count - 1].Start.Date.AddDays(7));
            if (lessons.Count > count)
            {
                var removed = lessons.Skip(count).ToList();
                lessons.RemoveRange(count, lessons.Count - count);
                Delete(state, removed);
            }

            return changed;
        }

        /// <summary>修改首课日期：所有课次一起平移。</summary>
        public static int MoveFirst(List<Entry> lessons, DateTime day)
        {
            int days = (day.Date - lessons.Min(e => e.Start).Date).Days;
            foreach (var e in lessons)
            {
                e.Start = e.Start.AddDays(days);
                e.End = e.End.AddDays(days);
                e.Notified = false;
            }

            return lessons.Count;
        }
    }

    /// <summary>
    /// “已导入课程”页：同名课程合并显示，点击单元格原位修改。
    /// </summary>
    public partial class CourseImportDialog
    {
        const int MarkColumn = 0, NameColumn = 1, RowColumn = 2, ClassColumn = 3, DayColumn = 4, StartColumn = 5, EndColumn = 6, RoomColumn = 7, RemindColumn = 8, DateColumn = 9;
        readonly HashSet<string> marked = new HashSet<string>();
        readonly Stack<State> undo = new Stack<State>();
        Dictionary<string, Entry> baseline = new Dictionary<string, Entry>();
        State draft;
        Button applyButton;
        DataGridView book;
        List<CourseSlot> slots = new List<CourseSlot>();
        ComboBox scope;
        TextBox search, renamer;
        Label bookStatus;
        Button undoButton;
        ToolStripDropDown bookMenu;
        Action pendingEdit;
        bool loading;

        internal DataGridView BookGrid
        {
            /// <summary>读取课程表格。</summary>
            get
            {
                return book;
            }
        }

        internal List<CourseSlot> BookSlots
        {
            /// <summary>读取当前显示的时段。</summary>
            get
            {
                return slots;
            }
        }

        internal string BookMessage
        {
            /// <summary>读取页面提示。</summary>
            get
            {
                return bookStatus.Text;
            }
        }

        internal bool EditAll
        {
            /// <summary>读取修改范围。</summary>
            get
            {
                return scope.SelectedIndex == 1;
            }

            /// <summary>设置修改范围。</summary>
            set
            {
                scope.SelectedIndex = value ? 1 : 0;
            }
        }

        /// <summary>
        /// 搭建页面控件。
        /// </summary>
        void BuildBook(TabPage page)
        {
            var bar = CourseUI.Bar(page, DockStyle.Top);
            bar.Controls.Add(new Label { Text = L.T("搜索"), AutoSize = true, Margin = new Padding(6, 12, 4, 4) });
            search = new TextBox { Width = 170, Margin = new Padding(4, 9, 14, 4) };
            search.TextChanged += delegate
            {
                RefreshBook();
            };
            bar.Controls.Add(search);
            bar.Controls.Add(new Label { Text = L.T("修改范围"), AutoSize = true, Margin = new Padding(4, 12, 4, 4) });
            scope = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 130, Margin = new Padding(4, 8, 14, 4) };
            scope.Items.AddRange(new object[] { L.T("今天及以后"), L.T("全部课次") });
            scope.SelectedIndex = 0;
            bar.Controls.Add(scope);
            CourseUI.Button(bar, L.T("删除选中"), 110, DeleteMarked);
            undoButton = CourseUI.Button(bar, L.T("撤销"), 80, UndoBook);
            undoButton.Enabled = applyButton.Enabled = false;
            CourseUI.Note(page, "点单元格直接修改，点图标或圆点换样式，点日期改周数或选日子。勾选多行后改一格会一起改。改完点“应用”才保存。", 48);
            bookStatus = new Label { Dock = DockStyle.Bottom, Height = 30, Padding = new Padding(10, 6, 10, 0), ForeColor = Palette.Muted };
            page.Controls.Add(bookStatus);
            book = CourseUI.Table();
            book.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            book.AllowUserToDeleteRows = false;
            book.AllowUserToResizeRows = false;
            book.SelectionMode = DataGridViewSelectionMode.CellSelect;
            book.MultiSelect = false;
            book.EditMode = DataGridViewEditMode.EditProgrammatically;
            book.DefaultCellStyle.SelectionBackColor = Palette.Paper;
            book.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
            var day = new DataGridViewComboBoxColumn { HeaderText = L.T("星期"), FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing };
            day.Items.AddRange(CourseBook.Days.Cast<object>().ToArray());
            var remind = new DataGridViewComboBoxColumn { HeaderText = L.T("提醒"), FlatStyle = FlatStyle.Flat, DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing };
            remind.Items.AddRange(CourseBook.MinuteNames.Cast<object>().ToArray());
            book.Columns.Add("mark", "");
            book.Columns.Add("course", L.T("课程"));
            book.Columns.Add("row", "");
            book.Columns.Add("class", L.T("班级"));
            book.Columns.Add(day);
            book.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.T("开始"), CellTemplate = new TimeCell() });
            book.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L.T("结束"), CellTemplate = new TimeCell() });
            book.Columns.Add("room", L.T("教室"));
            book.Columns.Add(remind);
            book.Columns.Add("range", L.T("日期"));
            book.Columns.Add("count", L.T("节数"));
            book.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            foreach (DataGridViewColumn column in book.Columns)
                column.SortMode = DataGridViewColumnSortMode.NotSortable;
            book.SizeChanged += delegate
            {
                FitBookColumns();
            };
            book.Columns[DateColumn].ReadOnly = book.Columns[10].ReadOnly = true;
            book.CellFormatting += FormatBook;
            FormClosing += ConfirmBookClose;
            var filter = new EditCommitFilter(this);
            Application.AddMessageFilter(filter);
            FormClosed += delegate
            {
                Application.RemoveMessageFilter(filter);
            };
            book.CellPainting += PaintBook;
            book.CellMouseClick += ClickBook;
            book.CellEndEdit += EndBookEdit;
            book.DataError += delegate (object sender, DataGridViewDataErrorEventArgs e)
            {
                e.ThrowException = false;
            };
            book.FontChanged += delegate
            {
                FitBookRows();
                FitBookColumns();
            };
            page.Controls.Add(book);
            book.BringToFront();
        }

        /// <summary>
        /// 重新读取课程并填表，保留勾选和滚动位置。
        /// </summary>
        internal void RefreshBook()
        {
            if (book == null || loading)
                return;
            loading = true;
            try
            {
                int top = book.FirstDisplayedScrollingRowIndex;
                book.Rows.Clear();
                if (draft == null || undo.Count == 0)
                    LoadDraft();
                slots = CourseBook.Slots(draft, search.Text);
                marked.IntersectWith(draft.Events.Select(e => e.Id));
                foreach (var s in slots)
                {
                    string range = s.Lessons.First().Start.ToString("M/d") + "–" + s.Lessons.Last().Start.ToString("M/d");
                    int reminder = Array.IndexOf(CourseBook.Minutes, s.Reminder);
                    book.Rows.Add("", "", "", s.Class, CourseBook.Days[s.Day - 1], s.Start.ToString(@"hh\:mm"), s.End.ToString(@"hh\:mm"), s.Room, CourseBook.MinuteNames[reminder < 0 ? 0 : reminder], range, s.Lessons.Count);
                }

                FitBookRows();
                ShowMarks();
                if (top >= 0 && top < book.Rows.Count)
                    book.FirstDisplayedScrollingRowIndex = top;
            }
            finally
            {
                loading = false;
            }
        }

        /// <summary>
        /// 从日历复制一份草稿，修改都先落在草稿上。
        /// </summary>
        void LoadDraft()
        {
            var state = latest();
            draft = new State { Events = state.Events.Where(e => e.Id.StartsWith("course-")).Select(e => e.Copy()).ToList(), Calendars = state.Calendars.ToList() };
            baseline = draft.Events.ToDictionary(e => e.Id, e => e.Copy());
            undo.Clear();
        }

        /// <summary>
        /// 把草稿写回日历。
        /// </summary>
        internal void ApplyBook()
        {
            CommitEdits();
            if (undo.Count == 0)
                return;
            var lessons = draft.Events.Select(e => e.Copy()).ToList();
            Action write = delegate
            {
                var state = latest();
                state.Events.RemoveAll(e => e.Id.StartsWith("course-"));
                state.Events.AddRange(lessons);
                state.Calendars.RemoveAll(c => (c.Id.StartsWith("course-type-") || c.Id.StartsWith("class-")) && !state.Events.Any(e => e.CalendarId == c.Id));
            };
            if (apply == null)
                write();
            else if (!apply(write))
                return;
            undo.Clear();
            // 应用后清空勾选，避免下一次删除时连带之前选中的课程。
            marked.Clear();
            SyncBookButtons();
            RefreshBook();
            bookStatus.Text = L.T("已应用");
        }

        /// <summary>同步撤销和应用按钮。</summary>
        void SyncBookButtons()
        {
            undoButton.Enabled = applyButton.Enabled = undo.Count > 0;
        }

        /// <summary>
        /// 有未应用的修改时，询问应用、放弃或继续编辑。
        /// </summary>
        void ConfirmBookClose(object sender, FormClosingEventArgs e)
        {
            CommitEdits();
            if (undo.Count == 0 || DialogResult == DialogResult.OK)
                return;
            using (var ask = new NoticeDialog("有 " + undo.Count + L.T(" 处修改未应用。"), L.T("课表管理"), MessageBoxButtons.YesNoCancel))
            {
                var answer = ask.ShowDialog(this);
                if (answer == DialogResult.Yes)
                    ApplyBook();
                else if (answer != DialogResult.No)
                    e.Cancel = true;
            }
        }

        /// <summary>判断格子的内容是否和日历里的不同。</summary>
        bool Changed(CourseSlot slot, int column)
        {
            foreach (var e in slot.Lessons)
            {
                Entry old;
                if (!baseline.TryGetValue(e.Id, out old))
                {
                    if (column == DateColumn)
                        return true;
                    continue;
                }

                bool differs = column == ClassColumn ? old.ClassName != e.ClassName : column == DayColumn ? old.Start.DayOfWeek != e.Start.DayOfWeek : column == StartColumn ? old.Start.TimeOfDay != e.Start.TimeOfDay : column == EndColumn ? old.End.TimeOfDay != e.End.TimeOfDay : column == RoomColumn ? CourseBook.Room(old) != CourseBook.Room(e) : column == RemindColumn ? old.Reminder != e.Reminder : column == DateColumn ? old.Start.Date != e.Start.Date : column == NameColumn && (old.Title != e.Title || old.Color != e.Color || old.Emoji != e.Emoji || old.UseEmoji != e.UseEmoji || old.EmojiTint != e.EmojiTint || old.EmojiOriginal != e.EmojiOriginal || old.Highlight != e.Highlight);
                if (differs)
                    return true;
            }

            return false;
        }

        /// <summary>改过的格子用淡色标出。</summary>
        void FormatBook(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < slots.Count && Changed(slots[e.RowIndex], e.ColumnIndex))
                e.CellStyle.BackColor = e.CellStyle.SelectionBackColor = ColorTranslator.FromHtml(Skins.HighlightAt(2));
        }

        /// <summary>
        /// 取得课程所占的行范围。
        /// </summary>
        void GroupOf(int row, out int first, out int last)
        {
            first = last = row;
            while (first > 0 && slots[first - 1].Title == slots[row].Title)
                first--;
            while (last + 1 < slots.Count && slots[last + 1].Title == slots[row].Title)
                last++;
        }

        /// <summary>
        /// 按比例分配列宽，保证“提前 15 分钟”和日期范围显示完整。
        /// </summary>
        void FitBookColumns()
        {
            float[] weights = { 28, 100, 28, 50, 46, 78, 78, 60, 98, 100, 42 };
            int width = book.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2;
            if (width < 200)
                return;
            float total = weights.Sum();
            for (int i = 0; i < weights.Length; i++)
                book.Columns[i].Width = Math.Max(26, (int)(width * weights[i] / total));
        }

        /// <summary>
        /// 设置行高：合并后的课程格要放下课程名和图标两行。
        /// </summary>
        void FitBookRows()
        {
            int font = book.Font.Height, row = (int)(font * 2.1f), block = (int)(font * 3.6f);
            for (int i = 0; i < book.Rows.Count && i < slots.Count; i++)
            {
                int first, last;
                GroupOf(i, out first, out last);
                book.Rows[i].Height = Math.Max(row, (block + last - first) / (last - first + 1));
            }
        }

        /// <summary>
        /// 显示勾选状态。
        /// </summary>
        void ShowMarks()
        {
            Color on = Color.FromArgb((Skins.Button.R + Palette.Paper.R) / 2, (Skins.Button.G + Palette.Paper.G) / 2, (Skins.Button.B + Palette.Paper.B) / 2);
            for (int i = 0; i < book.Rows.Count && i < slots.Count; i++)
            {
                bool mark = IsMarked(slots[i]);
                book.Rows[i].DefaultCellStyle.BackColor = mark ? on : Palette.Paper;
                book.Rows[i].DefaultCellStyle.SelectionBackColor = mark ? on : Palette.Paper;
            }

            int rows = slots.Count(IsMarked);
            bookStatus.ForeColor = Palette.Muted;
            bookStatus.Text = (rows == 0 ? slots.Select(s => s.Title).Distinct().Count() + L.T(" 门课程 · ") + slots.Sum(s => s.Lessons.Count) + " 节" : L.T("已选 ") + rows + " 行 · 共 " + slots.Where(IsMarked).Sum(s => s.Lessons.Count) + " 节") + Pending;
            book.Invalidate();
        }

        /// <summary>
        /// 判断一行是否被勾选。勾选按课次编号记录，修改时间或提醒后仍然保持。
        /// </summary>
        bool IsMarked(CourseSlot slot)
        {
            return slot.Lessons.All(e => marked.Contains(e.Id));
        }

        /// <summary>设置一行的勾选。</summary>
        void SetMark(CourseSlot slot, bool on)
        {
            foreach (var e in slot.Lessons)
                if (on)
                    marked.Add(e.Id);
                else
                    marked.Remove(e.Id);
        }

        /// <summary>未应用修改的提示。</summary>
        string Pending
        {
            /// <summary>读取提示文字。</summary>
            get
            {
                return undo.Count > 0 ? " · 有 " + undo.Count + L.T(" 处修改未应用") : "";
            }
        }

        /// <summary>切换一行的勾选。</summary>
        internal void MarkRow(int row)
        {
            SetMark(slots[row], !IsMarked(slots[row]));
            ShowMarks();
        }

        /// <summary>勾选或取消整门课。</summary>
        internal void MarkCourse(int row)
        {
            int first, last;
            GroupOf(row, out first, out last);
            bool all = Enumerable.Range(first, last - first + 1).All(i => IsMarked(slots[i]));
            for (int i = first; i <= last; i++)
                SetMark(slots[i], !all);
            ShowMarks();
        }

        /// <summary>
        /// 课程格的布局：课程名在上，图标和圆点在下。
        /// </summary>
        void CourseLayout(Rectangle group, out Rectangle name, out Rectangle icon, out Rectangle dot)
        {
            int font = book.Font.Height, line = (int)(font * 1.4f), side = (int)(font * 1.35f), dotSide = (int)(font * .72f), gap = (int)(font * .7f);
            int top = group.Y + (group.Height - line - side) / 2, center = group.X + group.Width / 2;
            name = new Rectangle(group.X + 3, top, group.Width - 6, line);
            icon = new Rectangle(center - (side + gap + dotSide) / 2, top + line, side, side);
            dot = new Rectangle(icon.Right + gap, icon.Y + (side - dotSide) / 2, dotSide, dotSide);
        }

        /// <summary>
        /// 取得整门课合并后的矩形，cell 是当前行的格子。
        /// </summary>
        Rectangle GroupBounds(Rectangle cell, int row)
        {
            int first, last, above = 0, height = 0;
            GroupOf(row, out first, out last);
            for (int i = first; i <= last; i++)
            {
                if (i < row)
                    above += book.Rows[i].Height;
                height += book.Rows[i].Height;
            }

            return new Rectangle(cell.X, cell.Y - above, cell.Width, height);
        }

        /// <summary>
        /// 绘制合并的勾选格和课程格：每行只画属于自己的那一段，滚动时也完整。
        /// </summary>
        void PaintBook(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= slots.Count || e.ColumnIndex > RowColumn)
                return;
            if (e.ColumnIndex == RowColumn)
            {
                e.PaintBackground(e.ClipBounds, true);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                PaintCourseMark(e.Graphics, e.CellBounds, IsMarked(slots[e.RowIndex]));
                e.Graphics.SmoothingMode = SmoothingMode.None;
                e.Handled = true;
                return;
            }

            int first, last;
            GroupOf(e.RowIndex, out first, out last);
            bool whole = Enumerable.Range(first, last - first + 1).All(i => IsMarked(slots[i]));
            var g = e.Graphics;
            var saved = g.Save();
            g.SetClip(e.CellBounds);
            g.SmoothingMode = SmoothingMode.None;
            bool edited = e.ColumnIndex == NameColumn && Enumerable.Range(first, last - first + 1).Any(i => Changed(slots[i], NameColumn));
            using (var back = new SolidBrush(edited ? ColorTranslator.FromHtml(Skins.HighlightAt(2)) : whole ? book.Rows[e.RowIndex].DefaultCellStyle.BackColor : Palette.Paper))
                g.FillRectangle(back, e.CellBounds);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var group = GroupBounds(e.CellBounds, e.RowIndex);
            if (e.ColumnIndex == MarkColumn)
                PaintCourseMark(g, group, whole);
            else
                PaintCourseName(g, group, e.RowIndex);
            g.Restore(saved);
            g.SmoothingMode = SmoothingMode.None;
            using (var line = new Pen(Skins.Line))
            {
                g.DrawLine(line, e.CellBounds.Right - 1, e.CellBounds.Top, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
                if (e.RowIndex == last)
                    g.DrawLine(line, e.CellBounds.Left, e.CellBounds.Bottom - 1, e.CellBounds.Right - 1, e.CellBounds.Bottom - 1);
            }

            e.Handled = true;
        }

        /// <summary>
        /// 绘制整门课的勾选框。
        /// </summary>
        void PaintCourseMark(Graphics g, Rectangle group, bool on)
        {
            int side = (int)(book.Font.Height * .85f);
            var box = new RectangleF(group.X + (group.Width - side) / 2f, group.Y + (group.Height - side) / 2f, side, side);
            using (var path = Chrome.Round(box, 4))
            {
                if (on)
                    using (var fill = new SolidBrush(Skins.Accent))
                        g.FillPath(fill, path);
                else
                    using (var pen = new Pen(Palette.Muted, 1.2f))
                        g.DrawPath(pen, path);
            }

            if (on)
                using (var pen = new Pen(Palette.Paper, 2))
                    g.DrawLines(pen, new[] { new PointF(box.X + side * .22f, box.Y + side * .52f), new PointF(box.X + side * .43f, box.Y + side * .73f), new PointF(box.X + side * .78f, box.Y + side * .28f) });
        }

        /// <summary>
        /// 绘制课程名、图标和颜色圆点。
        /// </summary>
        void PaintCourseName(Graphics g, Rectangle group, int row)
        {
            var slot = slots[row];
            var sample = slot.Lessons[0];
            Rectangle name, icon, dot;
            CourseLayout(group, out name, out icon, out dot);
            TextRenderer.DrawText(g, slot.Title, book.Font, name, Palette.Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
            if (sample.UseEmoji)
                EmojiArt.Draw(g, sample.Emoji, icon, sample.EmojiOriginal, Skins.EmojiInk(sample));
            else
                TextRenderer.DrawText(g, L.T("无"), book.Font, icon, Palette.Muted, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            using (var brush = new SolidBrush(ColorTranslator.FromHtml(sample.Color)))
                g.FillEllipse(brush, dot);
        }

        /// <summary>
        /// 处理表格点击：课程格按位置区分，其他格子直接进入编辑。
        /// </summary>
        void ClickBook(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= slots.Count)
                return;
            if (e.ColumnIndex == MarkColumn)
            {
                MarkCourse(e.RowIndex);
                return;
            }

            if (e.ColumnIndex == RowColumn)
            {
                MarkRow(e.RowIndex);
                return;
            }

            if (e.ColumnIndex == DateColumn)
            {
                var rect = book.GetCellDisplayRectangle(DateColumn, e.RowIndex, false);
                ShowDateMenu(e.RowIndex, book.PointToScreen(new Point(rect.Left, rect.Bottom)));
                return;
            }

            if (e.ColumnIndex == NameColumn)
            {
                var cell = book.GetCellDisplayRectangle(NameColumn, e.RowIndex, false);
                ClickCourse(e.RowIndex, GroupBounds(cell, e.RowIndex), new Point(cell.X + e.X, cell.Y + e.Y));
                return;
            }

            if (book.Columns[e.ColumnIndex].ReadOnly)
                return;
            book.CurrentCell = book.Rows[e.RowIndex].Cells[e.ColumnIndex];
            book.BeginEdit(true);
            var combo = book.EditingControl as ComboBox;
            if (combo != null)
                BeginInvoke((Action)delegate
                {
                    if (!combo.IsDisposed && combo.Visible)
                        combo.DroppedDown = true;
                });
        }

        /// <summary>
        /// 区分点在课程名、图标还是圆点上。
        /// </summary>
        void ClickCourse(int row, Rectangle group, Point point)
        {
            Rectangle name, icon, dot;
            CourseLayout(group, out name, out icon, out dot);
            dot.Inflate(5, 5);
            if (dot.Contains(point))
                ShowColorMenu(slots[row].Title, book.PointToScreen(new Point(dot.X, dot.Bottom)));
            else if (icon.Contains(point))
                ShowIconMenu(slots[row].Title, book.PointToScreen(new Point(icon.X, icon.Bottom)));
            else
                BeginRename(row, name);
        }

        /// <summary>
        /// 在课程名的位置直接改名。
        /// </summary>
        void BeginRename(int row, Rectangle bounds)
        {
            EndRename(false);
            string title = slots[row].Title;
            renamer = new TextBox { Text = title, Font = book.Font, Bounds = bounds, BorderStyle = BorderStyle.FixedSingle, TextAlign = HorizontalAlignment.Center, BackColor = Palette.Paper, ForeColor = Palette.Ink, Tag = title };
            renamer.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Escape)
                {
                    e.SuppressKeyPress = true;
                    EndRename(e.KeyCode == Keys.Enter);
                }
            };
            renamer.Leave += delegate
            {
                EndRename(true);
            };
            book.Controls.Add(renamer);
            renamer.Focus();
            renamer.SelectAll();
        }

        /// <summary>
        /// 结束改名。
        /// </summary>
        void EndRename(bool save)
        {
            var box = renamer;
            if (box == null)
                return;
            renamer = null;
            string title = (string)box.Tag, name = box.Text.Trim();
            book.Controls.Remove(box);
            box.Dispose();
            if (save && name != title)
                RenameCourse(title, name);
        }

        /// <summary>修改课程名。</summary>
        internal void RenameCourse(string title, string name)
        {
            Change(state => CourseBook.Rename(state, title, name));
        }

        /// <summary>
        /// 执行一次修改：先存档用于撤销，再写入数据并刷新表格。
        /// </summary>
        void Change(Func<State, int> edit)
        {
            var before = CourseBook.Snapshot(draft);
            try
            {
                int changed = edit(draft);
                if (changed > 0)
                    undo.Push(before);
                SyncBookButtons();
                RefreshBook();
                bookStatus.ForeColor = Palette.Muted;
                bookStatus.Text = changed > 0 ? L.T("已修改 ") + changed + L.T(" 节课，点“应用”后生效") + Pending : "没有可修改的课次：这些课都已上过，可以把修改范围改成全部课次。";
            }
            catch (Exception ex)
            {
                if (!(ex is ArgumentException || ex is FormatException))
                    throw;
                CourseBook.Restore(draft, before);
                RefreshBook();
                bookStatus.ForeColor = FortuneColors.BadInk;
                bookStatus.Text = ex.Message;
            }
        }

        /// <summary>
        /// 单元格编辑结束后写入数据；表格不能在这个事件里重建，所以排到下一轮。
        /// </summary>
        void EndBookEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (loading || e.RowIndex < 0 || e.RowIndex >= slots.Count)
                return;
            int row = e.RowIndex, column = e.ColumnIndex;
            string value = Convert.ToString(book.Rows[row].Cells[column].Value);
            pendingEdit = delegate
            {
                if (!IsDisposed && row < slots.Count)
                    EditBook(row, column, value);
            };
            if (!IsDisposed && IsHandleCreated)
                BeginInvoke((Action)RunPendingEdit);
        }

        /// <summary>执行还没写入的单元格修改。</summary>
        void RunPendingEdit()
        {
            var edit = pendingEdit;
            pendingEdit = null;
            if (edit != null)
                edit();
        }

        /// <summary>
        /// 结束正在编辑的单元格并写入。点空白处时表格不会自己提交，由窗口统一调用。
        /// </summary>
        internal void CommitEdits(Control clicked = null)
        {
            foreach (var table in new[] { grid, book })
            {
                var editor = table == null ? null : table.EditingControl;
                if (editor != null && !(clicked != null && (clicked == editor || editor.Contains(clicked))))
                    table.EndEdit();
            }

            RunPendingEdit();
        }

        /// <summary>
        /// 监听窗口内的鼠标按下，先提交编辑，再让点击继续生效。
        /// </summary>
        sealed class EditCommitFilter : IMessageFilter
        {
            readonly CourseImportDialog dialog;

            /// <summary>绑定窗口。</summary>
            public EditCommitFilter(CourseImportDialog dialog)
            {
                this.dialog = dialog;
            }

            /// <summary>只处理本窗口内的左键按下，下拉列表等独立窗口不受影响。</summary>
            public bool PreFilterMessage(ref Message m)
            {
                if (m.Msg == 0x201)
                {
                    var control = Control.FromHandle(m.HWnd);
                    if (control != null && control.FindForm() == dialog)
                        dialog.CommitEdits(control);
                }

                return false;
            }
        }

        /// <summary>
        /// 修改一格：这一行被勾选时，同样的修改应用到所有勾选的行。
        /// </summary>
        internal void EditBook(int row, int column, string value)
        {
            var slot = slots[row];
            if (Same(slot, column, value))
                return;
            var chosen = IsMarked(slot) ? slots.Where(IsMarked).ToList() : new List<CourseSlot> { slot };
            var lessons = CourseBook.Targets(chosen, EditAll, DateTime.Today);
            Change(delegate
            {
                switch (column)
                {
                    case ClassColumn:
                        lessons.ForEach(e => e.ClassName = value.Trim());
                        return lessons.Count;
                    case DayColumn:
                        return CourseBook.SetDay(lessons, Array.IndexOf(CourseBook.Days, value) + 1);
                    case StartColumn:
                        return CourseBook.SetStart(lessons, CourseBook.Time(value));
                    case EndColumn:
                        return CourseBook.SetEnd(lessons, CourseBook.Time(value));
                    case RoomColumn:
                        lessons.ForEach(e => CourseBook.SetRoom(e, value));
                        return lessons.Count;
                    case RemindColumn:
                        return CourseBook.SetReminder(lessons, CourseBook.Minutes[Math.Max(0, Array.IndexOf(CourseBook.MinuteNames, value))]);
                }

                return 0;
            });
        }

        /// <summary>判断单元格的值是否没有变化。</summary>
        static bool Same(CourseSlot slot, int column, string value)
        {
            switch (column)
            {
                case ClassColumn:
                    return slot.Class == value.Trim();
                case DayColumn:
                    return CourseBook.Days[slot.Day - 1] == value;
                case StartColumn:
                    return slot.Start.ToString(@"hh\:mm") == value;
                case EndColumn:
                    return slot.End.ToString(@"hh\:mm") == value;
                case RoomColumn:
                    return slot.Room == value.Trim();
                case RemindColumn:
                    return Array.IndexOf(CourseBook.MinuteNames, value) == Array.IndexOf(CourseBook.Minutes, slot.Reminder);
            }

            return true;
        }

        /// <summary>
        /// 删除勾选的行。
        /// </summary>
        internal void DeleteMarked()
        {
            var chosen = slots.Where(IsMarked).ToList();
            if (chosen.Count == 0)
            {
                bookStatus.ForeColor = FortuneColors.BadInk;
                bookStatus.Text = L.T("先勾选要删除的课程或行。");
                return;
            }

            var lessons = CourseBook.Targets(chosen, EditAll, DateTime.Today);
            Change(state => CourseBook.Delete(state, lessons));
        }

        /// <summary>
        /// 撤销上一次未应用的修改。
        /// </summary>
        internal void UndoBook()
        {
            if (undo.Count == 0)
                return;
            CourseBook.Restore(draft, undo.Pop());
            SyncBookButtons();
            RefreshBook();
            bookStatus.Text = L.T("已撤销") + Pending;
        }

        /// <summary>
        /// 把样式改到这门课的全部课次。
        /// </summary>
        internal void StyleCourse(string title, Action<Entry> edit)
        {
            Change(delegate (State state)
            {
                var lessons = CourseBook.Lessons(state, title);
                lessons.ForEach(edit);
                return lessons.Count;
            });
        }

        /// <summary>
        /// 在点击位置打开小面板。
        /// </summary>
        void ShowBookMenu(Panel panel, Point at)
        {
            if (bookMenu != null && !bookMenu.IsDisposed)
                bookMenu.Close();
            var menu = new ToolStripDropDown { Padding = new Padding(8), BackColor = Palette.Paper };
            bookMenu = menu;
            menu.Items.Add(new ToolStripControlHost(panel) { AutoSize = false, Size = panel.Size, Margin = Padding.Empty });
            menu.Closed += delegate
            {
                // 面板不能在自己的关闭回调里释放；窗口已关闭时没有消息队列可排，直接释放。
                if (!IsDisposed && IsHandleCreated)
                    BeginInvoke((Action)menu.Dispose);
                else
                    menu.Dispose();
            };
            FormClosed += delegate
            {
                menu.Dispose();
            };
            menu.Show(at);
        }

        /// <summary>
        /// 图标面板：黑白、标记色、原彩，加 27 个课程图标。
        /// </summary>
        void ShowIconMenu(string title, Point at)
        {
            var panel = new FlowLayoutPanel { Size = new Size(306, 228), BackColor = Palette.Paper, Padding = new Padding(4) };
            string[] tones = { L.T("黑白"), L.T("标记色"), L.T("原彩") };
            for (int i = 0; i < 3; i++)
            {
                int tone = i;
                var b = new SoftButton { Text = tones[i], Size = new Size(88, 32), BackColor = Skins.Button, ForeColor = Palette.Ink };
                b.Click += delegate
                {
                    bookMenu.Close();
                    StyleCourse(title, e =>
                    {
                        e.UseEmoji = true;
                        e.EmojiTint = tone == 1;
                        e.EmojiOriginal = tone == 2;
                    });
                };
                panel.Controls.Add(b);
            }

            var none = new SoftButton { Text = L.T("无"), Size = new Size(36, 36), BackColor = Palette.Cream, ForeColor = Palette.Ink };
            none.Click += delegate
            {
                bookMenu.Close();
                StyleCourse(title, e => e.UseEmoji = false);
            };
            panel.Controls.Add(none);
            foreach (string symbol in CourseIcons.Emoji)
            {
                string chosen = symbol;
                var b = new EmojiButton { Emoji = symbol, Original = false, Size = new Size(36, 36) };
                b.Click += delegate
                {
                    bookMenu.Close();
                    StyleCourse(title, e =>
                    {
                        e.UseEmoji = true;
                        e.Emoji = chosen;
                    });
                };
                panel.Controls.Add(b);
            }

            ShowBookMenu(panel, at);
        }

        /// <summary>
        /// 颜色面板：标记色和笔刷高亮。
        /// </summary>
        void ShowColorMenu(string title, Point at)
        {
            var panel = new FlowLayoutPanel { Size = new Size(306, 164), BackColor = Palette.Paper, Padding = new Padding(4) };
            for (int i = 0; i < 8; i++)
            {
                int slot = i;
                var dot = new ColorDot { Size = new Size(30, 30), DotColor = ColorTranslator.FromHtml(Palette.Hex[i]), AccessibleName = L.T("课程颜色") };
                dot.Click += delegate
                {
                    bookMenu.Close();
                    ColorCourse(title, Palette.Hex[slot], slot);
                };
                panel.Controls.Add(dot);
            }

            var own = new SoftButton { Text = L.T("自选颜色"), Size = new Size(96, 30), BackColor = Skins.Button, ForeColor = Palette.Ink };
            own.Click += delegate
            {
                bookMenu.Close();
                using (var picker = new HexColorDialog { Color = ColorTranslator.FromHtml(CourseBook.Lessons(draft, title)[0].Color) })
                    if (picker.ShowDialog(this) == DialogResult.OK)
                        ColorCourse(title, ColorTranslator.ToHtml(picker.Color), -1);
            };
            panel.Controls.Add(own);
            var off = new SoftButton { Text = L.T("关闭高亮"), Size = new Size(96, 30), BackColor = Palette.Cream, ForeColor = Palette.Ink };
            off.Click += delegate
            {
                bookMenu.Close();
                StyleCourse(title, e => e.Highlight = false);
            };
            panel.Controls.Add(off);
            panel.SetFlowBreak(off, true);
            for (int i = 0; i < 8; i++)
            {
                int slot = i;
                var dot = new ColorDot { Size = new Size(30, 30), DotColor = ColorTranslator.FromHtml(Skins.HighlightAt(i)), AccessibleName = L.T("高亮颜色") };
                dot.Click += delegate
                {
                    bookMenu.Close();
                    StyleCourse(title, e =>
                    {
                        e.Highlight = true;
                        e.HighlightSlot = slot;
                        e.HighlightColor = Skins.HighlightAt(slot);
                    });
                };
                panel.Controls.Add(dot);
            }

            ShowBookMenu(panel, at);
        }

        /// <summary>
        /// 换标记色；和日程一样，选了颜色后图标跟随这个颜色。
        /// </summary>
        internal void ColorCourse(string title, string color, int slot)
        {
            StyleCourse(title, e =>
            {
                e.Color = color;
                e.ColorSlot = slot;
                if (e.UseEmoji)
                {
                    e.EmojiTint = true;
                    e.EmojiOriginal = false;
                }
            });
        }
    }
}
