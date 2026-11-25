#if TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace PaperDays {
 public static class Version51Tests {
  static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
  static object Field(object value,string name){return value.GetType().GetField(name,flags).GetValue(value);}
  static object Call(object value,string name,params object[] args){return value.GetType().GetMethod(name,flags).Invoke(value,args);}
  static IEnumerable<Control> All(Control control){foreach(Control child in control.Controls){yield return child;foreach(var item in All(child))yield return item;}}
  static Point Center(RectangleF r){return new Point((int)(r.Left+r.Width/2),(int)(r.Top+r.Height/2));}
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
  static void Mouse(Control view,int message,Point point){SendMessage(view.Handle,message,IntPtr.Zero,new IntPtr((point.Y<<16)|(point.X&65535)));}
  static bool visiblePreviews;
  static void Snapshot(Form form,string path){if(!visiblePreviews)return;form.Show();Application.DoEvents();var handle=form.Handle;form.PerformLayout();Application.DoEvents();using(var b=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(path);}form.Hide();}
  public static int Run(string directory,bool showPreviews=false){
   visiblePreviews=showPreviews;
   Directory.CreateDirectory(directory);Data.Root=directory;OnlineData.Enabled=false;L.Language="zh-CN";
   var lines=new List<string>();int failures=0;Action<bool,string> check=(pass,label)=>{lines.Add((pass?"PASS ":"FAIL ")+label);if(!pass)failures++;};
   try {
    var state=new State{Skin="glass-light",ShowCalendar=false,ShowPlans=false,ShowWidget=false};Skins.Apply(state);
    state.Todos.Add(new Todo{Title="内容保留"});state.Events.Add(new Entry{Title="保留的日程",Start=DateTime.Today,End=DateTime.Today.AddHours(1)});
    Data.Write(state);state=Data.Read();check(!state.ShowCalendar&&!state.ShowPlans&&!state.ShowWidget,"all-hidden persists after restart");
    var scene=new Scene{State=state};using(var b=scene.Render(new Size(1366,768)))check(scene.Hits.Count==0,"all-hidden produces no interactive regions");
    using(var host=new Form())using(var surface=new DesktopSurface(scene,host.Handle)){
     surface.Attach(new Rectangle(0,0,1366,768));check(surface.Connected,"zero-paint hidden surface is not a connection failure");
    }
    using(var controller=new Controller(state,false,true,true)) {
     var tray=(NotifyIcon)Field(controller,"tray");tray.Visible=false;
     typeof(Controller).GetField("wallpaperReady",flags).SetValue(controller,true);
     Call(controller,"Connect");check(((List<DesktopSurface>)Field(controller,"surfaces")).Count==0&&Field(controller,"fallback")==null&&(string)Field(controller,"desktopError")=="","hidden startup skips desktop/fallback and error");
     Call(controller,"OpenComponentManager");var manager=(Form)Field(controller,"componentManager");
     check(manager!=null&&All(manager).OfType<CheckBox>().Count()==3,"independent recovery window with three toggles");
     Snapshot(manager,Path.Combine(directory,"component-manager.png"));
     var live=(Scene)Field(controller,"scene");
     using(var host=new Form())using(var surface=new DesktopSurface(live,host.Handle)){
      surface.Size=new Size(1366,768);var handle=surface.Handle;var surfaces=(List<DesktopSurface>)Field(controller,"surfaces");surfaces.Add(surface);
      foreach(var box in All(manager).OfType<CheckBox>().ToArray())box.Checked=true;
      using(var frame=live.Render(surface.Size)){}check(state.ShowCalendar&&state.ShowWidget&&state.ShowPlans,"recovery toggles restore all components");
      check(state.Todos.Count==1&&state.Events.Count==1,"recovery retains content");
      foreach(string component in new[]{"calendar","widget","plans"}){
       var close=live.Hits.Single(h=>h.Action=="hide"+component);check(live.ComponentRects[component].Contains(close.Box),"close is inside "+component);
       foreach(string edge in new[]{"n","s","e","w","ne","nw","se","sw"}){
        var hit=live.Hits.Single(h=>h.Action=="layoutresize"&&h.Id==component+":"+edge);check(live.At(Center(hit.Box),surface.Size).Action=="layoutresize","edge target without mode "+component+edge);
       }
      }
      live.EditingLayout=false;var old=live.ComponentRects["calendar"];var resize=live.Hits.Single(h=>h.Action=="layoutresize"&&h.Id=="calendar:e");var start=Center(resize.Box);
      Mouse(surface,0x201,start);Mouse(surface,0x200,new Point(start.X+25,start.Y));Mouse(surface,0x202,new Point(start.X+25,start.Y));
      live.PrepareComponents(surface.Size);check(live.ComponentRects["calendar"].Width>old.Width,"direct edge drag resizes without edit mode");
      surfaces.Clear();
     }
     manager.Dispose();
     Call(controller,"OpenGlassSettings");var settings=(Form)Field(controller,"glassSettings");Snapshot(settings,Path.Combine(directory,"glass-settings.png"));
     var sliders=All(settings).OfType<TrackBar>().ToArray();check(sliders.Length==2&&sliders.All(s=>s.Minimum==0&&s.Maximum==100),"two 0-100 sliders");
     sliders[0].Value=91;sliders[1].Value=24;check(GlassOptions.Get(state,"glass-light","calendar").Outer==91&&GlassOptions.Get(state,"glass-light","calendar").Content==24,"sliders update selected region settings");
     var combos=All(settings).OfType<ComboBox>().ToArray();combos[1].SelectedIndex=1;sliders[0].Value=73;check(GlassOptions.Get(state,"glass-light","calendar").Outer==91&&GlassOptions.Get(state,"glass-light","plans").Outer==73,"component settings independent");
     var apply=All(settings).OfType<CheckBox>().Single(c=>c.Text==L.T("应用到全部组件"));apply.Checked=true;sliders[1].Value=31;
     check(new[]{"calendar","plans","widget"}.All(id=>GlassOptions.Get(state,"glass-light",id).Content==31),"apply all updates component contents");
     int savedLight=GlassOptions.Get(state,"glass-light","calendar").Outer;combos[0].SelectedIndex=1;sliders[0].Value=62;check(GlassOptions.Get(state,"glass-light","calendar").Outer==savedLight&&GlassOptions.Get(state,"glass","calendar").Outer==62,"dark and light independent");
     settings.Close();settings.Dispose();Data.Write(state);var loaded=Data.Read();check(GlassOptions.Get(loaded,"glass","calendar").Outer==62,"glass values persist");
    }
    foreach(string skin in new[]{"glass","glass-light"}){
     state.Skin=skin;Skins.Apply(state);GlassWindow.Preferences=state;GlassWindow.Component="calendar";GlassWindow.Backdrop=null;
     using(var bitmap=new Bitmap(100,100))using(var g=Graphics.FromImage(bitmap)){
      var pref=GlassOptions.Get(state,skin,"calendar");pref.Outer=100;pref.Content=0;
      GlassWindow.Card(g,new RectangleF(0,0,100,100),0,false,-1,false);check(bitmap.GetPixel(50,50).A==0,"100 percent outer fully clear "+skin);
      GlassWindow.Card(g,new RectangleF(0,0,100,100));check(bitmap.GetPixel(50,50).A==255,"zero percent content opaque fallback "+skin);
      pref.Content=100;GlassWindow.Card(g,new RectangleF(0,0,100,100));check(bitmap.GetPixel(50,50).A==0,"100 percent content replaces prior backing "+skin);
     }
     using(var notice=new NoticeDialog("所有组件已隐藏，可随时恢复。","桌面组件",MessageBoxButtons.OK)){
      check(notice.Height<220,"short notice compact "+skin);check(notice.BackColor==Palette.Paper,"notice uses skin "+skin);Snapshot(notice,Path.Combine(directory,skin+"-notice.png"));
      var target=WindowPlacement.Target;WindowPlacement.Target=()=>new Rectangle(1920,0,360,240);notice.FitToScreen();check(WindowPlacement.WorkArea.Contains(notice.Bounds),"notice fits offset small screen "+skin);WindowPlacement.Target=target;
     }
    }
    state=new State{Skin="glass-light"};for(int i=0;i<25;i++)state.Todos.Add(new Todo{Title="较长的计划内容用于确认换行和滚动，不会丢失正文。 "+i});
    scene=new Scene{State=state};Skins.Apply(state);using(var frame=scene.Render(new Size(1366,768))){}
    check(scene.ScrollLimits["plansday"]>0,"long plan content scrolls");
    using(var host=new Form())using(var view=new SceneView(scene)){view.Size=new Size(1366,768);host.Controls.Add(view);var handle=view.Handle;var area=scene.Hits.First(h=>h.Action=="contentscroll"&&h.Id=="plansday");
     typeof(SceneView).GetMethod("OnMouseWheel",flags).Invoke(view,new object[]{new MouseEventArgs(MouseButtons.None,0,Center(area.Box).X,Center(area.Box).Y,-120)});
     check(scene.ScrollOffsets["plansday"]>0,"wheel changes body scroll position");
    }
    foreach(var language in new[]{"en","ko","ja","zh-TW"}){L.Language=language;check(L.T("外围透明度")!="外围透明度"&&L.T("帮助与诊断")!="帮助与诊断","new controls localized "+language);}L.Language="zh-CN";
   }catch(Exception ex){failures++;lines.Add("ERROR "+ex);}
   File.WriteAllLines(Path.Combine(directory,"test-results.txt"),lines);return failures==0?0:1;
  }
 }
}
#endif
