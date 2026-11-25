using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace PaperDays
{

#if TESTS
    public static class Version28Tests
    {
        /// <summary>
        /// 执行Version28Tests中的All验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static IEnumerable<Control> All(Control p)
        {
            foreach (Control c in p.Controls)
            {
                yield return c;
                foreach (var d in All(c))
                    yield return d;
            }
        }

        /// <summary>
        /// 执行Version28Tests中的Shot验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static void Shot(Control c, string file)
        {
            using (var b = new Bitmap(c.Width, c.Height))
            {
                c.DrawToBitmap(b, new Rectangle(Point.Empty, c.Size));
                b.Save(file);
            }
        }

        /// <summary>
        /// 执行Version28Tests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string dir)
        {
            Skins.Apply(new State());
            using (var editor = new Editor(new State(), null, new DateTime(2028, 2, 29)))
            {
                Tests.ShowHost(editor);
                var until = DateTime.UtcNow.AddMilliseconds(250);
                while (DateTime.UtcNow < until)
                {
                    Application.DoEvents();
                    System.Threading.Thread.Sleep(5);
                }

                editor.Activate();
                Application.DoEvents();
                check(editor.Reminder.Width <= 170, "reminder is a compact content-sized control");
                editor.StartDate.OpenPicker();
                Application.DoEvents();
                var popup = editor.StartDate.Popup;
                Shot(popup, Path.Combine(dir, "date-popup28.png"));
                check(All(popup).OfType<Button>().Any(b => b.AccessibleName == "2028-02-29"), "custom calendar includes leap day");
                All(popup).OfType<Button>().Single(b => b.Text == "›").PerformClick();
                check(All(popup).OfType<Button>().Any(b => b.AccessibleName == "2028-03-31") && !All(popup).OfType<Button>().Any(b => b.AccessibleName == "2028-02-29"), "calendar changes months without stale date controls");
                All(popup).OfType<Button>().Single(b => b.AccessibleName == "2028-03-02").PerformClick();
                check(editor.StartDate.Value == new DateTime(2028, 3, 2) && editor.StartDate.Popup == null, "calendar selection applies and closes popup");
                Application.DoEvents();
                editor.StartDate.Text = "2028-02-30";
                check(!editor.StartDate.CommitInput(), "invalid typed calendar date rejected");
                editor.StartDate.Text = "2028/03/02";
                check(editor.StartDate.CommitInput(), "typed date remains available");
                editor.EndDate.Value = editor.StartDate.Value;
                editor.StartTime.Text = "09:00";
                editor.StartTime.OpenPicker();
                Application.DoEvents();
                popup = editor.StartTime.Popup;
                Shot(popup, Path.Combine(dir, "time-popup28.png"));
                All(popup).OfType<Button>().Single(b => b.Text == "09:15").PerformClick();
                check(editor.StartTime.Text == "09:15" && editor.StartTime.Popup == null, "custom time grid applies selected time");
                editor.EndTime.Text = "10：07";
                editor.TitleInput.Text = "配色与时间预览";
                Shot(editor, Path.Combine(dir, "editor28.png"));
                check(editor.TrySave() && editor.Result.End.Minute == 7 && editor.Result.Start.Minute == 15, "custom picker saves selected and manually entered times");
            }

            using (var skins = new SkinDialog("cream"))
            {
                Tests.ShowHost(skins);
                check(All(skins).OfType<Button>().Where(b => Skins.All.Any(s => b.Text.StartsWith(s.Name))).All(b => b.Font.Name != "Segoe UI Emoji"), "Chinese skin names use Chinese text font");
                Shot(skins, Path.Combine(dir, "skins28.png"));
                skins.Close();
            }

            Skins.Apply(new State { Skin = "blue" });
            check(Skins.Current.Background == "#EDF3F5", "approved blue palette retained");
            Skins.Apply(new State());
        }
    }
#endif

}