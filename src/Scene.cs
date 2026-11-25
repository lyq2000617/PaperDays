using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;

namespace PaperDays
{
    public class Hit
    {
        public RectangleF Box;
        public string Action, Id;
        public DateTime Date;
    }

    public partial class Scene
    {
        public Bitmap GlassBackdrop;
        public string HoverAction;
        public string[] FortuneNumbers = new[]{"","",""};
        public State State;
        public DateTime Month = DateTime.Today, Selected = DateTime.Today, PlanAnchor = DateTime.Today;
        public string PlanMode = "day", Status = "";
        public int ReservedLeft;
        public DateTime Today = DateTime.Today;
        public ResponsiveLayout Layout;
        public List<CalendarMark> Holidays = new List<CalendarMark>();
        public List<Hit> Hits = new List<Hit>();
        static Color Ink
        {
            /// <summary>
            /// 读取Ink属性，供日历视图使用。
            /// </summary>
            get
            {
                return Palette.Ink;
            }
        }

        static Color Muted
        {
            /// <summary>
            /// 读取Muted属性，供日历视图使用。
            /// </summary>
            get
            {
                return Palette.Muted;
            }
        }

        static Color Line
        {
            /// <summary>
            /// 读取Line属性，供日历视图使用。
            /// </summary>
            get
            {
                return Skins.Glass ? Color.FromArgb(25, Skins.Dark ? Color.White : Color.Black) : Skins.Line;
            }
        }

        public bool IsWeek
        {
            /// <summary>
            /// 读取IsWeek属性，供日历视图使用。
            /// </summary>
            get
            {
                return State.CalendarView == "week";
            }
        }

        /// <summary>
        /// 处理日历视图中的 At 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public Hit At(Point p, Size size)
        {
            return Hits.LastOrDefault(h => h.Box.Contains(p));
        }

        /// <summary>
        /// 更新当天状态，保留用户已浏览的其他日期。
        /// </summary>
        public bool UpdateDay(DateTime date)
        {
            date = date.Date;
            if (date == Today)
                return false;
            bool follow = Selected.Date == Today;
            Today = date;
            if (follow)
            {
                Selected = date;
                Month = date;
            }

            if (PlanMode == "day")
                PlanAnchor = date;
            return true;
        }

        /// <summary>
        /// 按月或周移动浏览日期。
        /// </summary>
        public void Navigate(int direction)
        {
            if (IsWeek)
            {
                Selected = Selected.Date.AddDays(direction * 7);
                Month = Selected;
            }
            else
            {
                Month = Month.AddMonths(direction);
                Selected = new DateTime(Month.Year, Month.Month, 1);
            }
        }

        /// <summary>
        /// 生成当前视图位图，调用方负责释放。
        /// </summary>
        public Bitmap Render(Size size)
        {
            var bitmap = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            using (var g = Graphics.FromImage(bitmap))
                Paint(g, size);
            return bitmap;
        }

        public bool ShowSidebar
        {
            /// <summary>
            /// 读取ShowSidebar属性，供日历视图使用。
            /// </summary>
            get
            {
                return true;
            }
        }

        /// <summary>
        /// 绘制完整视图并同步点击区域。
        /// </summary>
        public void Paint(Graphics g, Size size)
        {
            PaintComponents(g,size);
        }

        /// <summary>
        /// 绘制日历视图中的 DrawPanel 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void DrawPanel(Graphics g, RectangleF panel, PointF oldOrigin, Action draw)
        {
            int start = Hits.Count;
            var saved = g.Save();
            float scale = Layout.Scale;
            RectangleF pixel = Layout.Pixel(panel);
            g.SetClip(pixel,CombineMode.Intersect);
            g.TranslateTransform(pixel.X - oldOrigin.X * scale, pixel.Y - oldOrigin.Y * scale);
            g.ScaleTransform(scale, scale);
            draw();
            g.Restore(saved);
            for (int i = start; i < Hits.Count; i++)
            {
                var b = Hits[i].Box;
                Hits[i].Box = new RectangleF(pixel.X + (b.X - oldOrigin.X) * scale, pixel.Y + (b.Y - oldOrigin.Y) * scale, b.Width * scale, b.Height * scale);
            }
        }

