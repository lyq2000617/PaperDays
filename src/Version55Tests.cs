#if TESTS
using System;using System.IO;using System.Drawing;using System.Linq;using System.Collections.Generic;using System.Reflection;using System.Windows.Forms;
namespace PaperDays {public static class Version55Tests {
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
 public static int Run(string dir){Directory.CreateDirectory(dir);Data.Root=dir;OnlineData.Enabled=false;L.Language="zh-CN";var log=new List<string>();int failures=0;Action<bool,string> check=(ok,n)=>{log.Add((ok?"PASS ":"FAIL ")+n);if(!ok)failures++;};try{
 foreach(string skin in new[]{"glass-light","glass","rose"})foreach(int width in new[]{220,280,360}){
  var s=new State{Skin=skin,Widget="占卜",ShowCalendar=false,ShowPlans=false};Skins.Apply(s);var scene=new Scene{State=s};s.WidgetLayouts[scene.PlacementKey]=new[]{new WidgetPlacement{Id="widget",X=.02f,Y=.02f,W=width/900f,H=.96f}};
  using(var bg=new Bitmap(900,1000)){using(var g=Graphics.FromImage(bg))g.Clear(Color.FromArgb(80,120,135));scene.GlassBackdrop=bg;scene.ScrollOffsets["sidebarfortune"]=0;
   using(var b=scene.Render(bg.Size)){if(width==280)b.Save(Path.Combine(dir,skin+"-empty.png"));}
   var inputs=scene.Hits.Where(h=>h.Action=="fortuneinput").ToArray();var start=scene.Hits.Single(h=>h.Action=="drawfortune");var cats=scene.Hits.Where(h=>h.Action.StartsWith("fortunecat")).ToArray();
   check(inputs.Length==3&&inputs.All(h=>scene.ComponentRects["widget"].Contains(h.Box)),"inputs inside widget "+skin+width);
   check(start.Box.Height>=40&&cats.All(h=>h.Box.Top>=start.Box.Bottom+16),"prominent start precedes spaced categories "+skin+width);
   check(cats.All(h=>h.Box.Left>=scene.ComponentRects["widget"].Left+30&&h.Box.Right<=scene.ComponentRects["widget"].Right-30),"category side padding "+skin+width);
   check(!scene.Hits.Any(h=>h.Action=="reading"),"no details action before result "+skin+width);
   s.Divination=new DivinationRecord{A=3,B=9,C=27,Category="工作"};scene.LoadFortune();using(var b=scene.Render(bg.Size)){}scene.ScrollOffsets["sidebarfortune"]=scene.ScrollLimits["sidebarfortune"];using(var b=scene.Render(bg.Size)){if(width==280)b.Save(Path.Combine(dir,skin+"-result.png"));}
   check(scene.Hits.Any(h=>h.Action=="reading"),"result details reachable after scroll "+skin+width);
  }
 }
 var state=new State{Skin="glass-light",Widget="占卜",ShowCalendar=false,ShowWidget=false,ShowPlans=false,Divination=new DivinationRecord{A=3,B=9,C=27,Category="工作"}};Skins.Apply(state);Data.Write(state);
 using(var controller=new Controller(state,false,true,true)){
  ((NotifyIcon)typeof(Controller).GetField("tray",flags).GetValue(controller)).Visible=false;
  state.Divination=new DivinationRecord{A=3,B=9,C=27,Category="工作"};Data.Write(state);var scene=(Scene)typeof(Controller).GetField("scene",flags).GetValue(controller);scene.LoadFortune();string before=String.Join(",",scene.FortuneNumbers);var record=state.Divination;
  typeof(Controller).GetMethod("ReadingDetails",flags).Invoke(controller,null);Application.DoEvents();var detail=Application.OpenForms.OfType<DivinationResultDialog>().Single();
  check(!All(detail).OfType<IntegerInput>().Any(),"detail viewer has no number input requirement");check(All(detail).OfType<Label>().Any(l=>l.Text.Contains("3  /  9  /  27")),"detail displays original numbers");
  check(All(detail).OfType<FlowLayoutPanel>().First(p=>p.Dock==DockStyle.Fill).AutoScrollPosition.Y==0,"details opens at result heading");detail.Size=new Size(360,460);Application.DoEvents();using(var b=new Bitmap(detail.Width,detail.Height)){detail.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(dir,"details.png"));}detail.Close();Application.DoEvents();
  check(Object.ReferenceEquals(record,state.Divination)&&before==String.Join(",",scene.FortuneNumbers),"view and close preserve desktop record and input");check(Data.Read().Divination.C==27,"view does not erase persisted result");
 }
 }catch(Exception ex){failures++;log.Add("ERROR "+ex);}File.WriteAllLines(Path.Combine(dir,"test-results.txt"),log);return failures==0?0:1;}
}}
#endif
