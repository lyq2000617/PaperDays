using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    /// <summary>
    /// 小六壬数字格。自行绘制：平时只有淡色底，只有获得焦点的格子显示柔和光圈。
    /// 输入时只重绘本格，不触发桌面或窗口重新排版。
    /// </summary>
    public class IntegerInput : Control
    {
        public int Minimum = 1, Maximum = Divination.InputMaximum;
        public string Placeholder = "";
        public bool Invalid;
        public IntegerInput Next, Previous;
        public Action Submit;
        public event EventHandler ValueChanged;
        readonly Timer caret = new Timer { Interval = 530 };
        bool caretOn, replace;

        /// <summary>
        /// 初始化自绘样式和键盘焦点。
        /// </summary>
        public IntegerInput()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
            ImeMode = ImeMode.Disable;
            Cursor = Cursors.IBeam;
            AccessibleRole = AccessibleRole.Text;
            caret.Tick += delegate
            {
                caretOn = !caretOn;
                Invalidate();
            };
        }

        /// <summary>数字位数，由最大值决定。</summary>
        public int Digits
        {
            /// <summary>读取最多可输入的位数。</summary>
            get
            {
                return Maximum.ToString().Length;
            }
        }

        /// <summary>判断当前文本是否是范围内的整数。</summary>
        public bool Valid
        {
            /// <summary>检查文本及数字范围。</summary>
            get
            {
                int n;
                return Int32.TryParse(Text, out n) && n >= Minimum && n <= Maximum;
            }
        }

        /// <summary>读取或设置数字。</summary>
        public decimal Value
        {
            /// <summary>读取有效文本对应的数字。</summary>
            get
            {
                int n;
                return Int32.TryParse(Text, out n) ? n : 0;
            }

            /// <summary>将给定数字显示到格子里。</summary>
            set
            {
                Text = value.ToString("0");
            }
        }

        /// <summary>
        /// 文字变化时只重绘本格并通知调用方。
        /// </summary>
        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);
            Invalid = false;
            Invalidate();
            if (ValueChanged != null)
                ValueChanged(this, EventArgs.Empty);
        }

        /// <summary>
        /// 让方向键和退格键进入本控件。
        /// </summary>
        protected override bool IsInputKey(Keys keyData)
        {
            return keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Back || keyData == Keys.Delete || base.IsInputKey(keyData);
        }

        /// <summary>
        /// 获得焦点：已有内容时下一次输入直接替换。
        /// </summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            replace = Text.Length > 0;
            caretOn = true;
            caret.Start();
            Invalidate();
        }

        /// <summary>
        /// 失去焦点：停止光标闪烁。
        /// </summary>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            caret.Stop();
            replace = false;
            Invalidate();
        }

        /// <summary>
        /// 点击时把焦点放到本格。
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        /// <summary>
        /// 处理退格、方向键、回车和粘贴。
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Back)
            {
                if (Text.Length == 0 && Previous != null)
                    Previous.Focus();
                else
                    Text = replace ? "" : Text.Substring(0, Math.Max(0, Text.Length - 1));
                replace = false;
            }
            else if (e.KeyCode == Keys.Delete)
            {
                Text = "";
                replace = false;
            }
            else if (e.KeyCode == Keys.Left && Previous != null)
                Previous.Focus();
            else if (e.KeyCode == Keys.Right && Next != null)
                Next.Focus();
            else if (e.KeyCode == Keys.Enter && Submit != null)
            {
                e.SuppressKeyPress = true;
                Submit();
            }
            else if (e.Control && e.KeyCode == Keys.V && Clipboard.ContainsText())
                Type(new string(Clipboard.GetText().Where(Char.IsDigit).Take(Digits).ToArray()));
        }

        /// <summary>
        /// 只接受数字；空格跳到下一格，不接受其他字符。
        /// </summary>
        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            if (e.KeyChar >= '0' && e.KeyChar <= '9')
                Type(e.KeyChar.ToString());
            else if (e.KeyChar == ' ' && Next != null)
                Next.Focus();
            e.Handled = true;
        }

        /// <summary>
        /// 写入数字；填满位数后自动跳到下一格。
        /// </summary>
        public void Type(string digits)
        {
            if (digits.Length == 0)
                return;
            string value = replace ? digits : Text + digits;
            replace = false;
            Text = value.Length > Digits ? value.Substring(value.Length - Digits) : value;
            caretOn = true;
            if (Text.Length >= Digits && Next != null && Focused)
                Next.Focus();
        }

        /// <summary>
        /// 绘制格子本身，周围留出光圈的空间。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(Parent == null ? Palette.Paper : Parent.BackColor);
            bool focused = Focused;
            float inset = focused ? 2.5f : 0, radius = Math.Max(6, Height * .24f);
            var box = new RectangleF(inset, inset, Width - 1 - inset * 2, Height - 1 - inset * 2);
            if (focused)
                using (var soft = Chrome.Round(new RectangleF(.5f, .5f, Width - 2, Height - 2), radius + 1))
                using (var halo = new Pen(Color.FromArgb(70, Skins.Accent), 3))
                    g.DrawPath(halo, soft);
            using (var path = Chrome.Round(box, radius))
            {
                using (var fill = new SolidBrush(Invalid ? FortuneColors.BadFill : focused ? Palette.Paper : FortuneColors.Slot))
                    g.FillPath(fill, path);
                if (focused)
                    using (var ring = new Pen(Color.FromArgb(190, Skins.Accent), 1.3f))
                        g.DrawPath(ring, path);
            }

            bool empty = Text.Length == 0;
            string shown = empty ? (focused ? "" : Placeholder) : Text;
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
            using (var small = new Font(Font.FontFamily, Font.Size * .64f, Font.Style, Font.Unit))
            using (var ink = new SolidBrush(Invalid ? FortuneColors.BadInk : empty ? Palette.Muted : Palette.Ink))
            {
                var font = empty ? small : Font;
                var size = g.MeasureString(shown, font, 1000, format);
                if (focused && replace && !empty)
                    using (var mark = Chrome.Round(new RectangleF((Width - size.Width) / 2 - 1, (Height - size.Height) / 2 + 2, size.Width + 2, size.Height - 4), 4))
                    using (var fill = new SolidBrush(Skins.Button))
                        g.FillPath(fill, mark);
                g.DrawString(shown, font, ink, new RectangleF(0, 0, Width, Height), format);
                if (focused && !replace && caretOn)
                {
                    float x = Width / 2f + (empty ? 0 : size.Width / 2f - 2), half = Font.GetHeight(g) * .42f;
                    using (var pen = new Pen(Skins.Accent, 1.5f))
                        g.DrawLine(pen, x, Height / 2f - half, x, Height / 2f + half);
                }
            }
        }

        /// <summary>
        /// 释放光标计时器。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                caret.Dispose();
            base.Dispose(disposing);
        }

        /// <summary>
        /// 把三个格子串起来，支持自动跳格、退格返回和回车提交。
        /// </summary>
        public static void Chain(IntegerInput[] inputs, Action submit)
        {
            for (int i = 0; i < inputs.Length; i++)
            {
                inputs[i].Previous = i > 0 ? inputs[i - 1] : null;
                inputs[i].Next = i + 1 < inputs.Length ? inputs[i + 1] : null;
                inputs[i].Submit = submit;
                inputs[i].Placeholder = L.T("数" + (i + 1));
                inputs[i].AccessibleName = "数字" + (i + 1) + "，1 至 " + Divination.InputMaximum;
            }
        }
    }

    public partial class SceneView
    {
        Form fortuneHost;
        /// <summary>
        /// 一键清空数字，并把光标放回第一格，可以直接重新输入。
        /// </summary>
        internal void ClearFortuneInput()
        {
            scene.ClearFortune();
            if (fortuneHost != null && !fortuneHost.IsDisposed)
                foreach (var input in fortuneHost.Controls.OfType<IntegerInput>())
                    input.Text = "";
            Invalidate();
            BeginFortuneInput(0);
        }

        /// <summary>
        /// 在桌面卡片原位置激活数字格。输入期间不重绘桌面，离开时保留草稿，不自动计算。
        /// </summary>
        internal void BeginFortuneInput(int index)
        {
            index = Math.Max(0, Math.Min(2, index));
            if (fortuneHost != null && !fortuneHost.IsDisposed)
            {
                fortuneHost.Controls[index].Focus();
                return;
            }

            var hits = scene.Hits.Where(h => h.Action == "fortuneinput").OrderBy(h => h.Id).ToArray();
            if (hits.Length != 3)
                return;
            var union = Rectangle.Round(hits[0].Box);
            foreach (var hit in hits)
                union = Rectangle.Union(union, Rectangle.Round(hit.Box));
            var host = new TodoInputHost
            {
                FormBorderStyle = FormBorderStyle.None,
                ShowInTaskbar = false,
                StartPosition = FormStartPosition.Manual,
                Bounds = RectangleToScreen(union),
                BackColor = Palette.Paper,
                Text = "小六壬数字输入",
                KeyPreview = false
            };
            fortuneHost = host;
            float pixels = Math.Max(13, 16 * 96f / 72f * scene.Layout.Scale);
            var inputs = new IntegerInput[3];
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                var r = Rectangle.Round(hits[i].Box);
                inputs[i] = new IntegerInput
                {
                    Text = scene.FortuneNumbers[i],
                    Invalid = scene.FortuneBad[i],
                    Font = new Font(UiFont.Family("Segoe UI"), pixels, GraphicsUnit.Pixel),
                    Bounds = new Rectangle(r.X - union.X, r.Y - union.Y, r.Width, r.Height)
                };
                inputs[i].Invalid = scene.FortuneBad[i];
                inputs[i].ValueChanged += delegate
                {
                    scene.FortuneNumbers[slot] = inputs[slot].Text;
                    scene.FortuneBad[slot] = false;
                };
                host.Controls.Add(inputs[i]);
            }

            IntegerInput.Chain(inputs, delegate
            {
                host.Close();
                if (HitAction != null)
                    HitAction(new Hit { Action = "drawfortune" });
            });
            host.Deactivate += delegate
            {
                if (!host.IsDisposed)
                    host.Close();
            };
            host.FormClosed += delegate
            {
                fortuneHost = null;
                Invalidate();
                host.BeginInvoke((Action)host.Dispose);
            };
            EventHandler close = delegate
            {
                if (!host.IsDisposed)
                    host.Close();
            };
            Disposed += close;
            host.FormClosed += delegate
            {
                Disposed -= close;
            };
            var region=new Region();region.MakeEmpty();foreach(var input in inputs)using(var path=Chrome.Round(input.Bounds,11))region.Union(path);host.Region=region;
            host.Show();
            inputs[index].Focus();
        }
    }
}
