#if TESTS
using System;using System.Drawing;using System.IO;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;
namespace PaperDays {public static class Version56Tests {
 static IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
 public static int Run(string dir){Directory.CreateDirectory(dir);Data.Root=dir;OnlineData.Enabled=false;L.Language="zh-CN";int failures=0;var log=new List<string>();Action<bool,string> check=(ok,n)=>{log.Add((ok?"PASS ":"FAIL ")+n);if(!ok)failures++;};try{
 foreach(var skin in new[]{"glass","glass-light","rose"})foreach(int width in new[]{220,280,360}){
  var s=new State{Skin=skin,Widget="占卜",ShowCalendar=false,ShowPlans=false};Skins.Apply(s);var scene=new Scene{State=s};s.WidgetLayouts[scene.PlacementKey]=new[]{new WidgetPlacement{Id="widget",X=.02f,Y=.02f,W=width/900f,H=.94f}};
  using(var bg=new Bitmap(900,800)){using(var g=Graphics.FromImage(bg))g.Clear(Color.SlateGray);scene.GlassBackdrop=bg;
   using(var b=scene.Render(bg.Size)){if(width==280)b.Save(Path.Combine(dir,skin+"-widget.png"));}
   var frame=scene.ComponentRects["widget"];var area=scene.Hits.Single(h=>h.Action=="contentscroll"&&h.Id=="sidebarfortune").Box;var source=scene.Hits.Single(h=>h.Action=="sources").Box;
   check(Math.Abs(area.Top-(frame.Top+frame.Height/3+64))<1,"content starts in lower two-thirds "+skin+width);
   check(!area.IntersectsWith(source)&&area.Right<frame.Right&&area.Bottom<frame.Bottom,"wheel area confined inside glass "+skin+width);
   var start=scene.Hits.Single(h=>h.Action=="drawfortune").Box;var clear=scene.Hits.Single(h=>h.Action=="fortuneclear").Box;
   check(start.Top==clear.Top&&start.Height==clear.Height&&clear.Left-start.Right>=7,"start and clear same row "+skin+width);
   var tab=scene.Hits.Single(h=>h.Action=="leftfortune").Box;
   scene.ScrollOffsets["sidebarfortune"]=scene.ScrollLimits["sidebarfortune"];using(var b=scene.Render(bg.Size)){}
   check(scene.Hits.Single(h=>h.Action=="leftfortune").Box==tab&&!area.IntersectsWith(tab),"tabs fixed outside scroll "+skin+width);
   check(scene.Hits.Single(h=>h.Action=="sources").Box==source,"header stays fixed while content scrolls "+skin+width);
  }
 }
 string text=String.Join(" ",Enumerable.Repeat("这是一段用于验证长正文换行和滚动的测试内容。",12));
 foreach(var skin in new[]{"stone","glass-light"}){
  Skins.Apply(new State{Skin=skin});var r=new Reading{Title="双子座",IsOnline=true,SourceDate="2026-11-23",SourceName="布局测试资料",SourceUrl="",Outlook=text,Interpretation=text,CareerText=text,LoveText=text,WealthText=text,HealthText=text,TravelText=text,Caution=text};
  using(var f=new ReadingDialog(r,"双子座 · 今日运势")){f.Show();Application.DoEvents();float font=f.Font.Size;var page=All(f).OfType<FlowLayoutPanel>().Single(p=>p.Dock==DockStyle.Fill);check(page.AutoScrollPosition.Y==0,"horoscope starts at top "+skin);
   foreach(int width in new[]{360,580}){f.Size=new Size(width,520);Application.DoEvents();var scrolling=All(f).OfType<ScrollableControl>().Where(c=>c.VerticalScroll.Visible).ToArray();check(scrolling.Length==1&&scrolling[0]==page,"one vertical scrollbar "+skin+width);check(!All(f).OfType<ScrollableControl>().Any(c=>c.HorizontalScroll.Visible),"no unwanted horizontal scrollbar "+skin+width);check(Math.Abs(f.Font.Size-font)<.01,"stable font "+skin+width);}
   page.AutoScrollPosition=Point.Empty;Application.DoEvents();using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(dir,skin+"-reading.png"));}
   page.AutoScrollPosition=new Point(0,100000);Application.DoEvents();var close=All(f).OfType<Button>().Single(b=>b.Text==L.T("关闭"));log.Add("SCROLL "+page.AutoScrollPosition+" min="+page.AutoScrollMinSize+" close="+page.PointToClient(close.PointToScreen(Point.Empty))+" size="+close.Size+" page="+page.ClientSize);using(var b=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(Path.Combine(dir,skin+"-bottom.png"));}check(!page.Contains(close)&&f.RectangleToScreen(f.ClientRectangle).Contains(close.RectangleToScreen(close.ClientRectangle)),"bottom controls reachable "+skin);f.Close();
  }
 }
 }catch(Exception ex){log.Add("ERROR "+ex);failures++;}File.WriteAllLines(Path.Combine(dir,"test-results.txt"),log);return failures==0?0:1;}
}}
#endif
