#if TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{
    public static class Version46Tests
    {
        [DllImport("user32.dll")] static extern bool SetLayeredWindowAttributes(IntPtr h, uint color, byte alpha, uint flags);
        [DllImport("user32.dll")] static extern bool GetLayeredWindowAttributes(IntPtr h, out uint color, out byte alpha, out uint flags);
        public static int Run(string directory)
        {
            Directory.CreateDirectory(directory);
            Data.Root = directory;
            OnlineData.Enabled = false;
            var results = new List<string>();
            int failures = 0;
            Action<bool, string> check = (ok, message) => { results.Add((ok ? "PASS " : "FAIL ") + message); if (!ok) failures++; };
            try
            {
                var legacy = Data.Json().Deserialize<State>("{\"Todos\":[{\"Title\":\"今天\"}],\"Events\":[],\"Language\":null}");
                Data.Normalize(legacy);
                check(!legacy.TransparentDesktop && legacy.PanelOpacity == 85 && legacy.Language == "zh-CN", "v0.45 data defaults to opaque Chinese, opacity 85");
                legacy.TransparentDesktop = true; legacy.PanelOpacity = 65; legacy.Language = "en";
                Data.Write(legacy);
                var loaded = Data.Read();
                check(loaded.TransparentDesktop && loaded.PanelOpacity == 65 && loaded.Language == "en", "display preferences survive disk roundtrip");
                check(loaded.Todos[0].Title == "今天", "user text equal to a UI label is unchanged");
                loaded.Language = "invalid"; loaded.PanelOpacity = -100; Data.Normalize(loaded);
                check(loaded.Language == "zh-CN" && loaded.PanelOpacity == 35, "invalid settings normalized safely");
                loaded.PanelOpacity = 200; Data.Normalize(loaded);
                check(loaded.PanelOpacity == 100, "opacity upper bound");
                L.Language = "en";
                check(L.T("今天") == "Today" && L.T("unregistered") == "unregistered", "English labels and unknown fallback");
                check(L.Date(new DateTime(2026,11,16), "yyyy 年 M 月", "MMMM yyyy") == "November 2026", "English month formatting");
                check(CourseBook.Days[0] == "Mon", "course weekday labels in English");
                L.Language = "zh-CN";
                check(CourseBook.Days[0] == "周一" && L.T("今天") == "今天", "switching back refreshes course labels");
                using (var menu = new ThemedMenu())
                {
                    menu.Items.Add("显示屏幕"); L.Language = "en"; L.RefreshMenu(menu.Items);
                    check(menu.Items[0].Text == "Display", "existing tray menu switches to English");
                    L.Language = "zh-CN"; L.RefreshMenu(menu.Items);
                    check(menu.Items[0].Text == "显示屏幕", "existing tray menu switches back");
                }
                foreach (var language in new[] { "zh-CN", "en" })
                foreach (var size in new[] { new Size(1920,1080), new Size(1366,768), new Size(1080,1920) })
                foreach (var view in new[] { "month", "week" })
                {
                    L.Language = language;
                    var state = new State { Language = language, TransparentDesktop = true, PanelOpacity = 85, CalendarView = view, IconColumns = -1 };
                    state.Todos.Add(new Todo { Title = "我的原始待办 / Original task" });
                    Skins.Apply(state);
                    var scene = new Scene { State = state, ReservedLeft = 180, Month = new DateTime(2026,11,1), Selected = new DateTime(2026,11,16) };
                    using (var bitmap = scene.Render(size))
                    using (var region = DesktopAppearance.CreateRegion(scene,size))
                    {
                        check(!region.IsVisible(20,200) && !region.IsVisible(size.Width-1,0), "transparent icon space / outer gaps " + language + " " + size + " " + view);
                        check(scene.Hits.Where(h => h.Action == "today" || h.Action == "tools" || h.Action == "addtodo").All(h => region.IsVisible(h.Box.X + h.Box.Width/2, h.Box.Y+h.Box.Height/2)), "interactive buttons remain in visible region " + language + " " + size + " " + view);
                        if (view == "month" && size.Width == 1920) bitmap.Save(Path.Combine(directory, "calendar-" + language + ".png"));
                    }
                    check(state.Todos[0].Title == "我的原始待办 / Original task", "rendering never translates user content");
                }
                var regular = new Scene { State = new State { IconColumns = 0 } };
                check(DesktopAppearance.CreateRegion(regular,new Size(1920,1080)) == null, "normal mode retains original full surface");
                // Hidden native host: does not attach to Explorer or touch the user's wallpaper.
                using (var host = new Form())
                {
                    var sc = new Scene { State = new State { TransparentDesktop = true } };
                    using (var surface = new DesktopSurface(sc,host.Handle))
                    {
                        surface.Size = new Size(1000,700);
                        bool set = SetLayeredWindowAttributes(surface.Handle,0,217,2);
                        uint color,flags; byte alpha;
                        bool read = GetLayeredWindowAttributes(surface.Handle,out color,out alpha,out flags);
                        check(set && read && alpha == 217 && flags == 2, "native layered child supports alpha without color-key click-through");
                    }
                }
                L.Language = "en";
                using (var editor = new Editor(new State(),null,DateTime.Today))
                {
                    editor.CreateControl();
                    using (var bitmap = new Bitmap(editor.Width,editor.Height))
                    { editor.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size)); bitmap.Save(Path.Combine(directory,"editor-en.png")); }
                    check(editor.SaveButton.Text == "Save event", "event editor uses English controls");
                }
                var course = new Entry { Notes = "教室：A101" };
                check(CourseBook.Room(course) == "A101", "Chinese stored room identifiers work in English");
                CourseBook.SetRoom(course,"B202");
                check(CourseBook.Room(course) == "B202", "room editing preserves stable storage keys");
            }
            catch (Exception ex) { failures++; results.Add("ERROR " + ex); }
            finally { L.Language = "zh-CN"; File.WriteAllLines(Path.Combine(directory,"test-results.txt"),results); }
            return failures == 0 ? 0 : 1;
        }
    }
}
#endif
