using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{
    // Store explicit black character formatting in RichEdit, including the insertion point.
    // This opaque, borderless host stays exactly over the calendar's inline input.
    // Its native color messages are handled locally rather than by the Explorer parent.
    public class TodoInputHost : Form
    {
        /// <summary>
        /// 调用 Windows 原生 SetTextColor；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("gdi32.dll")]
        static extern uint SetTextColor(IntPtr dc, uint color);
        /// <summary>
        /// 调用 Windows 原生 SetBkColor；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("gdi32.dll")]
        static extern uint SetBkColor(IntPtr dc, uint color);
        /// <summary>
        /// 调用 Windows 原生 CreateSolidBrush；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("gdi32.dll")]
        static extern IntPtr CreateSolidBrush(uint color);
        /// <summary>
        /// 调用 Windows 原生 DeleteObject；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("gdi32.dll")]
        static extern bool DeleteObject(IntPtr h);
        IntPtr inputBackground;
        protected override CreateParams CreateParams
        {
            /// <summary>
            /// 读取CreateParams属性，供行内输入显示使用。
            /// </summary>
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x80;
                return cp;
            }
        }

        /// <summary>
        /// 处理当前控件需要的 Windows 消息，其余消息交给基类。
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x133 || m.Msg == 0x138)
            {
                SetTextColor(m.WParam, (uint)ColorTranslator.ToWin32(ReadableTodoInput.TextInk));
                SetBkColor(m.WParam, (uint)ColorTranslator.ToWin32(BackColor));
                if (inputBackground == IntPtr.Zero)
                    inputBackground = CreateSolidBrush((uint)ColorTranslator.ToWin32(BackColor));
                m.Result = inputBackground;
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// 释放控件、计时器、图形和系统资源，并调用基类清理。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && inputBackground != IntPtr.Zero)
            {
                DeleteObject(inputBackground);
                inputBackground = IntPtr.Zero;
            }

            base.Dispose(disposing);
        }
    }

    public class ReadableTodoInput : RichTextBox
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct CharFormat
        {
            public uint Size, Mask, Effects;
            public int Height, Offset;
            public uint Color;
            public byte Charset, Pitch;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string Face;
        }

        /// <summary>
        /// 调用 Windows 原生 SendMessage；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, ref CharFormat value);
        /// <summary>
        /// 调用 Windows 原生 SetWindowTheme；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        static extern int SetWindowTheme(IntPtr h, string app, string ids);
        bool formatting;
        public static Color TextInk
        {
            /// <summary>
            /// 读取TextInk属性，供行内输入显示使用。
            /// </summary>
            get
            {
                return Skins.Dark ? Palette.Ink : Color.Black;
            }
        }

        public bool AcceptsReturn
        {
            /// <summary>
            /// 读取AcceptsReturn属性，供行内输入显示使用。
            /// </summary>
            get
            {
                return true;
            }

            /// <summary>
            /// 更新AcceptsReturn属性，供行内输入显示使用。
            /// </summary>
            set
            {
            }
        }

        /// <summary>
        /// 初始化行内输入显示的ReadableTodoInput，准备状态、布局与必要的事件绑定。
        /// </summary>
        public ReadableTodoInput()
        {
            ForeColor = TextInk;
            BackColor = Palette.Paper;
            DetectUrls = false;
            EnableAutoDragDrop = false;
            RichTextShortcutsEnabled = false;
        }

        /// <summary>
        /// 设置输入文字和插入点的可读颜色，兼容明暗皮肤。
        /// </summary>
        internal void EnsureBlack()
        {
            if (!IsHandleCreated || formatting)
                return;
            formatting = true;
            try
            {
                var format = new CharFormat
                {
                    Size = (uint)Marshal.SizeOf(typeof(CharFormat)),
                    Mask = 0x40000000,
                    Color = (uint)ColorTranslator.ToWin32(TextInk),
                    Face = ""
                };
                SendMessage(Handle, 0x444, new IntPtr(4), ref format);
                SendMessage(Handle, 0x444, new IntPtr(1), ref format);
            }
            finally
            {
                formatting = false;
            }
        }

        /// <summary>
        /// 响应HandleCreated事件，同步行内输入显示状态并保留基类行为。
        /// </summary>
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                SetWindowTheme(Handle, "", "");
            }
            catch (DllNotFoundException)
            {
            }

            EnsureBlack();
        }

        /// <summary>
        /// 响应TextChanged事件，同步行内输入显示状态并保留基类行为。
        /// </summary>
        protected override void OnTextChanged(EventArgs e)
        {
            EnsureBlack();
            base.OnTextChanged(e);
        }

        /// <summary>
        /// 响应SelectionChanged事件，同步行内输入显示状态并保留基类行为。
        /// </summary>
        protected override void OnSelectionChanged(EventArgs e)
        {
            EnsureBlack();
            base.OnSelectionChanged(e);
        }
    }

    public class SmoothCourseGrid : DataGridView
    {
        /// <summary>
        /// 响应CellPainting事件，同步行内输入显示状态并保留基类行为。
        /// </summary>
        protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
        {
            base.OnCellPainting(e);
            if (e.Handled || e.RowIndex < 0 || e.ColumnIndex < 0 || !(Columns[e.ColumnIndex] is DataGridViewButtonColumn))
                return;
            e.PaintBackground(e.ClipBounds, true);
            var bounds = new RectangleF(e.CellBounds.X + 7, e.CellBounds.Y + 5, e.CellBounds.Width - 14, e.CellBounds.Height - 10);
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;
            var old = e.Graphics.SmoothingMode;
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = Chrome.Round(bounds, 8))
            using (var brush = new SolidBrush(Color.FromArgb(244, 229, 224)))
            {
                e.Graphics.FillPath(brush, path);
                if (CurrentCell == Rows[e.RowIndex].Cells[e.ColumnIndex] && Focused)
                    using (var pen = new Pen(Color.FromArgb(166, 104, 90)))
                        e.Graphics.DrawPath(pen, path);
            }

            TextRenderer.DrawText(e.Graphics, Convert.ToString(e.FormattedValue), Font, Rectangle.Round(bounds), Color.FromArgb(128, 67, 55), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.Graphics.SmoothingMode = old;
            e.Handled = true;
        }

        /// <summary>
        /// 初始化行内输入显示的SmoothCourseGrid，准备状态、布局与必要的事件绑定。
        /// </summary>
        public SmoothCourseGrid()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }

    public static class ShutdownFlow
    {
        // Each operation is independent: a save or legacy-restoration failure must
        // not prevent restoration of the selected monitor or closing other windows.
        /// <summary>
        /// 执行任务并返回结果，异常交由调用方处理。
        /// </summary>
        public static List<string> Run(params Action[] steps)
        {
            var errors = new List<string>();
            foreach (var step in steps)
                try
                {
                    step();
                }
                catch (Exception ex)
                {
                    errors.Add(ex.Message);
                }

            return errors;
        }
    }
}