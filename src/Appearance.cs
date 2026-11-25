using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{
    public class SoftButton : Button
    {
        bool hover, down;
        /// <summary>
        /// 初始化Appearance的SoftButton，准备状态、布局与必要的事件绑定。
        /// </summary>
        public SoftButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Default;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        /// <summary>
        /// 响应MouseEnter事件，同步Appearance状态并保留基类行为。
        /// </summary>
        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        /// <summary>
        /// 响应MouseLeave事件，同步Appearance状态并保留基类行为。
        /// </summary>
        protected override void OnMouseLeave(EventArgs e)
        {
            hover = down = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <summary>
        /// 响应MouseDown事件，同步Appearance状态并保留基类行为。
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            down = true;
            Invalidate();
            base.OnMouseDown(e);
        }

        /// <summary>
        /// 响应MouseUp事件，同步Appearance状态并保留基类行为。
        /// </summary>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            down = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        /// <summary>
        /// 绘制控件当前状态，保持主题配色和内容可见。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent is PaperField ? Skins.Field : Parent == null ? Palette.Paper : Parent.BackColor);
            Color bg = BackColor;
            if (hover || down)
                bg = ControlPaint.Dark(bg, down ? .08f : .025f);
            using (var path = Chrome.Round(new RectangleF(1, 1, Width - 3, Height - 3), 10))
            using (var brush = new SolidBrush(bg))
            {
                e.Graphics.FillPath(brush, path);
                if (Focused)
                    using (var pen = new Pen(Skins.Accent, 1.4f))
                        e.Graphics.DrawPath(pen, path);
            }

            int pad = Text.Length <= 3 ? 1 : 8;
            TextRenderer.DrawText(e.Graphics, Text, Font, new Rectangle(Padding.Left + pad, 0, Width - Padding.Horizontal - pad * 2, Height), Enabled ? ForeColor : Palette.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis | (TextAlign == ContentAlignment.MiddleLeft ? TextFormatFlags.Left : TextFormatFlags.HorizontalCenter));
        }
    }

    public static class Chrome
    {
        /// <summary>
        /// 创建圆角路径，调用方负责释放图形资源。
        /// </summary>
        public static GraphicsPath Round(RectangleF r, float radius)
        {
            float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>
        /// 为输入控件安装圆角外壳并统一明暗主题文字颜色。
        /// </summary>
        public static void SoftenFields(Control parent)
        {
            foreach (Control c in parent.Controls.Cast<Control>().ToArray())
            {
                var grid = c as DataGridView;
                if (grid != null)
                {
                    grid.BackgroundColor = Palette.Paper;
                    grid.DefaultCellStyle.BackColor = Palette.Paper;
                    grid.DefaultCellStyle.ForeColor = Palette.Ink;
                    grid.DefaultCellStyle.SelectionBackColor = Skins.Button;
                    grid.DefaultCellStyle.SelectionForeColor = Palette.Ink;
                    grid.EnableHeadersVisualStyles = false;
                    grid.ColumnHeadersDefaultCellStyle.BackColor = Skins.Field;
                    grid.ColumnHeadersDefaultCellStyle.ForeColor = Palette.Ink;
                    grid.GridColor = Skins.Line;
                }

                if (c is ListBox)
                {
                    c.BackColor = Palette.Paper;
                    c.ForeColor = Palette.Ink;
                }

                if (c is FieldShell || c is DataGridView || c is PaperField)
                    continue;
                var text = c as TextBox;
                var number = c as NumericUpDown;
                var combo = c as ComboBox;
                var date = c as DateTimePicker;
                if (text != null && !text.ReadOnly || number != null || combo != null || date != null)
                {
                    int index = parent.Controls.GetChildIndex(c);
                    var shell = new FieldShell
                    {
                        Margin = c.Margin,
                        Anchor = c.Anchor,
                        Bounds = new Rectangle(c.Left - 4, c.Top - 5, c.Width + 8, c.Height + 10),
                        TabIndex = c.TabIndex
                    };
                    parent.Controls.Remove(c);
                    parent.Controls.Add(shell);
                    shell.Controls.Add(c);
                    c.Location = new Point(4, 5);
                    c.TabIndex = 0;
                    c.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                    if (text != null)
                    {
                        text.BorderStyle = BorderStyle.None;
                        text.BackColor = Skins.Field;
                        text.ForeColor = Palette.Ink;
                        if (!text.Multiline) {
                            Action center = delegate { text.SetBounds(9, Math.Max(2, (shell.ClientSize.Height-text.PreferredHeight)/2), Math.Max(12,shell.ClientSize.Width-18),text.PreferredHeight); };
                            shell.SizeChanged += delegate { center(); };
                            text.FontChanged += delegate { center(); };
                            center();
                        }
                    }
                    else if (number != null)
                    {
                        number.BorderStyle = BorderStyle.None;
                        number.BackColor = Skins.Field;
                        number.ForeColor = Palette.Ink;
                    }
                    else
                    {
                        c.BackColor = Skins.Field;
                        c.ForeColor = Palette.Ink;
                        if (combo != null)
                            combo.FlatStyle = FlatStyle.Flat;
                        RoundNativeField(c);
                        c.SizeChanged += delegate
                        {
                            RoundNativeField(c);
                        };
                    }

                    if (parent is FlowLayoutPanel)
                        parent.Controls.SetChildIndex(shell, index);
                    else
                        shell.BringToFront();
                }
                else
                {
                    SoftenFields(c);
                    // 可调整布局的容器不固定裁切区域，避免尺寸变化后内容被旧区域截断。
                    if (c is Panel && c.Region != null) { var old=c.Region; c.Region=null; old.Dispose(); }
                }
            }
        }

        /// <summary>
        /// 处理Appearance中的 RoundNativeField 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static void RoundNativeField(Control c)
        {
            if (c.Width < 1 || c.Height < 1)
                return;
            var old = c.Region;
            using (var path = Round(new RectangleF(0, 0, c.Width, c.Height), 6))
                c.Region = new Region(path);
            if (old != null)
                old.Dispose();
        }

        /// <summary>
        /// 为窗口安装统一外观和布局，保留关闭、最小化与拖动行为。
        /// </summary>
        public static void Install(Form f)
        {
            if (f.Tag as string == "chrome-ready")
                return;
            f.Tag = "chrome-ready";
            f.SuspendLayout();
            SoftenFields(f);
            var original = f.ClientSize;
            var children = f.Controls.Cast<Control>().ToArray();
            var viewport=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=f.BackColor,Name="dialog-scroll-viewport"};
            var body=new Panel{Size=original,BackColor=f.BackColor,Name="dialog-canvas"};
            viewport.SuspendLayout();body.SuspendLayout();
            viewport.Controls.Add(body);
            foreach (var c in children)
            {
                f.Controls.Remove(c);
                body.Controls.Add(c);
            }

            f.Controls.Add(viewport);
            var frame = DialogHeader.Install(f, original);
            f.Size=new Size(Math.Min(f.Width,700),Math.Min(f.Height,680));
            WindowPlacement.Prepare(f);
            body.ResumeLayout(false);viewport.ResumeLayout(false);f.ResumeLayout(false);
            f.PerformLayout();
            // 内容区在标题栏和窗口内边距以内，比例以这个区域的设计尺寸为准。
            var design = new Size(Math.Max(1, original.Width - 8), Math.Max(1, original.Height - 6));
            var responsive = f as IScaledDialog;
            Action fitCanvas=()=>{if(body.IsDisposed)return;float dpi=UiDpi.Of(f)/96f;var wanted=new Size(Math.Max((int)(design.Width*dpi),viewport.ClientSize.Width-SystemInformation.VerticalScrollBarWidth),Math.Max((int)(design.Height*dpi),viewport.ClientSize.Height-SystemInformation.HorizontalScrollBarHeight));if(body.Size!=wanted)body.Size=wanted;};
            viewport.SizeChanged+=delegate{fitCanvas();};fitCanvas();
            f.Shown+=delegate{viewport.AutoScrollPosition=Point.Empty;};
            if (f is IResponsiveDialog)
            {
                var scale = new ResponsiveScale(f, body, design);
                if (responsive != null)
                    responsive.Attach(scale);
                frame.DpiChanged = delegate { scale.Apply(true); };
                scale.Apply(true);
            }
            else
            {
                var fluid = FluidForm.Attach(body, f, design);
                frame.DpiChanged = delegate { fluid.Arrange(true); };
            }
        }
    }

    public class FieldShell : Panel
    {
        /// <summary>布局或 DPI 缩放结束后按实际字体高度居中单行文本，避免子控件缩放覆盖偏移。</summary>
        protected override void OnLayout(LayoutEventArgs e) {
            base.OnLayout(e);
            foreach(Control child in Controls) {
                var text=child as TextBox;
                if(text!=null && !text.Multiline) {
                    var bounds=new Rectangle(9, Math.Max(2,(ClientSize.Height-text.PreferredHeight)/2),Math.Max(12,ClientSize.Width-18),text.PreferredHeight);
                    if(text.Bounds!=bounds)text.Bounds=bounds;
                }
                else if(text!=null) {
                    var bounds=new Rectangle(6,5,Math.Max(12,ClientSize.Width-12),Math.Max(12,ClientSize.Height-10));
                    if(text.Bounds!=bounds)text.Bounds=bounds;
                }
            }
        }

        /// <summary>
        /// 初始化Appearance的FieldShell，准备状态、布局与必要的事件绑定。
        /// </summary>
        public FieldShell()
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
            using (var path = Chrome.Round(new RectangleF(0, 0, Width - 1, Height - 1), 11))
            using (var fill = new SolidBrush(Skins.Field))
            {
                e.Graphics.FillPath(fill, path);
                using (var pen = new Pen(Skins.Line, 1))
                    e.Graphics.DrawPath(pen, path);
            }
        }
    }

    public static class Notice
    {
        /// <summary>
        /// 显示Appearance中的 ShowInfo 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static async void ShowInfo(Form f)
        {
            using (f)
                await IndependentWindows.Show(f);
        }

        /// <summary>
        /// 显示窗口或对话框，并返回用户操作结果。
        /// </summary>
        public static DialogResult Show(string text)
        {
            return Show(null, text, "提示", MessageBoxButtons.OK);
        }

        /// <summary>
        /// 显示窗口或对话框，并返回用户操作结果。
        /// </summary>
        public static DialogResult Show(string text, string title)
        {
            return Show(null, text, title, MessageBoxButtons.OK);
        }

        /// <summary>
        /// 显示窗口或对话框，并返回用户操作结果。
        /// </summary>
        public static DialogResult Show(string text, string title, MessageBoxButtons buttons)
        {
            return Show(null, text, title, buttons);
        }

        /// <summary>
        /// 显示窗口或对话框，并返回用户操作结果。
        /// </summary>
        public static DialogResult Show(IWin32Window owner, string text)
        {
            return Show(owner, text, "提示", MessageBoxButtons.OK);
        }

        /// <summary>
        /// 显示窗口或对话框，并返回用户操作结果。
        /// </summary>
        public static DialogResult Show(IWin32Window owner, string text, string title)
        {
            return Show(owner, text, title, MessageBoxButtons.OK);
        }

        /// <summary>
        /// 显示窗口或对话框，并返回用户操作结果。
        /// </summary>
        public static DialogResult Show(IWin32Window owner, string text, string title, MessageBoxButtons buttons)
        {
            var f = new NoticeDialog(text, title, buttons);
            if (buttons != MessageBoxButtons.YesNo && Application.MessageLoop)
            {
                ShowInfo(f);
                return DialogResult.OK;
            }

            using (f)
                return owner == null ? f.ShowDialog() : f.ShowDialog(owner);
        }
    }

    public class ResponsiveLayout
    {
        public float Scale;
        public PointF Origin;
        public bool Portrait;
        public RectangleF Calendar, Plans, Sidebar;
        /// <summary>屏幕比设计画布更宽或更高时，多出来的空间由日历区吸收。</summary>
        public float ExtraWidth, ExtraHeight;
        /// <summary>
        /// 初始化Appearance的ResponsiveLayout，准备状态、布局与必要的事件绑定。
        /// </summary>
        public ResponsiveLayout(Size size, int reserve, bool sidebar = false)
        {
            float width = Math.Max(300, size.Width - reserve);
            Portrait = width / Math.Max(1, size.Height) < 1.12f;
            if (Portrait) {
                float logicalHeight=sidebar?1684:1388;
                Scale=.94f*Math.Min(width/1002,size.Height/logicalHeight);
                Origin=new PointF(reserve+(width-1002*Scale)/2,(size.Height-logicalHeight*Scale)/2);
                Calendar=new RectangleF(16,16,970,868);
                Plans=new RectangleF(16,900,970,440);
                Sidebar=sidebar?new RectangleF(16,1356,970,312):RectangleF.Empty;
                return;
            }
            float offset = sidebar ? 266 : 0;
            float dw = (Portrait ? 1002 : 1334) + offset, dh = Portrait ? 1356 : 900;
            Scale = .94f * Math.Min(width / dw, size.Height / dh);
            float calendarHeight = 868;
            if (Portrait)
            {
                calendarHeight = Math.Max(868, size.Height * .94f / Scale - 488);
                dh = calendarHeight + 488;
            }

            if (!Portrait)
            {
                ExtraWidth = Math.Max(0, width * .94f / Scale - dw);
                ExtraHeight = Math.Max(0, size.Height * .94f / Scale - dh);
                dw += ExtraWidth;
                dh += ExtraHeight;
                calendarHeight += ExtraHeight;
            }

            Origin = new PointF(reserve + (width - dw * Scale) / 2, (size.Height - dh * Scale) / 2);
            Calendar = new RectangleF(16 + offset, 16, 970 + ExtraWidth, calendarHeight);
            Plans = Portrait ? new RectangleF(16 + offset, calendarHeight + 32, 970, 440) : new RectangleF(1002 + offset + ExtraWidth, 16, 316, calendarHeight);
            Sidebar = sidebar ? new RectangleF(16, 16, 250, calendarHeight) : RectangleF.Empty;
        }

        /// <summary>
        /// 处理Appearance中的 Pixel 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public RectangleF Pixel(RectangleF r)
        {
            return new RectangleF(Origin.X + r.X * Scale, Origin.Y + r.Y * Scale, r.Width * Scale, r.Height * Scale);
        }
    }
}