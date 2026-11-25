using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaperDays
{
    public static class DesktopAppearance
    {
        // Window regions leave the wallpaper and native desktop input untouched between cards.
        public static bool NeedsSolidBackground(State state){return state.FillScreen&&!Skins.IsGlass(state.Skin)&&(state.ShowCalendar||state.ShowWidget||state.ShowPlans);}
        public static Region CreateRegion(Scene scene, Size size)
        {
            // Fill mode uses the wallpaper backend, keeping native desktop icons above its background.
            var region=new Region(); region.MakeEmpty();
            foreach(var item in scene.ComponentRects)
                if(scene.ComponentVisible(item.Key)) { var bounds=item.Value;if(scene.EditingLayout)bounds.Inflate(10,10);using(var path=Chrome.Round(bounds,14)) region.Union(path); }
            return region;
        }
    }

    public sealed partial class Controller
    {
        void AddDisplayPreferences(ThemedMenu menu)
        {
            menu.Items.Add(new ToolStripSeparator());
            AddComponentPreferences(menu);
            var relief = new ToolStripMenuItem(L.T("日期起伏")) { Checked = state.GlassRelief, Enabled = Skins.IsGlass(state.Skin) };
            menu.Opening += delegate { relief.Checked = state.GlassRelief; relief.Enabled = Skins.IsGlass(state.Skin); };
            relief.Click += delegate { if (Change(delegate { state.GlassRelief = !state.GlassRelief; })) RefreshSurface(); };
            menu.Items.Add(relief);
            AddLanguageMenu(menu);
        }

        void AddLanguageMenu(ThemedMenu menu)
        {
            var language = new ToolStripMenuItem("语言 / Language");
            foreach (var option in new[] { new[] { "zh-CN", "简体中文" }, new[] { "zh-TW", "繁體中文" }, new[] { "en", "English" }, new[] { "ko", "한국어" }, new[] { "ja", "日本語" } })
            {
                string code = option[0];
                var item = new ToolStripMenuItem(option[1]) { Checked = state.Language == code };
                item.Click += delegate { ApplyPreferences(false, 100, code); };
                menu.Opening += delegate { item.Checked = state.Language == code; };
                language.DropDownItems.Add(item);
            }
            menu.Items.Add(language);
        }

        void ApplyPreferences(bool transparent, int opacity, string language)
        {
            // Defer until the originating menu has finished processing its click.
            dispatcher.BeginInvoke((Action)delegate
            {
                if (language != state.Language)
                    foreach (Form form in Application.OpenForms)
                        if (form.Visible && form != fallback)
                        {
                            Notice.Show(L.T("关闭编辑窗口后再切换语言。"));
                            return;
                        }
                foreach (var surface in surfaces)
                    if (!surface.FinishInline()) return;
                if (!Change(delegate { state.TransparentDesktop = transparent; state.PanelOpacity = opacity; state.Language = language; state.LanguageConfirmed=true; })) return;
                L.Language = state.Language;
                ReloadCalendars();
                RefreshSurface();
            });
        }
    }
}
