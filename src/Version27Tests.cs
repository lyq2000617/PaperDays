using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version27Tests
    {
        /// <summary>
        /// 执行Version27Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string dir)
        {
            var d = DateTime.Today;
            var s = new State();
            var t = new Todo
            {
                Date = d.AddDays(-2).ToString("yyyy-MM-dd"),
                Title = "准备课件"
            };
            s.Todos.Add(t);
            DailyArchive.Enable(s, d.AddDays(-2));
            DailyArchive.Sync(s, d.AddDays(-1));
            check(s.TodoArchive.Count == 2 && s.TodoArchive.All(x => !x.Done), "new day creates dated pending archive without overwriting previous day");
            DailyArchive.Sync(s, d);
            t.Title = "准备课件并核对练习";
            t.Done = true;
            t.CompletedAt = d.AddHours(10);
            DailyArchive.Sync(s, d);
            check(s.TodoArchive.Count == 3 && s.TodoArchive.Count(x => x.Done) == 1 && s.TodoArchive.Single(x => x.Date == d.AddDays(-2).ToString("yyyy-MM-dd")).Title == "准备课件", "completion and edits update only current day");
            DailyArchive.Sync(s, d.AddDays(3));
            check(s.TodoArchive.Count == 3, "completed task does not generate future archives");
            string report = TodoExport.Build(s.TodoArchive, "最近三天", false);
            check(report.Contains("仅含用户勾选的 1 项") && report.Contains("已完成 1 项") && report.Contains(d.AddDays(-2).ToString("yyyy-MM-dd")), "export merges same task and lists selected archive dates");
            var past = TodoExport.Filter(s, d.AddDays(-2), d.AddDays(-1), false, 0);
            string pastReport = TodoExport.Build(past, "过去两天", false);
            check(past.Count == 2 && pastReport.Contains("未完成 1 项") && !pastReport.Contains("准备课件并核对练习"), "past export cannot leak later completion or edited content");
            Data.Write(s);
            var saved = Data.Read();
            check(saved.ArchiveEnabled && saved.TodoArchive.Count == 3, "daily snapshots survive restart");
            var removed = s.TodoArchive[1].Id;
            s.DeleteTodo(removed);
            DailyArchive.Sync(s, d);
            check(!s.TodoArchive.Any(x => x.Id == removed) && s.Todos.Count == 1, "deleting an archive neither deletes active task nor regenerates archive");
            var offline = new State();
            offline.Todos.Add(new Todo { Title = "离线期间计划", Date = d.AddDays(-4).ToString("yyyy-MM-dd") });
            DailyArchive.Enable(offline, d);
            check(offline.TodoArchive.Count == 5 && offline.TodoArchive.Count(x => x.Reconstructed) == 4, "offline gap backfilled and marked as reconstructed");
            using (var history = new TodoHistory(() => offline, id =>
            {
            }))
            {
                Tests.ShowHost(history);
                history.From.Value = d;
                history.To.Value = d;
                history.UseRange.Checked = true;
                history.RefreshRows();
                check(history.Items.Items.Count == 1, "history current-day filter includes carried task");
                history.SetAll(true);
                check(history.ExportText().Contains("离线期间计划"), "current-day archive can be checked and exported");
                using (var b = new Bitmap(history.Width, history.Height))
                {
                    history.DrawToBitmap(b, new Rectangle(Point.Empty, history.Size));
                    b.Save(Path.Combine(dir, "daily-archive.png"));
                }

                history.Close();
            }

            foreach (var skin in Skins.All)
            {
                var st = new State
                {
                    Skin = skin.Id
                };
                Skins.Apply(st);
                using (var editor = new Editor(st, null, d))
                {
                    Tests.ShowHost(editor);
                    editor.TitleInput.Text = "高亮日程";
                    editor.Highlight.Checked = true;
                    editor.Important.Checked = false;
                    editor.ColorButtons[2].PerformClick();
                    if (skin.Id == "cream")
                        using (var b = new Bitmap(editor.Width, editor.Height))
                        {
                            editor.DrawToBitmap(b, new Rectangle(Point.Empty, editor.Size));
                            b.Save(Path.Combine(dir, "highlight-editor.png"));
                        }

                    check(editor.TrySave() && editor.Result.Highlight && !editor.Result.Important && editor.Result.ColorSlot == 2, "highlight independent from importance and preset follows theme " + skin.Id);
                    st.Events.Add(editor.Result);
                    var normal = new Entry
                    {
                        Title = "普通日程",
                        Start = d.AddHours(11),
                        End = d.AddHours(12),
                        Color = Palette.Hex[4],
                        ColorSlot = 4
                    };
                    st.Events.Add(normal);
                    var scene = new Scene
                    {
                        State = st,
                        Today = d,
                        Month = d,
                        Selected = d
                    };
                    using (var b = scene.Render(new Size(1600, 900)))
                        b.Save(Path.Combine(dir, "palette-" + skin.Id + ".png"));
                    st.CalendarView = "week";
                    using (var b = scene.Render(new Size(1600, 900)))
                        b.Save(Path.Combine(dir, "highlight-week-" + skin.Id + ".png"));
                    Data.Write(st);
                    check(Data.Read().Events[0].Highlight, "highlight survives persistence " + skin.Id);
                }
            }

            var midnight = new State();
            midnight.Todos.Add(new Todo { Title = "凌晨完成", Date = d.ToString("yyyy-MM-dd"), Done = true, CompletedAt = d.AddMinutes(30) });
            DailyArchive.Enable(midnight, d);
            Data.Write(midnight);
            var midnightRead = Data.Read();
            check(midnightRead.Todos[0].CompletedAt.Value.Date == d && midnightRead.TodoArchive[0].CompletedAt.Value.Date == d, "completion near local midnight survives serialization without changing date");
            var themed = new State
            {
                Skin = "cream"
            };
            themed.Events.Add(new Entry { Title = "随皮肤", Start = d, End = d, ColorSlot = 0 });
            themed.Events.Add(new Entry { Title = "固定颜色", Start = d, End = d, ColorSlot = -1, Color = "#123456" });
            Skins.Apply(themed);
            string before = themed.Events[0].Color;
            themed.Skin = "rose";
            Skins.Apply(themed);
            check(themed.Events[0].Color != before && themed.Events[1].Color == "#123456", "skin switch changes preset slot but preserves custom color");
            Skins.Apply(new State());
        }
    }
#endif

}