        float FooterY
        {
            /// <summary>
            /// 读取FooterY属性，供日历视图使用。
            /// </summary>
            get
            {
                return Layout.Calendar.Height - 46;
            }
        }

        /// <summary>
        /// 处理日历视图中的 Utilities 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void Utilities(Graphics g)
        {
            if (ShowSidebar)
                return;
            Button(g, "widgetzodiac", "☆", new RectangleF(991 + WideX, FooterY, 43, 36));
            Button(g, "widgetfortune", "☯", new RectangleF(1042 + WideX, FooterY, 43, 36));
        }

        /// <summary>
        /// 绘制日历视图中的 DrawHeader 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void DrawHeader(Graphics g)
        {
            DateTime monday = WeekLayout.Monday(Selected);
            string title = IsWeek ? monday.ToString("M.d") + " — " + monday.AddDays(6).ToString("M.d") : L.Date(Month, "yyyy 年 M 月", "MMMM yyyy");
            if(Layout.Calendar.Width*Layout.Scale<780) {
                Text(g,title,306,30,600,38,21,Ink);
                Button(g,"tools",L.T("⚙ 工具"),new RectangleF(1090+WideX,31,100,30));
                float bx=306;
                foreach(var item in new[]{new[]{"viewmonth",L.T("月")},new[]{"viewweek",L.T("周")},new[]{"prev","‹"},new[]{"today",L.T("今天")},new[]{"next","›"},new[]{"headermore",L.T("更多")}}) {
                    float width=item[0]=="prev"||item[0]=="next"?52:120;
                    Button(g,item[0],item[1],new RectangleF(bx,73,width,28),item[0]=="viewmonth"&&!IsWeek||item[0]=="viewweek"&&IsWeek);bx+=width+12;
                }
                return;
            }
            Text(g, title, 306, 35, 390, 43, 21, Ink);
            Button(g, "viewmonth", L.T("月"), new RectangleF(734 + WideX - 36, 39, 58, 34), !IsWeek);
            Button(g, "viewweek", L.T("周"), new RectangleF(800 + WideX - 36, 39, 58, 34), IsWeek);
            Button(g, "prev", "‹", new RectangleF(884 + WideX - 36, 39, 37, 34));
            Button(g, "today", L.T("今天"), new RectangleF(929 + WideX - 36, 39, 68, 34));
            Button(g, "next", "›", new RectangleF(1005 + WideX - 36, 39, 37, 34));
            Button(g, "settings", L.T("＋日历"), new RectangleF(1048 + WideX - 36, 39, 78, 34));
            Button(g, "tools", L.T("⚙ 工具"), new RectangleF(1134 + WideX - 36, 39, 92, 34));
            Text(g, Status, 306, 81, 916 + WideX, 19, 9, Muted);
            Hits.Add(new Hit { Action = "holidaystatus", Box = new RectangleF(306, 79, 916 + WideX, 20) });
        }

