#if TESTS
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Windows.Forms;
namespace PaperDays {
 public static class Version50Tests {
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
  static void Mouse(Control view,int msg,Point p){SendMessage(view.Handle,msg,IntPtr.Zero,new IntPtr((p.Y<<16)|(p.X&65535)));}
  static Point Center(RectangleF r){return new Point((int)(r.X+r.Width/2),(int)(r.Y+r.Height/2));}
  static string Digest(Bitmap b){using(var stream=new MemoryStream()){b.Save(stream,ImageFormat.Png);using(var hash=SHA256.Create())return Convert.ToBase64String(hash.ComputeHash(stream.ToArray()));}}
  public static int Run(string directory) {
   Directory.CreateDirectory(directory);Data.Root=directory;OnlineData.Enabled=false;
   var log=new List<string>();int failures=0;Action<bool,string> check=(ok,label)=>{log.Add((ok?"PASS ":"FAIL ")+label);if(!ok)failures++;};
   try {
    using(var backdrop=new Bitmap(1600,1000)) {
     using(var g=Graphics.FromImage(backdrop))using(var fill=new LinearGradientBrush(new Rectangle(0,0,1600,1000),Color.FromArgb(15,80,96),Color.FromArgb(180,197,175),30)){
      g.FillRectangle(fill,0,0,1600,1000);using(var pen=new Pen(Color.FromArgb(70,230,240,230),3))for(int n=0;n<55;n++)g.DrawArc(pen,-200+n*12,130+n*9,1600,270,0,180);
     }
     using(var blur=GlassBackdropCache.Blur(backdrop)) {
      var state=new State{Skin="glass-light",GlassRelief=true};var scene=new Scene{State=state,GlassBackdrop=blur,Month=new DateTime(2026,11,1),Selected=new DateTime(2026,11,16),Today=new DateTime(2026,11,16)};scene.LoadFortune();
      foreach(string target in new[]{"glass","glass-light"}) {
       state.Skin=target;Skins.Apply(state);string baseline;using(var frame=scene.Render(backdrop.Size))baseline=Digest(frame);
       foreach(var skin in Skins.All) {state.Skin=skin.Id;Skins.Apply(state);using(var frame=scene.Render(backdrop.Size)){}state.Skin=target;Skins.Apply(state);using(var frame=scene.Render(backdrop.Size))check(Digest(frame)==baseline,"identical glass after "+skin.Id+" -> "+target);}
      }
      state.Todos.Add(new Todo{Title="准备课程资料"});state.Todos.Add(new Todo{Title="完成今日阅读"});state.Divination=new DivinationRecord{A=3,B=4,C=5};
      state.Events.Add(new Entry{Title="韩语课",Color="#61D1DE",Start=new DateTime(2026,11,16,9,0,0),End=new DateTime(2026,11,16,10,0,0)});
      state.Events.Add(new Entry{Title="旅行",UseEmoji=true,Emoji="✈️",Highlight=true,HighlightColor="#FFF59D",Start=new DateTime(2026,11,18),End=new DateTime(2026,11,18)});
      foreach(string target in new[]{"glass","glass-light"}) {
       state.Skin=target;Skins.Apply(state);var watch=Stopwatch.StartNew();using(var frame=scene.Render(backdrop.Size))using(var output=(Bitmap)backdrop.Clone())using(var g=Graphics.FromImage(output)){g.DrawImageUnscaled(frame,0,0);output.Save(Path.Combine(directory,target+"-preview.png"));}
       log.Add("INFO rendered preview "+target+" "+watch.ElapsedMilliseconds+" ms");
      }
     }
    }
    foreach(var skin in Skins.All) foreach(var size in new[]{new Size(1366,768),new Size(1920,1080),new Size(1080,1920),new Size(768,1024),new Size(3840,2160)})foreach(string mode in new[]{"month","week"}) {
     var state=new State{Skin=skin.Id,CalendarView=mode};Skins.Apply(state);var scene=new Scene{State=state,ReservedLeft=size.Width>1200?180:0,ScreenDpi=size.Width>=3840?192:96};scene.LoadFortune();
     using(var b=scene.Render(size)) {
      check(scene.ComponentRects.Values.All(r=>r.Left>=0&&r.Top>=0&&r.Right<=size.Width&&r.Bottom<=size.Height),"all widgets fit "+skin.Id+size+mode);
      // In short plan widgets the actions are deliberately below the scroll fold.
      if(!scene.Hits.Any(h=>h.Action=="openplans")&&scene.ScrollLimits.ContainsKey("planviewport")){scene.ScrollOffsets["planviewport"]=scene.ScrollLimits["planviewport"];using(var scrolled=scene.Render(size)){} }
      check(scene.Hits.Any(h=>h.Action=="viewmonth")&&scene.Hits.Any(h=>h.Action=="viewweek")&&scene.Hits.Any(h=>h.Action=="menustars")&&scene.Hits.Any(h=>h.Action=="openplans"),"common functions "+skin.Id+size+mode);
      check(scene.Hits.All(h=>h.Box.Width>0&&h.Box.Height>0&&h.Box.Left>=0&&h.Box.Top>=0&&h.Box.Right<=size.Width+1&&h.Box.Bottom<=size.Height+1),"hits on screen "+skin.Id+size+mode);
      using(var region=DesktopAppearance.CreateRegion(scene,size))check(scene.Hits.Where(h=>h.Action=="viewmonth"||h.Action=="viewweek"||h.Action=="menustars").All(h=>region.IsVisible(Center(h.Box))),"buttons inside native region "+skin.Id+size+mode);
     }
     var prior=scene.ComponentRects["calendar"];state.ShowPlans=false;state.ShowWidget=false;using(var b=scene.Render(size)){}
     check(scene.ComponentRects["calendar"]==prior,"hide preserves calendar "+skin.Id+size+mode);
    }
    foreach(string language in new[]{"zh-CN","zh-TW","en","ko","ja"}) {
     L.Language=language;var state=new State{Skin="glass-light",Language=language};Skins.Apply(state);var scene=new Scene{State=state};scene.LoadFortune();
     using(var frame=scene.Render(new Size(1366,768))) if(language=="ko")frame.Save(Path.Combine(directory,"korean-alpha.png"));
     scene.ScreenDpi=144;using(var frame=scene.Render(new Size(1366,768))) check(scene.Hits.All(h=>h.Box.Bottom<=768),"high DPI localized layout "+language);
     check(L.T("桌面组件")!="" && L.T("玻璃·浅色")!="","localized component controls "+language);
    }
    L.Language="zh-CN";
    var flags=BindingFlags.Instance|BindingFlags.NonPublic;
    var defaults=new Scene{State=new State()};defaults.PrepareComponents(new Size(1920,1080));
    check(defaults.ComponentRects["widget"].Right<defaults.ComponentRects["calendar"].Left && defaults.ComponentRects["calendar"].Right<defaults.ComponentRects["plans"].Left,"original left-center-right default layout");
    var shared=new State();
    using(var controller=new Controller(shared,false,true,true)) {
     var tray=(NotifyIcon)typeof(Controller).GetField("tray",flags).GetValue(controller);tray.Visible=false;
     check(tray.ContextMenuStrip.Items.Count==5,"tray has exactly five entries");
     check(tray.ContextMenuStrip.Items[0].Text==L.T("桌面组件") && tray.ContextMenuStrip.Items[1].Text==L.T("显示屏幕") && tray.ContextMenuStrip.Items[2].Text==L.T("开机自启动") && tray.ContextMenuStrip.Items[3].Text=="语言 / Language" && tray.ContextMenuStrip.Items[4].Text==L.T("退出"),"tray entries match requested functions");
     var scene=(Scene)typeof(Controller).GetField("scene",flags).GetValue(controller);
     var dispatch=typeof(Controller).GetMethod("Dispatch",flags);
     using(var host=new Form())using(var view=new SceneView(scene)){host.Controls.Add(view);view.Size=new Size(1600,1000);IntPtr handle=view.Handle;view.HitAction=h=>dispatch.Invoke(controller,new object[]{h,false});
      foreach(var skin in Skins.All){shared.Skin=skin.Id;Skins.Apply(shared);using(var frame=scene.Render(view.Size)){}
       foreach(string action in new[]{"viewweek","viewmonth"}) {var point=Center(scene.Hits.First(h=>h.Action==action).Box);Mouse(view,0x201,point);Mouse(view,0x202,point);check(shared.CalendarView==(action=="viewweek"?"week":"month"),"native mouse dispatch "+skin.Id+action);using(var b=scene.Render(view.Size)){} }
      }
      scene.EditingLayout=false;using(var frame=scene.Render(view.Size)){}
      var old=scene.ComponentRects["widget"];var move=scene.Hits.Last(h=>h.Action=="layoutmove"&&h.Id=="widget");var from=Center(move.Box);var to=new Point(from.X+35,from.Y+25);
      Mouse(view,0x201,from);Mouse(view,0x200,to);Mouse(view,0x202,to);using(var b=scene.Render(view.Size)){}
      check(scene.ComponentRects["widget"].X>old.X && scene.ComponentRects["widget"].Y>old.Y,"drag changes only selected widget");
      scene.EditingLayout=true;using(var b=scene.Render(view.Size)){}var cal=scene.ComponentRects["calendar"];var resize=scene.Hits.Last(h=>h.Action=="layoutresize"&&h.Id=="calendar:se");from=Center(resize.Box);to=new Point(from.X-40,from.Y-30);Mouse(view,0x201,from);Mouse(view,0x200,to);Mouse(view,0x202,to);using(var b=scene.Render(view.Size)){}
      check(scene.ComponentRects["calendar"].Width<cal.Width,"corner resize persists");
      Data.Write(shared);var loaded=Data.Read();check(loaded.WidgetLayouts.ContainsKey(scene.MonitorKey),"layout survives disk roundtrip");
      scene.EditingLayout=false;using(var b=scene.Render(view.Size)){}
      var arrow=scene.Hits.First(h=>h.Action=="menuplans");var function=scene.Hits.First(h=>h.Action=="footerplans");check(!arrow.Box.IntersectsWith(function.Box),"dropdown and function have separate hits");
     }
     // Isolated native ordinary child: no Explorer attachment or wallpaper mutation.
     foreach(var skin in Skins.All) {shared.Skin=skin.Id;Skins.Apply(shared);using(var host=new Form())using(var surface=new DesktopSurface(scene,host.Handle))using(var frame=scene.Render(new Size(1600,1000))){surface.Size=frame.Size;surface.CreateControl();surface.UpdateRegion();check(surface.Connected,"ordinary native child "+skin.Id);
       surface.HitAction=h=>dispatch.Invoke(controller,new object[]{h,false});
       scene.EditingLayout=true;using(var render=scene.Render(surface.Size)){}surface.UpdateRegion();
       var corner=scene.Hits.Last(h=>h.Action=="layoutresize"&&h.Id=="calendar:se");var screenPoint=surface.PointToScreen(Center(corner.Box));
       var hitResult=SendMessage(surface.Handle,0x84,IntPtr.Zero,new IntPtr((screenPoint.Y<<16)|(screenPoint.X&65535)));
       check(hitResult.ToInt32()==1,"native resize corner hit "+skin.Id);scene.EditingLayout=false;
       foreach(string action in new[]{"viewweek","viewmonth"}) {
        using(var render=scene.Render(surface.Size)){}surface.UpdateRegion();
        var at=Center(scene.Hits.First(h=>h.Action==action).Box);Mouse(surface,0x201,at);Mouse(surface,0x202,at);
        check(shared.CalendarView==(action=="viewweek"?"week":"month"),"desktop subclass mouse dispatch "+skin.Id+action);
       }}}
    }
    var menuState=new State{Skin="glass"};Skins.Apply(menuState);var menuScene=new Scene{State=menuState};
    using(var closed=menuScene.Render(new Size(1600,1000))) {
     var arrow=menuScene.Hits.First(h=>h.Action=="menuplans").Box;arrow.Inflate(3,3);menuScene.OpenComponentMenu="menuplans";
     using(var open=menuScene.Render(closed.Size)) {
      int differences=0,outside=0;for(int y=0;y<open.Height;y++)for(int x=0;x<open.Width;x++)if(open.GetPixel(x,y)!=closed.GetPixel(x,y)){differences++;if(!arrow.Contains(x,y))outside++;}
      check(differences>0 && outside==0,"only dropdown arrow highlights when menu opens");
     }
    }
    check(Scene.HighlightInk(new Entry{HighlightColor="#FFF59D"}).R<80,"yellow highlight uses dark text");
   } catch(Exception ex){failures++;log.Add("ERROR "+ex);}
   finally {File.WriteAllLines(Path.Combine(directory,"test-results.txt"),log);}
   return failures==0?0:1;
  }
 }
}
#endif
