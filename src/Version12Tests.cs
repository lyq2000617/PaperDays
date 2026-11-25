using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PaperDays
{

#if TESTS
    public static class Version12Tests
    {
        /// <summary>
        /// 执行Version12Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            var state = Program.Demo();
            state.IconColumns = 0;
            var scene = new Scene
            {
                State = state
            };
            foreach (var size in new[]
            {
                new Size(1920, 1080),
                new Size(1080, 1920),
                new Size(1366, 768)
            }

            )
            {
                using (var image = scene.Render(size))
                {
                    if (size.Width == 1920)
                        image.Save(Path.Combine(directory, "expanded-zodiac.png"));
                }

                check(!scene.Layout.Sidebar.IsEmpty && scene.Hits.Any(h => h.Action == "leftzodiac") && scene.Hits.Any(h => h.Action == "leftfortune") && !scene.Hits.Any(h => h.Action == "widgetzodiac"), "no-icon-reserve layout displays sidebar contents " + size);
                check(scene.Hits.All(h => h.Box.Left >= 0 && h.Box.Top >= 0 && h.Box.Right <= size.Width && h.Box.Bottom <= size.Height), "expanded sidebar and controls fit " + size);
                check(!scene.Layout.Sidebar.IntersectsWith(scene.Layout.Calendar), "sidebar does not overlap calendar " + size);
            }

            state.Widget = "占卜";
            state.Divination = new DivinationRecord
            {
                A = 3,
                B = 5,
                C = 2,
                Category = "工作",
                Created = DateTime.UtcNow
            };
            using (var image = scene.Render(new Size(1920, 1080)))
                image.Save(Path.Combine(directory, "expanded-divination.png"));
            check(scene.Hits.Any(h => h.Action == "drawfortune") && scene.Hits.Any(h => h.Action == "reading"), "expanded divination shows result and edit/detail actions");
            Data.Write(state);
            check(Data.Read().IconColumns == 0 && Data.Read().Widget == "占卜", "expanded mode and chosen sidebar tab persist");
            state.IconColumns = 2;
            scene.ReservedLeft = 208;
            using (var image = scene.Render(new Size(1920, 1080)))
            {
            }

            check(scene.Layout.Sidebar.IsEmpty && scene.Hits.Any(h => h.Action == "widgetzodiac") && !scene.Hits.Any(h => h.Action == "leftzodiac"), "reserving icons returns to compact footer entries");
            DateTime date = new DateTime(2026, 11, 2);
            var reports = new State();
            reports.Todos.Add(new Todo { Title = "完成课程资料整理", Date = date.ToString("yyyy-MM-dd"), Done = true, CompletedAt = date });
            reports.Todos.Add(new Todo { Title = "提交阶段方案", Date = date.ToString("yyyy-MM-dd"), Done = true, CompletedAt = date.AddDays(1) });
            reports.Todos.Add(new Todo { Title = "尚未完成", Date = date.ToString("yyyy-MM-dd") });
            string weekly = PlanReview.Export(reports, "week", date), monthly = PlanReview.Export(reports, "month", date);
            check(weekly.Contains("AI 周报") && weekly.Contains("2026-11-02 至 2026-11-08") && weekly.Contains("已完成 2 项") && !weekly.Contains("尚未完成"), "AI weekly report contains accurate range and only completed tasks");
            check(monthly.Contains("AI 月报") && monthly.Contains("2026-11-01 至 2026-11-30"), "monthly export uses monthly report instructions and dates");
            check(weekly.Contains("重点工作") && weekly.Contains("问题与风险") && weekly.Contains("简短汇报版") && weekly.Contains("不编造") && weekly.Contains("建议，待本人确认"), "report prompt specifies structure evidence constraints and separate suggestions");
            check(PlanReview.Export(new State(), "week", date).Contains("已完成 0 项") && PlanReview.Export(new State(), "week", date).Contains("不生成虚构周报"), "empty export explicitly prevents invented reports");
            File.WriteAllText(Path.Combine(directory, "AI周报导出示例.txt"), weekly, System.Text.Encoding.UTF8);
            check(PlanReview.ExportButton("week").Contains("AI") && PlanReview.ExportButton("week").Contains("导出") && PlanReview.ExportButton("month").Contains("月报"), "export label explains both AI use and export action");
            check(Wallpaper.StartupCommand(@"C:\Calendar App\PaperDays.exe") == "\"C:\\Calendar App\\PaperDays.exe\" --startup", "autostart command quotes executable with spaces and enables desktop wait");
        }
    }
#endif

}