using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace PaperDays
{
    public static class EmojiArt
    {
        static readonly Dictionary<string, Image> cache = new Dictionary<string, Image>();
        /// <summary>
        /// 取得 emoji 的文件编号，忽略变体选择符。
        /// </summary>
        public static string Key(string text)
        {
            var code = new List<string>();
            for (int i = 0; i < (text ?? "").Length; i++)
            {
                int c = Char.IsSurrogatePair(text, i) ? Char.ConvertToUtf32(text, i++) : text[i];
                if (c != 0xfe0f)
                    code.Add(c.ToString("x"));
            }

            return String.Join("-", code);
        }

        /// <summary>
        /// 读取缓存或本地图形资源；缺失时返回空值供调用方回退。
        /// </summary>
        public static Image Get(string text)
        {
            if (String.IsNullOrWhiteSpace(text) || text.Length > 32)
                return null;
            string key = Key(text);
            Image found;
            if (cache.TryGetValue(key, out found))
                return found;
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "emoji", key + ".png");
            if (!File.Exists(file))
                return null;
            using (var loaded = Image.FromFile(file))
                found = new Bitmap(loaded);
            cache[key] = found;
            return found;
        }

        /// <summary>
        /// 绘制原彩 emoji。
        /// </summary>
        public static void Draw(Graphics g, string emoji, RectangleF box)
        {
            Draw(g, emoji, box, true, ColorTranslator.ToHtml(Palette.Ink));
        }

        /// <summary>
        /// 计算颜色亮度。
        /// </summary>
        public static double Brightness(Color c)
        {
            return (.299 * c.R + .587 * c.G + .114 * c.B) / 255;
        }

        /// <summary>
        /// 绘制 emoji；original 为假时用 hex 指定的单色。
        /// </summary>
        public static void Draw(Graphics g, string emoji, RectangleF box, bool original, string hex)
        {
            if (String.IsNullOrEmpty(emoji) || box.Width < 1 || box.Height < 1)
                return;
            if (!original && LineIcons.Draw(g, emoji, box, ColorTranslator.FromHtml(hex)))
                return;
            Image image = original ? Get(emoji) : null;
            if (image != null)
            {
                float side = Math.Min(box.Width, box.Height);
                var target = new RectangleF(box.X + (box.Width - side) / 2, box.Y + (box.Height - side) / 2, side, side);
                var quality = g.InterpolationMode;
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.DrawImage(image, target);
                g.InterpolationMode = quality;
                return;
            }

            using (var format = (StringFormat)StringFormat.GenericTypographic.Clone())
            {
                format.Alignment = StringAlignment.Center;
                format.LineAlignment = StringAlignment.Center;
                format.FormatFlags = StringFormatFlags.NoWrap;
                float pixels = box.Height * .78f;
                string family = UiFont.Family(original ? "Segoe UI Emoji" : "Segoe UI Symbol");
                using (var measure = new Font(family, pixels, FontStyle.Regular, GraphicsUnit.Pixel))
                {
                    var size = g.MeasureString(emoji, measure, 1000, format);
                    if (size.Width > box.Width)
                        pixels *= box.Width / size.Width;
                }

                using (var font = new Font(family, Math.Max(1, pixels), FontStyle.Regular, GraphicsUnit.Pixel))
                using (var brush = new SolidBrush(original ? Palette.Ink : ColorTranslator.FromHtml(hex)))
                {
                    if (original)
                        g.DrawString(emoji, font, brush, box, format);
                    else
                        using (var path = new System.Drawing.Drawing2D.GraphicsPath(System.Drawing.Drawing2D.FillMode.Winding))
                        {
                            path.AddString(emoji, font.FontFamily, (int)font.Style, font.Size, box, format);
                            g.FillPath(brush, path);
                        }
                }
            }
        }
    }

    public class TimeBlock
    {
        public Entry Entry;
        public DateTime Start, End;
        public int Lane, Columns;
    }

    /// <summary>
    /// 周视图时间轴：一屏放下 24 小时。默认把 00–06 点压成窄带，勾选等高后每小时高度相同。
    /// </summary>
    public sealed class DayScale
    {
        public const double NightEnd = 6, NightShare = .12;
        public readonly bool Equal;
        public readonly float Top, Height;
        /// <summary>
        /// 初始化时间轴的位置和模式。
        /// </summary>
        public DayScale(float top, float height, bool equal)
        {
            Top = top;
            Height = height;
            Equal = equal;
        }

        /// <summary>
        /// 把当天的小时数换算成纵向坐标。
        /// </summary>
        public float Y(double hour)
        {
            hour = Math.Max(0, Math.Min(24, hour));
            double share = Equal ? hour / 24 : hour <= NightEnd ? hour / NightEnd * NightShare : NightShare + (hour - NightEnd) / (24 - NightEnd) * (1 - NightShare);
            return Top + (float)share * Height;
        }

        /// <summary>
        /// 取得点击格子的时长（分钟）；压缩的夜间按整点选择，方便点中。
        /// </summary>
        public int SlotMinutes(double hour)
        {
            return !Equal && hour < NightEnd ? 60 : 30;
        }
    }

    public static class WeekLayout
    {
        /// <summary>
        /// 处理周布局和 emoji 图形中的 Monday 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static DateTime Monday(DateTime date)
        {
            return date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        }

        /// <summary>
        /// 处理周布局和 emoji 图形中的 ForDay 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static List<TimeBlock> ForDay(IEnumerable<Entry> events, DateTime day)
        {
            var result = new List<TimeBlock>();
            foreach (var e in events.Where(e => !e.AllDay))
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(e.TimeZone);
                DateTime start = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(e.Start, DateTimeKind.Unspecified), zone, TimeZoneInfo.Local), end = TimeZoneInfo.ConvertTime(DateTime.SpecifyKind(e.End, DateTimeKind.Unspecified), zone, TimeZoneInfo.Local);
                if (end < day || start >= day.AddDays(1) || end == day && start < day)
                    continue;
                result.Add(new TimeBlock { Entry = e, Start = start < day ? day : start, End = end > day.AddDays(1) ? day.AddDays(1) : end });
            }

            result = result.OrderBy(e => e.Start).ThenByDescending(e => e.End).ToList();
            var cluster = new List<TimeBlock>();
            var lanes = new List<DateTime>();
            DateTime latest = DateTime.MinValue;
            foreach (var block in result)
            {
                if (cluster.Count > 0 && block.Start >= latest)
                {
                    foreach (var b in cluster)
                        b.Columns = lanes.Count;
                    cluster.Clear();
                    lanes.Clear();
                }

                int lane = lanes.FindIndex(end => end <= block.Start);
                if (lane < 0)
                {
                    lane = lanes.Count;
                    lanes.Add(block.End);
                }
                else
                    lanes[lane] = block.End;
                block.Lane = lane;
                cluster.Add(block);
                if (block.End > latest)
                    latest = block.End;
            }

            foreach (var b in cluster)
                b.Columns = lanes.Count;
            return result;
        }

        /// <summary>
        /// 按全天时间轴取得日程块位置；过短的日程保留能显示一行文字的高度。
        /// </summary>
        public static RectangleF Box(TimeBlock block, DateTime day, float x, float columnWidth, DayScale scale, float minimumHeight)
        {
            double start = (block.Start - day).TotalHours, end = (block.End - day).TotalHours;
            if (end < 0 || start > 24 || end < start)
                return RectangleF.Empty;
            float width = columnWidth / Math.Max(1, block.Columns), top = scale.Y(start), height = Math.Max(minimumHeight, scale.Y(end) - top);
            float bottom = scale.Top + scale.Height;
            if (top + height > bottom)
                top = Math.Max(scale.Top, bottom - height);
            return new RectangleF(x + block.Lane * width + 2, top, width - 4, Math.Min(height, bottom - top));
        }
    }
}
