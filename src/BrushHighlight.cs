using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Collections.Generic;

namespace PaperDays
{
    public static class BrushHighlight
    {
        /// <summary>
        /// 绘制内容到指定区域，保持裁切与图形状态正确。
        /// </summary>
        public static void Draw(Graphics g, RectangleF box, string hex)
        {
            Draw(g, box, hex, false);
        }

        /// <summary>
        /// 绘制内容到指定区域，保持裁切与图形状态正确。
        /// </summary>
        public static void Draw(Graphics g, RectangleF box, string hex, bool direct)
        {
            if (box.Width < 3 || box.Height < 3)
                return;
            var saved = g.Save();
            g.SetClip(box, CombineMode.Intersect);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float pad = Math.Min(3, box.Height * .13f), top = box.Top + pad, bottom = box.Bottom - pad, edge = Math.Min(5, box.Width * .08f);
            var points = new List<PointF>();
            int count = Math.Max(5, (int)(box.Width / 7));
            for (int i = 0; i <= count; i++)
                points.Add(new PointF(box.Left + edge + (box.Width - 2 * edge) * i / count, top + (float)Math.Sin(i * 2.19) * .7f));
            for (int i = 1; i <= 6; i++)
                points.Add(new PointF(box.Right - edge + (i % 3) * edge * .5f, top + (bottom - top) * i / 7));
            for (int i = count; i >= 0; i--)
                points.Add(new PointF(box.Left + edge + (box.Width - 2 * edge) * i / count, bottom + (float)Math.Cos(i * 1.87) * .8f));
            for (int i = 6; i >= 1; i--)
                points.Add(new PointF(box.Left + (i % 3) * edge * .5f, top + (bottom - top) * i / 7));
            Color color = direct ? ColorTranslator.FromHtml(hex) : Skins.Highlight(hex);
            using (var path = new GraphicsPath())
            {
                path.AddPolygon(points.ToArray());
                using (var brush = new SolidBrush(color))
                    g.FillPath(brush, path);
                g.SetClip(path, CombineMode.Intersect);
                using (var pen = new Pen(Color.FromArgb(70, Palette.Paper), .8f))
                {
                    for (int i = 0; i < 7; i++)
                    {
                        float start = box.Left + box.Width * (.03f + i * .13f), y = top + (bottom - top) * (.18f + (i % 3) * .26f);
                        g.DrawLine(pen, start, y, Math.Min(box.Right, start + box.Width * .17f), y + .6f);
                    }
                }

                using (var pen = new Pen(Color.FromArgb(30, Palette.Ink), .6f))
                    g.DrawLine(pen, box.Left + edge, bottom - 1, box.Left + box.Width * .42f, bottom - 1.4f);
            }

            g.Restore(saved);
        }
    }
}