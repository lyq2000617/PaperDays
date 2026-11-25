using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PaperDays
{
    public class PaletteSection : Panel
    {
        public string Caption;
        /// <summary>
        /// 初始化ReleaseUI的PaletteSection，准备状态、布局与必要的事件绑定。
        /// </summary>
        public PaletteSection()
        {
            DoubleBuffered = true;
            BackColor = Palette.Paper;
        }

        /// <summary>
        /// 绘制控件当前状态，保持主题配色和内容可见。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = Chrome.Round(new RectangleF(0, 0, Width - 1, Height - 1), 12))
            using (var pen = new Pen(Skins.Line))
                e.Graphics.DrawPath(pen, p);
            TextRenderer.DrawText(e.Graphics, Caption, Font, new Point(Math.Max(10, Font.Height), Math.Max(5, Font.Height / 3)), Palette.Muted);
        }
    }

    public class EmojiButton : SoftButton
    {
        public string Emoji, Ink;
        public bool Original = true, Selected;
        /// <summary>
        /// 绘制控件当前状态，保持主题配色和内容可见。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent == null ? Palette.Paper : Parent.BackColor);
            bool hover = ClientRectangle.Contains(PointToClient(MousePosition));
            using (var path = Chrome.Round(new RectangleF(.5f, .5f, Width - 2, Height - 2), Math.Min(Width, Height) * .24f))
            {
                if (hover)
                    using (var fill = new SolidBrush(Skins.Field))
                        g.FillPath(fill, path);
                using (var pen = new Pen(Selected || Focused ? Skins.Accent : Skins.Line, Selected ? 1.6f : 1))
                    g.DrawPath(pen, path);
            }

            float side = Math.Min(Width, Height) * .6f;
            EmojiArt.Draw(g, Emoji, new RectangleF((Width - side) / 2f, (Height - side) / 2f, side, side), Original, Ink ?? ColorTranslator.ToHtml(Palette.Ink));
        }
    }

    public class ReminderDialog : Form
    {
        /// <summary>
        /// 初始化ReleaseUI的ReminderDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public ReminderDialog(System.Collections.Generic.List<Entry> entries) : this(entries[0].Title, entries[0].Start.ToString("MM-dd HH:mm"))
        {
            if (entries.Count <= 1)
                return;
            Controls.Clear();
            int listHeight = Math.Min(330, entries.Count * 86);
            ClientSize = new Size(380, listHeight + 82);
            var list = new FlowLayoutPanel
            {
                Bounds = new Rectangle(18, 14, 344, listHeight),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            Controls.Add(list);
            foreach (var entry in entries)
            {
                var label = new Label
                {
                    Text = entry.Title + "\n" + entry.Start.ToString("MM-dd HH:mm"),
                    Size = new Size(320, 76),
                    Font = UiFont.New(13),
                    TextAlign = ContentAlignment.MiddleCenter,
                    AutoEllipsis = true
                };
                list.Controls.Add(label);
            }

            var ok = UI.Button(this, L.T("知道了"), 132, listHeight + 30, 116, delegate
            {
                Close();
            });
            AcceptButton = ok;
            CancelButton = ok;
        }

        /// <summary>
        /// 初始化ReleaseUI的ReminderDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public ReminderDialog(string title, string time)
        {
            UI.Style(this, L.T("日程提醒"), 380, 202);
            var heading = new Label
            {
                Text = title,
                Bounds = new Rectangle(24, 22, 332, 54),
                Font = UiFont.New(16, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                AutoEllipsis = true
            };
            Controls.Add(heading);
            var when = new Label
            {
                Text = time,
                Bounds = new Rectangle(24, 85, 332, 30),
                Font = UiFont.New(12),
                ForeColor = Palette.Muted,
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(when);
            var ok = UI.Button(this, L.T("知道了"), 132, 143, 116, delegate
            {
                Close();
            });
            AcceptButton = ok;
            CancelButton = ok;
        }
    }
}