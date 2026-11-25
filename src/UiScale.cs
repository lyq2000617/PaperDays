using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{
    /// <summary>
    /// 统一创建字体：缺少首选字体时换成系统已有字体，避免不同版本 Windows 上出现方框字。
    /// </summary>
    public static class UiFont
    {
        const string Main = "Microsoft YaHei UI";
        static readonly string[] ChineseFallback = { "Microsoft YaHei UI", "Microsoft YaHei", "DengXian", "SimHei", "SimSun" };
        static readonly string[] EmojiFallback = { "Segoe UI Emoji", "Segoe UI Symbol", "Segoe UI" };
        static readonly Dictionary<string, string> resolved = new Dictionary<string, string>();
        static readonly Dictionary<string, Font> cache = new Dictionary<string, Font>();
        static HashSet<string> installed;
        /// <summary>界面绘制时的画布缩放，用于把过小的文字抬到最小可读字号。</summary>
        public static float SceneScale = 1;
        /// <summary>主界面文字的最小实际像素；随系统缩放比例增加。</summary>
        public static float SceneMinimumPixels = 0;

        /// <summary>测试用：模拟某台电脑只安装了部分字体。</summary>
        internal static void UseInstalled(IEnumerable<string> names)
        {
            installed = names == null ? null : new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
            resolved.Clear();
        }

        /// <summary>
        /// 取得实际可用的字体族；优先使用调用方要求的字体。
        /// </summary>
        public static string Family(string wanted)
        {
            if (wanted == Main) wanted = L.Language == "ko" ? "Malgun Gothic" : L.Language == "ja" ? "Yu Gothic UI" : L.Language == "zh-TW" ? "Microsoft JhengHei UI" : Main;
            string found;
            if (resolved.TryGetValue(wanted, out found))
                return found;
            var order = new List<string> { wanted };
            order.AddRange(wanted.IndexOf("Emoji", StringComparison.OrdinalIgnoreCase) >= 0 || wanted.IndexOf("Symbol", StringComparison.OrdinalIgnoreCase) >= 0 ? EmojiFallback : ChineseFallback);
            found = order.FirstOrDefault(Installed) ?? SystemFonts.MessageBoxFont.FontFamily.Name;
            resolved[wanted] = found;
            return found;
        }

        /// <summary>
        /// 只检查单个字体是否存在；枚举全部字体在字体多的电脑上要等很久。
        /// </summary>
        static bool Installed(string name)
        {
            if (installed != null)
                return installed.Contains(name);
            try
            {
                using (new FontFamily(name))
                    return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        /// <summary>创建中文界面字体。</summary>
        public static Font New(float size, FontStyle style = FontStyle.Regular)
        {
            return New(L.Language == "ko" ? "Malgun Gothic" : L.Language == "ja" ? "Yu Gothic UI" : L.Language == "zh-TW" ? "Microsoft JhengHei UI" : Main, size, style);
        }

        /// <summary>创建指定字体族的界面字体。</summary>
        public static Font New(string family, float size, FontStyle style = FontStyle.Regular)
        {
            return new Font(Family(family), size, style);
        }

        /// <summary>
        /// 创建主界面字体。使用像素单位，避免 Windows 缩放比例把文字放大到超出画布布局。
        /// </summary>
        public static Font Scene(float size, FontStyle style = FontStyle.Regular)
        {
            return Scene(Main, size, style);
        }

        /// <summary>
        /// 创建指定字体族的主界面字体，并保证缩放后不低于最小可读像素。
        /// </summary>
        public static Font Scene(string family, float size, FontStyle style = FontStyle.Regular)
        {
            float pixels = size * 96f / 72f;
            if (SceneMinimumPixels > 0 && SceneScale > 0)
                pixels = Math.Max(pixels, SceneMinimumPixels / SceneScale);
            return new Font(Family(family), pixels, style, GraphicsUnit.Pixel);
        }

        /// <summary>
        /// 取得缓存字体。窗口缩放时控件共用，不随拖动不断创建系统字体对象。
        /// </summary>
        public static Font Shared(Font basis, float factor)
        {
            float size = (float)Math.Round(basis.Size * factor * 4) / 4f;
            string key = basis.FontFamily.Name + "|" + size + "|" + (int)basis.Style + "|" + (int)basis.Unit;
            Font font;
            if (!cache.TryGetValue(key, out font))
            {
                font = new Font(basis.FontFamily, Math.Max(1, size), basis.Style, basis.Unit);
                cache[key] = font;
            }

            return font;
        }
    }

    /// <summary>
    /// 读取窗口实际 DPI。旧系统没有逐屏接口时回落到系统 DPI。
    /// </summary>
    public static class UiDpi
    {
        [DllImport("user32.dll")]
        static extern uint GetDpiForWindow(IntPtr window);
        static int system;
        /// <summary>测试用：模拟 125%、150%、200% 等缩放比例。</summary>
        public static int Simulated;

        /// <summary>进程启动时 Windows 换算字号使用的 DPI。</summary>
        public static int System
        {
            /// <summary>读取系统 DPI。</summary>
            get
            {
                if (system == 0)
                    using (var g = Graphics.FromHwnd(IntPtr.Zero))
                        system = Math.Max(96, (int)Math.Round(g.DpiX));
                return system;
            }
        }

        /// <summary>
        /// 取得控件所在屏幕的 DPI。
        /// </summary>
        public static int Of(Control control)
        {
            if (Simulated > 0)
                return Simulated;
            try
            {
                if (control != null && control.IsHandleCreated)
                {
                    uint value = GetDpiForWindow(control.Handle);
                    if (value > 0)
                        return (int)value;
                }
            }
            catch (EntryPointNotFoundException)
            {
            }

            return System;
        }

        /// <summary>
        /// 字号相对系统 DPI 还要乘的比例。模拟 DPI 时字号同样放大。
        /// </summary>
        public static float FontFactor(Control control)
        {
            return Of(control) / (float)(Simulated > 0 ? 96 : System);
        }
    }

    /// <summary>
    /// 窗口外框：去掉系统粗边框，窗口四周只留一条皮肤色细线，并保留拖动边缘缩放和跨屏 DPI 重排。
    /// </summary>
    public sealed class DialogFrame : NativeWindow
    {
        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct Margins
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
        [DllImport("dwmapi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr window, ref Margins margins);
        const int CalcSize = 0x83, HitTest = 0x84, NcActivate = 0x86, DpiMessage = 0x02E0;
        readonly Form form;
        /// <summary>窗口跨到缩放比例不同的屏幕后执行。</summary>
        public Action DpiChanged;

        /// <summary>
        /// 绑定窗口句柄，并让系统阴影和 Windows 11 圆角继续生效。
        /// </summary>
        public DialogFrame(Form form)
        {
            this.form = form;
            AssignHandle(form.Handle);
            form.HandleDestroyed += delegate
            {
                ReleaseHandle();
            };
            try
            {
                var shadow = new Margins { Bottom = 1 };
                DwmExtendFrameIntoClientArea(Handle, ref shadow);
                int round = 2, color = ColorTranslator.ToWin32(Skins.Line);
                DwmSetWindowAttribute(Handle, 33, ref round, 4);
                DwmSetWindowAttribute(Handle, 34, ref color, 4);
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }

        /// <summary>
        /// 判断鼠标在窗口哪条边上，返回 Windows 的命中代码；不在边上时返回 0。
        /// </summary>
        public static int Edge(Point p, Size size, int grip)
        {
            bool left = p.X < grip, right = p.X >= size.Width - grip, top = p.Y < grip, bottom = p.Y >= size.Height - grip;
            if (top)
                return left ? 13 : right ? 14 : 12;
            if (bottom)
                return left ? 16 : right ? 17 : 15;
            return left ? 10 : right ? 11 : 0;
        }

        /// <summary>
        /// 处理外框相关消息，其余消息交给窗口原有逻辑。
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            bool normal = form.WindowState == FormWindowState.Normal;
            if (m.Msg == CalcSize && m.WParam != IntPtr.Zero && normal)
            {
                m.Result = IntPtr.Zero;
                return;
            }

            if (m.Msg == HitTest && normal)
            {
                long value = m.LParam.ToInt64();
                var point = form.PointToClient(new Point(unchecked((short)(value & 65535)), unchecked((short)((value >> 16) & 65535))));
                int edge = Edge(point, form.ClientSize, Math.Max(4, form.Padding.Left));
                if (edge != 0)
                {
                    m.Result = new IntPtr(edge);
                    return;
                }
            }

            if (m.Msg == NcActivate)
                m.LParam = new IntPtr(-1);
            base.WndProc(ref m);
            if (m.Msg != DpiMessage)
                return;
            var r = (Rect)Marshal.PtrToStructure(m.LParam, typeof(Rect));
            SetWindowPos(Handle, IntPtr.Zero, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top, 0x0014);
            if (DpiChanged != null)
                DpiChanged();
        }
    }

    /// <summary>
    /// 为自带布局的窗口统一缩放字体和表格行高；布局本身仍由窗口负责。
    /// </summary>
    public sealed class ResponsiveScale
    {
        public const float Minimum = .72f, Maximum = 1.25f;
        readonly Form form;
        readonly Control body;
        readonly Size design;
        readonly Dictionary<Control, Font> fonts = new Dictionary<Control, Font>();
        readonly Dictionary<DataGridView, int> rows = new Dictionary<DataGridView, int>();
        float applied = -1;
        bool busy;
        /// <summary>当前内容比例（不含系统 DPI）。</summary>
        public float Ratio = 1;
        /// <summary>内容比例乘以屏幕 DPI，供窗口自带布局换算像素。</summary>
        public float Layout = 1;
        /// <summary>比例变化后由窗口重新排版。</summary>
        public Action Changed;

        /// <summary>
        /// 记录窗口原始设计尺寸，并在窗口尺寸变化时同步字体。
        /// </summary>
        public ResponsiveScale(Form form, Control body, Size design)
        {
            this.form = form;
            this.body = body;
            this.design = design;
            Collect(body);
            body.SizeChanged += delegate
            {
                Apply();
            };
        }

        /// <summary>按内容比例和屏幕 DPI 计算窗口比例，取 0.04 的档位避免拖动时抖动。</summary>
        public static float Quantize(float value)
        {
            float clamped = Math.Max(Minimum, Math.Min(Maximum, value));
            return (float)Math.Round(clamped / .04f) * .04f;
        }

        /// <summary>
        /// 记录控件的基准字体；新增的控件在下次缩放时纳入。
        /// </summary>
        void Collect(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (!fonts.ContainsKey(c))
                    fonts[c] = c.Font;
                var grid = c as DataGridView;
                if (grid != null && !rows.ContainsKey(grid))
                    rows[grid] = grid.RowTemplate.Height;
                Collect(c);
            }
        }

        /// <summary>
        /// 根据当前窗口尺寸计算比例并更新字体。
        /// </summary>
        public void Apply(bool force = false)
        {
            if (busy || form.IsDisposed || body.Width < 1 || body.Height < 1)
                return;
            busy = true;
            try
            {
                Collect(body);
                float dpi = UiDpi.Of(form) / 96f;
                float ratio = 1;
                Ratio = ratio;
                Layout = ratio * dpi;
                float factor = ratio * UiDpi.FontFactor(form);
                if (force || Math.Abs(factor - applied) > .001f)
                {
                    applied = factor;
                    foreach (var pair in fonts.ToArray())
                    {
                        if (pair.Key.IsDisposed)
                        {
                            fonts.Remove(pair.Key);
                            continue;
                        }

                        pair.Key.Font = UiFont.Shared(pair.Value, factor);
                    }

                    foreach (var pair in rows)
                        if (!pair.Key.IsDisposed)
                        {
                            pair.Key.RowTemplate.Height = (int)(pair.Value * Layout);
                            foreach (DataGridViewRow row in pair.Key.Rows)
                                row.Height = pair.Key.RowTemplate.Height;
                        }

                    DialogHeader.Rescale(form, ratio);
                }

                if (Changed != null)
                    Changed();
            }
            finally
            {
                busy = false;
            }
        }
    }
}
