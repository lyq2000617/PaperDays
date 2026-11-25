using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version13Tests
    {
        /// <summary>
        /// 执行Version13Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            foreach (int count in new[]
            {
                1,
                2,
                3,
                4
            }

            )
            {
                var monitor = new DisplayOption
                {
                    Bounds = new Rectangle(-1920, 0, 1920, 1080),
                    Primary = false
                };
                check(IconArea.For(monitor, count, -1, IntPtr.Zero) > 0, "automatic icon reserve on selected monitor with screen count " + count);
                for (int columns = 1; columns <= 4; columns++)
                    check(IconArea.For(monitor, count, columns, IntPtr.Zero) > 0, "explicit icon columns " + columns + " with screens " + count);
            }

            var state = new State
            {
                IconColumns = 3
            };
            Data.Write(state);
            check(Data.Read().IconColumns == 3, "icon columns persist");
            foreach (var area in new[]
            {
                new Rectangle(-2560, 0, 2560, 1440),
                new Rectangle(1920, -420, 1080, 1880),
                new Rectangle(0, 0, 800, 600)
            }

            )
            {
                var b = WindowPlacement.Fit(new Size(660, 750), area);
                check(area.Contains(b), "dialog stays inside selected monitor " + area);
                WindowPlacement.Target = delegate
                {
                    return area;
                };
                using (var form = new Form())
                {
                    UI.Style(form, "位置测试", 660, 750);
                    check(form.StartPosition == FormStartPosition.Manual && area.Contains(form.Bounds), "styled prompts use selected monitor");
                }
            }

            WindowPlacement.Target = null;
            DateTime date = new DateTime(2030, 1, 2);
            string sign = "天秤座";
            string json = Data.Json().Serialize(new HoroscopeResponse { code = 1, data = new HoroscopeDay { sign = sign, date = "2030-01-02", summary = "来源测试", source = "upstream", degraded = false, dimensions = new System.Collections.Generic.Dictionary<string, HoroscopeDimension> { { "overall", new HoroscopeDimension { score = 3, text = "综合测试" } }, { "career", new HoroscopeDimension { score = 4, text = "事业测试" } }, { "love", new HoroscopeDimension { score = 3, text = "感情测试" } } }, lucky = new HoroscopeLucky { number = 5, color = "蓝色" } } });
            var r = Horoscopes.Parse(json, sign, date, DateTime.UtcNow);
            check(r.IsOnline && r.CareerText == "事业测试" && r.LoveText == "感情测试" && String.IsNullOrEmpty(r.HealthText), "source categories preserved without invented missing sections");
            foreach (string bad in new[]
            {
                json.Replace("2030-01-02", "2029-01-02"),
                json.Replace("upstream", "fallback_local"),
                json.Replace("\"degraded\":false", "\"degraded\":true"),
                json.Replace("天秤座", "白羊座")
            }

            )
            {
                bool rejected = false;
                try
                {
                    Horoscopes.Parse(bad, sign, date, DateTime.UtcNow);
                }
                catch
                {
                    rejected = true;
                }

                check(rejected, "reject mismatched or generated horoscope");
            }

            check(!Horoscopes.Cached(sign, date.AddDays(1)).IsOnline, "next day never inherits previous day horoscope");
            bool missing = false;
            try
            {
                HolidayUpdates.ParseChina("{\"year\":2027,\"papers\":[],\"days\":[]}", 2027);
            }
            catch
            {
                missing = true;
            }

            check(missing, "unpublished China schedule rejected");
            string status;
            var future = new Scene
            {
                State = new State(),
                Month = new DateTime(2032, 2, 1)
            };
            future.Holidays = Calendars.Load(future.State, future.Month, out status);
            using (var image = future.Render(new Size(1600, 900)))
            {
            }

            check(future.Month.Year == 2032 && status.Contains("2032"), "calendar renders future years without expiry");
        }
    }
#endif

}