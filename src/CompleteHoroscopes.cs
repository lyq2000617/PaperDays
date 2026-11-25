using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PaperDays
{
    public class CompleteHoroscopeResponse
    {
        public int code;
        public CompleteHoroscope data;
    }

    public class CompleteHoroscope
    {
        public Dictionary<string, int> fortune;
        public Dictionary<string, string> fortunetext;
        public string luckycolor, luckynumber, luckyconstellation, name, shortcomment, time, title, type;
        public Dictionary<string, string> todo;
    }

    public static class CompleteHoroscopes
    {
        /// <summary>
        /// 去掉来源夹在正文里的“星$座$屋”水印，其余内容不改。
        /// </summary>
        public static string Clean(string text)
        {
            return System.Text.RegularExpressions.Regex.Replace(text ?? "", @"星[\s$＄zZｚＺ☆★·•_-]{0,8}座[\s$＄zZｚＺ☆★·•_-]{0,8}屋", "").Trim();
        }

        static readonly string[] ids =
        {
            "aries",
            "taurus",
            "gemini",
            "cancer",
            "leo",
            "virgo",
            "libra",
            "scorpio",
            "sagittarius",
            "capricorn",
            "aquarius",
            "pisces"
        };
        /// <summary>
        /// 处理CompleteHoroscopes中的 Url 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Url(string sign)
        {
            int i = Array.IndexOf(Reading.Signs, sign);
            if (i < 0)
                throw new ArgumentException("未知星座");
            return "https://v2.xxapi.cn/api/horoscope?type=" + ids[i] + "&time=today";
        }

        /// <summary>
        /// 解析输入为业务对象，并拒绝格式错误的数据。
        /// </summary>
        public static Reading Parse(string json, string sign, DateTime date, DateTime fetched)
        {
            var response = Data.Json().Deserialize<CompleteHoroscopeResponse>(json);
            var d = response == null ? null : response.data;
            int index = Array.IndexOf(Reading.Signs, sign);
            if (response == null || response.code != 200 || d == null)
                throw new IOException("来源没有返回有效内容");
            if (index < 0 || d.name != ids[index] || d.title != sign || d.type != "今日运势")
                throw new IOException("来源星座或周期不匹配");
            if (d.time != date.ToString("M月d日") || fetched.ToLocalTime().Date != date.Date)
                throw new IOException("来源不是当天内容");
            string[] sections =
            {
                "all",
                "work",
                "love",
                "money",
                "health"
            };
            if (d.fortunetext == null || sections.Any(k => !d.fortunetext.ContainsKey(k) || String.IsNullOrWhiteSpace(d.fortunetext[k])))
                throw new IOException("来源缺少主要运势分类");
            if (d.fortune == null || sections.Any(k => !d.fortune.ContainsKey(k) || d.fortune[k] < 0 || d.fortune[k] > 5))
                throw new IOException("来源评分不完整");
            int number;
            if (!Int32.TryParse(d.luckynumber, out number) || number < 0 || String.IsNullOrWhiteSpace(d.luckycolor) || d.luckycolor.All(Char.IsDigit))
                throw new IOException("来源幸运信息格式错误");
            string caution = "";
            if (d.todo != null)
                caution = String.Join("；", d.todo.Values.Where(v => !String.IsNullOrWhiteSpace(v)));
            return new Reading
            {
                Title = sign,
                Grade = L.T("来源评分 ") + d.fortune["all"] + " / 5",
                Outlook = Clean(d.shortcomment),
                Interpretation = Clean(d.fortunetext["all"]),
                CareerText = Clean(d.fortunetext["work"]),
                LoveText = Clean(d.fortunetext["love"]),
                WealthText = Clean(d.fortunetext["money"]),
                HealthText = Clean(d.fortunetext["health"]),
                Caution = Clean(caution),
                Number = number,
                LuckyName = d.luckycolor,
                Overall = d.fortune["all"],
                Work = d.fortune["work"],
                Social = d.fortune["love"],
                Energy = d.fortune["health"],
                SourceDate = date.ToString("yyyy-MM-dd"),
                SourceName = "小小 API · 星座屋内容",
                SourceUrl = Url(sign),
                FetchedUtc = fetched,
                IsOnline = true
            };
        }
    }
}