using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace PaperDays
{
    public class SkinTile : Button
    {
        public Skin Theme;
        public bool Chosen;
        /// <summary>
        /// 初始化SkinTile的SkinTile，准备状态、布局与必要的事件绑定。
        /// </summary>
        public SkinTile()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Default;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        /// <summary>
        /// 绘制控件当前状态，保持主题配色和内容可见。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Parent.BackColor);
            Color bg = ColorTranslator.FromHtml(Theme.Background), paper = ColorTranslator.FromHtml(Theme.Paper), ink = (Theme.Id == "stone" || Theme.Id == "glass") ? Color.FromArgb(238, 239, 243) : Color.FromArgb(48, 52, 58);
            using (var p = Chrome.Round(new RectangleF(1, 1, Width - 3, Height - 3), 14))
            using (var b = new SolidBrush(bg))
            {
                g.FillPath(b, p);
                if (Chosen || Focused)
                    using (var pen = new Pen(ColorTranslator.FromHtml(Theme.Accent), 1.5f))
                        g.DrawPath(pen, p);
            }

            TextRenderer.DrawText(g, L.T(Theme.Name) + (Chosen ? "  ✓" : ""), Font, new Rectangle(17, 10, Width - 34, 26), ink, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            using (var p = Chrome.Round(new RectangleF(16, 43, Width - 32, 51), 8))
            using (var b = new SolidBrush(paper))
                g.FillPath(b, p);
            using (var b = new SolidBrush(ColorTranslator.FromHtml(Theme.ButtonTone)))
                g.FillRectangle(b, 27, 53, 38, 9);
            using (var pen = new Pen(ColorTranslator.FromHtml(Theme.Decor)))
            {
                g.DrawLine(pen, 27, 71, Width - 27, 71);
                g.DrawLine(pen, 27, 83, Width - 27, 83);
                for (int i = 0; i < 6; i++)
                    g.DrawLine(pen, 27 + i * (Width - 54) / 6, 66, 27 + i * (Width - 54) / 6, 88);
            }

            for (int i = 0; i < 8; i++)
                using (var b = new SolidBrush(ColorTranslator.FromHtml(Theme.Colors[i])))
                    g.FillEllipse(b, 20 + i * (Width - 55) / 7, 105, 15, 15);
        }
    }
}
