using System;
using System.Linq;



namespace PaperDays
{
    public class DivinationRecord
    {
        public int A, B, C;
        public string Category = "日常", Version = "three-number-inclusive-v1";
        public DateTime Created;
    }

    public class DivinationResult
    {
        public string Name, Grade, Meaning, Advice, Steps;
    }

    public static class Divination
    {
        /// <summary>桌面和详细解读窗口允许输入的最大数字。算法每 6 步循环，1–99 足够覆盖全部结果。</summary>
        public const int InputMaximum = 99;
        public static readonly string[] Names =
        {
            "大安",
            "留连",
            "速喜",
            "赤口",
            "小吉",
            "空亡"
        };
        public static readonly string[] Categories =
        {
            "日常",
            "学习",
            "工作",
            "出行",
            "人际"
        };
        static readonly string[] Grades =
        {
            "传统寓意 · 偏吉",
            "传统寓意 · 偏滞",
            "传统寓意 · 偏吉",
            "传统寓意 · 偏凶",
            "传统寓意 · 小吉",
            "传统寓意 · 偏空"
        };
        static readonly string[] Meanings =
        {
            "偏向安定与守成。适合按现有节奏推进，不必为了求快频繁改变安排。",
            "偏向拖延与反复。事情可能需要复查和等待，不等于一定失败。",
            "偏向消息与进展。可以留意反馈，但不要把期待当成已经确认的结果。",
            "偏向口舌与分歧。重点是表达是否清楚、信息是否一致，不是断言会发生争执。",
            "偏向小的进展与协助。适合完成具体的小目标，不宜据此扩大承诺。",
            "偏向未定与落空。先核实条件和消息，不表示已经失去所有机会。"
        };
        static readonly string[][] Advice =
        {
            new[]
            {
                "沿用有效安排，先完成已承诺的事项。",
                "把任务拆成小段，并预留返工时间。",
                "先确认消息来源，再决定下一步。",
                "重要约定用文字确认，避免猜测。",
                "选择一件当天能完成的小事，核对进度。",
                "列出尚未确认的条件，补齐后再行动。"
            },
            new[]
            {
                "按原计划复习，先巩固已学内容。",
                "留出订正时间，检查卡住的知识点。",
                "及时查看反馈，把错题重新做一遍。",
                "遇到不同解法先核对题意，再讨论。",
                "设一个小学习目标，完成后及时复盘。",
                "先检查资料和截止时间，不凭感觉判断掌握程度。"
            },
            new[]
            {
                "按既定优先级推进，核对交付标准。",
                "拆分交付节点，为审批和修改留时间。",
                "主动确认反馈，明确下一步负责人。",
                "把需求、分工和截止时间写清楚。",
                "完成一个可交付的小成果，及时同步。",
                "确认依赖和授权后再承诺完成时间。"
            },
            new[]
            {
                "按已确认的路线安排，出发前再检查一次。",
                "预留换乘余量，准备可用的备选路线。",
                "查看最新行程通知，确认后再调整。",
                "核对地址、人数和集合时间，避免误会。",
                "保持行程简单，逐项确认预订。",
                "核实预订和开放信息，不把未确认安排当成确定。"
            },
            new[]
            {
                "保持平稳沟通，兑现已有约定。",
                "给对方回复时间，避免连续催问。",
                "有消息时先听完整，再表达自己的想法。",
                "说明事实和需求，避免把推测当作对方的意图。",
                "从一次具体的小帮助或沟通开始。",
                "直接确认对方的意思，不替别人下结论。"
            }
        };
        /// <summary>
        /// 处理本地占卜卡片中的 Calculate 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static DivinationResult Calculate(DivinationRecord record)
        {
            if (record == null || record.Version != "three-number-inclusive-v1")
                throw new ArgumentException("不支持的起课规则版本");
            int[] nums =
            {
                record.A,
                record.B,
                record.C
            };
            if (nums.Any(n => n < 1 || n > 999))
                throw new ArgumentException("请输入 1–999 的整数");
            int first = (nums[0] - 1) % 6, second = (first + nums[1] - 1) % 6, last = (second + nums[2] - 1) % 6;
            int category = Array.IndexOf(Categories, record.Category);
            if (category < 0)
                category = 0;
            return new DivinationResult
            {
                Name = Names[last],
                Grade = Grades[last],
                Meaning = L.T(Meanings[last]),
                Advice = L.T(Advice[category][last]),
                Steps = "第一数 " + nums[0] + " → " + Names[first] + "；第二数 " + nums[1] + " → " + Names[second] + "；第三数 " + nums[2] + " → " + Names[last] + "。"
            };
        }

        /// <summary>
        /// 读取并解析本地或系统数据，调用方负责处理失败。
        /// </summary>
        public static DivinationResult Read(DivinationRecord record)
        {
            try
            {
                return Calculate(record);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }
    }
}