        /// <summary>
        /// 绘制月视图、假日、日程和当天整格描边。
        /// </summary>
        void DrawMonth(Graphics g)
        {
            float x0 = 304, y0 = 154, cw = 132 + WideX / 7, rh = (Layout.Calendar.Height - 238) / 6;
            string[] days =
            {
                L.T("周一"),
                L.T("周二"),
                L.T("周三"),
                L.T("周四"),
                L.T("周五"),
                L.T("周六"),
                L.T("周日")
            };
            for (int i = 0; i < 7; i++)
                Centered(g, days[i],new RectangleF(x0+i*cw,111,cw,28),11,Ink,"Microsoft YaHei UI");
            DateTime first = new DateTime(Month.Year, Month.Month, 1);
            first = WeekLayout.Monday(first);
            var events = State.VisibleEvents().ToList();
            if (!Skins.Glass) using (var pen = new Pen(Line, 1))
            {
                for (int r = 0; r <= 6; r++)
                    g.DrawLine(pen, x0, y0 + r * rh, x0 + 7 * cw, y0 + r * rh);
                for (int c = 1; c < 7; c++)
                    g.DrawLine(pen, x0 + c * cw, y0, x0 + c * cw, y0 + 6 * rh);
            }

            for (int i = 0; i < 42; i++)
            {
                DateTime day = first.AddDays(i);
                float x = x0 + i % 7 * cw, y = y0 + i / 7 * rh;
                Hits.Add(new Hit { Action = "date", Date = day, Box = new RectangleF(x, y, cw, rh) });
                bool today = day == Today;
                if (Skins.Glass) {
                    GlassWindow.Card(g,new RectangleF(x+3,y+3,cw-6,rh-6),State.GlassRelief ? Math.Sign((day-Today).Days) : 0);
                    if (today) using(var underline = new Pen(Ink,2)) g.DrawLine(underline,x+11,y+33,x+34,y+33);
                }
                if (today && !Skins.Glass)
                    using (var path = Chrome.Round(new RectangleF(x + 1.5f, y + 1.5f, cw - 3, rh - 3), 9))
                    using (var outline = new Pen(Skins.Accent, 1.8f))
                        g.DrawPath(outline, path);
                Text(g, day.Day.ToString(), x + 11, y + 5, 40, 32, 14, day.Month == Month.Month ? Ink : Muted, "Segoe UI", FontStyle.Bold);
                if (State.Lunar)
                    Text(g, Data.Lunar(day), x + 53, y + 10, cw - 59, 23, 10, Muted);
                var marks = State.Marks.Concat(Holidays).Where(m => m.Date == day.ToString("yyyy-MM-dd")).ToList();
                var entries = events.Where(e => e.On(day)).OrderByDescending(e => e.Important).ThenBy(e => e.Start).ToList();
                int total = marks.Count + entries.Count, capacity=Math.Max(0,Math.Min(3,(int)((rh-42)/21))), limit=total>capacity?Math.Max(0,capacity-1):capacity, rows=0;
                float yy = y + 39;
                foreach (var m in marks.Take(limit))
                {
                    DrawHoliday(g, m, new RectangleF(x + 5, yy, cw - 10, 20));
                    Hits.Add(new Hit { Action = "day", Date = day, Box = new RectangleF(x, yy, cw, 22) });
                    rows++;
                    yy += 21;
                }

                foreach (var e in entries.Take(limit - rows))
                {
                    DrawEvent(g, e, new RectangleF(x + 6, yy, cw - 12, 22));
                    Hits.Add(new Hit { Action = "event", Id = e.Id, Date = day, Box = new RectangleF(x, yy, cw, 22) });
                    rows++;
                    yy += 21;
                }

                if (total > rows)
                {
                    Text(g, "+" + (total - rows) + L.T(" 更多"), x + 9, y + 39 + rows*21, cw - 18, 22, 10, Muted);
                    Hits.Add(new Hit { Action = "day", Date = day, Box = new RectangleF(x, y + 39 + rows*21, cw, 22) });
                }
            }

            DrawComponentFooter(g);
        }

        /// <summary>
        /// 绘制单条日程；圆点与 emoji 互斥，高亮与标记颜色独立。
        /// </summary>
        void DrawEvent(Graphics g, Entry e, RectangleF box)
        {
            DrawAlignedEvent(g,e,box);
        }

