#if TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
namespace PaperDays {
 public static class Native50Tests {
  [DllImport("user32.dll")]static extern IntPtr WindowFromPoint(Point p);
  [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr h,int command);
  [DllImport("user32.dll")]static extern bool SetCursorPos(int x,int y);
  [DllImport("user32.dll")]static extern void mouse_event(uint flags,uint x,uint y,uint data,UIntPtr extra);
  [DllImport("user32.dll")]static extern int GetWindowLong(IntPtr h,int index);
  static void Pump(){for(int i=0;i<8;i++){Application.DoEvents();Thread.Sleep(15);}}
  static Point Center(RectangleF r){return new Point((int)(r.X+r.Width/2),(int)(r.Y+r.Height/2));}
  public static int Run(string directory,bool routeOnly=false) {
   Directory.CreateDirectory(directory);Data.Root=directory;OnlineData.Enabled=false;
   var log=new List<string>();Point cursor=Cursor.Position;bool held=false;
   try {
    var state=new State{Skin="glass"};Skins.Apply(state);var scene=new Scene{State=state};scene.LoadFortune();
    using(var host=new Form{Text="PaperDays 鼠标交互隔离验证",StartPosition=FormStartPosition.Manual,Bounds=new Rectangle(60,60,1250,760),TopMost=true}) {
     host.Show();ShowWindow(host.Handle,5);Pump();
     using(var surface=new DesktopSurface(scene,host.Handle)) {
      surface.HitAction=h=>{log.Add("INPUT "+h.Action);if(h.Action=="viewmonth")state.CalendarView="month";if(h.Action=="viewweek")state.CalendarView="week";surface.Invalidate();};
      surface.Clicked=(point,dbl)=>{var hit=scene.At(point,surface.ClientSize);if(hit!=null)surface.HitAction(hit);};
      surface.Attach(host.RectangleToScreen(host.ClientRectangle));Pump();
      if((GetWindowLong(surface.Handle,-20)&0x80000)!=0)throw new Exception("Unexpected layered window style");
      log.Add("PASS ordinary region-clipped native child");
      foreach(var skin in Skins.All) {
       state.Skin=skin.Id;Skins.Apply(state);surface.Invalidate();surface.Update();Pump();
       foreach(string action in new[]{"viewweek","viewmonth"}) {
        var point=surface.PointToScreen(Center(scene.Hits.First(h=>h.Action==action).Box));
        if(WindowFromPoint(point)!=surface.Handle)throw new Exception("OS hit target mismatch: "+skin.Id+action);
        if(routeOnly){log.Add("PASS Windows hit target "+skin.Id+" "+action);continue;}
        SetCursorPos(point.X,point.Y);mouse_event(2,0,0,0,UIntPtr.Zero);held=true;Pump();mouse_event(4,0,0,0,UIntPtr.Zero);held=false;Pump();
        if(state.CalendarView!=(action=="viewweek"?"week":"month"))throw new Exception("Actual input did not dispatch: "+skin.Id+action+" counts="+surface.ClickCount+"/"+surface.DoubleClickCount+" cursor="+Cursor.Position+" wanted="+point);
        log.Add("PASS Windows hit target and mouse input "+skin.Id+" "+action);
       }
      }
      scene.EditingLayout=false;surface.Invalidate();surface.Update();Pump();
      var original=scene.ComponentRects["widget"];
      var grip=surface.PointToScreen(Center(scene.Hits.First(h=>h.Action=="layoutmove"&&h.Id=="widget").Box));
      if(WindowFromPoint(grip)!=surface.Handle)throw new Exception("Move grip not reachable");
      if(routeOnly){log.Add("PASS move grip receives Windows hit test");return 0;}
      SetCursorPos(grip.X,grip.Y);mouse_event(2,0,0,0,UIntPtr.Zero);held=true;Pump();SetCursorPos(grip.X+30,grip.Y+20);Pump();mouse_event(4,0,0,0,UIntPtr.Zero);held=false;Pump();
      if(scene.ComponentRects["widget"].X<=original.X)throw new Exception("Actual drag did not move widget");
      log.Add("PASS Windows mouse drag without edit mode");
     }
    }
    log.Add("LIMIT isolated visible host; not an Explorer desktop or physical multi-monitor test");
    return 0;
   } catch(Exception ex){log.Add("FAIL "+ex);return 1;}
   finally {if(held)mouse_event(4,0,0,0,UIntPtr.Zero);if(!routeOnly)SetCursorPos(cursor.X,cursor.Y);File.WriteAllLines(Path.Combine(directory,"native-results.txt"),log);}
  }
 }
}
#endif
