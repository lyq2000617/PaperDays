using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PaperDays
{
    /// <summary>
    /// 单色图标：读取 Tabler 线条图标的矢量路径并按实际尺寸描线，任何缩放比例下都清晰。
    /// </summary>
    public static class LineIcons
    {
        const float Box = 24, Stroke = 1.6f;
        static Dictionary<string, GraphicsPath> paths;

        /// <summary>
        /// 读取随程序附带的路径文件；文件缺失时返回空表，由调用方回退。
        /// </summary>
        static Dictionary<string, GraphicsPath> All()
        {
            if (paths != null)
                return paths;
            paths = new Dictionary<string, GraphicsPath>();
            string file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "emoji", "line-icons.txt");
            if (File.Exists(file))
                foreach (string line in File.ReadAllLines(file))
                {
                    int tab = line.IndexOf('\t');
                    if (tab > 0)
                        paths[line.Substring(0, tab)] = Parse(line.Substring(tab + 1));
                }

            return paths;
        }

        /// <summary>判断 emoji 是否有对应的线条图标。</summary>
        public static bool Has(string emoji)
        {
            return All().ContainsKey(EmojiArt.Key(emoji));
        }

        /// <summary>
        /// 在区域内描绘线条图标；没有对应图标时返回假。
        /// </summary>
        public static bool Draw(Graphics g, string emoji, RectangleF box, Color ink)
        {
            GraphicsPath source;
            if (!All().TryGetValue(EmojiArt.Key(emoji), out source))
                return false;
            float side = Math.Min(box.Width, box.Height), scale = side / Box;
            using (var shape = (GraphicsPath)source.Clone())
            using (var matrix = new Matrix(scale, 0, 0, scale, box.X + (box.Width - side) / 2, box.Y + (box.Height - side) / 2))
            using (var pen = new Pen(ink, Stroke * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round, LineJoin = LineJoin.Round })
            {
                shape.Transform(matrix);
                var smoothing = g.SmoothingMode;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawPath(pen, shape);
                g.SmoothingMode = smoothing;
            }

            return true;
        }

        /// <summary>
        /// 解析 SVG 路径命令。
        /// </summary>
        public static GraphicsPath Parse(string d)
        {
            var path = new GraphicsPath(FillMode.Winding);
            int i = 0;
            char command = ' ';
            float x = 0, y = 0, startX = 0, startY = 0, controlX = 0, controlY = 0;
            char previous = ' ';
            Func<float> number = delegate
            {
                while (i < d.Length && (d[i] == ' ' || d[i] == ','))
                    i++;
                int begin = i;
                if (i < d.Length && (d[i] == '-' || d[i] == '+'))
                    i++;
                bool dot = false;
                while (i < d.Length && (Char.IsDigit(d[i]) || d[i] == '.' && !dot))
                {
                    if (d[i] == '.')
                        dot = true;
                    i++;
                }

                return Single.Parse(d.Substring(begin, i - begin), CultureInfo.InvariantCulture);
            };
            Func<float> flag = delegate
            {
                while (i < d.Length && (d[i] == ' ' || d[i] == ','))
                    i++;
                return d[i++] == '1' ? 1 : 0;
            };
            while (i < d.Length)
            {
                while (i < d.Length && (d[i] == ' ' || d[i] == ','))
                    i++;
                if (i >= d.Length)
                    break;
                if (Char.IsLetter(d[i]))
                    command = d[i++];
                else if (command == 'M')
                    command = 'L';
                else if (command == 'm')
                    command = 'l';
                bool relative = Char.IsLower(command);
                float ox = relative ? x : 0, oy = relative ? y : 0;
                switch (Char.ToUpperInvariant(command))
                {
                    case 'M':
                        x = ox + number();
                        y = oy + number();
                        path.StartFigure();
                        startX = x;
                        startY = y;
                        break;
                    case 'L':
                        {
                            float nx = ox + number(), ny = oy + number();
                            path.AddLine(x, y, nx, ny);
                            x = nx;
                            y = ny;
                            break;
                        }
                    case 'H':
                        {
                            float nx = ox + number();
                            path.AddLine(x, y, nx, y);
                            x = nx;
                            break;
                        }
                    case 'V':
                        {
                            float ny = oy + number();
                            path.AddLine(x, y, x, ny);
                            y = ny;
                            break;
                        }
                    case 'C':
                    case 'S':
                        {
                            bool smooth = Char.ToUpperInvariant(command) == 'S';
                            bool follows = "CcSs".IndexOf(previous) >= 0;
                            float ax = smooth ? (follows ? 2 * x - controlX : x) : ox + number();
                            float ay = smooth ? (follows ? 2 * y - controlY : y) : oy + number();
                            float bx = ox + number(), by = oy + number(), nx = ox + number(), ny = oy + number();
                            path.AddBezier(x, y, ax, ay, bx, by, nx, ny);
                            controlX = bx;
                            controlY = by;
                            x = nx;
                            y = ny;
                            break;
                        }
                    case 'Q':
                    case 'T':
                        {
                            bool smooth = Char.ToUpperInvariant(command) == 'T';
                            bool follows = "QqTt".IndexOf(previous) >= 0;
                            float qx = smooth ? (follows ? 2 * x - controlX : x) : ox + number();
                            float qy = smooth ? (follows ? 2 * y - controlY : y) : oy + number();
                            float nx = ox + number(), ny = oy + number();
                            path.AddBezier(x, y, x + 2f / 3 * (qx - x), y + 2f / 3 * (qy - y), nx + 2f / 3 * (qx - nx), ny + 2f / 3 * (qy - ny), nx, ny);
                            controlX = qx;
                            controlY = qy;
                            x = nx;
                            y = ny;
                            break;
                        }
                    case 'A':
                        {
                            float rx = number(), ry = number(), rotation = number(), large = flag(), sweep = flag();
                            float nx = ox + number(), ny = oy + number();
                            Arc(path, x, y, rx, ry, rotation, large != 0, sweep != 0, nx, ny);
                            x = nx;
                            y = ny;
                            break;
                        }
                    case 'Z':
                        path.CloseFigure();
                        x = startX;
                        y = startY;
                        break;
                    default:
                        throw new FormatException("不支持的路径命令：" + command);
                }

                previous = command;
            }

            return path;
        }

        /// <summary>
        /// 把 SVG 椭圆弧转成贝塞尔曲线段。
        /// </summary>
        static void Arc(GraphicsPath path, float x1, float y1, float rx, float ry, float rotation, bool large, bool sweep, float x2, float y2)
        {
            if (rx == 0 || ry == 0)
            {
                path.AddLine(x1, y1, x2, y2);
                return;
            }

            double phi = rotation * Math.PI / 180, cos = Math.Cos(phi), sin = Math.Sin(phi);
            double dx = (x1 - x2) / 2.0, dy = (y1 - y2) / 2.0;
            double px = cos * dx + sin * dy, py = -sin * dx + cos * dy;
            double a = Math.Abs(rx), b = Math.Abs(ry), lambda = px * px / (a * a) + py * py / (b * b);
            if (lambda > 1)
            {
                a *= Math.Sqrt(lambda);
                b *= Math.Sqrt(lambda);
            }

            double top = a * a * b * b - a * a * py * py - b * b * px * px, bottom = a * a * py * py + b * b * px * px;
            double factor = (large == sweep ? -1 : 1) * Math.Sqrt(Math.Max(0, top / bottom));
            double cx1 = factor * a * py / b, cy1 = -factor * b * px / a;
            double cx = cos * cx1 - sin * cy1 + (x1 + x2) / 2.0, cy = sin * cx1 + cos * cy1 + (y1 + y2) / 2.0;
            double start = Math.Atan2((py - cy1) / b, (px - cx1) / a), end = Math.Atan2((-py - cy1) / b, (-px - cx1) / a), delta = end - start;
            if (sweep && delta < 0)
                delta += 2 * Math.PI;
            else if (!sweep && delta > 0)
                delta -= 2 * Math.PI;
            int segments = Math.Max(1, (int)Math.Ceiling(Math.Abs(delta) / (Math.PI / 2)));
            double step = delta / segments, t = 4.0 / 3 * Math.Tan(step / 4);
            Func<double, double, PointF> place = (ux, uy) => new PointF((float)(cos * a * ux - sin * b * uy + cx), (float)(sin * a * ux + cos * b * uy + cy));
            for (int s = 0; s < segments; s++)
            {
                double t1 = start + s * step, t2 = t1 + step;
                double c1 = Math.Cos(t1), s1 = Math.Sin(t1), c2 = Math.Cos(t2), s2 = Math.Sin(t2);
                path.AddBezier(place(c1, s1), place(c1 - t * s1, s1 + t * c1), place(c2 + t * s2, s2 - t * c2), place(c2, s2));
            }
        }
    }

    /// <summary>
    /// 课程专用图标：学科和兴趣课共 27 个，按课程名关键词自动匹配。
    /// </summary>
    public static class CourseIcons
    {
        public sealed class Icon
        {
            public string Name, Emoji;
            public string[] Keywords;
        }

        /// <summary>未匹配到关键词时使用的图标。</summary>
        public const string Other = "🎓";

        // 顺序影响匹配：物理实验算物理，钢琴课算器乐不算音乐，所以具体的排在前面。
        public static readonly Icon[] All =
        {
            New("语文", "✍", "语文", "阅读", "写作", "作文", "文学"),
            New("数学", "🧮", "数学", "高数", "微积分", "代数", "几何", "统计", "奥数"),
            New("英语", "🔤", "英语", "外语", "日语", "韩语", "法语", "德语", "口语", "雅思", "托福"),
            New("物理", "⚛", "物理", "力学", "电学"),
            New("化学", "🧪", "化学", "实验"),
            New("生物", "🧬", "生物", "医学", "解剖"),
            New("历史", "🏛", "历史", "文物", "考古"),
            New("地理", "🌍", "地理", "环境", "天文"),
            New("政治", "⚖", "政治", "法律", "思政", "道德", "马克思", "毛概"),
            New("器乐", "🎹", "钢琴", "吉他", "小提琴", "古筝", "二胡", "架子鼓", "尤克里里", "器乐"),
            New("音乐", "🎵", "音乐", "声乐", "合唱", "乐理"),
            New("书法", "🖌", "书法", "毛笔", "硬笔"),
            New("美术", "🎨", "美术", "绘画", "素描", "设计", "国画", "油画", "画"),
            New("舞蹈", "💃", "舞蹈", "芭蕾", "街舞", "舞"),
            New("戏剧", "🎭", "戏剧", "话剧", "表演", "主持"),
            New("摄影", "📷", "摄影", "摄像", "影视"),
            New("棋类", "♟", "围棋", "象棋", "五子棋", "棋"),
            New("手工", "✂", "手工", "陶艺", "编织", "折纸"),
            New("烹饪", "🍳", "烹饪", "烘焙", "厨艺"),
            New("游泳", "🏊", "游泳"),
            New("体育", "🏀", "体育", "篮球", "足球", "排球", "羽毛球", "乒乓", "网球", "田径", "健身", "跆拳道", "武术"),
            New("信息", "💻", "信息", "计算机", "编程", "程序", "人工智能", "机器人"),
            New("心理", "🧠", "心理", "哲学"),
            New("技术", "🔧", "技术", "工程", "劳动", "机械", "电路"),
            New("班会", "👥", "班会", "活动", "社团"),
            New("自习", "📚", "自习", "复习", "晚修", "早读"),
            New("其他", Other)
        };

        /// <summary>全部课程图标，用于选择器。</summary>
        public static string[] Emoji
        {
            /// <summary>读取图标列表。</summary>
            get
            {
                return All.Select(i => i.Emoji).ToArray();
            }
        }

        static Icon New(string name, string emoji, params string[] keywords)
        {
            return new Icon { Name = name, Emoji = emoji, Keywords = keywords };
        }

        /// <summary>
        /// 按课程名匹配图标。
        /// </summary>
        public static string For(string course)
        {
            string name = course ?? "";
            var match = All.FirstOrDefault(i => i.Keywords.Any(k => name.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0));
            return match == null ? Other : match.Emoji;
        }
    }
}