        /// <summary>
        /// 绘制日历视图中的 DrawLeft 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void DrawLeft(Graphics g)
        {
            Text(g, L.Date(Month, "M 月", "MMM"), 39, 33, 200, 50, 24, Ink);
            Text(g, Month.Year.ToString(), 39, 95, 200, 27, 12, Muted, "Segoe UI");
            Text(g, L.T("我的日历"), 39, 148, 205, 27, 11, Muted);
            int row = 0;
            foreach (var cal in State.Calendars.Where(c => c.Enabled).Take(2))
            {
                Dot(g, 40, 194 + row * 31, 8, ColorTranslator.FromHtml(cal.Color));
                Text(g, cal.Name, 58, 184 + row * 31, 180, 27, 11, Ink);
                row++;
            }

            Button(g, "sources", L.T("国家假日 · ") + State.Countries.Count, new RectangleF(37, 265, 207, 34));
            using (var pen = new Pen(Line, 1))
                g.DrawLine(pen, 37, 324, 244, 324);
            DrawWidget(g);
        }

        /// <summary>
        /// 绘制日历视图中的 DrawWidget 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void DrawWidget(Graphics g)
        {
            ContentGlass(g,new RectangleF(30,400,222,400+TallY));
            bool lot = State.Widget == "抽签卡片" || State.Widget == "占卜";
            Button(g, "leftzodiac", L.T("星座"), new RectangleF(37, 346, 98, 36), !lot);
            Button(g, "leftfortune", L.T("占卜"), new RectangleF(145, 346, 99, 36), lot);
            if (lot)
            {
                DrawFortuneCard(g);
                return;
            }

            Reading z = Reading.Daily(State.Zodiac, DateTime.Today);
            Text(g, L.T(State.Zodiac), 38, 414, 208, 34, 18, Ink);
            Hits.Add(new Hit { Action = "zodiacchoose", Box = new RectangleF(37, 409, 208, 43) });
            Text(g, z.Grade, 38, 454, 208, 26, 10.5f, Muted);
            Text(g, L.T("综合运势 ") + new string ('★', z.Overall), 38, 482, 207, 28, 12, Ink);
            bool extra = !String.IsNullOrWhiteSpace(z.Interpretation) && z.Interpretation != z.Outlook;
            ScrollText(g, z.Outlook + (extra ? "\n" + z.Interpretation : ""), new RectangleF(38, 512, 207, 222 + TallY),"widgetzodiac",10.5f);
            Text(g, z.IsOnline ? L.T("幸运色：") + z.LuckyName + L.T(" / 数字 ") + z.Number : L.T("尚未获取幸运信息"), 38, 738 + TallY, 207, 24, 10, Muted);
            Wrap(g, L.T("注意：") + z.Caution, new RectangleF(38, 762 + TallY, 207, 42), 10, Ink);
            Button(g, "zodiacchoose", L.T("换星座"), new RectangleF(37, 808 + TallY, 98, 35));
            Button(g, "reading", L.T("详细运势"), new RectangleF(145, 808 + TallY, 99, 35));
            Text(g, L.T("娱乐内容"), 38, 850 + TallY, 207, 23, 9.5f, Muted);
        }

        /// <summary>
        /// 取得当前日期范围的计划条目。
        /// </summary>
        public List<Todo> PlanItems()
        {
            return PlanReview.Items(State, PlanMode, PlanAnchor);
        }

