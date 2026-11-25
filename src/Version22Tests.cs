using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version22Tests
    {
        /// <summary>
        /// 执行Version22Tests中的GetDC验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr h);
        /// <summary>
        /// 执行Version22Tests中的ReleaseDC验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr h, IntPtr dc);
        /// <summary>
        /// 执行Version22Tests中的SendMessage验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        /// <summary>
        /// 执行Version22Tests中的GetTextColor验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("gdi32.dll")]
        static extern uint GetTextColor(IntPtr dc);
        /// <summary>
        /// 执行Version22Tests中的GetBkColor验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("gdi32.dll")]
        static extern uint GetBkColor(IntPtr dc);
        /// <summary>
        /// 执行Version22Tests中的SetTextColor验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("gdi32.dll")]
        static extern uint SetTextColor(IntPtr dc, uint color);
        [StructLayout(LayoutKind.Sequential)]
        struct Rect
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct FormatRange
        {
            public IntPtr DC, Target;
            public Rect Area, Page;
            public int First, Last;
        }

        /// <summary>
        /// 执行Version22Tests中的SendMessage验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, ref FormatRange range);
        /// <summary>
        /// 执行Version22Tests中的RenderBlack验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static bool RenderBlack(ReadableTodoInput input, string path)
        {
            using (var bmp = new Bitmap(700, 150))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.White);
                    var dc = g.GetHdc();
                    try
                    {
                        var r = new Rect
                        {
                            Right = 10000,
                            Bottom = 2100
                        };
                        var f = new FormatRange
                        {
                            DC = dc,
                            Target = dc,
                            Area = r,
                            Page = r,
                            First = 0,
                            Last = input.TextLength
                        };
                        SendMessage(input.Handle, 0x439, new IntPtr(1), ref f);
                    }
                    finally
                    {
                        SendMessage(input.Handle, 0x439, IntPtr.Zero, IntPtr.Zero);
                        g.ReleaseHdc(dc);
                    }
                }

                int dark = 0;
                for (int y = 0; y < bmp.Height; y++)
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        var c = bmp.GetPixel(x, y);
                        if (c.R < 80 && c.G < 80 && c.B < 80)
                            dark++;
                    }

                bmp.Save(path);
                return dark > 100;
            }
        }

        /// <summary>
        /// 执行Version22Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string dir)
        {
            for (int n = 0; n < 3; n++)
                using (var form = new CourseImportDialog(new State()))
                {
                    int handles = 0;
                    bool ready = false;
                    form.HandleCreated += delegate
                    {
                        handles++;
                    };
                    form.Shown += delegate
                    {
                        ready = (string)form.Tag == "chrome-ready" && form.FormBorderStyle == FormBorderStyle.Sizable && form.ControlBox && form.MinimizeBox;
                    };
                    form.Show();
                    Application.DoEvents();
                    var before = form.Bounds;
                    Application.DoEvents();
                    check(ready && handles == 1 && form.Bounds == before, "course dialog prepared before Shown without visible handle recreation " + n);
                    form.Close();
                }

            foreach (var skin in Skins.All.Where(x => x.Id != "stone"))
            {
                var state = new State
                {
                    Skin = skin.Id
                };
                Skins.Apply(state);
                var scene = new Scene
                {
                    State = state
                };
                using (var form = new Form
                {
                    ClientSize = new Size(1280, 800)
                }

                )
                using (var view = new SceneView(scene)
                {
                    Dock = DockStyle.Fill,
                    KeepInlineOnBlur = true
                }

                )
                {
                    form.Controls.Add(view);
                    form.Show();
                    view.BeginInline();
                    view.InlineInput.Text = "这是一条可见的深色待办\r\n继续输入第二行";
                    Application.DoEvents();
                    view.InlineInput.SelectAll();
                    view.InlineInput.SelectionColor = Color.White;
                    view.InlineInput.EnsureBlack();
                    check(view.InlineInput.SelectionColor == Color.Black, "RichEdit stored character color is black for " + skin.Id);
                    view.InlineInput.Select(view.InlineInput.TextLength, 0);
                    check(view.InlineInput.SelectionColor == Color.Black, "insertion color is black for " + skin.Id);
                    check(RenderBlack(view.InlineInput, Path.Combine(dir, "native-text-" + skin.Id + ".png")), "native RichEdit renders dark text for " + skin.Id);
                    if (skin.Id == "cream")
                        using (var image = new Bitmap(view.Width, view.Height))
                        {
                            view.DrawToBitmap(image, new Rectangle(Point.Empty, view.Size));
                            image.Save(Path.Combine(dir, "readable-inline-todo.png"));
                        }

                    view.FinishInline();
                    form.Close();
                }
            }

            foreach (var skin in Skins.All.Where(x => x.Id != "stone"))
            {
                Skins.Apply(new State { Skin = skin.Id });
                using (var form = new Form
                {
                    ClientSize = new Size(1280, 800)
                }

                )
                using (var view = new SceneView(new Scene { State = new State() })
                {
                    Dock = DockStyle.Fill,
                    IsolatedInput = true
                }

                )
                {
                    form.Controls.Add(view);
                    Tests.ShowHost(form);
                    view.KeepInlineOnBlur = true;
                    view.BeginInline();
                    Application.DoEvents();
                    var input = view.InlineInput;
                    check(input != null && input.FindForm() is TodoInputHost && input.FindForm().FormBorderStyle == FormBorderStyle.None, "desktop input is isolated in opaque inline host " + skin.Id);
                    input.Text = "输入中的文字必须是黑色\r\n第二行";
                    Application.DoEvents();
                    check(input.SelectionColor == Color.Black && RenderBlack(input, Path.Combine(dir, "isolated-input-" + skin.Id + ".png")), "isolated input renders black while typing " + skin.Id);
                    var hdc = GetDC(input.Handle);
                    try
                    {
                        SetTextColor(hdc, 0xFFFFFF);
                        SendMessage(input.FindForm().Handle, 0x133, hdc, input.Handle);
                        check(GetTextColor(hdc) == 0, "inline host overrides inherited white text DC " + skin.Id);
                    }
                    finally
                    {
                        ReleaseDC(input.Handle, hdc);
                    }

                    SendMessage(input.Handle, 0x102, new IntPtr(0x9ED1), IntPtr.Zero);
                    Application.DoEvents();
                    check(input.Text.Contains("黑") && input.SelectionColor == Color.Black, "native typing retains black insertion color " + skin.Id);
                    var host = input.FindForm();
                    view.KeepInlineOnBlur = false;
                    form.Activate();
                    Application.DoEvents();
                    // 其他程序占着前台时收不到失焦消息，这里补一次，保存和关闭的结果照常检查。
                    if (view.InlineInput != null && !input.ContainsFocus)
                        view.FinishInline();
                    check(view.InlineInput == null, "leaving isolated editor saves and removes input " + skin.Id);
                    view.FinishInline();
                    check(host.IsDisposed, "inline host closes on save " + skin.Id);
                    form.Close();
                }
            }

            check(HolidayStyle.Colors.Distinct().Count() == Calendars.Codes.Length && Skins.All.All(s => !s.Colors.Intersect(HolidayStyle.Colors, StringComparer.OrdinalIgnoreCase).Any()), "holiday country colors unique and separate from every built-in event palette");
            var cn = HolidayUpdates.Read("CN", 2026);
            check(Enumerable.Range(1, 7).All(d => cn.Any(m => m.Date == "2026-10-0" + d && !HolidayStyle.Work(m))), "all seven announced National Day leave dates visible");
            var us = HolidayUpdates.Read("US", 2027);
            check(us.Any(m => m.Date == "2027-07-05" && m.Label.Contains("独立")) && us.Any(m => m.Date == "2027-12-24" && m.Label.Contains("圣诞")), "US observed holidays include Monday July 5 and Friday December 24");
            var future = HolidayUpdates.Read("CN", 2027);
            check(Enumerable.Range(5, 4).All(d => future.Any(m => m.Date == "2027-02-0" + d)) && future.All(m => !m.Label.StartsWith("节 ·")), "four statutory Chinese New Year dates without festival-only prefix");
            var holidayScene = new Scene
            {
                State = new State
                {
                    Countries = new List<string>
                    {
                        "CN",
                        "KR"
                    }
                },
                Month = new DateTime(2026, 10, 1)
            };
            string status;
            holidayScene.Holidays = HolidayUpdates.Load(holidayScene.State, holidayScene.Month, out status);
            using (var bmp = holidayScene.Render(new Size(1600, 900)))
                bmp.Save(Path.Combine(dir, "country-holidays.png"));
            Skins.Apply(new State());
            var order = new List<string>();
            var errors = ShutdownFlow.Run(() => order.Add("restore selected wallpaper"), () =>
            {
                order.Add("restore legacy wallpaper");
                throw new IOException("legacy failure");
            }, () => order.Add("remove calendar surface"), () =>
            {
                order.Add("save data");
                throw new IOException("save failure");
            });
            check(order.SequenceEqual(new[] { "restore selected wallpaper", "restore legacy wallpaper", "remove calendar surface", "save data" }) && errors.Count == 2, "shutdown restores before removing surface and independently reports later failures");
        }
    }
#endif

}