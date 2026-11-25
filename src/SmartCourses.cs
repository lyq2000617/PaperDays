using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PaperDays
{
    public static class SmartCourses
    {
        /// <summary>
        /// 处理SmartCourses中的 Key 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static string Key(string v)
        {
            return Regex.Replace(v ?? "", @"\s+", "").ToLowerInvariant();
        }

        /// <summary>
        /// 处理SmartCourses中的 Day 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static int Day(string v)
        {
            var match = Regex.Match(v ?? "", @"(?:星期|周)[一二三四五六日天]");
            if (match.Success)
                v = match.Value;
            try
            {
                return CourseImporter.Day(v);
            }
            catch
            {
            }

            string[] en =
            {
                "monday",
                "tuesday",
                "wednesday",
                "thursday",
                "friday",
                "saturday",
                "sunday"
            };
            for (int i = 0; i < 7; i++)
                if (Key(v) == en[i] || Key(v) == en[i].Substring(0, 3))
                    return i + 1;
            return 0;
        }

        /// <summary>
        /// 查找SmartCourses中的 Find 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static int Find(List<string> row, params string[] keys)
        {
            return row.FindIndex(v => keys.Contains(Key(v)));
        }

        /// <summary>
        /// 处理SmartCourses中的 Range 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static Match Range(string value)
        {
            return Regex.Match(value ?? "", @"(\d{1,2}[:：]\d{2})\s*[-—–~～至]\s*(\d{1,2}[:：]\d{2})");
        }

        /// <summary>
        /// 处理SmartCourses中的 Time 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static TimeSpan Time(string value)
        {
            try
            {
                return CourseImporter.Time(value);
            }
            catch
            {
                return TimeSpan.FromMinutes(-1);
            }
        }

        /// <summary>
        /// 处理SmartCourses中的 Col 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static int Col(string cell)
        {
            int n = 0;
            foreach (char c in cell.TakeWhile(Char.IsLetter))
                n = n * 26 + Char.ToUpperInvariant(c) - 'A' + 1;
            return n - 1;
        }

        /// <summary>
        /// 解析输入为业务对象，并拒绝格式错误的数据。
        /// </summary>
        public static List<CourseRow> Parse(SheetData sheet)
        {
            var rows = sheet.Rows.Select(r => r.ToList()).ToList();
            var merged = new Dictionary<string, int[]>();
            foreach (var area in sheet.Merges)
            {
                var ends = area.Split(':');
                if (ends.Length != 2)
                    continue;
                int a = Int32.Parse(Regex.Match(ends[0], @"\d+").Value) - 1, b = Int32.Parse(Regex.Match(ends[1], @"\d+").Value) - 1, c = Col(ends[0]), d = Col(ends[1]);
                if (a < 0 || b >= rows.Count || c < 0 || d >= 100)
                    continue;
                string value = CourseImporter.Cell(rows[a], c);
                for (int r = a; r <= b; r++)
                    for (int x = c; x <= d; x++)
                    {
                        while (rows[r].Count <= x)
                            rows[r].Add("");
                        rows[r][x] = value;
                        merged[r + "," + x] = new[]
                        {
                            a,
                            b,
                            c,
                            d
                        };
                    }
            }

            var result = new List<CourseRow>();
            for (int h = 0; h < Math.Min(30, rows.Count); h++)
            {
                var header = rows[h];
                int day = Find(header, "星期", "周几", "星期几", "weekday", "day"), title = Find(header, "课程", "科目", "课程名称", "内容", "事项", "subject", "course", "title");
                if (day < 0 || (title < 0 && Find(header, "班级", "授课班级", "class") < 0))
                    continue;
                int start = Find(header, "开始时间", "开始", "上课时间", "start"), end = Find(header, "结束时间", "结束", "下课时间", "end"), time = Find(header, "时间", "时段", "time"), group = Find(header, "班级", "授课班级", "class"), room = Find(header, "教室", "地点", "room"), emoji = Find(header, "emoji", "标记"), period = Find(header, "节次", "节数", "课时", "period");
                for (int r = h + 1; r < rows.Count; r++)
                {
                    var row = rows[r];
                    string name = CourseImporter.Cell(row, title);
                    if (String.IsNullOrWhiteSpace(name) && String.IsNullOrWhiteSpace(CourseImporter.Cell(row, group)))
                        continue;
                    int dow = Day(CourseImporter.Cell(row, day));
                    if (dow == 0)
                        throw new Exception("第 " + (r + 1) + " 行无法确定星期，请在导入预览中使用周课表。");
                    var range = Range(CourseImporter.Cell(row, time));
                    result.Add(new CourseRow { Day = dow, Title = name, Period = Period(CourseImporter.Cell(row, period)), EndPeriod = Period(CourseImporter.Cell(row, period), true), Class = group < 0 ? sheet.Name : CourseImporter.Cell(row, group), Room = CourseImporter.Cell(row, room), Emoji = CourseImporter.Cell(row, emoji), Start = range.Success ? Time(range.Groups[1].Value) : Time(CourseImporter.Cell(row, start)), End = range.Success ? Time(range.Groups[2].Value) : Time(CourseImporter.Cell(row, end)) });
                }

                return Require(result);
            }

            for (int h = 0; h < Math.Min(30, rows.Count); h++)
            {
                int[] days = rows[h].Select(Day).ToArray();
                if (days.Count(d => d > 0) < 2)
                    continue;
                for (int r = h + 1; r < rows.Count; r++)
                    for (int c = 0; c < days.Length; c++)
                    {
                        if (days[c] == 0)
                            continue;
                        string text = CourseImporter.Cell(rows[r], c);
                        if (String.IsNullOrWhiteSpace(text))
                            continue;
                        int last = r;
                        int[] merge;
                        if (merged.TryGetValue(r + "," + c, out merge))
                        {
                            if (r != merge[0])
                                continue;
                            last = merge[1];
                        }

                        var firstTime = RowTime(rows[r], days);
                        var lastTime = RowTime(rows[last], days);
                        string firstPeriod = RowPeriod(rows[r], days, false), lastPeriod = RowPeriod(rows[last], days, true);
                        string[] parts = text.Split(new[] { '|', '｜' }, StringSplitOptions.RemoveEmptyEntries);
                        result.Add(new CourseRow { Day = days[c], Period = firstPeriod, EndPeriod = lastPeriod, Title = parts.Length > 1 ? parts[1].Trim() : text.Trim(), Class = parts.Length > 1 ? parts[0].Trim() : sheet.Name, Room = parts.Length > 2 ? parts[2].Trim() : "", Emoji = parts.Length > 3 ? parts[3].Trim() : "", Start = firstTime == null ? TimeSpan.FromMinutes(-1) : firstTime[0], End = lastTime == null ? TimeSpan.FromMinutes(-1) : lastTime[1] });
                    }

                return Require(result);
            }

            throw new Exception("尚未识别此布局。支持按行列出课程，或星期横排的周课表；可以提供原表以适配，不必先改 Excel。");
        }

        /// <summary>
        /// 处理SmartCourses中的 Period 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Period(string value, bool last = false)
        {
            value = Regex.Replace(value ?? "", @"\s+", "");
            if (String.IsNullOrEmpty(value) || value.Contains(":") || value.Contains("："))
                return null;
            var m = Regex.Match(value, @"^(?:上午|下午|晚上)?第?([一二三四五六七八九十]|\d{1,2})(?:[-—～~至、]([一二三四五六七八九十]|\d{1,2}))?(?:节|课)?$");
            if (!m.Success)
                return null;
            string n = last && m.Groups[2].Success ? m.Groups[2].Value : m.Groups[1].Value;
            int i = "一二三四五六七八九十".IndexOf(n);
            return i >= 0 ? (i + 1).ToString() : n;
        }

        /// <summary>
        /// 处理SmartCourses中的 RowPeriod 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static string RowPeriod(List<string> row, int[] days, bool last)
        {
            return row.Where((v, i) => i >= days.Length || days[i] == 0).Select(v => Period(v, last)).FirstOrDefault(v => v != null);
        }

        /// <summary>
        /// 处理SmartCourses中的 RowTime 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static TimeSpan[] RowTime(List<string> row, int[] days)
        {
            var cells = row.Where((v, i) => i >= days.Length || days[i] == 0).ToList();
            var range = cells.Select(Range).FirstOrDefault(m => m.Success);
            if (range != null)
                return new[]
                {
                    Time(range.Groups[1].Value),
                    Time(range.Groups[2].Value)
                };
            var times = cells.Where(v => v.Contains(":") || v.Contains("：") || Regex.IsMatch(v, @"^0\.\d+$")).Select(Time).Where(v => v >= TimeSpan.Zero).ToArray();
            return times.Length == 2 ? times : null;
        }

        /// <summary>
        /// 读取课程节次映射，供旧格式工作簿兼容导入。
        /// </summary>
        public static Dictionary<string, TimeSpan[]> ReadPeriods(IEnumerable<SheetData> sheets)
        {
            var result = new Dictionary<string, TimeSpan[]>();
            var ambiguous = new HashSet<string>();
            foreach (var sheet in sheets)
                for (int h = 0; h < Math.Min(30, sheet.Rows.Count); h++)
                {
                    var header = sheet.Rows[h];
                    int p = Find(header, "节次", "节数", "课时", "period"), a = Find(header, "开始", "开始时间", "上课时间", "start"), b = Find(header, "结束", "结束时间", "下课时间", "end"), t = Find(header, "时间", "时段", "time");
                    if (p < 0 || ((a < 0 || b < 0) && t < 0))
                        continue;
                    foreach (var row in sheet.Rows.Skip(h + 1))
                    {
                        string key = Period(CourseImporter.Cell(row, p));
                        if (key == null)
                            continue;
                        var range = Range(CourseImporter.Cell(row, t));
                        var start = range.Success ? Time(range.Groups[1].Value) : Time(CourseImporter.Cell(row, a));
                        var end = range.Success ? Time(range.Groups[2].Value) : Time(CourseImporter.Cell(row, b));
                        if (start < TimeSpan.Zero || end <= start)
                            continue;
                        TimeSpan[] old;
                        if (result.TryGetValue(key, out old) && (old[0] != start || old[1] != end))
                            ambiguous.Add(key);
                        else
                            result[key] = new[]
                            {
                                start,
                                end
                            };
                    }

                    break;
                }

            foreach (string key in ambiguous)
                result.Remove(key);
            return result;
        }

        /// <summary>
        /// 处理SmartCourses中的 Require 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static List<CourseRow> Require(List<CourseRow> rows)
        {
            if (rows.Count == 0)
                throw new Exception("没有识别到课程");
            return rows;
        }
    }
}