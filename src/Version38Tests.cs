using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    /// <summary>
    /// 测试用的一周示例日程，不含个人记录。
    /// </summary>
    public static class Visual38
    {
        /// <summary>构造一周示例日程，覆盖清晨、深夜、半小时和并行日程。</summary>
        public static State Sample()
        {
            var s = new State { IconColumns = 0, Widget = "占卜", CalendarView = "week", FortuneCategory = "学习" };
            DateTime m = WeekLayout.Monday(DateTime.Today);
            Action<int, double, double, string, int> add = delegate (int day, double a, double b, string title, int color)
            {
                s.Events.Add(new Entry { Title = title, Start = m.AddDays(day).AddHours(a), End = m.AddDays(day).AddHours(b), Color = Palette.Hex[color], ColorSlot = color });
            };
            add(0, 8, 9.5, "高等数学", 4);
            add(0, 14, 15.5, "物理实验", 5);
            add(1, 1, 2, "夜间航班", 2);
            add(1, 9, 9.5, "站会", 3);
            add(2, 8, 10, "英语", 4);
            add(2, 19, 21, "自习", 6);
            add(3, 22.5, 23.5, "复盘", 0);
            add(3, 10, 12, "组会", 7);
            add(3, 10.5, 11.5, "并行讨论", 1);
            add(4, 6.5, 7, "晨跑", 3);
            add(5, 16, 17, "看电影", 5);
            s.Events.Add(new Entry { Title = "交报告", Start = m.AddDays(4), End = m.AddDays(4).AddHours(23.9), AllDay = true, Color = Palette.Hex[7], ColorSlot = 7 });
            s.Divination = new DivinationRecord { A = 3, B = 5, C = 2, Category = "学习", Created = DateTime.UtcNow };
            return s;
        }

    }

    /// <summary>统计桌面绘制次数的测试视图。</summary>
    public sealed class CountingView : SceneView
    {
        public int Paints;
        /// <summary>绑定场景。</summary>
        public CountingView(Scene scene) : base(scene)
        {
        }

        /// <summary>记录绘制次数。</summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            Paints++;
            base.OnPaint(e);
        }
    }

    public static class Version38Tests
    {
        /// <summary>
        /// 执行 v0.38 的功能检查；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            // 周视图：全天时间轴
            var compressed = new DayScale(100, 600, false);
            var equal = new DayScale(100, 600, true);
            check(compressed.Y(0) == 100 && Math.Abs(compressed.Y(24) - 700) < .01f && equal.Y(0) == 100 && Math.Abs(equal.Y(24) - 700) < .01f, "both week scales cover 00:00 to 24:00");
            check(Math.Abs((equal.Y(13) - equal.Y(12)) - (equal.Y(3) - equal.Y(2))) < .01f, "equal scale gives every hour the same height");
            check(compressed.Y(13) - compressed.Y(12) > equal.Y(13) - equal.Y(12) && compressed.Y(3) - compressed.Y(2) < equal.Y(3) - equal.Y(2), "compressed scale makes daytime taller and night shorter");
            var state = Visual38.Sample();
            state.CalendarView = "week";
            var scene = new Scene { State = state };
            foreach (bool mode in new[] { false, true })
            {
                state.WeekEqualHours = mode;
                using (scene.Render(new Size(1920, 1080)))
                {
                    var slots = scene.Hits.Where(h => h.Action == "timeslot" && h.Date.Date == WeekLayout.Monday(DateTime.Today)).OrderBy(h => h.Date).ToArray();
                    check(slots.First().Date.Hour == 0 && slots.Last().Date.Hour == 23, "week view reaches every hour without scrolling, equal=" + mode);
                    check(slots.Zip(slots.Skip(1), (a, b) => Math.Abs(a.Box.Bottom - b.Box.Top) < .5f).All(x => x), "time slots tile the day with no gaps, equal=" + mode);
                    check(scene.Hits.Count(h => h.Action == "event") >= state.Events.Count, "night, early and late events are all on screen, equal=" + mode);
                    check(scene.Hits.Any(h => h.Action == "weekequal"), "equal-height checkbox is on the week footer, equal=" + mode);
                    check(!scene.Hits.Any(h => h.Action == "hoursup" || h.Action == "hoursdown"), "old hour scroll buttons are gone, equal=" + mode);
                }
            }

            state.WeekEqualHours = false;
            foreach (var view in new[] { new { Left = 0, Icons = 0 }, new { Left = 208, Icons = -1 } })
            {
                var single = new Scene { State = new State { CalendarView = "week", IconColumns = view.Icons }, ReservedLeft = view.Left };
                using (single.Render(new Size(1920, 1080)))
                    check(single.Hits.Any(h => h.Action == "weekequal"), "checkbox appears with and without sidebar, reserve=" + view.Left);
            }

            var saved = new State { WeekEqualHours = true, FortuneCategory = "工作" };
            Data.Write(saved);
            var read = Data.Read();
            check(read.WeekEqualHours && read.FortuneCategory == "工作", "equal-height choice and fortune category persist");

            // 主界面：不同屏幕铺满
            foreach (var size in new[] { new Size(1366, 768), new Size(1600, 900), new Size(1920, 1200), new Size(2560, 1600), new Size(3440, 1440), new Size(5120, 1440), new Size(1280, 1024) })
            {
                var layout = new ResponsiveLayout(size, 0, true);
                var outer = RectangleF.Union(layout.Pixel(layout.Calendar), RectangleF.Union(layout.Pixel(layout.Plans), layout.Pixel(layout.Sidebar)));
                check(outer.Width >= size.Width * .9f && outer.Height >= size.Height * .9f, "layout fills screen " + size);
                check(outer.Left >= 0 && outer.Top >= 0 && outer.Right <= size.Width && outer.Bottom <= size.Height, "layout stays on screen " + size);
            }

            var wide = new Scene { State = Visual38.Sample() };
            using (wide.Render(new Size(3440, 1440)))
            {
                var tools = wide.Hits.First(h => h.Action == "tools");
                check(tools.Box.Right <= wide.Layout.Pixel(wide.Layout.Calendar).Right && tools.Box.Right > wide.Layout.Pixel(wide.Layout.Calendar).Right - 100, "header buttons stay right aligned on ultrawide");
            }

            // 字体
            UiFont.UseInstalled(new[] { "SimSun", "Segoe UI" });
            check(UiFont.Family("Microsoft YaHei UI") == "SimSun", "missing Chinese UI font falls back to an installed font");
            UiFont.UseInstalled(new[] { "Segoe UI Symbol" });
            check(UiFont.Family("Segoe UI Emoji") == "Segoe UI Symbol", "missing emoji font falls back on older Windows");
            check(DialogFrame.Edge(new Point(2, 2), new Size(400, 300), 5) == 13 && DialogFrame.Edge(new Point(398, 150), new Size(400, 300), 5) == 11 && DialogFrame.Edge(new Point(200, 298), new Size(400, 300), 5) == 15 && DialogFrame.Edge(new Point(200, 150), new Size(400, 300), 5) == 0, "window edges still resize after removing the system frame");
            UiFont.UseInstalled(null);
            float small = 0, large = 0;
            using (var bitmap = new Bitmap(100, 100))
            using (var g = Graphics.FromImage(bitmap))
            using (var font = UiFont.Scene(11))
            {
                small = font.Unit == GraphicsUnit.Pixel ? font.Size : 0;
                large = font.GetHeight(g);
            }

            check(small > 0 && large > 0, "desktop text uses pixel units so Windows scaling cannot overflow the canvas");
            using (var tiny = new Scene { State = new State() }.Render(new Size(800, 450)))
            using (var font = UiFont.Scene(9))
                check(font.Size * UiFont.SceneScale >= Scene.MinimumTextPixels - .01f, "text never renders under the readable minimum on small screens");

            // 小六壬
            var card = new Scene { State = new State { Widget = "占卜", IconColumns = 0 } };
            using (card.Render(new Size(1920, 1080)))
            {
                check(card.Hits.Count(h => h.Action == "fortuneinput") == 3, "card has three number boxes");
                check(card.Hits.Count(h => h.Action.StartsWith("fortunecat")) == 5, "card offers five fortune categories");
                check(card.Hits.Any(h => h.Action == "drawfortune") && card.Hits.Any(h => h.Action == "reading"), "card has view and detail buttons");
                var slots = card.Hits.Where(h => h.Action == "fortuneinput").ToArray();
                var button = card.Hits.First(h => h.Action == "drawfortune");
                check(button.Box.Top - slots[0].Box.Bottom < 100 * card.Layout.Scale, "inputs sit next to the view button");
            }

            card.FortuneNumbers = new[] { "12", "100", "" };
            check(card.ReadFortune() == null && !card.FortuneBad[0] && card.FortuneBad[1] && card.FortuneBad[2] && card.FortuneError.Contains("1–99"), "only invalid boxes are marked");
            card.FortuneNumbers = new[] { "3", "5", "2" };
            card.State.FortuneCategory = "出行";
            var record = card.ReadFortune();
            check(record != null && record.Category == "出行" && card.FortuneError == "" && !card.FortuneBad.Any(b => b), "valid numbers produce a record in the chosen category");
            card.State.Divination = record;
            using (card.Render(new Size(1920, 1080)))
                check(Divination.Read(card.State.Divination).Name == "留连", "result shows directly on the desktop card");
            card.State.Divination = new DivinationRecord { A = 999, B = 1, C = 1 };
            card.FortuneNumbers = new[] { "", "", "" };
            card.LoadFortune();
            check(Divination.Read(card.State.Divination) != null && card.FortuneNumbers.All(String.IsNullOrEmpty), "older three digit results stay readable without filling the boxes");

            check(Enumerable.Range(1, 6).All(a => Divination.Calculate(new DivinationRecord { A = a, B = 1, C = 1 }).Name == Divination.Calculate(new DivinationRecord { A = a + 6, B = 1, C = 1 }).Name), "results repeat every six numbers");
            check(Enumerable.Range(1, 6).Select(a => Divination.Calculate(new DivinationRecord { A = a, B = 1, C = 1 }).Name).Distinct().Count() == 6, "range 1 to 99 still reaches all six palaces");
            check(CompleteHoroscopes.Clean("整体平和。星$座$屋") == "整体平和。" && CompleteHoroscopes.Clean("星座运势") == "星座运势", "source watermark is removed without touching normal text");
            check(DivinationDetails.Attributes.Length == 6 && DivinationDetails.Images.Length == 6 && DivinationDetails.Suitable.Length == 6 && DivinationDetails.Careful.Length == 6, "every palace has detailed text");

            using (var host = new Form { ClientSize = new Size(300, 120) })
            {
                var a = new IntegerInput { Bounds = new Rectangle(0, 0, 60, 44) };
                var b = new IntegerInput { Bounds = new Rectangle(70, 0, 60, 44) };
                var c = new IntegerInput { Bounds = new Rectangle(140, 0, 60, 44) };
                host.Controls.AddRange(new Control[] { a, b, c });
                int submitted = 0, paints = 0;
                IntegerInput.Chain(new[] { a, b, c }, delegate { submitted++; });
                b.Paint += delegate { paints++; };
                host.Show();
                Application.DoEvents();
                paints = 0;
                a.Type("4");
                a.Type("2");
                Application.DoEvents();
                check(a.Text == "42" && a.Valid && a.Maximum == 99, "two digit input accepted");
                a.Type("7");
                check(a.Text.Length == 2, "input never exceeds two digits");
                a.Text = "";
                a.Value = 5;
                check(a.Value == 5 && b.Text == "" && c.Text == "", "typing in one box leaves other boxes untouched");
                a.Text = "0";
                check(!a.Valid, "zero is rejected");
                a.Submit();
                check(submitted == 1, "enter submits");
                check(b.Previous == a && b.Next == c && a.Previous == null && c.Next == null, "boxes chain in order");
                host.Close();
            }

            var live = new Scene { State = new State { Widget = "占卜", IconColumns = 0 } };
            using (var form = new Form { ClientSize = new Size(1600, 900), StartPosition = FormStartPosition.Manual, Location = new Point(0, 0) })
            using (var view = new CountingView(live) { Dock = DockStyle.Fill })
            {
                form.Controls.Add(view);
                string fired = null;
                view.HitAction = h => fired = h.Action;
                form.Show();
                Application.DoEvents();
                view.BeginFortuneInput(0);
                Application.DoEvents();
                var input = Application.OpenForms.Cast<Form>().First(x => x.Text == "小六壬数字输入");
                var boxes = input.Controls.OfType<IntegerInput>().ToArray();
                check(boxes.Length == 3 && input.FormBorderStyle == FormBorderStyle.None, "desktop input uses custom number boxes");
                Application.DoEvents();
                int before = view.Paints;
                boxes[0].Type("1");
                boxes[0].Type("2");
                boxes[1].Type("5");
                Application.DoEvents();
                check(view.Paints == before, "typing does not repaint the desktop card");
                check(live.FortuneNumbers[0] == "12" && live.FortuneNumbers[1] == "5", "typed numbers reach the card");
                boxes[2].Type("2");
                boxes[0].Submit();
                Application.DoEvents();
                check(fired == "drawfortune", "enter runs view fortune from the desktop input");
                Application.DoEvents();
                live.FortuneNumbers = new[] { "12", "0", "7" };
                live.ReadFortune();
                view.Refresh();
                check(live.Hits.Any(h => h.Action == "fortuneclear"), "desktop card has a clear button");
                view.ClearFortuneInput();
                Application.DoEvents();
                var again = Application.OpenForms.Cast<Form>().FirstOrDefault(x => x.Text == "小六壬数字输入");
                check(live.FortuneNumbers.All(String.IsNullOrEmpty) && live.FortuneError == "" && !live.FortuneBad.Any(b => b), "clear empties all numbers and the error");
                check(again != null && again.Controls.OfType<IntegerInput>().All(i => i.Text == ""), "clear reopens empty boxes for typing");
                again.Close();
                live.State.Divination = new DivinationRecord { A = 3, B = 5, C = 2 };
                live.ClearFortune();
                check(Divination.Read(live.State.Divination).Name == "留连", "clear keeps the saved result until the next view");
                form.Close();
            }

            using (var f = new DivinationDialog(new DivinationRecord { A = 3, B = 5, C = 2, Category = "学习" }))
            {
                f.Show();
                Application.DoEvents();
                var text = String.Join("\n", AllLabels(f).Select(l => l.Text));
                check(Divination.Categories.All(c => text.Contains(c + "：") || text.Contains(c + "（当前）：")), "details list all five fortune categories");
                check(text.Contains("起课规则") && text.Contains("六宫对照") && text.Contains("本宫详解") && text.Contains("起课过程"), "details include rule, palace table and process in the window");
                check(text.Contains("落在留连"), "details explain each counting step");
                f.Numbers[0].Value = 100;
                f.CalculateButton.PerformClick();
                f.ClearButton.PerformClick();
                check(f.Numbers.All(n => n.Text == "" && !n.Invalid) && f.Status.Text == "" && f.Result != null, "details clear button resets the boxes and keeps the reading");
                f.Close();
            }

            // 分享给朋友：全新目录里没有任何记录
            string keepRoot = Data.Root, blank = Path.Combine(directory, "blank-friend");
            Directory.CreateDirectory(blank);
            Data.Root = blank;
            var fresh = Data.Read();
            Data.Root = keepRoot;
            check(fresh.Events.Count == 0 && fresh.Todos.Count == 0 && fresh.Divination == null && fresh.Marks.Count == 0, "a new user starts with no records");
            check(new Entry().TimeZone == TimeZoneInfo.Local.Id, "new events follow the computer time zone");

            // 窗口缩放
            foreach (int dpi in new[] { 96, 120, 144, 192 })
            {
                UiDpi.Simulated = dpi;
                float d = dpi / 96f;
                using (var editor = new Editor(new State(), null, DateTime.Today))
                {
                    editor.Show();
                    Application.DoEvents();
                    var dots = editor.ColorButtons.Concat(editor.HighlightButtons).ToArray();
                    float before = 0;
                    foreach (var size in new[] { new Size(980, 880), new Size(660, 880), new Size(540, 720), new Size(430, 600), new Size(360, 500) })
                    {
                        editor.Size = new Size((int)(size.Width * d), (int)(size.Height * d));
                        Application.DoEvents();
                        check(dots.All(b => b.Width == b.Height), "swatches stay square dpi=" + dpi + " " + size);
                        var emoji = Descendants(editor).OfType<EmojiButton>().ToArray();
                        check(emoji.Length == 11 && emoji.All(b => b.Width == b.Height), "emoji tiles stay square dpi=" + dpi + " " + size);
                        var grids = Descendants(editor).OfType<SwatchGrid>().ToArray();
                        check(grids.All(g => g.Controls.Count % g.Columns == 0), "swatch rows divide evenly dpi=" + dpi + " " + size);
                        check(grids.All(g => g.Controls.Cast<Control>().All(c => c.Right <= g.Width)), "swatches stay inside their section dpi=" + dpi + " " + size);
                        var body = editor.Controls.OfType<Panel>().First(p => p.AutoScroll);
                        check(!body.HorizontalScroll.Visible, "no horizontal scrollbar dpi=" + dpi + " " + size);
                        var field = editor.TitleInput;
                        if (size.Width == 660)
                            before = field.Font.Size;
                        // 模拟 200% 时这台 1080p 屏幕放不下窗口，两档都已到最小比例，只检查方形和分行。
                        if (size.Width == 430 && dpi < 192)
                            check(field.Font.Size < before, "text shrinks with the window dpi=" + dpi);
                    }

                    editor.Close();
                }
            }

            UiDpi.Simulated = 0;
            check(SwatchGrid.ColumnsFor(10, 1000, 36, 8) == 10 && SwatchGrid.ColumnsFor(10, 300, 36, 8) == 5 && SwatchGrid.ColumnsFor(8, 300, 32, 8) == 4 && SwatchGrid.ColumnsFor(10, 90, 28, 8) == 2, "swatch rows never leave a short last row");
            check(SwatchGrid.Cell(1) > SwatchGrid.Cell(.9f) && SwatchGrid.Cell(.9f) > SwatchGrid.Cell(.75f) && SwatchGrid.Cell(1.1f) == SwatchGrid.Cell(1), "swatches change in three fixed sizes");
            check(ResponsiveScale.Quantize(.3f) >= ResponsiveScale.Minimum - .02f && ResponsiveScale.Quantize(3) <= ResponsiveScale.Maximum + .02f, "content scale stays in the readable range");

            // emoji 和课程样式
            check(!new Entry().EmojiOriginal && !new Entry().EmojiTint, "new events default to black and white emoji");
            check(Skins.EmojiInk(new Entry { Color = "#FF7899" }) == ColorTranslator.ToHtml(Palette.Ink), "default emoji ink follows the skin text color");
            check(EmojiArt.Brightness(ColorTranslator.FromHtml(Skins.EmojiInk(new Entry { Color = "#F2E96F", EmojiTint = true }))) < .63, "very light tints are darkened to stay readable");
            foreach (string file in Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "emoji"), "*.png"))
            {
                string code = Path.GetFileNameWithoutExtension(file);
                string text = Char.ConvertFromUtf32(Convert.ToInt32(code, 16));
                check(LineIcons.Has(text), "every bundled emoji has a line icon " + code);
            }

            check(CourseIcons.All.Length == 27 && CourseIcons.Emoji.Distinct().Count() == 27 && CourseIcons.Emoji.All(e => LineIcons.Has(e) && EmojiArt.Get(e) != null), "27 course icons all have line and color versions");
            check(CourseIcons.For("高等数学") == "🧮" && CourseIcons.For("大学物理实验") == "⚛" && CourseIcons.For("化学实验") == "🧪" && CourseIcons.For("钢琴课") == "🎹" && CourseIcons.For("音乐欣赏") == "🎵" && CourseIcons.For("街舞") == "💃" && CourseIcons.For("围棋") == "♟" && CourseIcons.For("大学英语") == "🔤", "course names match the right icon");
            check(CourseIcons.For("未知课程名") == CourseIcons.Other && CourseIcons.For(null) == CourseIcons.Other, "unmatched courses use the fallback icon");
            var parsed = LineIcons.Parse("M1 2h3v4l2 2a2 2 0 1 0 4 0c1 1 2 2 3 3s1 2 2 2q1 1 2 2t2 2z M5 5L6 6");
            check(parsed.PointCount > 10 && parsed.GetBounds().Width > 5, "svg path parser handles every command");
            using (var bitmap = new Bitmap(40, 40))
            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.White);
                EmojiArt.Draw(g, "🎂", new RectangleF(10, 10, 20, 20), false, "#000000");
                int dark = 0, gaps = 0;
                for (int y = 10; y < 30; y++)
                    for (int x = 10; x < 30; x++)
                        if (bitmap.GetPixel(x, y).R < 128)
                            dark++;
                        else
                            gaps++;
                check(dark > 30 && gaps > dark, "line icon keeps open space instead of a solid blob");
                var size = EmojiArt.Key("✈️");
                check(size == "2708", "emoji key ignores variation selector");
            }

            var styled = new State();
            using (var editor = new Editor(styled, null, DateTime.Today))
            {
                editor.Show();
                Application.DoEvents();
                check(editor.EmojiTone.SelectedIndex == 0, "editor opens with black and white emoji");
                check(!Descendants(editor).OfType<TextBox>().Any(t => t == editor.EmojiInput), "emoji text box is not shown as a dark glyph");
                editor.EmojiInput.Text = "🎨";
                check(editor.EmojiInput.Text == "🎨" && Descendants(editor).OfType<EmojiButton>().Count(b => b.Emoji == "🎨") == 2, "preview follows the chosen emoji");
                editor.TitleInput.Text = "样式";
                editor.EmojiMode.Checked = true;
                editor.ColorButtons[4].PerformClick();
                check(editor.EmojiTone.SelectedIndex == 1, "choosing a color in emoji mode tints the emoji");
                editor.TrySave();
                check(editor.Result.EmojiTint && !editor.Result.EmojiOriginal && editor.Result.UseEmoji, "tint choice is saved");
            }

            var from = new Entry { Color = "#112233", ColorSlot = 3, UseEmoji = true, Emoji = "📚", EmojiOriginal = true, Highlight = true, HighlightColor = "#ABCDEF", HighlightSlot = 2 };
            var to = new Entry { Title = "保留标题", Start = new DateTime(2026, 1, 1) };
            to.CopyStyle(from);
            check(to.Color == "#112233" && to.EmojiOriginal && to.Highlight && to.HighlightSlot == 2 && to.Emoji == "📚" && to.Title == "保留标题" && to.Start.Year == 2026, "copying style keeps title and time");
            var lessons = new State();
            using (var import = new CourseImportDialog(lessons))
            {
                import.Show();
                Application.DoEvents();
                var table = Descendants(import).OfType<DataGridView>().First(g => g.Columns.Count == 11);
                for (int i = 0; i < 2; i++)
                    table.Rows.Add("周一", "08:00", "09:00", "一班", "数学", "A101", "", DateTime.Today.ToString("yyyy-MM-dd"));
                check(table.Columns[10].HeaderText == "样式", "course preview has a style column");
                using (var editor = import.StyleEditor(table.Rows[0]))
                {
                    editor.Show();
                    Application.DoEvents();
                    check(editor.Text.StartsWith("课程样式") && editor.SaveButton.Text == "应用到这门课", "course style window reuses the event style controls");
                    check(Descendants(editor).OfType<EmojiButton>().Count() == 28, "course style window shows the course icon set");
                    editor.Highlight.Checked = true;
                    editor.EmojiMode.Checked = true;
                    editor.ColorButtons[2].PerformClick();
                    editor.EmojiTone.SelectedIndex = 2;
                    check(editor.TrySave(), "course style saves without schedule fields");
                    import.ApplyStyle(table.Rows[0], editor.Result);
                }

                check(Convert.ToString(table.Rows[1].Cells[9].Value) == Palette.Hex[2], "style reaches every row of the same course");
                check(Descendants(editorProbe(lessons)).OfType<EmojiButton>().Count() == 11, "ordinary events keep the everyday icon set");
                import.ConfirmImport();
                check(import.Courses != null && import.Courses.All(c => c.Style != null && c.Style.Highlight && c.Style.EmojiOriginal), "style is passed to import");
                CourseImporter.Commit(lessons, import.Courses, DateTime.Today, 1, true);
                check(lessons.Events.Count > 0 && lessons.Events.All(e => e.Highlight && e.EmojiOriginal && e.Color == Palette.Hex[2]), "imported lessons keep highlight, emoji and color");
            }

            using (var editor = new Editor(lessons, lessons.Events[0], DateTime.Today))
            {
                editor.Show();
                Application.DoEvents();
                check(editor.SameCourse != null && editor.SameCourse.Checked, "editing a lesson offers syncing the whole course");
                editor.Close();
            }

            using (var editor = new Editor(lessons, new Entry { Title = "普通日程", Start = DateTime.Today, End = DateTime.Today.AddHours(1) }, DateTime.Today))
            {
                check(editor.SameCourse == null, "normal events have no course sync option");
            }

            // 已导入课程
            var bookState = new State();
            DateTime mon = WeekLayout.Monday(DateTime.Today);
            Func<int, int, int, string, string, string, Entry> lesson = (week, day, hour, title, cls, room) => new Entry { Id = "course-" + Guid.NewGuid().ToString("N"), Title = title, ClassName = cls, Notes = "教室：" + room, Start = mon.AddDays(week * 7 + day).AddHours(hour), End = mon.AddDays(week * 7 + day).AddHours(hour + 1), Reminder = 15, Color = Palette.Hex[1], UseEmoji = true, Emoji = "🧮" };
            for (int week = -1; week <= 2; week++)
            {
                bookState.Events.Add(lesson(week, 0, 8, "数学", "一班", "A1"));
                bookState.Events.Add(lesson(week, 2, 10, "数学", "一班", "A1"));
                bookState.Events.Add(lesson(week, 1, 9, "英语", "一班", "B2"));
            }

            bookState.Events.Add(new Entry { Title = "个人日程", Start = mon, End = mon.AddHours(1) });
            var book = CourseBook.Slots(bookState);
            check(book.Count == 3 && book[0].Title == "数学" && book[1].Title == "数学" && book[2].Title == "英语" && book.All(x => x.Lessons.Count == 4), "lessons group into recurring slots sorted by course name");
            check(book[0].Day == 1 && book[1].Day == 3 && book[0].Room == "A1", "slots inside a course sort by weekday and read the room");
            check(CourseBook.Slots(bookState, "英").Count == 1, "search filters courses");
            var note = new Entry { Notes = "课程：数学\r\n教室：A1" };
            CourseBook.SetRoom(note, "C3");
            check(CourseBook.Room(note) == "C3" && note.Notes.Contains("课程：数学"), "room edit keeps other notes");
            var future = CourseBook.Targets(book.Take(1), false, mon.AddDays(7));
            check(future.Count == 2 && CourseBook.Targets(book.Take(1), true, mon.AddDays(7)).Count == 4, "default scope leaves past lessons alone");
            bool refused = false;
            try { CourseBook.SetStart(future, new TimeSpan(23, 0, 0)); } catch (ArgumentException) { refused = true; }
            check(refused, "start time after the end is rejected");
            CourseBook.SetStart(future, new TimeSpan(7, 30, 0));
            CourseBook.SetEnd(future, new TimeSpan(8, 30, 0));
            check(future.All(e => e.Start.Hour == 7 && e.Start.Minute == 30 && e.End.Hour == 8) && CourseBook.Slots(bookState).Count == 4, "partial time change splits the row at today");
            CourseBook.SetReminder(future, 60);
            check(future.All(e => e.Reminder == 60 && !e.Notified), "reminder change applies and can fire again");
            var moving = CourseBook.Slots(bookState).First(x => x.Title == "英语").Lessons;
            var moved = moving[0].Start;
            CourseBook.SetDay(moving, 5);
            check(moving[0].Start == moved.AddDays(3) && moving[0].End.DayOfWeek == moving[0].Start.DayOfWeek, "weekday change stays in the same week");
            var snapshot = CourseBook.Snapshot(bookState);
            check(CourseBook.Rename(bookState, "数学", "高等数学") == 8 && CourseBook.Slots(bookState).All(x => x.Title != "数学"), "rename updates every lesson");
            CourseBook.Delete(bookState, CourseBook.Lessons(bookState, "英语"));
            check(bookState.Events.Any(e => e.Title == "个人日程") && !bookState.Events.Any(e => e.Title == "英语"), "delete leaves personal events alone");
            CourseBook.Restore(bookState, snapshot);
            check(bookState.Events.Count(e => e.Title == "英语") == 4 && bookState.Events.Count(e => e.Title == "数学") == 8, "undo restores lessons");
            int writes = 0;
            using (var dialog = new CourseImportDialog(bookState, () => bookState, edit => { edit(); writes++; return true; }))
            {
                dialog.Show();
                Application.DoEvents();
                check(dialog.BookGrid.Columns.Count == 11 && dialog.BookGrid.Columns[5].HeaderText == "开始" && dialog.BookGrid.Columns[6].HeaderText == "结束" && dialog.BookGrid.Columns[8].HeaderText == "提醒", "course page has separate start, end and reminder columns");
                dialog.RefreshBook();
                int rows = dialog.BookSlots.Count;
                check(dialog.BookGrid.Rows.Count == rows && rows >= 3, "course page lists slots");
                dialog.EditAll = true;
                int first = dialog.BookSlots.FindIndex(x => x.Title == "英语");
                dialog.EditBook(first, 7, "C9");
                check(writes == 0 && CourseBook.Lessons(bookState, "英语").All(e => CourseBook.Room(e) == "B2") && dialog.BookMessage.Contains("未应用"), "edits wait for apply");
                dialog.UndoBook();
                check(dialog.BookSlots.Where(x => x.Title == "英语").All(x => x.Room == "B2") && !dialog.BookMessage.Contains("未应用"), "undo reverts an unapplied edit");
                dialog.EditBook(first, 7, "C9");
                dialog.ApplyBook();
                check(writes == 1 && CourseBook.Lessons(bookState, "英语").All(e => CourseBook.Room(e) == "C9"), "apply saves through the app");
                var keepHour = CourseBook.Lessons(bookState, "英语")[0].Start;
                dialog.EditBook(dialog.BookSlots.FindIndex(x => x.Title == "英语"), 5, "99:00");
                check(dialog.BookMessage.Contains("24 小时制"), "bad time shows inline without changing data");
                int math = dialog.BookSlots.FindIndex(x => x.Title == "数学");
                dialog.MarkCourse(math);
                check(dialog.BookMessage.StartsWith("已选"), "checking a course selects its rows");
                dialog.EditBook(math, 8, "提前 1 天");
                dialog.EditBook(dialog.BookSlots.FindIndex(x => x.Title == "数学"), 7, "D7");
                dialog.ColorCourse("数学", Palette.Hex[3], 3);
                dialog.UndoBook();
                dialog.ColorCourse("数学", Palette.Hex[3], 3);
                dialog.RenameCourse("数学", "");
                check(dialog.BookMessage.Contains("不能为空"), "empty course name is refused inline");
                dialog.ApplyBook();
                var mathNow = CourseBook.Lessons(bookState, "数学");
                check(mathNow.Count == 8 && mathNow.All(e => e.Reminder == 1440 && CourseBook.Room(e) == "D7" && e.Color == Palette.Hex[3] && e.EmojiTint), "batch edits reach every selected row after apply");
                check(CourseBook.Lessons(bookState, "英语")[0].Start == keepHour, "undo and apply keep lesson times exact");
                int english = dialog.BookSlots.FindIndex(x => x.Title == "英语");
                dialog.MarkRow(english);
                dialog.DeleteMarked();
                check(CourseBook.Lessons(bookState, "英语").Count == 4 && dialog.BookSlots.All(x => x.Title != "英语"), "row delete waits for apply");
                dialog.ApplyBook();
                check(!bookState.Events.Any(e => e.Title == "英语") && bookState.Events.Any(e => e.Title == "个人日程"), "row delete removes only course lessons");
                var list = dialog.BookSlots[0].Lessons.ToList();
                var weeksState = new State { Events = list.ToList() };
                int start = list.Count;
                CourseBook.SetWeeks(weeksState, list, start + 2);
                check(list.Count == start + 2 && list.Last().Start.Date == list[start - 1].Start.Date.AddDays(14) && list.Last().Start.TimeOfDay == list[0].Start.TimeOfDay && list.Last().End.TimeOfDay == list[0].End.TimeOfDay && list.Select(e => e.Id).Distinct().Count() == list.Count, "more weeks add weekly lessons with the same time");
                CourseBook.SetWeeks(weeksState, list, start - 1);
                check(list.Count == start - 1 && weeksState.Events.Count == start - 1, "fewer weeks remove the last lessons");
                var extra = list.Last().Start.Date.AddDays(3);
                CourseBook.ToggleDate(weeksState, list, extra);
                check(list.Any(e => e.Start.Date == extra), "clicking an empty day adds a lesson");
                CourseBook.ToggleDate(weeksState, list, extra);
                check(!list.Any(e => e.Start.Date == extra), "clicking a lesson day removes it");
                var firstDay = list.Min(e => e.Start).Date;
                CourseBook.MoveFirst(list, firstDay.AddDays(2));
                check(list.Min(e => e.Start).Date == firstDay.AddDays(2) && list.All(e => e.End > e.Start), "moving the first date shifts the series");
                var pageTabs = Descendants(dialog).OfType<TabControl>().First();
                pageTabs.SelectedIndex = 1;
                Application.DoEvents();
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var click = typeof(CourseImportDialog).GetMethod("ClickBook", flags);
                var grid = dialog.BookGrid;
                foreach (int column in new[] { 3, 4, 5, 6, 7, 8 })
                {
                    click.Invoke(dialog, new object[] { grid, new DataGridViewCellMouseEventArgs(column, 0, 5, 5, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)) });
                    Application.DoEvents();
                    check(grid.IsCurrentCellInEditMode && grid.EditingControl != null, "clicking a cell edits it in place, column " + column);
                    grid.CancelEdit();
                    grid.EndEdit();
                    Application.DoEvents();
                }

                dialog.EditAll = true;
                click.Invoke(dialog, new object[] { grid, new DataGridViewCellMouseEventArgs(5, 0, 5, 5, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)) });
                Application.DoEvents();
                ((TimeEdit)grid.EditingControl).Text = "07:15";
                dialog.CommitEdits(grid.EditingControl);
                check(grid.IsCurrentCellInEditMode, "clicking inside the time box keeps editing");
                dialog.CommitEdits(grid);
                check(!grid.IsCurrentCellInEditMode && dialog.BookSlots.Any(x => x.Start == new TimeSpan(7, 15, 0)) && dialog.BookMessage.Contains("未应用"), "clicking blank space commits the time edit");
                click.Invoke(dialog, new object[] { grid, new DataGridViewCellMouseEventArgs(6, 0, 5, 5, new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0)) });
                Application.DoEvents();
                ((TimeEdit)grid.EditingControl).Text = "09:05";
                dialog.ApplyBook();
                check(bookState.Events.Any(e => e.End.TimeOfDay == new TimeSpan(9, 5, 0)), "apply saves an edit still open in the cell");
                var title = dialog.BookSlots[0].Title;
                foreach (string menuName in new[] { "ShowIconMenu", "ShowColorMenu" })
                {
                    typeof(CourseImportDialog).GetMethod(menuName, flags).Invoke(dialog, new object[] { title, new Point(50, 50) });
                    var menu = (ToolStripDropDown)typeof(CourseImportDialog).GetField("bookMenu", flags).GetValue(dialog);
                    check(menu != null, "style panel opens in place: " + menuName);
                    var buttons = Descendants(((ToolStripControlHost)menu.Items[0]).Control).OfType<Button>().ToArray();
                    check(menuName == "ShowIconMenu" ? buttons.Length == 31 : buttons.Length == 18, "style panel lists its choices: " + menuName);
                    if (!menu.IsDisposed)
                        menu.Close();
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(30);
                    Application.DoEvents();
                    check(menu.IsDisposed, "style panel releases after closing: " + menuName);
                }

                typeof(CourseImportDialog).GetMethod("ShowColorMenu", flags).Invoke(dialog, new object[] { title, new Point(50, 50) });
                var open = (ToolStripDropDown)typeof(CourseImportDialog).GetField("bookMenu", flags).GetValue(dialog);
                var nameRect = new Rectangle(10, 10, 100, 24);
                typeof(CourseImportDialog).GetMethod("BeginRename", flags).Invoke(dialog, new object[] { 0, nameRect });
                Application.DoEvents();
                var box = grid.Controls.OfType<TextBox>().FirstOrDefault(t => t.Text == title);
                check(box != null, "clicking the course name edits it in place");
                box.Text = title + "二";
                typeof(CourseImportDialog).GetMethod("EndRename", flags).Invoke(dialog, new object[] { true });
                check(dialog.BookSlots.Any(x => x.Title == title + "二") && !grid.Controls.OfType<TextBox>().Any(t => t == box), "in place rename saves and removes the editor");
                dialog.ShowDateMenu(0, new Point(50, 50));
                var dates = (ToolStripDropDown)typeof(CourseImportDialog).GetField("bookMenu", flags).GetValue(dialog);
                check(dates != null && Descendants(((ToolStripControlHost)dates.Items[0]).Control).OfType<LessonCalendar>().Count() == 1, "date panel shows a month calendar");
                dialog.ApplyBook();
                dialog.Close();
                Application.DoEvents();
                check(open.IsDisposed, "closing the window with a panel open cleans up without a crash");
            }

            check(new CourseRow().Reminder == 15, "imports remind 15 minutes ahead by default");
            check(CourseIcons.For("大学英语") == "🔤" && LineIcons.Has("🔤"), "english keeps its icon mapping");

            // 自启动：测试程序不碰真实注册表
            Wallpaper.SetStartup(true);
            check(Wallpaper.Startup(), "test build keeps autostart state in memory");
            Wallpaper.SetStartup(false);
            check(!Wallpaper.Startup(), "test build clears autostart state in memory");
            using (var controller = new Controller(new State(), false, true))
            {
                var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                var tray = (NotifyIcon)typeof(Controller).GetField("tray", flags).GetValue(controller);
                var menu = tray.ContextMenuStrip;
                var item = menu.Items.OfType<ToolStripMenuItem>().FirstOrDefault(i => i.Text == "开机自启动");
                check(item != null, "tray menu has autostart item");
                check(menu.Items.OfType<ToolStripMenuItem>().Count(i => i.Text == "收起到托盘" || i.Text == "显示日历") == 1 && !menu.Items.OfType<ToolStripMenuItem>().Any(i => i.Text.Contains("等高")), "tray menu has one hide item and no equal-height item");
                menu.Show(new Point(-2000, -2000));
                Application.DoEvents();
                check(!item.Checked, "tray autostart item starts unchecked when off");
                menu.Close();
                item.PerformClick();
                Application.DoEvents();
                check(Wallpaper.Startup(), "clicking tray item turns autostart on");
                menu.Show(new Point(-2000, -2000));
                Application.DoEvents();
                check(item.Checked, "tray item shows a check when autostart is on");
                menu.Close();
                item.PerformClick();
                check(!Wallpaper.Startup(), "clicking again turns autostart off");
                tray.Visible = false;
            }
        }

        /// <summary>创建一个普通日程窗口用于检查图标集。</summary>
        static Editor editorProbe(State state)
        {
            return new Editor(state, null, DateTime.Today);
        }

        /// <summary>列出窗口内全部子控件。</summary>
        static IEnumerable<Control> Descendants(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                yield return c;
                foreach (var child in Descendants(c))
                    yield return child;
            }
        }

        /// <summary>列出窗口内全部文字标签。</summary>
        static IEnumerable<Label> AllLabels(Control parent)
        {
            return Descendants(parent).OfType<Label>();
        }
    }
#endif

}
