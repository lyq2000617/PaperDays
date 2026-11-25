using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace PaperDays
{
    public static class WindowPlacement
    {
        public static Func<Rectangle> Target;
        public static Rectangle WorkArea
        {
            /// <summary>
            /// 读取WorkArea属性，供WindowPlacement使用。
            /// </summary>
            get
            {
                return Target == null ? Screen.PrimaryScreen.WorkingArea : Target();
            }
        }

        /// <summary>
        /// 将窗口约束在目标显示器工作区内。
        /// </summary>
        public static Rectangle Fit(Size size, Rectangle area)
        {
            int width = Math.Min(size.Width, Math.Max(100, area.Width - 24)), height = Math.Min(size.Height, Math.Max(100, area.Height - 24));
            return new Rectangle(area.Left + (area.Width - width) / 2, area.Top + (area.Height - height) / 2, width, height);
        }

        /// <summary>
        /// 设置窗口位置，避免超出当前显示屏。
        /// </summary>
        public static void Prepare(Form form)
        {
            form.StartPosition = FormStartPosition.Manual;
            form.Bounds = Fit(form.Size, WorkArea);
        }

        delegate bool EnumProc(IntPtr window, IntPtr state);
        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {
            public int L, T, R, B;
        }

        /// <summary>
        /// 调用 Windows 原生 GetCurrentThreadId；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();
        /// <summary>
        /// 调用 Windows 原生 EnumThreadWindows；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool EnumThreadWindows(uint thread, EnumProc callback, IntPtr state);
        /// <summary>
        /// 调用 Windows 原生 GetClassName；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern int GetClassName(IntPtr window, StringBuilder name, int length);
        /// <summary>
        /// 调用 Windows 原生 IsWindowVisible；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr window);
        /// <summary>
        /// 调用 Windows 原生 GetWindowRect；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr window, out Rect rect);
        /// <summary>
        /// 调用 Windows 原生 SetWindowPos；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        /// <summary>
        /// 显示窗口或对话框，并返回用户操作结果。
        /// </summary>
        public static DialogResult Show(CommonDialog dialog, IWin32Window owner = null)
        {
            using (var anchor = new Form
            {
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Bounds = Fit(new Size(10, 10), WorkArea)
            }

            )
            using (var timer = new Timer
            {
                Interval = 50
            }

            )
            {
                IntPtr handle = anchor.Handle;
                uint thread = GetCurrentThreadId();
                var seen = new HashSet<IntPtr>();
                timer.Tick += delegate
                {
                    EnumThreadWindows(thread, delegate (IntPtr window, IntPtr unused)
                    {
                        if (seen.Contains(window) || !IsWindowVisible(window))
                            return true;
                        var cls = new StringBuilder(80);
                        GetClassName(window, cls, 80);
                        if (cls.ToString() != "#32770")
                            return true;
                        Rect rect;
                        if (GetWindowRect(window, out rect))
                        {
                            var bounds = Fit(new Size(rect.R - rect.L, rect.B - rect.T), WorkArea);
                            SetWindowPos(window, IntPtr.Zero, bounds.X, bounds.Y, bounds.Width, bounds.Height, 0x0014);
                            seen.Add(window);
                        }

                        return true;
                    }, IntPtr.Zero);
                };
                timer.Start();
                try
                {
                    return dialog.ShowDialog(owner ?? anchor);
                }
                finally
                {
                    timer.Stop();
                }
            }
        }
    }
}