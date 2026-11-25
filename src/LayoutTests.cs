using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class LayoutTests
    {
        sealed class FakeWallpapers : IWallpaperBackend
        {
            public Dictionary<string, string> Paths = new Dictionary<string, string>
            {
                {
                    "left",
                    "original-left.bmp"
                },
                {
                    "main",
                    "original-main.bmp"
                },
                {
                    "portrait",
                    "original-portrait.bmp"
                }
            };
            /// <summary>
            /// 执行FakeWallpapers中的Find验证；使用隔离测试数据，不修改用户的真实记录。
            /// </summary>
            public string Find(Rectangle bounds)
            {
                return bounds.X < 0 ? "left" : bounds.X > 0 ? "portrait" : "main";
            }

            /// <summary>
            /// 执行FakeWallpapers中的Get验证；使用隔离测试数据，不修改用户的真实记录。
            /// </summary>
            public string Get(string m)
            {
                return Paths[m];
            }

            /// <summary>
            /// 执行FakeWallpapers中的Set验证；使用隔离测试数据，不修改用户的真实记录。
            /// </summary>
            public void Set(string m, string p)
            {
                if (String.IsNullOrEmpty(m))
                    throw new Exception("all-monitor mutation forbidden");
                Paths[m] = p;
            }

            /// <summary>
            /// 执行FakeWallpapers中的Dispose验证；使用隔离测试数据，不修改用户的真实记录。
            /// </summary>
            public void Dispose()
            {
            }
        }

        /// <summary>
        /// 执行LayoutTests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            var options = new[]
            {
                new DisplayOption
                {
                    Device = "left",
                    Bounds = new Rectangle(-2560, 0, 2560, 1440)
                },
                new DisplayOption
                {
                    Device = "main",
                    Bounds = new Rectangle(0, 0, 1920, 1080),
                    Primary = true
                },
                new DisplayOption
                {
                    Device = "portrait",
                    Bounds = new Rectangle(1920, -420, 1080, 1920)
                }
            };
            using (var picker = new MonitorDialog("main", options))
            {
                picker.Show();
                check(picker.ScreenButtons.Length == 3, "all three mixed-size monitors appear in selector");
                picker.ScreenButtons[2].PerformClick();
                check(picker.SelectedDevice == "portrait" && DisplayOption.Resolve(picker.SelectedDevice, options).Bounds.Y == -420, "third portrait monitor selected by device, not enumeration assumptions");
            }

            var scene = new Scene
            {
                State = Program.Demo(),
                ReservedLeft = 208
            };
            foreach (var size in new[]
            {
                new Size(1366, 768),
                new Size(1920, 1080),
                new Size(2560, 1440),
                new Size(3440, 1440),
                new Size(1080, 1920),
                new Size(1280, 1024),
                new Size(3840, 2160)
            }

            )
            {
                foreach (string mode in new[]
                {
                    "month",
                    "week"
                }

                )
                {
                    scene.State.CalendarView = mode;
                    using (var image = scene.Render(size))
                    {
                        if (mode == "month" && (size.Width == 1920 || size.Width == 1080))
                            image.Save(Path.Combine(directory, "layout-" + size.Width + "x" + size.Height + ".png"));
                    }

                    check(scene.Hits.All(h => h.Box.Left >= scene.ReservedLeft && h.Box.Top >= 0 && h.Box.Right <= size.Width && h.Box.Bottom <= size.Height), mode + " targets fit work area and avoid icon strip " + size);
                    var hit = scene.Hits.First(h => h.Action == (mode == "month" ? "date" : "timeslot"));
                    var actual = scene.At(new Point((int)(hit.Box.X + hit.Box.Width * .2f), (int)(hit.Box.Y + hit.Box.Height * .15f)), size);
                    check(actual != null && actual.Action == hit.Action && actual.Date == hit.Date, "responsive paint and click coordinates agree " + mode + " " + size);
                }
            }

            check(IconArea.Width(1920, 2, 96, 1) == 208 && IconArea.Width(3840, 2, 144, 1.5f) == 312, "icon reserve follows Windows icon grid and DPI");
            scene.State.CalendarView = "month";
            scene.Today = new DateTime(2026, 10, 27);
            scene.Selected = scene.Today;
            scene.Month = scene.Today;
            check(scene.UpdateDay(new DateTime(2026, 10, 28)) && scene.Today.Day == 28 && scene.Selected.Day == 28, "midnight advances today and selection when following today");
            scene.Selected = new DateTime(2026, 11, 15);
            scene.Month = scene.Selected;
            scene.UpdateDay(new DateTime(2026, 10, 29));
            check(scene.Selected.Day == 15 && scene.Month.Month == 11 && scene.Today.Day == 29, "midnight updates today without interrupting future-month browsing");
            scene.Month = new DateTime(2026, 10, 1);
            using (var image = scene.Render(new Size(1920, 1080)))
            {
                var today = scene.Hits.First(h => h.Action == "date" && h.Date == scene.Today);
                var old = scene.Hits.First(h => h.Action == "date" && h.Date == new DateTime(2026, 10, 27));
                Func<Hit, Color> pixel = h => image.GetPixel((int)(h.Box.Right - 8), (int)(h.Box.Bottom - 8));
                check(pixel(today).ToArgb() == pixel(old).ToArgb(), "today cell keeps the normal background without fill");
                var markedDay = scene.Today;
                scene.Today = DateTime.MinValue;
                using (var plain = scene.Render(new Size(1920, 1080)))
                {
                    int changed = 0;
                    for (int yy = (int)today.Box.Top; yy < (int)(today.Box.Top + 38 * scene.Layout.Scale); yy++)
                        for (int xx = (int)today.Box.Left; xx < (int)(today.Box.Left + 42 * scene.Layout.Scale); xx++)
                            if (image.GetPixel(xx, yy) != plain.GetPixel(xx, yy))
                                changed++;
                    check(changed > 20, "today date has a visible outline compared with an unmarked date");
                }

                scene.Today = markedDay;
            }

            using (var parent = new Form
            {
                ClientSize = new Size(1000, 700)
            }

            )
            {
                Tests.ShowHost(parent);
                Application.DoEvents();
                using (var surface = new DesktopSurface(scene, parent.Handle))
                {
                    surface.Attach(parent.RectangleToScreen(parent.ClientRectangle));
                    check(surface.Region != null && !surface.Region.IsVisible(20, 200) && surface.Region.IsVisible(400, 200), "native window excludes icon strip from painting and mouse region");
                }

                parent.Close();
            }

            var fake = new FakeWallpapers();
            string path = Path.Combine(directory, "background-test");
            var manager = new MonitorBackground(path, delegate
            {
                return fake;
            });
            manager.Apply(new Rectangle(0, 0, 320, 240), Palette.Cream);
            check(fake.Paths["main"] != "original-main.bmp" && fake.Paths["left"] == "original-left.bmp" && fake.Paths["portrait"] == "original-portrait.bmp", "background changes only the selected monitor");
            manager.Apply(new Rectangle(0, 0, 320, 240), Color.Beige);
            new MonitorBackground(path, delegate
            {
                return fake;
            }).Restore();
            check(fake.Paths["main"] == "original-main.bmp", "journal restores original background after theme changes and simulated restart");
            manager.Apply(new Rectangle(1920, 0, 240, 320), Palette.Cream);
            fake.Paths["portrait"] = "user-new-wallpaper.bmp";
            manager.Restore();
            check(fake.Paths["portrait"] == "user-new-wallpaper.bmp", "exit does not replace a wallpaper the user changed independently");
            using (var editor = new Editor(new State(), null, DateTime.Today))
            {
                editor.Show();
                Application.DoEvents();
                check(editor.FormBorderStyle == FormBorderStyle.Sizable && editor.ControlBox && editor.MinimizeBox && editor.SaveButton is SoftButton, "editor uses rounded chrome and soft buttons");
                check(editor.SaveButton.Parent is Panel, "small-screen dialog uses scrollable content container");
                editor.Close();
            }

            using (var reading = new ReadingDialog(Reading.Daily("天秤座", DateTime.Today), "今日运势"))
            {
                reading.Show();
                Application.DoEvents();
                reading.Controls.OfType<Panel>().SelectMany(p => p.Controls.OfType<Button>()).First(b => b.Text == "换星座").PerformClick();
                check(reading.DialogResult == DialogResult.Retry, "zodiac details provide a working change-sign action");
            }
        }
    }
#endif

}