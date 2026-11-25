using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    /// <summary>
    /// 把固定坐标的窗口转成随边框变化的布局：
    /// 先让文字、输入框和按钮按比例缩放；到最小比例后再换行；放不下时才出现纵向滚动条。
    /// </summary>
    public sealed class FluidForm
    {
        sealed class Item
        {
            public Control C;
            public Rectangle Bounds;
            public FluidForm Child;
            public List<FluidForm> Pages;
        }

        readonly Control parent;
        readonly FluidForm root;
        readonly List<List<Item>> rows = new List<List<Item>>();
        readonly List<Item> corners = new List<Item>();
        readonly Dictionary<Control, Font> fonts;
        readonly Form form;
        readonly Size design;
        float appliedFont = -1;
        bool busy, scrolling;

        /// <summary>当前内容比例，不含屏幕 DPI。</summary>
        public float Ratio = 1;
        /// <summary>内容比例乘以屏幕 DPI，用于换算控件尺寸。</summary>
        public float Scale = 1;

        /// <summary>
        /// 记录设计时的行列关系，后续依据实际客户区重新排版。
        /// </summary>
        FluidForm(Control panel, FluidForm owner, Form form, Size design)
        {
            parent = panel;
            root = owner ?? this;
            this.form = form;
            this.design = design;
            fonts = owner == null ? new Dictionary<Control, Font>() : null;
            var items = panel.Controls.Cast<Control>().Where(c => c.Dock == DockStyle.None).OrderBy(c => c.Top).ThenBy(c => c.Left).ToArray();
            foreach (var c in items)
            {
                var item = new Item { C = c, Bounds = c.Bounds };
                if (c.Tag as string == "corner")
                {
                    corners.Add(item);
                    continue;
                }

                if (rows.Count == 0 || Math.Abs(rows.Last()[0].Bounds.Top - c.Top) > 17)
                    rows.Add(new List<Item>());
                rows.Last().Add(item);
                if (c is Panel && !(c is FieldShell) && !(c is FlowLayoutPanel) && !(c is TableLayoutPanel) && !(c is SwatchGrid) && c.Controls.Count > 0)
                    item.Child = new FluidForm(c, root, form, design);
                var tabs = c as TabControl;
                if (tabs != null)
                    item.Pages = tabs.TabPages.Cast<TabPage>().Select(p => new FluidForm(p, root, form, design)).ToList();
                c.TextChanged += delegate
                {
                    root.Arrange();
                };
            }
        }

        /// <summary>
        /// 安装到窗口内容区。之后窗口尺寸、DPI、文字变化都会触发重排。
        /// </summary>
        public static FluidForm Attach(Panel panel, Form form, Size design)
        {
            panel.AutoScrollMinSize = Size.Empty;
            var layout = new FluidForm(panel, null, form, design);
            layout.CollectFonts(panel);
            panel.SizeChanged += delegate
            {
                layout.Arrange();
            };
            layout.Arrange();
            return layout;
        }

        /// <summary>
        /// 保存每个控件的基准字体；缩放总是从基准计算，反复拖动不会累积误差。
        /// </summary>
        void CollectFonts(Control container)
        {
            foreach (Control c in container.Controls)
            {
                fonts[c] = c.Font;
                CollectFonts(c);
            }
        }

        /// <summary>
        /// 计算比例并排版。内容比窗口高时再收紧一次比例，只有到最小比例仍放不下才滚动。
        /// </summary>
        public void Arrange(bool force = false)
        {
            if (busy || parent.IsDisposed || parent.Width < 1 || parent.Height < 1)
                return;
            busy = true;
            parent.SuspendLayout();
            try
            {
                float dpi = UiDpi.Of(form) / 96f;
                // Keep font sizes stable. The outer viewport owns both scrollbars.
                float ratio=1;scrolling=false;
                Apply(ratio,dpi,force);
                int content=Place();
                if(parent.Height<content)parent.Height=content;
                var panel = parent as Panel;
                if (panel != null && panel.AutoScroll)
                {
                    var wanted = content > parent.Height ? new Size(0, content) : Size.Empty;
                    if (panel.AutoScrollMinSize != wanted)
                        panel.AutoScrollMinSize = wanted;
                }
            }
            finally
            {
                // 需要重新布局一次，滚动范围才会按缩小后的控件计算，否则残留横向滚动条。
                parent.ResumeLayout(true);
                busy = false;
            }
        }

        /// <summary>
        /// 应用内容比例：更新字体和标题栏。
        /// </summary>
        void Apply(float ratio, float dpi, bool force)
        {
            Ratio = ratio;
            Scale = ratio * dpi;
            float factor = ratio * UiDpi.FontFactor(form);
            if (!force && Math.Abs(factor - appliedFont) < .001f)
                return;
            appliedFont = factor;
            foreach (var pair in fonts)
                if (!pair.Key.IsDisposed)
                    pair.Key.Font = UiFont.Shared(pair.Value, factor);
            DialogHeader.Rescale(form, ratio);
        }

        /// <summary>
        /// 控件的最小可读宽度；文字按当前字号测量。
        /// </summary>
        int Minimum(Control c, int design)
        {
            float s = root.Scale;
            if (c is Button || c is CheckBox || c is RadioButton)
                return Math.Min(design, Math.Max((int)(42 * s), TextRenderer.MeasureText(c.Text, c.Font).Width + (int)(24 * s)));
            if (c is Label)
                return Math.Min(design, (int)(75 * s));
            if (c is Panel)
                return Math.Min(design, (int)(220 * s));
            return Math.Min(design, (int)(110 * s));
        }

        /// <summary>
        /// 重排各行并测量多行文字高度，返回内容总高度。
        /// </summary>
        int Place(int width = -1)
        {
            float s = root.Scale;
            if (width < 0)
                width = parent == root.parent ? parent.Width - (scrolling ? SystemInformation.VerticalScrollBarWidth : 0) : parent.ClientSize.Width;
            int margin = (int)(12 * s), gapX = (int)(10 * s), available = Math.Max(20, width - margin * 2);
            int offset = parent == root.parent ? ((ScrollableControl)parent).AutoScrollPosition.Y : 0;
            int y = (int)((parent is PaletteSection ? 34 : 16) * s), oldBottom = 0;
            for (int index = 0; index < rows.Count; index++)
            {
                var row = rows[index];
                var ordered = row.OrderBy(i => i.Bounds.Left).ToArray();
                int gap = (int)(Math.Max(7, Math.Min(22, ordered[0].Bounds.Top - oldBottom)) * s);
                if (oldBottom > 0)
                    y += gap;
                if (index + 1 < rows.Count && PairedSections(row, rows[index + 1], available))
                {
                    int half = (available - gapX) / 2;
                    int left = PlaceSection(row[0], margin, y + offset, half), right = PlaceSection(rows[index + 1][0], margin + half + gapX, y + offset, half);
                    y += Math.Max(left, right);
                    row[0].C.Height = rows[index + 1][0].C.Height = Math.Max(left, right);
                    oldBottom = rows[++index].Max(i => i.Bounds.Bottom);
                    continue;
                }

                int x = margin, height = 0;
                int sum = (int)(ordered.Sum(i => i.Bounds.Width) * s);
                float factor = Math.Min(1.6f, (available - Math.Max(0, ordered.Length - 1) * gapX) / (float)Math.Max(1, sum));
                // 靠右的一排操作按钮保持靠右，不拉宽。
                if (ordered.All(i => i.C is Button && !(i.C is ColorDot) && !(i.C is EmojiButton)) && ordered[0].Bounds.Left > design.Width * .3f)
                {
                    factor = Math.Min(1, factor);
                    int total = (int)(sum * factor) + (ordered.Length - 1) * gapX;
                    x = Math.Max(margin, margin + available - total);
                }

                foreach (var item in ordered)
                {
                    var c = item.C;
                    if (c.IsDisposed)
                        continue;
                    int bw = (int)(item.Bounds.Width * s), h = (int)(item.Bounds.Height * s);
                    // 色块和 emoji 永远保持方形，不随窗口拉伸。
                    bool square = c is ColorDot || c is EmojiButton;
                    int w = square ? Math.Min(bw, h) : c is SwatchGrid ? available : Math.Max(Minimum(c, bw), (int)(bw * factor));
                    w = Math.Min(available, w);
                    if (square)
                        h = w;
                    if (x > margin && x + w > available + margin)
                    {
                        y += height + gapX;
                        x = margin;
                        height = 0;
                    }

                    if (item.Pages != null)
                        w = available;

                    var label = c as Label;
                    if (label != null)
                    {
                        label.AutoSize = false;
                        label.MaximumSize = Size.Empty;
                        h = Math.Max(Math.Min(h, (int)(36 * s)), TextRenderer.MeasureText(label.Text, label.Font, new Size(Math.Max(1, w), 10000), TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height + (int)(6 * s));
                    }

                    var grid = c as SwatchGrid;
                    if (grid != null)
                        h = grid.Arrange(w, s, root.Ratio);
                    c.SetBounds(x, y + offset, w, h);
                    if (item.Child != null)
                    {
                        h = item.Child.Place();
                        c.Height = h;
                    }

                    if (item.Pages != null)
                    {
                        var tabs = (TabControl)c;
                        int inner = tabs.DisplayRectangle.Width, top = tabs.DisplayRectangle.Top;
                        h = item.Pages.Max(p => p.Place(inner)) + top + (int)(8 * s);
                        c.Height = h;
                    }

                    var flow = c as FlowLayoutPanel;
                    if (flow != null)
                        foreach (Control child in flow.Controls)
                        {
                            var l = child as Label;
                            if (l != null)
                            {
                                l.MaximumSize = new Size(Math.Max(60, w - (int)(36 * s)), 0);
                                l.AutoSize = true;
                            }
                        }

                    height = Math.Max(height, h);
                    x += w + gapX;
                }

                y += height;
                oldBottom = ordered.Max(i => i.Bounds.Bottom);
            }

            foreach (var item in corners)
            {
                int w = Math.Max(TextRenderer.MeasureText(item.C.Text, item.C.Font).Width + (int)(22 * s), (int)(56 * s)), h = (int)(26 * s);
                item.C.SetBounds(width - margin - w, (int)(4 * s), w, h);
            }

            return y + (int)(16 * s);
        }

        /// <summary>
        /// 判断相邻两个分区是否可以左右并排；只在窗口足够宽时才并排。
        /// </summary>
        bool PairedSections(List<Item> first, List<Item> second, int available)
        {
            return first.Count == 1 && second.Count == 1 && first[0].C is PaletteSection && second[0].C is PaletteSection && available >= 780 * root.Scale;
        }

        /// <summary>
        /// 放置并排分区，返回其内容高度。
        /// </summary>
        int PlaceSection(Item item, int x, int y, int width)
        {
            item.C.SetBounds(x, y, width, item.C.Height);
            int h = item.Child == null ? item.C.Height : item.Child.Place();
            item.C.Height = h;
            return h;
        }
    }

    /// <summary>
    /// 色块和 emoji 的网格：格子永远是方形，按大、中、小三档变化，整行均匀分配。
    /// </summary>
    public class SwatchGrid : Panel
    {
        const int MinimumGap = 8, MaximumGap = 14;
        int column;

        /// <summary>
        /// 初始化透明背景的网格容器。
        /// </summary>
        public SwatchGrid()
        {
            DoubleBuffered = true;
        }

        /// <summary>当前每行格子数。</summary>
        public int Columns
        {
            /// <summary>读取每行格子数。</summary>
            get
            {
                return column;
            }
        }

        /// <summary>
        /// 取得格子边长：比例只决定档位，不做连续缩放。
        /// </summary>
        public static int Cell(float ratio)
        {
            return ratio >= 1 ? 36 : ratio >= .86f ? 32 : 28;
        }

        /// <summary>
        /// 选择每行数量：只取能整除总数的列数，避免最后一行只剩一两个。
        /// </summary>
        public static int ColumnsFor(int count, int width, int cell, int gap)
        {
            for (int columns = count; columns > 1; columns--)
                if (count % columns == 0 && columns * cell + (columns - 1) * gap <= width)
                    return columns;
            return 1;
        }

        /// <summary>
        /// 排列子控件并返回网格高度。
        /// </summary>
        public int Arrange(int width, float scale, float ratio)
        {
            int count = Controls.Count;
            if (count == 0)
                return 0;
            float dpi = scale / Math.Max(.01f, ratio);
            int cell = (int)(Cell(ratio) * dpi), minimum = (int)(MinimumGap * dpi), maximum = (int)(MaximumGap * dpi);
            column = ColumnsFor(count, width, cell, minimum);
            int gap = column < 2 ? minimum : Math.Max(minimum, Math.Min(maximum, (width - column * cell) / (column - 1)));
            for (int i = 0; i < count; i++)
                Controls[i].SetBounds(i % column * (cell + gap), i / column * (cell + gap), cell, cell);
            int rows = (count + column - 1) / column;
            return rows * cell + (rows - 1) * gap;
        }
    }
}
