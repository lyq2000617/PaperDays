#if TESTS
using System;using System.Collections.Generic;using System.Diagnostics;using System.Drawing;using System.IO;using System.Linq;using System.Reflection;using System.Threading;using System.Windows.Forms;
namespace PaperDays {public static class Version54Tests {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Field(object o,string n){return o.GetType().GetField(n,flags).GetValue(o);}static object Call(object o,string n,params object[] args){return o.GetType().GetMethod(n,flags).Invoke(o,args);}
 static IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
 static void Click(Control c){typeof(Control).GetMethod("OnClick",flags).Invoke(c,new object[]{EventArgs.Empty});}
 static void Shot(Form f,string path){f.PerformLayout();Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(path);}}
 public static int Run(string dir,bool quick=false){
  Directory.CreateDirectory(dir);Data.Root=dir;OnlineData.Enabled=false;var lines=new List<string>();var timing=new List<long>();int fails=0;
  Action<bool,string> check=(ok,n)=>{lines.Add((ok?"PASS ":"FAIL ")+n);if(!ok)fails++;};
  var watchDog=new System.Threading.Timer(_=>{File.AppendAllText(Path.Combine(dir,"test-results.txt"),"ERROR UI watchdog timeout");Environment.Exit(9);},null,120000,Timeout.Infinite);
  try{
   foreach(string lang in (quick?new string[0]:new[]{"zh-CN","zh-TW","en","ko","ja"}))foreach(var skin in Skins.All){
    L.Language=lang;var state=new State{Skin=skin.Id,Language=lang};Skins.Apply(state);var sw=Stopwatch.StartNew();
    using(var f=new Editor(state,null,new DateTime(2026,11,20))){
     f.Show();Application.DoEvents();float font=f.TitleInput.Font.Size;var viewport=All(f).OfType<Panel>().Single(c=>c.Name=="dialog-scroll-viewport");
     for(int i=0;i<8;i++){f.Size=i%2==0?new Size(390,380):new Size(700,650);Application.DoEvents();}
     f.Size=new Size(390,380);Application.DoEvents();check(Math.Abs(font-f.TitleInput.Font.Size)<.01,"stable editor font "+lang+skin.Id);
     check(viewport.HorizontalScroll.Visible&&viewport.VerticalScroll.Visible,"two-axis editor scrolling "+lang+skin.Id);
     viewport.AutoScrollPosition=new Point(300,1000);Application.DoEvents();check(viewport.AutoScrollPosition.X<0&&viewport.AutoScrollPosition.Y<0,"scrollbars reach overflow "+lang+skin.Id);
     if(lang=="ko"&&skin.Id=="rose"){viewport.AutoScrollPosition=Point.Empty;f.Size=new Size(650,650);Shot(f,Path.Combine(dir,"korean-editor.png"));}
     f.TitleInput.Text="Save "+lang+skin.Id;check(f.TrySave()&&f.Result!=null,"editor saves after repeated resize "+lang+skin.Id);state.Events.Add(f.Result);Data.Write(state);check(Data.Read().Events.Any(e=>e.Title=="Save "+lang+skin.Id),"editor survives data reload "+lang+skin.Id);
    }timing.Add(sw.ElapsedMilliseconds);
   }
   if(timing.Count>0)lines.Add("TIMING editor open + 8 resize + save/reload: median="+timing.OrderBy(x=>x).ElementAt(timing.Count/2)+"ms max="+timing.Max()+"ms ("+timing.Count+" cases)");
   using(var retry=new Editor(new State(),null,DateTime.Today)){retry.Show();Application.DoEvents();retry.TitleInput.Text="Keep draft";retry.Commit=e=>false;check(!retry.TrySave()&&!retry.IsDisposed&&retry.TitleInput.Text=="Keep draft","failed persistence keeps editor draft open");retry.Commit=e=>true;check(retry.TrySave(),"draft can retry save after persistence recovers");}
   L.Language="zh-CN";var live=new State{Skin="glass-light",Language="zh-CN",ShowWidget=false,ShowPlans=false,ShowCalendar=false};Skins.Apply(live);
   using(var c=new Controller(live,false,true,true)){
    ((NotifyIcon)Field(c,"tray")).Visible=false;
    var sw=Stopwatch.StartNew();Call(c,"Edit",null,new DateTime(2026,11,20),null,null,false);Application.DoEvents();var editor=Application.OpenForms.OfType<Editor>().Single();editor.TitleInput.Text="Controller persisted";editor.TrySave();Application.DoEvents();check(live.Events.Any(e=>e.Title=="Controller persisted")&&Data.Read().Events.Any(e=>e.Title=="Controller persisted"),"controller add event persists after restart read");lines.Add("TIMING controller open+submit+persist "+sw.ElapsedMilliseconds+"ms");
    Call(c,"OpenGuide");var language=(CompactDialog)Field(c,"firstLanguage");check(language!=null&&Field(c,"guide")==null,"language precedes first tutorial");language.Show();Application.DoEvents();Click(All(language).OfType<Button>().Single(b=>b.Text=="继续 / Continue"));var guide=(GuideDialog)Field(c,"guide");check(live.LanguageConfirmed&&guide!=null,"confirm language starts tutorial");guide.Show();guide.Next();guide.Close();check(live.GuideVersion==2,"tutorial dismissal saved");
    Call(c,"OpenGlassSettings");var appearance=(Form)Field(c,"glassSettings");appearance.Show();Application.DoEvents();Shot(appearance,Path.Combine(dir,"appearance-top.png"));
    var tiles=All(appearance).OfType<SkinTile>().ToArray();var times=new List<long>();foreach(var tile in tiles){sw.Restart();Click(tile);Application.DoEvents();times.Add(sw.ElapsedMilliseconds);}lines.Add("TIMING skin selection median="+times.OrderBy(x=>x).ElementAt(times.Count/2)+"ms max="+times.Max()+"ms");
    Click(tiles.Single(t=>t.Theme.Id=="glass-light"));Application.DoEvents();var page=All(appearance).OfType<FlowLayoutPanel>().Single(p=>p.Dock==DockStyle.Fill);page.AutoScrollPosition=new Point(0,10000);Application.DoEvents();var done=All(appearance).OfType<Button>().Single(b=>b.Text==L.T("完成"));lines.Add("SCROLL page="+page.ClientSize+" min="+page.AutoScrollMinSize+" position="+page.AutoScrollPosition+" done="+page.PointToClient(done.PointToScreen(System.Drawing.Point.Empty))+" size="+done.Size);check(page.RectangleToScreen(page.ClientRectangle).Contains(done.RectangleToScreen(done.ClientRectangle)),"appearance completion button reachable in full-page scroll");Shot(appearance,Path.Combine(dir,"appearance-bottom.png"));appearance.Close();
    sw.Restart();Call(c,"ShowAllComponents");Application.DoEvents();check(live.ShowCalendar&&live.ShowWidget&&live.ShowPlans,"all hidden components recover");lines.Add("TIMING hidden widgets restore state "+sw.ElapsedMilliseconds+"ms");
   }
   foreach(string skin in new[]{"glass","glass-light","rose"})foreach(string mode in new[]{"星座","占卜"}){
    var s=new State{Skin=skin,Widget=mode,Divination=new DivinationRecord{A=3,B=9,C=27}};Skins.Apply(s);var scene=new Scene{State=s};scene.WidgetLayoutsForTest(new Size(1366,768));using(var b=scene.Render(new Size(1366,768))){}
    string key=mode=="占卜"?"sidebarfortune":"sidebarzodiac";check(scene.ScrollLimits.ContainsKey(key)&&scene.ScrollLimits[key]>0,"whole widget scrolls "+skin+mode);
    scene.ScrollOffsets[key]=scene.ScrollLimits[key];using(var b=scene.Render(new Size(1366,768))){if(skin=="glass-light")b.Save(Path.Combine(dir,"widget-bottom-"+(mode=="占卜"?"fortune":"zodiac")+".png"));}check(scene.Hits.Any(h=>h.Action=="reading"),"bottom detail action accessible "+skin+mode);
    check(scene.Hits.Where(h=>h.Action=="reading"||h.Action=="leftfortune"||h.Action=="fortuneinput").All(h=>scene.ComponentRects["widget"].Contains(h.Box)),"widget controls clipped to viewport "+skin+mode);
   }
   check(DialogFrame.Edge(new Point(1,1),new Size(500,400),7)==13&&DialogFrame.Edge(new Point(499,1),new Size(500,400),7)==14&&DialogFrame.Edge(new Point(1,399),new Size(500,400),7)==16&&DialogFrame.Edge(new Point(499,399),new Size(500,400),7)==17,"four resize corners");
  }catch(Exception ex){fails++;lines.Add("ERROR "+ex);}finally{watchDog.Dispose();}
  File.WriteAllLines(Path.Combine(dir,"test-results.txt"),lines);return fails==0?0:1;
 }
 }
 public partial class Scene {internal void WidgetLayoutsForTest(Size size){State.WidgetLayouts[PlacementKey]=new[]{new WidgetPlacement{Id="widget",X=.01f,Y=.01f,W=.2f,H=.42f}};}}
}
#endif
