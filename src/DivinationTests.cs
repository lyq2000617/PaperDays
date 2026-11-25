using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class DivinationTests
    {
        /// <summary>
        /// 执行DivinationTests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check)
        {
            bool all = true;
            for (int a = 1; a <= 6; a++)
                for (int b = 1; b <= 6; b++)
                    for (int c = 1; c <= 6; c++)
                    {
                        int at = 0;
                        foreach (int n in new[]
                        {
                            a,
                            b,
                            c
                        }

                        )
                            for (int i = 1; i < n; i++)
                                at = (at + 1) % 6;
                        var r = Divination.Calculate(new DivinationRecord { A = a, B = b, C = c });
                        all &= r.Name == Divination.Names[at];
                    }

            check(all, "all 216 six-palace combinations follow inclusive counting");
            check(Divination.Calculate(new DivinationRecord { A = 1, B = 1, C = 1 }).Name == "大安" && Divination.Calculate(new DivinationRecord { A = 6, B = 1, C = 1 }).Name == "空亡", "known starting-point and cycle boundary examples");
            check(Divination.Read(new DivinationRecord { A = 0, B = 1, C = 1 }) == null && Divination.Read(new DivinationRecord { A = 1000, B = 1, C = 1 }) == null, "out-of-range input rejected");
            var record = new DivinationRecord
            {
                A = 999,
                B = 999,
                C = 999,
                Category = "学习",
                Created = new DateTime(2026, 10, 31, 9, 0, 0, DateTimeKind.Utc)
            };
            var result = Divination.Calculate(record);
            var copy = Data.Json().Deserialize<DivinationRecord>(Data.Json().Serialize(record));
            check(Divination.Calculate(copy).Steps == result.Steps && copy.Created == record.Created, "numbers category rule version and timestamp survive serialization");
            var state = Data.Read();
            state.Divination = record;
            Data.Write(state);
            check(Data.Read().Divination.A == 999 && Divination.Calculate(Data.Read().Divination).Name == result.Name, "divination persists with existing calendar data");
            bool complete = true;
            foreach (string cat in Divination.Categories)
                for (int i = 1; i <= 6; i++)
                {
                    var r = Divination.Calculate(new DivinationRecord { A = i, B = 1, C = 1, Category = cat });
                    complete &= !String.IsNullOrWhiteSpace(r.Meaning) && !String.IsNullOrWhiteSpace(r.Advice);
                }

            check(complete, "all six outcomes have explanations in every category");
            using (var dialog = new DivinationDialog(null))
            {
                dialog.Show();
                Application.DoEvents();
                check(dialog.Result == null && !dialog.Changed, "empty divination has no result");
                dialog.Numbers[0].Value = 3;
                dialog.Numbers[1].Value = 5;
                dialog.Numbers[2].Value = 2;
                dialog.CalculateButton.PerformClick();
                check(dialog.Result != null && dialog.Changed && dialog.ResultTitle.Text.Contains("留连"), "number input and recalculation produce expected interpretation");
                dialog.Numbers[0].Value = 100;
                dialog.CalculateButton.PerformClick();
                check(dialog.Numbers[0].Invalid && !dialog.Numbers[1].Invalid && dialog.Status.Text.Contains("1–99"), "invalid number marks only its own box with inline text");
            }

            var colors = new State();
            using (var editor = new Editor(colors, null, DateTime.Today))
            {
                editor.Show();
                editor.TitleInput.Text = "手选颜色";
                editor.ColorButtons[2].PerformClick();
                string chosen = editor.ChosenColor;
                editor.TrySave();
                colors.Events.Add(editor.Result);
                colors.Skin = "blue";
                Skins.Apply(colors);
                check(colors.Events[0].Color == Skins.ColorAt(2) && colors.Events[0].ColorSlot == 2, "preset selection follows matching theme slot");
                editor.Close();
            }

            colors.Skin = "cream";
            Skins.Apply(colors);
            var scene = new Scene
            {
                ReservedLeft = 208,
                State = new State
                {
                    Widget = "占卜",
                    Divination = record
                }
            };
            using (var image = scene.Render(new Size(1920, 1080)))
            {
                check(image.GetPixel(30, 540).ToArgb() == Palette.Cream.ToArgb(), "side margin remains opaque theme background");
            }

            check(scene.Hits.Any(h => h.Action == "widgetfortune") && scene.Hits.Any(h => h.Action == "widgetzodiac"), "divination card exposes input and details");
        }
    }
#endif

}