        /// <summary>
        /// 绘制日历视图中的 DrawTodos 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void DrawTodos(Graphics g)
        {
            if (PlanMode == "day")
            {
                DrawDaily(g, false);
                return;
            }

            Card(g, new RectangleF(1268, 16, 316, Layout.Plans.Height));
            bool daily = PlanMode == "day";
            Text(g, daily ? L.T("今日计划") : PlanMode == "week" ? L.T("每周回顾") : L.T("每月回顾"), 1292, 35, 270, 43, 20, Ink);
            Button(g, "plandaily", L.T("今日"), new RectangleF(1290, 102, 82, 35), daily);
            Button(g, "planweek", L.T("周"), new RectangleF(1384, 102, 82, 35), PlanMode == "week");
            Button(g, "planmonth", L.T("月"), new RectangleF(1478, 102, 82, 35), PlanMode == "month");
            DateTime start = PlanReview.Start(PlanMode, PlanAnchor), end = PlanReview.End(PlanMode, PlanAnchor);
            Text(g, daily ? DateTime.Today.ToString("MM.dd dddd", L.Culture) : PlanMode == "month" ? L.Date(start, "yyyy 年 M 月", "MMMM yyyy") : start.ToString("MM.dd") + " – " + end.AddDays(-1).ToString("MM.dd"), 1292, 158, 206, 29, 11, Muted);
            if (!daily)
            {
                Button(g, "planprev", "‹", new RectangleF(1488, 155, 31, 30));
                Button(g, "plannext", "›", new RectangleF(1529, 155, 31, 30));
            }

            ContentGlass(g,new RectangleF(1282,200,288,510+TallY));
            var list = PlanItems();
            int page = Math.Max(0, Math.Min(State.TodoPage, Math.Max(0, (list.Count - 1) / 8))), offset = page * 8;
            for (int i = 0; i < 8 && offset + i < list.Count; i++)
            {
                Todo todo = list[offset + i];
                float y = 215 + i * 63;
                if (daily)
                {
                    using (var pen = new Pen(todo.Done ? Skins.Accent : Muted, 1.3f))
                        g.DrawEllipse(pen, 1291, y + 4, 15, 15);
                    if (todo.Done)
                        Text(g, "✓", 1290, y - 1, 22, 24, 10, Skins.Accent);
                    Text(g, todo.Title, 1318, y, 207, 43, 11, todo.Done ? Muted : Ink, "Microsoft YaHei UI", todo.Done ? FontStyle.Strikeout : FontStyle.Regular);
                    Hits.Add(new Hit { Action = "todo", Id = todo.Id, Box = new RectangleF(1288, y - 3, 28, 51) });
                }
                else
                {
                    Text(g, todo.Title, 1292, y, 235, 29, 11, Ink);
                    Text(g, PlanReview.CompletedDate(todo).ToString("MM.dd") + (todo.CompletedAt.HasValue ? L.T(" · 已完成") : L.T(" · 旧记录")), 1292, y + 28, 232, 24, 10, Muted);
                }

                Hits.Add(new Hit { Action = "todofull", Id = todo.Id, Box = new RectangleF(daily ? 1318 : 1292, y - 3, daily ? 207 : 233, 51) });
                Text(g, "×", 1534, y, 27, 29, 15, Muted);
                Hits.Add(new Hit { Action = "tododelete", Id = todo.Id, Box = new RectangleF(1530, y - 3, 34, 43) });
                using (var pen = new Pen(Line, 1))
                    g.DrawLine(pen, 1292, y + 53, 1560, y + 53);
            }

            if (daily)
                Button(g, "todohistory", L.T("全部记录 · 查看全文"), new RectangleF(1290, 721 + TallY, 270, 35));
            if (list.Count == 0)
                Text(g, daily ? L.T("暂无今日待办") : L.T("此时段没有完成事项"), 1292, 231, 269, 40, 12, Muted);
            Button(g, daily ? "addtodo" : "planexport", daily ? L.T("＋ 添加待办") : PlanReview.ExportButton(PlanMode), new RectangleF(1290, 774 + TallY, 270, 42));
            Text(g, daily ? list.Count(t => t.Done) + " / " + list.Count + L.T(" 已完成") : list.Count + L.T(" 件已完成"), 1292, 837 + TallY, 181, 28, 11, Muted);
            if (list.Count > 8)
            {
                Button(g, "todoprev", "‹", new RectangleF(1486, 831 + TallY, 32, 32));
                Button(g, "todonext", "›", new RectangleF(1528, 831 + TallY, 32, 32));
            }
        }

