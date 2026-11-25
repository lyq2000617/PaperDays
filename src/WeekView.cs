using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace PaperDays
{
    public partial class Scene
    {
        /// <summary>主界面文字的最小实际像素（100% 缩放时）。</summary>
        public const float MinimumTextPixels = 10;
        /// <summary>日历所在屏幕的 DPI，由显示窗口更新。</summary>
        public int ScreenDpi = 96;

        /// <summary>屏幕比画布更宽时，日历区多出来的宽度。</summary>
        float WideX
        {
            /// <summary>读取多出来的宽度。</summary>
            get
            {
                return Layout.Calendar.Width - 970;
            }
        }

        /// <summary>屏幕比画布更高时，三栏多出来的高度。</summary>
        float TallY
        {
            /// <summary>读取多出来的高度；竖屏布局由日历区自己吸收。</summary>
            get
            {
                return Layout.Portrait ? 0 : Layout.Calendar.Height - 868;
            }
        }

        /// <summary>
        /// 绘制周视图：一屏显示全天，时间块按时间轴定位。
        /// </summary>
        void DrawWeek(Graphics g)
        {
            DateTime monday = WeekLayout.Monday(Selected);
            float x0 = 354, cw = 124 + WideX / 7, y0 = 184, bottom = FooterY - 6, height = bottom - y0 - 32; bottom-=32;
            var scale = new DayScale(y0, height, State.WeekEqualHours);
            var visible = State.VisibleEvents().ToList();
            if (Skins.Glass) {
                GlassWindow.Card(g,new RectangleF(295,y0,54,height));
                for(int c=0;c<7;c++) GlassWindow.Card(g,new RectangleF(x0+c*cw+3,134,cw-6,bottom-134),State.GlassRelief ? Math.Sign((monday.AddDays(c)-Today).Days) : 0);
            }
            using (var pen = new Pen(Line, 1))
            using (var faint = new Pen(Color.FromArgb(Skins.Dark ? 90 : 120, Line), 1))
            {
                if (!scale.Equal)
                    using (var night = new SolidBrush(Skins.Glass ? Color.FromArgb(12,Color.White) : Skins.Field))
                        g.FillRectangle(night, x0, y0, 7 * cw, scale.Y(DayScale.NightEnd) - y0);
                for (int hour = 0; hour <= 24; hour++)
                {
                    if (!scale.Equal && hour > 0 && hour < DayScale.NightEnd)
                        continue;
                    float y = scale.Y(hour);
                    g.DrawLine(hour % 2 == 0 ? pen : faint, x0, y, x0 + 7 * cw, y);
                    if (hour % 2 == 0)
                        Text(g, hour.ToString("00") + ":00", 299, Math.Min(y - 10, bottom - 18), 53, 22, 9.5f, Muted, "Segoe UI");
                }

                for (int c = 0; !Skins.Glass && c <= 7; c++)
                    g.DrawLine(pen, x0 + c * cw, 105, x0 + c * cw, bottom);
            }

            for (int c = 0; c < 7; c++)
            {
                DateTime day = monday.AddDays(c);
                float x = x0 + c * cw;
                Centered(g,L.ShortDay(c)+" "+day.ToString("M/d"),new RectangleF(x,103,cw,28),11,Ink,"Microsoft YaHei UI");
                Hits.Add(new Hit { Action = "date", Date = day, Box = new RectangleF(x, 98, cw, 33) });
                var all = visible.Where(e => e.AllDay && e.On(day)).ToList();
                var marks = State.Marks.Concat(Holidays).Where(m => m.Date == day.ToString("yyyy-MM-dd")).ToList();
                int row = 0;
                foreach (var m in marks.Take(2))
                {
                    DrawHoliday(g, m, new RectangleF(x + 5, 134 + row * 23, cw - 10, 20));
                    Hits.Add(new Hit { Action = "day", Date = day, Box = new RectangleF(x, 134 + row * 23, cw, 22) });
                    row++;
                }

                foreach (var e in all.Take(2 - row))
                {
                    DrawEvent(g, e, new RectangleF(x + 5, 134 + row * 23, cw - 10, 22));
                    Hits.Add(new Hit { Action = "event", Id = e.Id, Date = day, Box = new RectangleF(x, 134 + row * 23, cw, 22) });
                    row++;
                }

                if (all.Count + marks.Count > 2)
                {
                    Text(g, "+" + (all.Count + marks.Count - row), x + cw - 37, 165, 34, 18, 9, Muted);
                    Hits.Add(new Hit { Action = "day", Date = day, Box = new RectangleF(x + cw - 40, 164, 40, 19) });
                }

                for (double hour = 0; hour < 24;)
                {
                    int minutes = scale.SlotMinutes(hour);
                    float top = scale.Y(hour), end = scale.Y(hour + minutes / 60.0);
                    Hits.Add(new Hit { Action = "timeslot", Date = day.AddHours(hour), Box = new RectangleF(x, top, cw, end - top) });
                    hour += minutes / 60.0;
                }

                var saved = g.Save();
                g.SetClip(new RectangleF(x, y0, cw, height));
                foreach (var block in WeekLayout.ForDay(visible, day))
                {
                    RectangleF box = WeekLayout.Box(block, day, x, cw, scale, 15);
                    if (box.IsEmpty)
                        continue;
                    DrawTimeBlock(g, block, box);
                    Hits.Add(new Hit { Action = "event", Id = block.Entry.Id, Date = day, Box = box });
                }

                g.Restore(saved);
            }

            int todayIndex = (Today.Date - monday).Days;
            if (todayIndex >= 0 && todayIndex < 7)
                using (var path = Chrome.Round(new RectangleF(x0 + todayIndex * cw + 1.5f, 97, cw - 3, bottom - 97), 10))
                using (var pen = new Pen(Skins.Accent, 1.8f))
                    g.DrawPath(pen, path);
            Text(g, L.T("全天"), 299, 136, 51, 24, 10, Muted);
            DrawCheck(g, "weekequal", L.T("24 小时等高"), State.WeekEqualHours, 306, FooterY - 32, 160, 34);
            DrawComponentFooter(g);
        }

        /// <summary>
        /// 绘制一条定时日程；空间不足时只保留单行标题，不让短日程变成没有文字的色条。
        /// </summary>
        void DrawTimeBlock(Graphics g, TimeBlock block, RectangleF box)
        {
            Color color = ColorTranslator.FromHtml(block.Entry.Color);
            if (block.Entry.Highlight)
                BrushHighlight.Draw(g, box, Skins.HighlightOf(block.Entry), true);
            else
                Round(g, box, Math.Min(6, box.Height / 2), Color.FromArgb(35, color));
            using (var brush = new SolidBrush(color))
                g.FillRectangle(brush, box.X, box.Y, 3, box.Height);
            var clipped = g.Save();
            g.SetClip(box, CombineMode.Intersect);
            bool tall = box.Height >= 38;
            float size = tall ? 10 : box.Height >= 20 ? 9.5f : 8.5f;
            float labelX = box.X + 7, line = (float)Math.Ceiling(size * 1.9f);
            float textY = tall ? box.Y + 3 : box.Y + (box.Height - line) / 2;
            if (block.Entry.UseEmoji && box.Width >= 45)
            {
                float side = Math.Min(20, box.Height - 3);
                EmojiArt.Draw(g, block.Entry.Emoji, new RectangleF(labelX, box.Y + (tall ? 4 : (box.Height - side) / 2), side, side), block.Entry.EmojiOriginal, Skins.EmojiInk(block.Entry));
                labelX += side + 2;
            }

            string title = block.Entry.Title + (String.IsNullOrWhiteSpace(block.Entry.ClassName) ? "" : " · " + block.Entry.ClassName);
            Text(g, title, labelX, textY, Math.Max(5, box.Right - labelX - 3), line + 4, size, block.Entry.Highlight ? HighlightInk(block.Entry) : Ink);
            if (tall)
                Text(g, block.Start.ToString("HH:mm") + "–" + block.End.ToString("HH:mm"), box.X + 7, box.Y + 23, box.Width - 12, 20, 9, block.Entry.Highlight ? HighlightInk(block.Entry) : Muted, "Segoe UI");
            g.Restore(clipped);
        }

        /// <summary>
        /// 在画布上绘制勾选框；整段文字都是点击区域。
        /// </summary>
        void DrawCheck(Graphics g, string action, string text, bool value, float x, float y, float w, float h)
        {
            var box = new RectangleF(x + 8, y + (h - 16) / 2, 16, 16);
            if (HoverAction == action)
                Round(g, new RectangleF(x, y, w, h), 9, Skins.Button);
            using (var path = Chrome.Round(box, 4))
            {
                if (value)
                    using (var fill = new SolidBrush(Skins.Accent))
                        g.FillPath(fill, path);
                else
                    using (var pen = new Pen(Muted, 1.2f))
                        g.DrawPath(pen, path);
            }

            if (value)
                using (var mark = new Pen(Palette.Paper, 2))
                    g.DrawLines(mark, new[] { new PointF(box.X + 3.5f, box.Y + 8.5f), new PointF(box.X + 6.8f, box.Y + 11.8f), new PointF(box.X + 12.5f, box.Y + 4.5f) });
            Text(g, text, x + 31, y + 6, w - 34, 24, 10.5f, Ink);
            Hits.Add(new Hit { Action = action, Box = new RectangleF(x, y, w, h) });
        }
    }
}
