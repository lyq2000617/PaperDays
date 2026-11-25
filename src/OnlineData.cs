using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PaperDays
{
    public static class OnlineData
    {
        public static bool Enabled = true;
        /// <summary>
        /// 在超时和大小限制内请求外部数据，网络失败向调用方报告。
        /// </summary>
        public static string Download(string url)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var r = (HttpWebRequest)WebRequest.Create(url);
            r.Timeout = 12000;
            r.ReadWriteTimeout = 12000;
            r.UserAgent = "PaperDays/0.13";
            using (var response = r.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var memory = new MemoryStream())
            {
                var buffer = new byte[8192];
                int n;
                while ((n = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (memory.Length + n > 4000000)
                        throw new IOException("数据超过大小限制");
                    memory.Write(buffer, 0, n);
                }

                return Encoding.UTF8.GetString(memory.ToArray()).TrimStart('\uFEFF');
            }
        }

        /// <summary>
        /// 保存联网缓存中的 Save 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static void Save(string path, string content)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(temporary, content, Encoding.UTF8);
            if (File.Exists(path))
                File.Replace(temporary, path, null);
            else
                File.Move(temporary, path);
        }
    }

    public class HoroscopeDimension
    {
        public int score;
        public string text;
    }

    public class HoroscopeLucky
    {
        public int number;
        public string color, time, direction, friend_sign;
    }

    public class HoroscopeDay
    {
        public string sign, date, summary, source;
        public bool? degraded;
        public Dictionary<string, HoroscopeDimension> dimensions;
        public HoroscopeLucky lucky;
    }

    public class HoroscopeResponse
    {
        public int code;
        public string msg;
        public HoroscopeDay data;
    }

    public class HoroscopeCache
    {
        public string Json;
        public DateTime FetchedUtc;
    }

    public static class Horoscopes
    {
        static readonly object gate = new object ();
        /// <summary>
        /// 处理联网缓存中的 Url 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Url(string sign, DateTime date)
        {
            return "https://tools.wktyl.com/api/horoscope/query?sign=" + Uri.EscapeDataString(sign) + "&date=" + date.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 处理联网缓存中的 PathFor 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static string PathFor(string sign, DateTime date)
        {
            int index = Array.IndexOf(Reading.Signs, sign);
            if (index < 0)
                throw new ArgumentException("未知星座");
            return Path.Combine(Data.Root, "horoscope-complete-cache", date.ToString("yyyy-MM-dd") + "-" + index + ".json");
        }

        /// <summary>
        /// 处理联网缓存中的 Dim 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static HoroscopeDimension Dim(HoroscopeDay d, string name)
        {
            HoroscopeDimension item;
            return d.dimensions.TryGetValue(name, out item) && item != null ? item : new HoroscopeDimension();
        }

        /// <summary>
        /// 解析输入为业务对象，并拒绝格式错误的数据。
        /// </summary>
        public static Reading Parse(string json, string sign, DateTime date, DateTime fetchedUtc)
        {
            var response = Data.Json().Deserialize<HoroscopeResponse>(json);
            var d = response == null ? null : response.data;
            if (response == null || response.code != 1 || d == null)
                throw new IOException("来源未返回可用运势");
            if (d.sign != sign || d.date != date.ToString("yyyy-MM-dd"))
                throw new IOException("来源日期或星座不匹配，未作为今日运势显示");
            if (d.degraded != false || !(new[]
            {
                "upstream",
                "cache",
                "db"
            }

            ).Contains(d.source))
                throw new IOException("来源返回了降级生成内容，已拒绝显示");
            if (d.dimensions == null || String.IsNullOrWhiteSpace(d.summary))
                throw new IOException("来源内容不完整");
            var overall = Dim(d, "overall");
            return new Reading
            {
                Title = sign,
                Grade = L.T("来源评分 ") + overall.score + " / 5",
                Outlook = d.summary,
                Interpretation = overall.text ?? "",
                Caution = "第三方娱乐解读，不保证实际预测。",
                Number = d.lucky == null ? 0 : d.lucky.number,
                Color = null,
                LuckyName = d.lucky == null ? L.T("未提供") : d.lucky.color,
                Overall = overall.score,
                Work = Dim(d, "career").score,
                Social = Dim(d, "love").score,
                Energy = Dim(d, "health").score,
                CareerText = Dim(d, "career").text,
                LoveText = Dim(d, "love").text,
                WealthText = Dim(d, "fortune").text,
                HealthText = Dim(d, "health").text,
                TravelText = Dim(d, "travel").text,
                SourceDate = d.date,
                SourceName = "闲言 API · 魅族源",
                SourceUrl = Url(sign, date),
                FetchedUtc = fetchedUtc,
                IsOnline = true
            };
        }

        /// <summary>
        /// 处理联网缓存中的 Missing 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static Reading Missing(string sign, DateTime date)
        {
            return new Reading
            {
                Title = sign,
                Grade = L.T("未获取今日数据"),
                Outlook = L.T("请联网刷新。不会用预设文案或旧日期内容代替。"),
                Interpretation = "",
                Caution = L.T("运势属于娱乐内容，不保证实际预测。"),
                SourceDate = date.ToString("yyyy-MM-dd"),
                SourceName = "小小 API · 星座屋内容",
                SourceUrl = CompleteHoroscopes.Url(sign),
                LuckyName = L.T("未提供")
            };
        }

        /// <summary>
        /// 处理联网缓存中的 Cached 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static Reading Cached(string sign, DateTime date)
        {
            return ReadingTranslation.Cached(RawCached(sign,date),L.Language);
        }

        static Reading RawCached(string sign, DateTime date)
        {
            try
            {
                var path = PathFor(sign, date);
                if (File.Exists(path))
                {
                    var c = Data.Json().Deserialize<HoroscopeCache>(File.ReadAllText(path));
                    return CompleteHoroscopes.Parse(c.Json, sign, date, c.FetchedUtc);
                }
            }
            catch
            {
            }

            return Missing(sign, date);
        }

        /// <summary>
        /// 更新联网缓存中的 UpdateAsync 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static Task<Reading> UpdateAsync(string sign, DateTime date, bool force = false)
        {
            string language = L.Language;
            if (!OnlineData.Enabled)
                return Task.FromResult(Cached(sign, date));
            return Task.Run(delegate
            {
                lock (gate)
                {
                    var old = RawCached(sign, date);
                    if (!force && old.IsOnline && DateTime.UtcNow - old.FetchedUtc.ToUniversalTime() < TimeSpan.FromMinutes(30))
                        return ReadingTranslation.Translate(old,language,force);
                    string json = OnlineData.Download(CompleteHoroscopes.Url(sign));
                    DateTime now = DateTime.UtcNow;
                    var value = CompleteHoroscopes.Parse(json, sign, date, now);
                    OnlineData.Save(PathFor(sign, date), Data.Json().Serialize(new HoroscopeCache { Json = json, FetchedUtc = now }));
                    return ReadingTranslation.Translate(value,language,force);
                }
            });
        }
    }

    public class ChinaDay
    {
        public string name, date;
        public bool isOffDay;
    }

    public class ChinaYear
    {
        public int year;
        public string[] papers;
        public List<ChinaDay> days;
    }

    public static class HolidayUpdates
    {
        static readonly object gate = new object ();
        static Dictionary<string, DateTime> attempts = new Dictionary<string, DateTime>();
        /// <summary>
        /// 处理联网缓存中的 Years 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static int[] Years(DateTime month)
        {
            return new[]
            {
                month.AddMonths(-1).Year,
                month.Year,
                month.AddMonths(1).Year
            }.Distinct().ToArray();
        }

        /// <summary>
        /// 处理联网缓存中的 CachePath 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string CachePath(string code, int year)
        {
            return Path.Combine(Data.Root, code == "CN" ? "holiday-cache" : "holiday-observed-cache", code + "-" + year + ".json");
        }

        /// <summary>
        /// 解析联网缓存中的 ParseChina 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static List<CalendarMark> ParseChina(string json, int year)
        {
            if (json.Contains("\"holiday\""))
                return HolidayFallback.ParseTimor(json, year);
            var doc = Data.Json().Deserialize<ChinaYear>(json);
            if (doc == null || doc.year != year || doc.days == null)
                throw new IOException("中国假日数据年份或结构有误");
            if (doc.days.Count == 0 || doc.papers == null || doc.papers.Length == 0)
                throw new IOException("中国 " + year + " 年放假安排尚未从来源获取到公告");
            if (!doc.papers.All(p =>
            {
                Uri u;
                return Uri.TryCreate(p, UriKind.Absolute, out u) && u.Scheme == "https" && (u.Host == "gov.cn" || u.Host.EndsWith(".gov.cn"));
            }))
                throw new IOException("中国假日数据缺少国务院公告来源");
            var list = new List<CalendarMark>();
            foreach (var d in doc.days)
            {
                DateTime date;
                if (!DateTime.TryParseExact(d.date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date) || Math.Abs(date.Year - year) > 1 || String.IsNullOrWhiteSpace(d.name))
                    throw new IOException("假日日期或名称无效");
                list.Add(new CalendarMark { Date = d.date, Label = d.isOffDay ? "休 · " + d.name : "班 · " + d.name, Source = "CN · 国务院公告（holiday-cn 整理） " + String.Join(" ", doc.papers), Color = d.isOffDay ? "#B68A7B" : "#9A967B" });
            }

            return list;
        }

        /// <summary>
        /// 读取并解析本地或系统数据，调用方负责处理失败。
        /// </summary>
        public static List<CalendarMark> Read(string code, int year)
        {
            foreach (string file in new[]
            {
                CachePath(code, year),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "holidays", code + "-" + year + ".json")
            }

            )
                if (File.Exists(file))
                {
                    try
                    {
                        var marks = code == "CN" ? ParseChina(File.ReadAllText(file), year) : Calendars.ParseHolidays(File.ReadAllText(file), code).Where(m => m.Date.StartsWith(year.ToString() + "-")).ToList();
                        if (marks.Count > 0)
                            return marks;
                    }
                    catch
                    {
                    }
                }

            return code == "CN" ? (year == 2026 ? Calendars.China() : HolidayFallback.Statutory(year)) : new List<CalendarMark>();
        }

        /// <summary>
        /// 更新联网缓存中的 UpdateAsync 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static Task<string> UpdateAsync(string[] codes, DateTime month, bool force = false)
        {
            if (!OnlineData.Enabled)
                return Task.FromResult("");
            return Task.Run(delegate
            {
                var errors = new List<string>();
                foreach (string code in codes.Distinct().Where(c => Calendars.Codes.Contains(c)))
                    foreach (int year in Years(month))
                    {
                        string key = code + year, cache = CachePath(code, year);
                        lock (gate)
                        {
                            DateTime last;
                            if (!force && ((File.Exists(cache) && DateTime.UtcNow - File.GetLastWriteTimeUtc(cache) < TimeSpan.FromDays(1)) || (attempts.TryGetValue(key, out last) && DateTime.UtcNow - last < TimeSpan.FromHours(1))))
                                continue;
                            attempts[key] = DateTime.UtcNow;
                        }

                        try
                        {
                            string json = HolidayFallback.Download(code, year);
                            var marks = code == "CN" ? ParseChina(json, year) : Calendars.ParseHolidays(json, code);
                            if (code != "CN" && marks.Any(m => !m.Date.StartsWith(year.ToString() + "-")))
                                throw new IOException("来源年份不匹配");
                            if (marks.Count == 0)
                                throw new IOException("来源暂无假日数据");
                            OnlineData.Save(cache, json);
                        }
                        catch (Exception ex)
                        {
                            errors.Add(code + " " + year + "：" + ex.Message);
                        }
                    }

                return String.Join("；", errors);
            });
        }

        /// <summary>
        /// 处理联网缓存中的 MonthStatus 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string MonthStatus(string code, DateTime month)
        {
            int i = Array.IndexOf(Calendars.Codes, code);
            string name = i < 0 ? code : Calendars.Names[i];
            var all = Read(code, month.Year);
            if (all.Count == 0)
                return name + "：" + month.Year + " 年" + (code == "CN" ? "放假安排尚未获取" : "假日数据尚未获取");
            if (code == "CN" && all.All(m => m.Source.Contains("现行法定日期")))
                return name + "：法定日期已显示，" + month.Year + " 年放假调休待公告更新";
            int count = all.Count(m => m.Date.StartsWith(month.ToString("yyyy-MM")));
            return name + "：" + (count == 0 ? "本月无已公布假日" : count + " 条");
        }

        /// <summary>
        /// 载入联网缓存中的 Load 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static List<CalendarMark> Load(State s, DateTime month, out string status)
        {
            var list = new List<CalendarMark>();
            foreach (string code in s.Countries.Distinct())
                foreach (int year in Years(month))
                    list.AddRange(Read(code, year));
            list = list.GroupBy(x => x.Source.Split('·')[0] + "|" + x.Date + "|" + x.Label).Select(g => g.First()).ToList();
            status = month.ToString("yyyy年M月") + " · " + String.Join("；", s.Countries.Distinct().Select(c => MonthStatus(c, month)));
            return list;
        }
    }
}