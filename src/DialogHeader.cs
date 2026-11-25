using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{
    public static class DialogHeader
    {
        /// <summary>
        /// 调用 Windows 原生 GetWindowLong；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr h, int index);
        /// <summary>
        /// 调用 Windows 原生 SetWindowLong；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr h, int index, int value);
        /// <summary>
        /// 调用 Windows 原生 SetWindowPos；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int height, uint flags);
        /// <summary>
        /// 调用 Windows 原生 ReleaseCapture；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool ReleaseCapture();
        /// <summary>
        /// 调用 Windows 原生 SendMessage；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        /// <summary>
        /// 标题栏随内容比例和屏幕 DPI 一起变化，最小高度保证按钮可点。
        /// </summary>
        public static void Rescale(Form form, float ratio)
        {
            var bar = form.Controls.Find("dialog-title-bar", false).FirstOrDefault();
            if (bar == null)
                return;
            float s = ratio * UiDpi.Of(form) / 96f;
            int height = Math.Max((int)(34 * UiDpi.Of(form) / 96f), (int)(44 * s)), side = height - (int)(8 * s);
            var title = bar.Controls["dialog-title"];
            var close = bar.Controls["dialog-close"];
            var minimize = bar.Controls["dialog-minimize"];
            bar.Height = height;
            close.SetBounds(form.ClientSize.Width - form.Padding.Horizontal - side - (int)(8 * s), (height - side) / 2, side, side);
            minimize.SetBounds(close.Left - side - (int)(4 * s), (height - side) / 2, side, side);
            title.SetBounds((int)(14 * s), 0, Math.Max(40, minimize.Left - (int)(20 * s)), height);
            float factor = ratio * UiDpi.FontFactor(form);
            title.Font = UiFont.Shared(TitleFont, factor);
            close.Font = UiFont.Shared(CloseFont, factor);
            minimize.Font = UiFont.Shared(MinimizeFont, factor);
        }

        static readonly Font TitleFont = UiFont.New(12.5f, FontStyle.Bold), CloseFont = UiFont.New("Segoe UI", 16), MinimizeFont = UiFont.New("Segoe UI", 14);

        /// <summary>
        /// 为窗口安装统一外观和布局，保留关闭、最小化与拖动行为。
        /// </summary>
        public static DialogFrame Install(Form form, Size original)
        {
            int style = GetWindowLong(form.Handle, -16);
            SetWindowLong(form.Handle, -16, style & ~0x00C00000);
            var frame = new DialogFrame(form);
            SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0027);
            float dpi = UiDpi.Of(form) / 96f;
            form.ClientSize = new Size((int)(original.Width * dpi), (int)((original.Height + 46) * dpi));
            form.Padding = new Padding((int)(5 * dpi));
            form.MinimumSize = new Size(Math.Min(300, WindowPlacement.WorkArea.Width-24), Math.Min(180, WindowPlacement.WorkArea.Height-24));
            var bar = new Panel
            {
                Dock = DockStyle.Top,
                Width = original.Width - 8,
                Height = 44,
                BackColor = form.BackColor,
                Name = "dialog-title-bar"
            };
            var title = new Label
            {
                Text = form.Text,
                Font = UiFont.New(12.5f, FontStyle.Bold),
                ForeColor = Palette.Ink,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Name = "dialog-title",
                Bounds = new Rectangle(14, 0, Math.Max(40, original.Width - 116), 44),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };
            bar.Controls.Add(title);
            var close = new SoftButton
            {
                Name = "dialog-close",
                Text = "×",
                AccessibleName = "关闭窗口",
                Font = UiFont.New("Segoe UI", 16),
                Bounds = new Rectangle(original.Width - 50, 4, 38, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Skins.Button,
                ForeColor = Palette.Ink
            };
            close.Click += delegate
            {
                form.Close();
            };
            bar.Controls.Add(close);
            var minimize = new SoftButton
            {
                Name = "dialog-minimize",
                Text = "−",
                AccessibleName = "最小化窗口",
                Font = UiFont.New("Segoe UI", 14),
                Bounds = new Rectangle(original.Width - 92, 4, 38, 36),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = form.BackColor,
                ForeColor = Palette.Ink
            };
            minimize.Click += delegate
            {
                form.WindowState = FormWindowState.Minimized;
            };
            bar.Controls.Add(minimize);
            MouseEventHandler drag = delegate (object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(form.Handle, 0xA1, new IntPtr(2), IntPtr.Zero);
                }
            };
            bar.MouseDown += drag;
            title.MouseDown += drag;
            form.Controls.Add(bar);
            bar.SendToBack();
            form.Paint += delegate (object sender, PaintEventArgs e)
            {
                using (var pen = new Pen(Skins.Line, 1))
                    e.Graphics.DrawRectangle(pen, 0, 0, form.ClientSize.Width - 1, form.ClientSize.Height - 1);
            };
            // 缩放时重画整条边框，否则旧线条留在窗口中间，四边颜色对不齐。
            form.Resize += delegate
            {
                form.Invalidate(false);
            };
            return frame;
        }
    }

    public sealed class StartupContext : ApplicationContext
    {
        readonly Control dispatcher = new Control();
        Form loading;
        Controller controller;
        Timer delay;
        bool stopped;
        /// <summary>
        /// 初始化窗口标题和启动准备的StartupContext，准备状态、布局与必要的事件绑定。
        /// </summary>
        public StartupContext(bool startup)
        {
            IntPtr h = dispatcher.Handle;
            delay = new Timer
            {
                Interval = 800
            };
            delay.Tick += delegate
            {
                delay.Stop();
                if (stopped || controller != null)
                    return;
                loading = new Form();
                UI.Style(loading, "正在打开日历", 360, 120);
                var text = UI.Label(loading, "正在读取本地记录…", 24, 30, 312);
                text.TextAlign = ContentAlignment.MiddleCenter;
                text.Height = 48;
                loading.FormClosed += delegate
                {
                    if (controller == null)
                    {
                        stopped = true;
                        ExitThread();
                    }
                };
                loading.Show();
            };
            delay.Start();
            dispatcher.BeginInvoke((Action)(async delegate
            {
                try
                {
                    State state = await System.Threading.Tasks.Task.Run(delegate
                    {
                        Data.StartFreshVersion19(Data.Root, System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PaperDays-v0.19"));
                        var value = Data.Read();
                        StartupTiming.Mark("Local data loaded");
                        DailyArchive.Enable(value, DateTime.Today);
                        Data.Write(value);
                        StartupTiming.Mark("Archive prepared off UI thread");
                        return value;
                    });
                    if (stopped)
                        return;
                    controller = new Controller(state, startup, false, true);
                    controller.ThreadExit += delegate
                    {
                        ExitThread();
                    };
                    delay.Stop();
                    if (loading != null)
                    {
                        loading.Hide();
                        loading.Dispose();
                        loading = null;
                    }
                }
                catch (Exception ex)
                {
                    delay.Stop();
                    if (loading != null)
                    {
                        loading.Hide();
                        loading.Dispose();
                    }

                    MessageBox.Show("未能读取日历，原数据没有被覆盖：\n" + ex.Message, "启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    ExitThread();
                }
            }));
        }

        /// <summary>
        /// 释放控件、计时器、图形和系统资源，并调用基类清理。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                stopped = true;
                delay.Dispose();
                dispatcher.Dispose();
                if (loading != null)
                    loading.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}