        /// <summary>
        /// 绘制紧凑布局的待办列表，保留操作命中区域。
        /// </summary>
        void DrawCompactTodos(Graphics g)
        {
            if (PlanMode == "day")
            {
                DrawDaily(g, true);
                return;
            }

            Card(g, new RectangleF(0, 0, 970, 440));
            bool daily = PlanMode == "day";
            Text(g, daily ? L.T("今日计划") : PlanMode == "week" ? L.T("每周回顾") : L.T("每月回顾"), 25, 19, 260, 40, 20, Ink);
            Button(g, "plandaily", L.T("今日"), new RectangleF(570, 22, 75, 34), daily);
            Button(g, "planweek", L.T("周"), new RectangleF(657, 22, 75, 34), PlanMode == "week");
            Button(g, "planmonth", L.T("月"), new RectangleF(744, 22, 75, 34), PlanMode == "month");
            if (!daily)
            {
                Button(g, "planprev", "‹", new RectangleF(838, 22, 40, 34));
                Button(g, "plannext", "›", new RectangleF(893, 22, 40, 34));
            }

            DateTime start = PlanReview.Start(PlanMode, PlanAnchor);
            Text(g, daily ? L.Date(Today, "M月d日", "MMM d") : L.Date(start, "yyyy年M月d日", "MMM d, yyyy") + L.T(" 起"), 25, 67, 850, 27, 11, Muted);
            var list = PlanItems();
            int page = Math.Max(0, Math.Min(State.TodoPage, Math.Max(0, (list.Count - 1) / 8)));
            for (int i = 0; i < 8 && page * 8 + i < list.Count; i++)
            {
                var t = list[page * 8 + i];
                float x = 25 + (i / 4) * 470, y = 112 + (i % 4) * 55;
                Text(g, (daily ? (t.Done ? "✓  " : "○  ") : "") + t.Title, x, y, 390, 29, 12, t.Done ? Muted : Ink, "Microsoft YaHei UI", daily && t.Done ? FontStyle.Strikeout : FontStyle.Regular);
                if (daily)
                    Hits.Add(new Hit { Action = "todo", Id = t.Id, Box = new RectangleF(x, y, 27, 43) });
                else
                    Text(g, PlanReview.CompletedDate(t).ToString("MM.dd") + L.T(" · 已完成"), x, y + 26, 390, 24, 9.5f, Muted);
                Hits.Add(new Hit { Action = "todofull", Id = t.Id, Box = new RectangleF(x + (daily ? 29 : 0), y, daily ? 365 : 394, 43) });
                Button(g, "tododelete", "×", new RectangleF(x + 400, y, 33, 32));
                Hits[Hits.Count - 1].Id = t.Id;
            }

            if (list.Count == 0)
                Text(g, daily ? L.T("暂无今日待办") : L.T("此时段没有完成事项"), 25, 122, 650, 40, 12, Muted);
            Button(g, daily ? "addtodo" : "planexport", daily ? L.T("＋ 添加待办") : PlanReview.ExportButton(PlanMode), new RectangleF(25, 374, 240, 38));
            Text(g, list.Count + L.T(" 项"), 289, 382, 120, 28, 11, Muted);
            if (daily)
                Button(g, "todohistory", L.T("全部记录"), new RectangleF(450, 374, 170, 38));
            if (list.Count > 8)
            {
                Button(g, "todoprev", "‹", new RectangleF(838, 376, 40, 34));
                Button(g, "todonext", "›", new RectangleF(893, 376, 40, 34));
            }
        }

