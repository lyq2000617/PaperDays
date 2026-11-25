using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version20Tests
    {
        /// <summary>
        /// 执行Version20Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string dir)
        {
            foreach (int y in new[]
            {
                2027,
                2028,
                2032,
                2050,
                2099
            }

            )
            {
                var days = HolidayUpdates.Read("CN", y);
                check(days.Count == 13 && days.All(d => d.Label.EndsWith("（法定）")), "future CN statutory dates distinct from annual leave schedule " + y);
            }

            check(HolidayUpdates.Read("CN", 2027).Any(d => d.Date == "2027-02-06" && d.Label.Contains("春节")), "HKO 2027 Spring Festival is February 6");
            foreach (string code in Calendars.Codes.Where(c => c != "CN"))
                foreach (int y in new[]
                {
                    2028,
                    2029,
                    2030
                }

                )
                {
                    var marks = HolidayUpdates.Read(code, y);
                    check(marks.Count > 0 && marks.All(m => !m.Label.Contains("译名待补")), "future holidays translated " + code + " " + y);
                }

            string v3 = "[{\"date\":\"2031-01-01\",\"name\":\"New Year's Day\",\"global\":true,\"types\":[\"Public\"]}]";
            int attempts = 0;
            string fallback = HolidayFallback.Fetch("US", 2031, url =>
            {
                attempts++;
                return v3;
            });
            check(attempts == 2 && Calendars.ParseHolidays(fallback, "US").First().Label == "美国 · 元旦", "observed-date v3 normalizes Chinese national holiday");
            bool rejected = false;
            try
            {
                HolidayFallback.Fetch("US", 2032, url => v3);
            }
            catch
            {
                rejected = true;
            }

            check(rejected, "wrong-year source rejected");
            string timor = "{\"code\":0,\"holiday\":{\"01-01\":{\"date\":\"2031-01-01\",\"name\":\"元旦\",\"holiday\":true}}}";
            attempts = 0;
            fallback = HolidayFallback.Fetch("CN", 2031, url =>
            {
                attempts++;
                return attempts == 1 ? "{\"year\":2031,\"papers\":[],\"days\":[]}" : timor;
            });
            check(attempts == 2 && HolidayUpdates.ParseChina(fallback, 2031).Single().Source.Contains("提莫"), "China independent fallback retains honest source attribution");
            string cache = HolidayUpdates.CachePath("CN", 2031);
            OnlineData.Save(cache, fallback);
            check(HolidayUpdates.Read("CN", 2031).Single().Label == "休 · 元旦", "offline read recognizes backup format");
            File.WriteAllText(cache, "broken");
            check(HolidayUpdates.Read("CN", 2031).Count == 13, "corrupt cache still displays future festivals");
            File.Delete(cache);
            var teacher = new CourseRow
            {
                Day = 1,
                Start = TimeSpan.FromHours(8),
                End = TimeSpan.FromHours(9),
                Class = "高一三班",
                Title = "",
                Teacher = true
            };
            var state = new State();
            CourseImporter.Commit(state, new List<CourseRow> { teacher }, new DateTime(2027, 1, 1), 2, false);
            check(state.Events.Count == 2 && state.Events.All(e => e.Title == "高一三班" && e.ClassName == "") && state.Events[0].Start == new DateTime(2027, 1, 4, 8, 0, 0), "teacher class-only schedule uses automatic date and repeats");
            var student = new CourseRow
            {
                Day = 1,
                Start = TimeSpan.FromHours(10),
                End = TimeSpan.FromHours(11),
                Class = "二班",
                Title = "数学",
                FirstDate = new DateTime(2027, 2, 3)
            };
            CourseImporter.Commit(state, new List<CourseRow> { student }, DateTime.Today, 2, false);
            check(state.Events.Any(e => e.Title == "数学" && e.ClassName == "二班" && e.Start == new DateTime(2027, 2, 3, 10, 0, 0)), "student retains course and class plus explicit first date");
            check(!CourseDates.Optional("").HasValue && CourseDates.Optional("2028-2-29").Value.Day == 29, "optional date supports leap year input");
            rejected = false;
            try
            {
                CourseDates.Optional("2027-02-29");
            }
            catch
            {
                rejected = true;
            }

            check(rejected, "invalid calendar date rejected");
            rejected = false;
            try
            {
                CourseImporter.Time("25:30");
            }
            catch
            {
                rejected = true;
            }

            check(rejected && CourseImporter.Time("8:05") == new TimeSpan(8, 5, 0), "time input enforces valid 24-hour time");
            var sheet = new SheetData
            {
                Name = "教师",
                Rows = new List<List<string>>
                {
                    new List<string>
                    {
                        "星期",
                        "开始",
                        "结束",
                        "班级"
                    },
                    new List<string>
                    {
                        "周一",
                        "08:00",
                        "09:00",
                        "三班"
                    }
                }
            };
            check(SmartCourses.Parse(sheet).Count == 1, "teacher Excel without course column recognized");
            using (var dialog = new CourseImportDialog(new State()))
            {
                dialog.Show();
                dialog.PreviewGrid.Rows.Add("周一", "8:00", "9:00", "三班", "", "", "", "");
                dialog.ConfirmImport();
                check(dialog.DialogResult == DialogResult.OK && dialog.Courses.Single().Title == "三班" && !dialog.Courses.Single().FirstDate.HasValue, "real import window saves blank course and first date");
            }

            string title = String.Concat(Enumerable.Repeat("完成课件修改，并整理各班教学进度与课堂反馈。", 35));
            state = new State();
            state.Todos.Add(new Todo { Title = title });
            state.Todos.Add(new Todo { Title = "准备明天的公开课材料，核对练习册的重点题目，将需要讲解的内容标注出来。" });
            var scene = new Scene
            {
                State = state
            };
            using (var image = new Bitmap(1600, 900))
            using (var g = Graphics.FromImage(image))
            {
                var pages = Scene.TodoPages(g, state.Todos, 205, 490);
                check(pages.Count > 1 && String.Concat(pages.SelectMany(a => a).Where(b => b.Todo == state.Todos[0]).Select(b => b.Text.Replace("\n", ""))) == title, "wrapped todo pagination preserves every character");
                check(pages.All(a => a.Sum(b => b.Height) <= 490), "wrapped todo pages never overlap footer");
            }

            state.Todos.RemoveAt(0);
            using (var image = scene.Render(new Size(1920, 1080)))
                image.Save(Path.Combine(dir, "wrapped-todo.png"));
            check(scene.Hits.Any(h => h.Action == "tododelete") && scene.Hits.Any(h => h.Action == "todo"), "wrapped todo retains checkoff and delete actions");
        }
    }
#endif

}