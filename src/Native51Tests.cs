#if TESTS
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace PaperDays {
 public static class Native51Tests {
  [DllImport("user32.dll")]static extern IntPtr ChildWindowFromPointEx(IntPtr parent,Point point,uint flags);
  [DllImport("user32.dll")]static extern int MapWindowPoints(IntPtr from,IntPtr to,ref Point point,uint count);
  static void Pump(){for(int i=0;i<5;i++){Application.DoEvents();System.Threading.Thread.Sleep(25);}}
  public static int Run(string directory){
   Directory.CreateDirectory(directory);Data.Root=directory;OnlineData.Enabled=false;var lines=new List<string>();
   try{
    var parent=DesktopSurface.FindDesktop();if(parent==IntPtr.Zero)throw new Exception("Explorer desktop unavailable");
    var area=Screen.PrimaryScreen.WorkingArea;var state=new State{Skin="glass-light",ShowCalendar=false,ShowPlans=false,ShowWidget=false};Skins.Apply(state);var scene=new Scene{State=state};
    using(var empty=new DesktopSurface(scene,parent)){empty.Attach(area);Pump();if(!empty.Connected)throw new Exception("Empty desktop detached");lines.Add("PASS actual Explorer all-hidden attachment does not report drawing failure");}
    state.ShowCalendar=true;
    state.WidgetLayouts["default"]=new[]{new WidgetPlacement{Id="calendar",X=.25f,Y=.15f,W=Math.Min(.7f,700f/area.Width),H=Math.Min(.8f,500f/area.Height)}};
    using(var surface=new DesktopSurface(scene,parent)){
     surface.Attach(area);Pump();
     foreach(var skin in Skins.All){state.Skin=skin.Id;Skins.Apply(state);surface.SynchronizeLayout();surface.Invalidate();surface.Update();Pump();
      foreach(string action in new[]{"viewmonth","viewweek","hidecalendar","layoutresize"}){
       var hit=scene.Hits.First(h=>h.Action==action);var point=new Point((int)(hit.Box.X+hit.Box.Width/2),(int)(hit.Box.Y+hit.Box.Height/2));MapWindowPoints(surface.Handle,parent,ref point,1);
       if(ChildWindowFromPointEx(parent,point,3)!=surface.Handle)throw new Exception("Explorer child route mismatch: "+skin.Id+" "+action);
       lines.Add("PASS actual Explorer child hit route "+skin.Id+" "+action);
      }
     }
    }
    lines.Add("LIMIT checks Explorer child routing without moving/clicking mouse; physical display hot-plug not exercised");
    return 0;
   }catch(Exception ex){lines.Add("FAIL "+ex);return 1;}
   finally{File.WriteAllLines(Path.Combine(directory,"native-results.txt"),lines);}
  }
 }
}
#endif