        /// <summary>
        /// 绘制或创建可操作按钮，保留选中反馈与点击区域。
        /// </summary>
        void Button(Graphics g, string action, string text, RectangleF r, bool selected = false)
        {
            bool prominent = action == "addtodo" || action == "tools";
            Round(g, r, 9, selected || prominent || HoverAction == action ? Skins.Button : Palette.Paper);
            if (selected)
                using (var p = new Pen(Skins.Accent, 2))
                    g.DrawLine(p, r.X + 12, r.Bottom - 3, r.Right - 12, r.Bottom - 3);
            using (var font = UiFont.Scene(10.5f))
            using (var brush = new SolidBrush(Ink))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            }

            )
            {
                if (Skins.Glass) using (var shadow = new SolidBrush(Color.FromArgb(Skins.Dark ? 65 : 35, Skins.Dark ? Color.Black : Color.White)))
                    g.DrawString(text, font, shadow, new RectangleF(r.X + 4, r.Y + 1, r.Width - 6, r.Height), format);
                g.DrawString(text, font, brush, new RectangleF(r.X + 3, r.Y, r.Width - 6, r.Height), format);
            }
            Hits.Add(new Hit { Action = action, Box = r });
        }

        /// <summary>
        /// 处理日历视图中的 Card 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static void Card(Graphics g, RectangleF r)
        {
            if (Skins.Glass) { GlassWindow.Card(g, r,0,false,-1,false); return; }
            Round(g, new RectangleF(r.X, r.Y + 2, r.Width, r.Height), 18, Color.FromArgb(Skins.Dark ? 28 : 9, Color.Black));
            Round(g, r, 18, Palette.Paper);
        }

        /// <summary>
        /// 处理日历视图中的 Dot 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static void Dot(Graphics g, float x, float y, float d, Color color)
        {
            using (var brush = new SolidBrush(color))
                g.FillEllipse(brush, x, y, d, d);
        }

        /// <summary>
        /// 创建圆角路径，调用方负责释放图形资源。
        /// </summary>
        static void Round(Graphics g, RectangleF r, float radius, Color color)
        {
            if (r.Width <= 0 || r.Height <= 0)
                return;
            if (Skins.Glass && color.A == 255)
            {
                if (color.ToArgb() == Palette.Paper.ToArgb() || color.ToArgb() == Palette.Cream.ToArgb()) color = Color.FromArgb(10, Color.White);
                else if (color.ToArgb() == Skins.Button.ToArgb() || color.ToArgb() == Skins.Field.ToArgb() || color.ToArgb() == FortuneColors.Slot.ToArgb()) color = Color.FromArgb(35, Skins.Dark ? Color.White : Color.Black);
            }
            float d = Math.Max(.1f, Math.Min(radius * 2, Math.Min(r.Width, r.Height)));
            using (var path = new GraphicsPath())
            {
                path.AddArc(r.X, r.Y, d, d, 180, 90);
                path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
                path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
                path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
                path.CloseFigure();
                using (var brush = new SolidBrush(color))
                    g.FillPath(brush, path);
            }
        }

        /// <summary>
        /// 按给定字体和区域绘制文字，超出区域时截断。
        /// </summary>
        static void Text(Graphics g, string text, float x, float y, float w, float h, float size, Color color, string family = "Microsoft YaHei UI", FontStyle style = FontStyle.Regular)
        {
            if (w <= 0 || h <= 0)
                return;
            using (var font = UiFont.Scene(family, size, style))
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            }

            )
            {
                if (Skins.Glass) using (var shadow = new SolidBrush(Color.FromArgb(Skins.Dark ? 65 : 35, Skins.Dark ? Color.Black : Color.White)))
                    g.DrawString(text, font, shadow, new RectangleF(x, y + .5f, w, h), format);
                g.DrawString(text, font, brush, new RectangleF(x, y, w, h), format);
            }
        }

        /// <summary>
        /// 在给定区域换行绘制文字。
        /// </summary>
        static void Wrap(Graphics g, string text, RectangleF box, float size, Color color)
        {
            using (var font = UiFont.Scene(size))
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat
            {
                Trimming = StringTrimming.EllipsisCharacter
            }

            )
            {
                if (Skins.Glass) using (var shadow = new SolidBrush(Color.FromArgb(Skins.Dark ? 65 : 35, Skins.Dark ? Color.Black : Color.White)))
                    g.DrawString(text, font, shadow, new RectangleF(box.X, box.Y + .5f, box.Width, box.Height), format);
                g.DrawString(text, font, brush, box, format);
            }
        }
    }
}
