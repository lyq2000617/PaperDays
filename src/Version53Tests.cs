#if TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
namespace PaperDays {
 public static class Version53Tests {
  static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
  static object Field(object o,string n){return o.GetType().GetField(n,flags).GetValue(o);}
  static object Call(object o,string n,params object[] a){return o.GetType().GetMethod(n,flags).Invoke(o,a);}
  static IEnumerable<Control> All(Control o){foreach(Control c in o.Controls){yield return c;foreach(var x in All(c))yield return x;}}
  static void Click(Control c){typeof(Control).GetMethod("OnClick",flags).Invoke(c,new object[]{EventArgs.Empty});}
  static void Shot(Form f,string path){f.Show();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(path);}}
  public static int Run(string dir){
   Directory.CreateDirectory(dir);Data.Root=dir;OnlineData.Enabled=false;L.Language="zh-CN";
   var lines=new List<string>();int failed=0;Action<bool,string> check=(b,n)=>{lines.Add((b?"PASS ":"FAIL ")+n);if(!b)failed++;};
   try{
    var s=new State{LanguageConfirmed=true,Skin="glass-light",ShowCalendar=false,ShowWidget=false,ShowPlans=false};Skins.Apply(s);
    s.Calendars.Add(new UserCalendar{Id="test-course",Name="测试课程"});s.Events.Add(new Entry{CalendarId="test-course",Title="保留到确认删除"});
    using(var f=new SettingsDialog(s,false)){
     check(f.DeleteCalendar("test-course"),"calendar deletion accepted in draft");check(s.Events.Count==1&&s.Calendars.Count==2,"draft deletion does not alter source");check(!f.DeleteCalendar("personal"),"personal calendar protected");
     check(f.Result==null,"cancel has no result");
    }
    using(var f=new SettingsDialog(s,false)){
     f.DeleteCalendar("test-course");Shot(f,Path.Combine(dir,"calendar-manager.png"));Click(All(f).OfType<Button>().Single(b=>b.Text==L.T("应用")));
     check(f.Result!=null&&f.Result.Events.Count==0&&f.Result.Calendars.Count==1,"apply removes calendar and its entries");
    }
    using(var f=new DivinationDialog(new DivinationRecord{A=1,B=2,C=3})){
     check(f.Result==null&&f.Numbers.All(n=>String.IsNullOrEmpty(n.Text)),"opening divination resets old input and result");
     f.Numbers[0].Text="1";f.Numbers[1].Text="99";f.Numbers[2].Text="2";f.Compute();check(f.Result!=null,"valid numbers compute");f.ClearNumbers();check(f.Result==null&&f.Numbers.All(n=>n.Text==""),"clear resets result and numbers");
     Shot(f,Path.Combine(dir,"divination-empty.png"));
    }
    using(var controller=new Controller(s,false,true,true)){
     ((NotifyIcon)Field(controller,"tray")).Visible=false;check(Field(controller,"componentManager")==null,"startup does not open component manager");
     Call(controller,"OpenComponentManager");var manager=(Form)Field(controller,"componentManager");check(manager.Icon==WindowIcons.Components&&manager.MinimizeBox,"manager has dedicated taskbar icon and minimize");Shot(manager,Path.Combine(dir,"widgets.png"));manager.Close();
     Call(controller,"ShowAllComponents");check(s.ShowCalendar&&s.ShowWidget&&s.ShowPlans,"restore all after all hidden");
     Call(controller,"OpenGlassSettings");var f=(Form)Field(controller,"glassSettings");Shot(f,Path.Combine(dir,"appearance-top.png"));
     foreach(var control in All(f).Where(c=>c.AccessibleName=="最小化"||c.AccessibleName=="关闭窗口"))lines.Add("CHROME "+control.AccessibleName+" "+control.Bounds+" parent "+control.Parent.Bounds);
     check(All(f).OfType<SkinTile>().Count()==Skins.All.Length,"all skin cards present");
     var mode=All(f).Single(c=>c.Name=="mode-widgets");check(mode.Parent.Top<All(f).OfType<SkinTile>().First().Parent.Top,"display mode above skins");
     Click(All(f).OfType<SkinTile>().First(t=>!Skins.IsGlass(t.Theme.Id)));Click(All(f).Single(c=>c.Name=="mode-fill"));Click(All(f).OfType<SkinTile>().First(t=>t.Theme.Id=="glass"));check(!s.FillScreen,"first glass selection defaults to widgets");
     Click(All(f).Single(c=>c.Name=="mode-fill"));Click(All(f).OfType<SkinTile>().First(t=>!Skins.IsGlass(t.Theme.Id)));Click(All(f).OfType<SkinTile>().First(t=>t.Theme.Id=="glass"));check(s.FillScreen,"explicit glass fill mode remembered");f.Close();
     Call(controller,"OpenGuide");var guide=(GuideDialog)Field(controller,"guide");check(guide.Step==-1,"guide opens welcome");Shot(guide,Path.Combine(dir,"guide-welcome.png"));guide.Next();check(guide.Step==0,"next starts step one");guide.Next();guide.Back();check(guide.Step==0,"back works");guide.Close();check(s.GuideVersion==2,"closing guide persists dismissal");
    }
    foreach(string lang in new[]{"zh-CN","en","ko","ja"}){
     L.Language=lang;using(var f=new GuideDialog(step=>null)){
      f.Show();for(int i=0;i<5;i++)f.Next();Application.DoEvents();check(f.Step==4,"five guide steps "+lang);Shot(f,Path.Combine(dir,"guide-last-"+lang+".png"));f.Close();
     }
    }
    L.Language="zh-CN";s.Widget="占卜";var scene=new Scene{State=s};scene.FortuneNumbers=new[]{"5","6","7"};using(var b=scene.Render(new Size(1366,768))){b.Save(Path.Combine(dir,"desktop-fortune.png"));}check(scene.FortuneNumbers[0]=="5","repaint preserves draft input");scene.ClearFortune();check(s.Divination==null&&scene.FortuneNumbers.All(n=>n==""),"scene clear resets both");
    foreach(var size in new[]{new Size(320,180),new Size(500,300),new Size(250,600)}){
     s.WidgetLayouts[scene.PlacementKey]=new[]{new WidgetPlacement{Id="widget",X=.05f,Y=.05f,W=size.Width/1366f,H=size.Height/768f}};
     using(var b=scene.Render(new Size(1366,768))){}
     scene.ScrollOffsets["sidebarfortune"]=Math.Min(170,scene.ScrollLimits["sidebarfortune"]);using(var b=scene.Render(new Size(1366,768))){}
     var box=scene.ComponentRects["widget"];var inputs=scene.Hits.Where(h=>h.Action=="fortuneinput").ToArray();check(inputs.Length==3&&inputs.All(h=>box.Contains(h.Box)),"resized fortune input bounds "+size);
     check(scene.Hits.Where(h=>h.Action=="drawfortune").All(h=>box.Contains(h.Box)),"resized fortune action bounds "+size);
    }
    Data.Write(s);var saved=Data.Read();check(saved.GuideVersion==2&&saved.SkinDisplayModes["glass"],"guide and per-skin mode persist");
   }catch(Exception ex){failed++;lines.Add("ERROR "+ex);}
   File.WriteAllLines(Path.Combine(dir,"test-results.txt"),lines);return failed==0?0:1;
  }
 }
}
#endif
