using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace PaperDays
{
    public class TimorDay
    {
        public string date, name;
        public bool? holiday;
    }

    public class TimorYear
    {
        public int? code;
        public Dictionary<string, TimorDay> holiday;
    }

    public static class HolidayFallback
    {
        static Dictionary<string, List<CalendarMark>> festivals;
        /// <summary>
        /// 处理HolidayFallback中的 Festivals 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static List<CalendarMark> Festivals(int year)
        {
            try
            {
                if (festivals == null)
                    festivals = Data.Json().Deserialize<Dictionary<string, List<CalendarMark>>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "holidays", "CN-festivals.json")));
                List<CalendarMark> days;
                if (festivals.TryGetValue(year.ToString(), out days))
                    return days.Select(d => new CalendarMark { Date = d.Date, Label = d.Label, Color = "#B68A7B", Source = "CN · 节日日期（非放假调休安排）· 香港天文台公农历对照表 https://www.hko.gov.hk/tc/gts/time/conversion.htm" }).ToList();
            }
            catch
            {
            }

            return new List<CalendarMark>();
        }

        /// <summary>
        /// 处理HolidayFallback中的 Statutory 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static List<CalendarMark> Statutory(int year)
        {
            if (year < 2025)
                return new List<CalendarMark>();
            var result = new List<CalendarMark>();
            foreach (var day in Festivals(year))
            {
                if (day.Label == "除夕")
                    continue;
                int count = day.Label == "春节" ? 4 : day.Label == "劳动节" ? 2 : day.Label == "国庆节" ? 3 : 1;
                var start = DateTime.ParseExact(day.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (day.Label == "春节")
                    start = start.AddDays(-1);
                for (int i = 0; i < count; i++)
                    result.Add(new CalendarMark { Date = start.AddDays(i).ToString("yyyy-MM-dd"), Label = "中国 · " + day.Label + "（法定）", Color = HolidayStyle.ColorFor("CN"), Source = "CN · 现行法定日期，年度放假调休待公告更新；按2025年起施行的法规推算，未来法规变动需更新。 https://app.www.gov.cn/govdata/gov/202411/12/521605/article.html" });
            }

            return result;
        }

        /// <summary>
        /// 解析HolidayFallback中的 ParseTimor 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static List<CalendarMark> ParseTimor(string json, int year)
        {
            var data = Data.Json().Deserialize<TimorYear>(json);
            if (data == null || data.code != 0 || data.holiday == null || data.holiday.Count == 0)
                throw new IOException("暂无该年放假安排");
            var result = new List<CalendarMark>();
            foreach (var item in data.holiday.Values)
            {
                DateTime date;
                if (item == null || !item.holiday.HasValue || String.IsNullOrWhiteSpace(item.name) || !DateTime.TryParseExact(item.date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) || date.Year != year)
                    throw new IOException("备用来源日期或结构有误");
                result.Add(new CalendarMark { Date = item.date, Label = (item.holiday.Value ? "休 · " : "班 · ") + item.name, Color = item.holiday.Value ? "#B68A7B" : "#9A967B", Source = "CN · 提莫节假日 API 整理 https://timor.tech/api/holiday/year/" + year + "/" });
            }

            if (result.Select(x => x.Date).Distinct().Count() != result.Count)
                throw new IOException("备用来源有重复日期");
            return result;
        }

        /// <summary>
        /// 在超时和大小限制内请求外部数据，网络失败向调用方报告。
        /// </summary>
        public static string Download(string code, int year)
        {
            return Fetch(code, year, OnlineData.Download);
        }

        /// <summary>
        /// 处理HolidayFallback中的 Fetch 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Fetch(string code, int year, Func<string, string> download)
        {
            string[] urls = code == "CN" ? new[]
            {
                "https://raw.githubusercontent.com/NateScarlet/holiday-cn/master/" + year + ".json",
                "https://timor.tech/api/holiday/year/" + year + "/"
            }

            : new[]
            {
                "https://date.nager.at/api/v3/PublicHolidays/" + year + "/" + code
            };
            var errors = new List<string>();
            foreach (string url in urls)
                try
                {
                    string json = download(url);
                    var marks = code == "CN" ? HolidayUpdates.ParseChina(json, year) : Calendars.ParseHolidays(json, code);
                    if (marks.Count == 0 || marks.Any(m => !m.Date.StartsWith(year + "-")))
                        throw new IOException("来源为空或年份不匹配");
                    if (code != "CN")
                    {
                        try
                        {
                            string extra = download("https://nagerholidays.com/api/v4/Holidays/" + code + "/" + year);
                            var extraMarks = Calendars.ParseHolidays(extra, code);
                            if (extraMarks.Count > 0 && extraMarks.All(m => m.Date.StartsWith(year + "-")))
                            {
                                var rows = Data.Json().Deserialize<List<Holiday>>(json);
                                rows.AddRange(Data.Json().Deserialize<List<Holiday>>(extra));
                                json = Data.Json().Serialize(rows);
                            }
                        }
                        catch
                        {
                        }
                    }

                    return json;
                }
                catch (Exception ex)
                {
                    errors.Add(new Uri(url).Host + "：" + ex.Message);
                }

            throw new IOException(String.Join("；", errors));
        }
    }
}

namespace PaperDays
{
    public static class HolidayStyle
    {
        public static readonly string[] Colors =
        {
            "#A24939",
            "#246A77",
            "#84528A",
            "#5268A0",
            "#80651D",
            "#32644A",
            "#9B466B",
            "#9A572A",
            "#526E27",
            "#3D6075"
        };
        /// <summary>
        /// 处理HolidayFallback中的 Code 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Code(CalendarMark m)
        {
            return (m.Source ?? "").Split('·')[0].Trim();
        }

        /// <summary>
        /// 处理HolidayFallback中的 ColorFor 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string ColorFor(string code)
        {
            int i = System.Array.IndexOf(Calendars.Codes, code);
            return Colors[i < 0 ? 0 : i];
        }

        /// <summary>
        /// 处理HolidayFallback中的 Work 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static bool Work(CalendarMark m)
        {
            return m.Label.StartsWith("班 ·") || m.Label == "补班";
        }

        /// <summary>
        /// 处理HolidayFallback中的 Label 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Label(CalendarMark m)
        {
            string code = Code(m), label = m.Label;
            if (code == "CN")
            {
                label = label.Replace("休 · ", "").Replace("班 · ", "").Replace("节 · ", "");
                if (!label.StartsWith("中国"))
                    label = "中国 · " + label;
            }

            return label;
        }
    }

    public partial class Scene
    {
        /// <summary>
        /// 绘制HolidayFallback中的 DrawHoliday 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void DrawHoliday(System.Drawing.Graphics g, CalendarMark m, System.Drawing.RectangleF r)
        {
            string code = HolidayStyle.Code(m);
            if (System.Array.IndexOf(Calendars.Codes, code) < 0)
            {
                Text(g, m.Label, r.X, r.Y, r.Width, r.Height, 10, Ink);
                return;
            }

            var color = System.Drawing.ColorTranslator.FromHtml(HolidayStyle.ColorFor(code));
            Round(g, r, 4, System.Drawing.Color.FromArgb(22, color));
            Round(g, new System.Drawing.RectangleF(r.X, r.Y + 1, 18, 18), 4, color);
            Text(g, HolidayStyle.Work(m) ? "班" : "休", r.X + 2, r.Y + 1, 17, 18, 9, System.Drawing.Color.White);
            Text(g, HolidayStyle.Label(m), r.X + 23, r.Y, r.Width - 25, r.Height, 10, color);
        }
    }
}