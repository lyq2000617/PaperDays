using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    /// <summary>
    /// 六宫的预写解读。内容是民俗说法的整理，不接 AI，也不预测实际结果。
    /// </summary>
    public static class DivinationDetails
    {
        public static readonly string[] Attributes =
        {
            "五行属木 · 东方 · 青龙",
            "五行属水 · 北方 · 玄武",
            "五行属火 · 南方 · 朱雀",
            "五行属金 · 西方 · 白虎",
            "五行属水 · 北方 · 六合",
            "五行属土 · 中央 · 勾陈"
        };
        public static readonly string[] Images =
        {
            "传统上把大安看作安稳、守成的一宫，象征根基稳、事情按部就班。它强调的不是突然的好运，而是局面平顺，适合把手上的事做扎实。",
            "传统上把留连看作拖延、反复的一宫，象征事情一时走不快，可能要多等一轮、多改一次。它提醒节奏放慢，并不表示事情一定不成。",
            "传统上把速喜看作消息和进展的一宫，象征事情来得快、回音近。它偏向短期的好消息，也提醒喜讯要等确认后才算数。",
            "传统上把赤口看作口舌、分歧的一宫，象征沟通里容易起摩擦。它提醒的是说话方式和信息一致，而不是断言一定会起争执。",
            "传统上把小吉看作小有所成的一宫，象征得到帮助、事情有小进展。它的好处在于具体而不大，适合落实眼前的小目标。",
            "传统上把空亡看作未定、落空的一宫，象征条件还不齐备、消息不实。它提醒先核实再行动，并不表示失去所有机会。"
        };
        public static readonly string[] Suitable =
        {
            "沿用已有计划、整理收尾、处理长期事务。",
            "复查细节、补齐材料、给事情留出缓冲时间。",
            "主动询问进度、及时回复消息、抓紧短期事项。",
            "把话说清楚、用文字确认约定、先听完再回应。",
            "完成一件具体的小事、请人帮忙、及时同步进展。",
            "核实信息、列清未确定的条件、先做准备工作。"
        };
        public static readonly string[] Careful =
        {
            "为了求快频繁改动安排。",
            "连续催促，或因等待而放弃已有进度。",
            "把还没确认的好消息当成定局。",
            "在情绪上做决定，或把推测当成事实。",
            "因为一点进展就扩大承诺。",
            "凭感觉下结论，或在条件不明时贸然投入。"
        };
        public const string Rule = "本程序采用三数顺数法，各步起点都计为 1。\n六宫按 大安 → 留连 → 速喜 → 赤口 → 小吉 → 空亡 循环计数。\n第一数从大安起数，第二数从第一步的落宫起数，第三数从第二步的落宫起数，第三步的落宫就是结果。\n因为六宫循环，数字每增加 6 结果相同，所以输入范围定为 1–99。\n不同流派的起课方式和五行方位说法并不一致，这里只采用其中一种。";
        public const string Note = "解读是预先写好的民俗说法整理，不接 AI，不代表实际吉凶，也不能作为重要决定的依据。";

        /// <summary>
        /// 计算每一步落在哪一宫，返回三个宫位序号。
        /// </summary>
        public static int[] Palaces(DivinationRecord record)
        {
            int first = (record.A - 1) % 6, second = (first + record.B - 1) % 6, last = (second + record.C - 1) % 6;
            return new[] { first, second, last };
        }

        /// <summary>
        /// 生成逐步起课说明。
        /// </summary>
        public static string Process(DivinationRecord record)
        {
            var p = Palaces(record);
            if (L.Language != "zh-CN") return String.Join("\n", p.Select((value,i) => String.Format(L.T("第 {0} 步：{1} → {2}"),i+1,new[] { record.A,record.B,record.C }[i],L.T(Divination.Names[value]))));
            return "第一步：从大安起数 " + record.A + "，落在" + Divination.Names[p[0]] + "。\n第二步：从" + Divination.Names[p[0]] + "起数 " + record.B + "，落在" + Divination.Names[p[1]] + "。\n第三步：从" + Divination.Names[p[1]] + "起数 " + record.C + "，落在" + Divination.Names[p[2]] + "，这就是本次结果。";
        }
    }

    /// <summary>
    /// 详细解读窗口：显示五类运势全部结果、本宫详解、起课过程和规则。
    /// </summary>
    public class DivinationDialog : Form, IResponsiveDialog, IScaledDialog
    {
        public IntegerInput[] Numbers = new IntegerInput[3];
        public Button CalculateButton, ClearButton, CloseButton;
        public DivinationRecord Result;
        public Label ResultTitle, Status;
        /// <summary>窗口内重新起课后为真，调用方据此更新桌面卡片。</summary>
        public bool Changed;
        readonly FlowLayoutPanel content;
        readonly Label heading, hint;
        readonly string preferred;
        ResponsiveScale scale;

        /// <summary>
        /// 初始化窗口；有已保存记录时直接显示它的解读。
        /// </summary>
        public DivinationDialog(DivinationRecord prior)
        {
            UI.Style(this, L.T("详细解读 · 数字小六壬"), 640, 720);
            preferred = prior == null ? "日常" : prior.Category;
            heading = UI.Label(this, L.T("数字小六壬"), 28, 18, 580);
            heading.Font = UiFont.New(19);
            hint = UI.Label(this, L.T("请在下面三个框内输入数字，每个数字为 1–99 的整数。"), 28, 60, 580);
            hint.ForeColor = Palette.Muted;
            for (int i = 0; i < 3; i++)
            {
                Numbers[i] = new IntegerInput { Font = UiFont.New("Segoe UI", 16), Bounds = new Rectangle(28 + i * 96, 96, 86, 50) };
                Controls.Add(Numbers[i]);
                Numbers[i].ValueChanged += delegate
                {
                    Status.Text = "";
                };
            }

            IntegerInput.Chain(Numbers, Compute);
            CalculateButton = UI.Button(this, L.T("开始占卜"), 330, 104, 130, delegate
            {
                Compute();
            });
            ClearButton = UI.Button(this, L.T("清空"), 470, 104, 80, delegate
            {
                ClearNumbers();
            });
            Status = UI.Label(this, "", 28, 150, 580);
            Status.ForeColor = FortuneColors.BadInk;
            ResultTitle = UI.Label(this, "", 28, 176, 580);
            ResultTitle.Font = UiFont.New(17);
            content = new FlowLayoutPanel
            {
                Location = new Point(28, 220),
                Size = new Size(584, 440),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = Palette.Paper
            };
            Controls.Add(content);
            CloseButton = UI.Button(this, L.T("关闭"), 482, 672, 130, delegate
            {
                Close();
            });
            CancelButton = CloseButton;
            AcceptButton = null;
            Populate();
            Load += delegate
            {
                MinimumSize = new Size(300, 360);
            };
            Shown += delegate
            {
                content.Focus();
            };
        }

        /// <summary>
        /// 接收窗口比例；比例变化时重新排列。
        /// </summary>
        public void Attach(ResponsiveScale value)
        {
            scale = value;
            scale.Changed = Arrange;
            ((Panel)content.Parent).AutoScroll = false;
        }

        /// <summary>
        /// 重新排列窗口内容；正文区单独滚动，数字格和关闭按钮始终可见。
        /// </summary>
        void Arrange()
        {
            var body = content.Parent;
            if (body == null)
                return;
            float s = scale == null ? 1 : scale.Layout;
            int left = (int)(20 * s), w = Math.Max(120, body.ClientSize.Width - left * 2), y = (int)(12 * s);
            heading.SetBounds(left, y, w, heading.Font.Height + (int)(8 * s));
            y = heading.Bottom;
            hint.SetBounds(left, y, w, TextRenderer.MeasureText(hint.Text,hint.Font,new Size(w,300),TextFormatFlags.WordBreak).Height + (int)(6 * s));
            y = hint.Bottom + (int)(8 * s);
            int cell = Math.Max(52, (int)(80 * s)), gap = (int)(10 * s), height = (int)(50 * s);
            int x = left;
            foreach (var input in Numbers)
            {
                input.SetBounds(x, y, cell, height);
                x += cell + gap;
            }

            int buttonWidth = (int)(110 * s), clearWidth = (int)(70 * s), buttonHeight = (int)(36 * s);
            if (x + gap + buttonWidth + gap + clearWidth <= left + w)
            {
                int top = y + (height - buttonHeight) / 2;
                CalculateButton.SetBounds(x + gap, top, buttonWidth, buttonHeight);
                ClearButton.SetBounds(CalculateButton.Right + gap, top, clearWidth, buttonHeight);
            }
            else
            {
                y += height + gap;
                CalculateButton.SetBounds(left, y, buttonWidth, buttonHeight);
                ClearButton.SetBounds(CalculateButton.Right + gap, y, clearWidth, buttonHeight);
                height = buttonHeight;
            }

            y += height + (int)(4 * s);
            int statusHeight = Status.Text.Length == 0 ? 0 : TextRenderer.MeasureText(Status.Text, Status.Font, new Size(w, 400), TextFormatFlags.WordBreak).Height + 4;
            Status.SetBounds(left, y, w, statusHeight);
            y += statusHeight + (int)(4 * s);
            ResultTitle.SetBounds(left, y, w, ResultTitle.Font.Height + (int)(10 * s));
            y = ResultTitle.Bottom + (int)(6 * s);
            int closeHeight = (int)(36 * s), closeTop = body.ClientSize.Height - closeHeight - (int)(10 * s);
            CloseButton.SetBounds(left + w - (int)(120 * s), closeTop, (int)(120 * s), closeHeight);
            content.SetBounds(left, y, w, Math.Max((int)(60 * s), closeTop - y - (int)(8 * s)));
            int text = Math.Max(60, w - SystemInformation.VerticalScrollBarWidth - (int)(12 * s));
            foreach (Control child in content.Controls)
                child.MaximumSize = new Size(text, 0);
        }

        /// <summary>
        /// 添加小标题和正文。
        /// </summary>
        void Section(string title, string text)
        {
            content.Controls.Add(new Label { Text = title, AutoSize = true, Font = UiFont.New(12, FontStyle.Bold), ForeColor = Palette.Ink, Margin = new Padding(0, 12, 0, 6) });
            content.Controls.Add(new Label { Text = text, AutoSize = true, Font = UiFont.New(11), ForeColor = Palette.Ink, Margin = new Padding(0, 0, 0, 8) });
        }

        /// <summary>
        /// 按当前结果重建正文。
        /// </summary>
        void Populate()
        {
            content.SuspendLayout();
            while (content.Controls.Count > 0)
                content.Controls[0].Dispose();
            var r = Divination.Read(Result);
            if (r == null)
            {
                ResultTitle.Text = L.T("输入三个数字后查看解读");
                Section(L.T("起课规则"), L.T(DivinationDetails.Rule));
                Section(L.T("说明"), L.T(DivinationDetails.Note));
            }
            else
            {
                var palaces = DivinationDetails.Palaces(Result);
                int last = palaces[2];
                ResultTitle.Text = L.T(r.Name) + "  /  " + L.T(r.Grade);
                Section(L.T("总体"), r.Meaning);
                string chosen = Array.IndexOf(Divination.Categories, Result.Category) >= 0 ? Result.Category : preferred;
                Section(L.T("五类运势"), String.Join("\n", Divination.Categories.Select(c => L.T(c) + (c == chosen ? L.T("（当前）") : "") + "：" + Divination.Calculate(new DivinationRecord { A = Result.A, B = Result.B, C = Result.C, Category = c }).Advice)));
                Section(L.T("本宫详解"), L.T(DivinationDetails.Attributes[last]) + "\n" + L.T(DivinationDetails.Images[last]) + "\n" + L.T("适合：") + L.T(DivinationDetails.Suitable[last]) + "\n" + L.T("留意：") + L.T(DivinationDetails.Careful[last]));
                Section(L.T("起课过程"), DivinationDetails.Process(Result));
                Section(L.T("六宫对照"), String.Join("\n", Divination.Names.Select((n, i) => L.T(n) + (i == last ? L.T("（本次）") : "") + "：" + L.T(DivinationDetails.Attributes[i]))));
                Section(L.T("起课规则"), L.T(DivinationDetails.Rule));
                Section(L.T("说明"), L.T(DivinationDetails.Note));
            }

            content.ResumeLayout();
            if (scale != null)
                scale.Apply(true);
        }

        /// <summary>
        /// 清空数字并把光标放回第一格；解读内容保留到重新起课。
        /// </summary>
        public void ClearNumbers()
        {
            foreach (var input in Numbers)
            {
                input.Text = "";
                input.Invalid = false;
            }

            Status.Text = "";Result=null;Changed=true;Populate();
            Arrange();
            Numbers[0].Focus();
        }

        /// <summary>
        /// 校验数字并重新起课；错误时只标出有问题的格子。
        /// </summary>
        public void Compute()
        {
            bool failed = false;
            foreach (var input in Numbers)
            {
                input.Invalid = !input.Valid;
                failed |= input.Invalid;
                input.Invalidate();
            }

            if (failed)
            {
                Status.Text = L.T("标红的格子请填 1–") + Divination.InputMaximum + L.T(" 的整数");
                Arrange();
                return;
            }

            Status.Text = "";
            Result = new DivinationRecord
            {
                A = (int)Numbers[0].Value,
                B = (int)Numbers[1].Value,
                C = (int)Numbers[2].Value,
                Category = preferred,
                Created = DateTime.UtcNow
            };
            Changed = true;
            Populate();
            Arrange();
        }
    }
}
