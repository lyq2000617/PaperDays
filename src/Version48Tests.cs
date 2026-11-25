#if TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace PaperDays {
 public static class Version48Tests {
  public static int Run(string directory) {
   Directory.CreateDirectory(directory);Data.Root=directory;OnlineData.Enabled=false;
   var log=new List<string>();int failures=0;
   Action<bool,string> check=(ok,text)=>{log.Add((ok?"PASS ":"FAIL ")+text);if(!ok)failures++;};
   try {
    var sample=new Entry {Title="韩语 / Korean",Color="#61D1DE",ColorSlot=0,Highlight=true,HighlightColor="#B66D75",HighlightSlot=-1};
    foreach(string skin in new[]{"glass","glass-light"}) foreach(bool relief in new[]{false,true}) {
     var state=new State {Skin=skin,GlassRelief=relief,GlassBaseSkin="cream",IconColumns=0,Language="zh-CN"};
     state.Events.Add(sample);Skins.Apply(state);
     check(sample.Color=="#61D1DE" && Skins.HighlightOf(sample)=="#B66D75",skin+" preserves stored colors "+relief);
     Data.Write(state);check(Data.Read().GlassRelief==relief && Data.Read().Skin==skin,"preferences round trip "+skin+relief);
     foreach(var size in new[]{new Size(1366,768),new Size(1920,1080),new Size(2560,1440),new Size(3840,2160),new Size(1080,1920),new Size(768,1024),new Size(3440,1440)}) foreach(string view in new[]{"month","week"}) {
      state.CalendarView=view;
      var scene=new Scene {State=state,Month=new DateTime(2026,11,1),Selected=new DateTime(2026,11,16),Today=new DateTime(2026,11,16),ScreenDpi=size.Width>=2560?144:96};scene.LoadFortune();
      using(var bitmap=scene.Render(size)) {
       check(bitmap.GetPixel(0,0).A==0,"outer transparency "+size+skin+view+relief);
       var layout=scene.Layout;
       check(new[]{layout.Calendar,layout.Plans,layout.Sidebar}.All(r=>{var b=layout.Pixel(r);return b.Left>=0&&b.Top>=0&&b.Right<=size.Width&&b.Bottom<=size.Height;}),"panels fit "+size+skin+view);
       check(!layout.Calendar.IntersectsWith(layout.Plans) && !layout.Sidebar.IntersectsWith(layout.Calendar) && !layout.Sidebar.IntersectsWith(layout.Plans),"panels separate "+size+skin+view);
       check(scene.Hits.Any(h=>h.Action=="tools") && scene.Hits.Any(h=>h.Action=="leftfortune"),"essential controls available "+size+skin+view);
       check(scene.Hits.All(h=>h.Box.Left>=0 && h.Box.Top>=0 && h.Box.Right<=size.Width+1 && h.Box.Bottom<=size.Height+1),"hit regions fit "+size+skin+view);
       if(size.Width==768 && relief && view=="month") bitmap.Save(Path.Combine(directory,skin+"-portrait.png"));
       check(scene.Hits.Where(h=>h.Action=="date").Count()==(view=="month"?42:7),"all dates accessible "+size+skin+view);
       if(size.Width==1920 && view=="month" && relief) bitmap.Save(Path.Combine(directory,skin+"-alpha.png"));
      }
     }
    }
    foreach(string language in new[]{"zh-CN","zh-TW","en","ko","ja"}) {
     L.Language=language;check(L.T("日期起伏")!="" && (language=="zh-CN" || L.T("透明 · 浅色壁纸")!="透明 · 浅色壁纸"),"new UI localized "+language);
    }
    foreach(string language in new[]{"en","ko","ja"}) foreach(int dpi in new[]{96,144,192}) {
     L.Language=language;var state=new State{Skin="glass-light",Widget="占卜",Divination=new DivinationRecord{A=3,B=4,C=5},Language=language};Skins.Apply(state);
     var scene=new Scene{State=state,ScreenDpi=dpi,ReservedLeft=240};scene.LoadFortune();
     using(var bitmap=scene.Render(new Size(1366,768))) {
       check(scene.Hits.All(h=>h.Box.Left>=240 && h.Box.Right<=1367 && h.Box.Bottom<=769),"reserved desktop icons and DPI "+language+dpi);
       if(language=="ko" && dpi==144)bitmap.Save(Path.Combine(directory,"korean-reserved.png"));
     }
    }
    File.WriteAllText(Path.Combine(directory,"calendar.json"),"{\"Skin\":\"glass\"}");
    check(Data.Read().GlassBaseSkin=="stone","legacy glass highlight palette migrates");
    L.Language="zh-CN";
    foreach(string skin in new[]{"glass","glass-light"}) {
      var state=new State{Skin=skin};Skins.Apply(state);var scene=new Scene{State=state};
      using(var host=new Form()) using(var surface=new DesktopSurface(scene,host.Handle)) using(var bitmap=scene.Render(new Size(1366,768))) {
        surface.Size=bitmap.Size;GlassWindow.Present(surface.Handle,bitmap);check(surface.Connected,"native layered window "+skin);
      }
    }
    var original=new State{Skin="mint"};Skins.Apply(original);var slotted=new Entry{ColorSlot=2,HighlightSlot=2};original.Events.Add(slotted);Skins.Apply(original);
    var originalColor=slotted.Color;var originalHighlight=Skins.HighlightOf(slotted);original.GlassBaseSkin=original.Skin;
    foreach(string skin in new[]{"glass","glass-light"}) {original.Skin=skin;Skins.Apply(original);check(slotted.Color==originalColor && Skins.HighlightOf(slotted)==originalHighlight,"slotted original palette retained "+skin);}
    using(var backdrop=new Bitmap(1920,1080)) {
     using(var g=Graphics.FromImage(backdrop)) using(var fill=new LinearGradientBrush(new Rectangle(0,0,1920,1080),Color.FromArgb(27,69,85),Color.FromArgb(170,195,188),35)) {
      g.FillRectangle(fill,0,0,1920,1080);using(var brush=new SolidBrush(Color.FromArgb(45,255,255,255))) for(int i=0;i<20;i++)g.FillEllipse(brush,i*130-300,300+i*12,700,60);
     }
     using(var blurred=GlassBackdropCache.Blur(backdrop)) foreach(string skin in new[]{"glass","glass-light"}) {
      var state=new State{Skin=skin,GlassRelief=true,IconColumns=0};state.Todos.Add(new Todo{Title="准备课程资料"});state.Events.Add(new Entry{Title="韩语课",Color="#61D1DE",Start=new DateTime(2026,11,16,9,0,0),End=new DateTime(2026,11,16,10,0,0)});Skins.Apply(state);
      var scene=new Scene{State=state,GlassBackdrop=blurred,Month=new DateTime(2026,11,1),Today=new DateTime(2026,11,16),Selected=new DateTime(2026,11,16)};scene.LoadFortune();
      using(var bitmap=scene.Render(backdrop.Size))using(var output=(Bitmap)backdrop.Clone())using(var g=Graphics.FromImage(output)){g.DrawImageUnscaled(bitmap,0,0);output.Save(Path.Combine(directory,skin+"-preview.png"));}
     }
    }
   } catch(Exception ex){failures++;log.Add("ERROR "+ex);}
   finally {File.WriteAllLines(Path.Combine(directory,"test-results.txt"),log);}
   return failures==0?0:1;
  }
 }
}
#endif
