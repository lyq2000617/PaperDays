using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version17Tests
    {
        /// <summary>
        /// 执行Version17Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string dir)
        {
            foreach (string code in Calendars.Codes.Where(c => c != "CN"))
            {
                var holidayRows = HolidayUpdates.Read(code, 2026);
                check(holidayRows.Count > 0 && holidayRows.All(r => !r.Label.Contains("译名待补")), "bundled holiday names translated for " + code);
            }

            string note;
            var state = new State
            {
                Countries = new List<string>
                {
                    "CN",
                    "KR",
                    "US"
                }
            };
            var marks = Calendars.Load(state, new DateTime(2026, 10, 1), out note);
            check(marks.Any(m => m.Date == "2026-10-01" && m.Label.Contains("国庆")) && marks.Any(m => m.Date == "2026-10-03" && m.Label.Contains("开天节")), "Chinese and Korean holidays loaded together with Chinese names");
            var scene = new Scene
            {
                State = state,
                Month = new DateTime(2026, 10, 1),
                Holidays = marks
            };
            using (var image = scene.Render(new Size(1920, 1080)))
                image.Save(Path.Combine(dir, "holidays-cn-kr-us.png"));
            check(scene.Hits.Any(h => h.Action == "day" && h.Date == new DateTime(2026, 10, 3)), "calendar holiday entries open full day details");
            var weekly = new Scene
            {
                State = new State
                {
                    CalendarView = "week"
                },
                Selected = new DateTime(2026, 10, 1),
                Holidays = new List<CalendarMark>
                {
                    new CalendarMark
                    {
                        Date = "2026-10-01",
                        Label = "A"
                    },
                    new CalendarMark
                    {
                        Date = "2026-10-01",
                        Label = "B"
                    },
                    new CalendarMark
                    {
                        Date = "2026-10-01",
                        Label = "C"
                    }
                }
            };
            using (var image = weekly.Render(new Size(1920, 1080)))
            {
            }

            check(weekly.Hits.Count(h => h.Action == "day" && h.Date == new DateTime(2026, 10, 1)) >= 3, "week exposes both holiday rows plus overflow details");
            string cache = HolidayUpdates.CachePath("US", 2026);
            Directory.CreateDirectory(Path.GetDirectoryName(cache));
            File.WriteAllText(cache, "broken");
            check(HolidayUpdates.Read("US", 2026).Count > 0, "invalid online cache falls back to bundled holidays");
            File.Delete(cache);
            var matrix = new SheetData
            {
                Name = "学校课表",
                Rows = new List<List<string>>
                {
                    new List<string>
                    {
                        "时间",
                        "周一",
                        "周二"
                    },
                    new List<string>
                    {
                        "08:00-08:45",
                        "数学",
                        "英语"
                    },
                    new List<string>
                    {
                        "08:55-09:40",
                        "",
                        "语文"
                    }
                },
                Merges = new List<string>
                {
                    "B2:B3"
                }
            };
            var courses = SmartCourses.Parse(matrix);
            check(courses.Count == 3 && courses.First(c => c.Title == "数学").End == new TimeSpan(9, 40, 0), "plain names and merged multi-period course parsed without editing Excel");
            var plain = new SheetData
            {
                Name = "我的表",
                Rows = new List<List<string>>
                {
                    new List<string>
                    {
                        "weekday",
                        "start",
                        "end",
                        "subject"
                    },
                    new List<string>
                    {
                        "Monday",
                        "09:00",
                        "10:00",
                        "科学"
                    }
                }
            };
            check(SmartCourses.Parse(plain).Single().Class == "我的表", "class column optional and English weekday supported");
            matrix.Rows[1][0] = "第一节";
            matrix.Rows[2][0] = "第二节";
            check(SmartCourses.Parse(matrix).All(c => c.Start < TimeSpan.Zero), "period-only schedules remain unresolved instead of invented times");
            var s = new State();
            var rows = new List<CourseRow>
            {
                new CourseRow
                {
                    Day = 1,
                    Start = TimeSpan.FromHours(8),
                    End = TimeSpan.FromHours(9),
                    Class = "一班",
                    Title = "数学"
                },
                new CourseRow
                {
                    Day = 2,
                    Start = TimeSpan.FromHours(8),
                    End = TimeSpan.FromHours(9),
                    Class = "二班",
                    Title = "数学"
                },
                new CourseRow
                {
                    Day = 3,
                    Start = TimeSpan.FromHours(8),
                    End = TimeSpan.FromHours(9),
                    Class = "一班",
                    Title = "英语"
                }
            };
            CourseImporter.Commit(s, rows, DateTime.Today, 2, true);
            check(s.Events.Where(e => e.Title == "数学").Select(e => e.Color).Distinct().Count() == 1 && s.Events.Select(e => e.Color).Distinct().Count() == 2, "same subject across classes shares color while different subjects differ");
            string batch = s.Events[0].ImportBatch;
            s.Events.Add(new Entry { Title = "个人日程", Start = DateTime.Today, End = DateTime.Today.AddHours(1) });
            check(CourseBook.Delete(s, s.Events.Where(e => e.ImportBatch == batch).ToList()) == 6 && s.Events.Count == 1 && s.Events[0].Title == "个人日程", "batch deletion leaves personal appointments untouched");
            using (var f = new DivinationDialog(null))
            {
                f.Show();
                Application.DoEvents();
                var buttons = f.Controls.OfType<Panel>().SelectMany(p => p.Controls.OfType<Button>());
                check(f.ControlBox && f.MinimizeBox && f.FormBorderStyle != FormBorderStyle.None, "divination exposes native close and minimize buttons");
                f.WindowState = FormWindowState.Minimized;
                Application.DoEvents();
                check(f.WindowState == FormWindowState.Minimized && !f.IsDisposed, "minimizing keeps dialog available for later");
                f.Close();
            }
        }
    }
#endif

}