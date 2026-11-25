using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
namespace PaperDays {
 public class WidgetPlacement { public string Id; public float X,Y,W,H; }
 public partial class Scene {
  public string MonitorKey="default", OpenComponentMenu;
  public bool EditingLayout;
  public string PlacementKey { get { return State.FillScreen ? MonitorKey+":fill" : MonitorKey; } }
  public Dictionary<string,RectangleF> ComponentRects=new Dictionary<string,RectangleF>();
  public Dictionary<string,RectangleF> DragPreview;
  public bool ComponentVisible(string id) { return id=="calendar"?State.ShowCalendar:id=="plans"?State.ShowPlans:State.ShowWidget; }
  public void PrepareComponents(Size size) {
   // Keep the original responsive three-column arrangement; hidden panels retain their space.
   var responsive=new ResponsiveLayout(size,Math.Min(ReservedLeft,Math.Max(0,size.Width-700)),true);
   var defaults=new Dictionary<string,RectangleF> {
    {"calendar",responsive.Pixel(responsive.Calendar)},
    {"widget",responsive.Pixel(responsive.Sidebar)},
    {"plans",responsive.Pixel(responsive.Plans)}
   };
   WidgetPlacement[] saved;
   if(State.WidgetLayouts!=null && State.WidgetLayouts.TryGetValue(PlacementKey,out saved) && saved!=null)
    foreach(var r in saved) if(r!=null && defaults.ContainsKey(r.Id) && r.W>0 && r.H>0 && !float.IsNaN(r.X+r.Y+r.W+r.H) && !float.IsInfinity(r.X+r.Y+r.W+r.H))
      defaults[r.Id]=new RectangleF(r.X*size.Width,r.Y*size.Height,r.W*size.Width,r.H*size.Height);
   if(DragPreview!=null) foreach(var pair in DragPreview)defaults[pair.Key]=pair.Value;
   foreach(var key in defaults.Keys.ToArray()) {
    var r=defaults[key];r.Width=Math.Min(size.Width,Math.Max(180,r.Width));r.Height=Math.Min(size.Height,Math.Max(140,r.Height));
    r.X=Math.Max(0,Math.Min(size.Width-r.Width,r.X));r.Y=Math.Max(0,Math.Min(size.Height-r.Height,r.Y));defaults[key]=r;
   }
   ComponentRects=defaults;
  }
  public void SaveComponents(Size size) {
   State.WidgetLayouts[PlacementKey]=ComponentRects.Select(x=>new WidgetPlacement{Id=x.Key,X=x.Value.X/size.Width,Y=x.Value.Y/size.Height,W=x.Value.Width/size.Width,H=x.Value.Height/size.Height}).ToArray();
   DragPreview=null;
  }
  void PaintComponents(Graphics g,Size size) {
   Hits.Clear();GlassWindow.Backdrop=GlassBackdrop;GlassWindow.Preferences=State;GlassWindow.Component="calendar";g.Clear(Color.Transparent);
   g.SmoothingMode=SmoothingMode.AntiAlias;g.TextRenderingHint=System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
   PrepareComponents(size);
   var calendar=ComponentRects["calendar"];
   float s=Math.Min(1.5f,Math.Min(calendar.Width/970f,calendar.Height/650f));
   Layout=new ResponsiveLayout(size,ReservedLeft,true);
   Layout.Scale=s;Layout.Origin=new PointF(calendar.X-16*s,calendar.Y-16*s);
   Layout.Calendar=new RectangleF(16,16,calendar.Width/s,calendar.Height/s);
   UiFont.SceneScale=s;UiFont.SceneMinimumPixels=10*Math.Max(1,ScreenDpi/96f);
   foreach(var item in ComponentRects.Where(x=>ComponentVisible(x.Key)))
    Hits.Add(new Hit{Action="layoutmove",Id=item.Key,Box=new RectangleF(item.Value.X+10,item.Value.Y+8,item.Value.Width-20,42)});
   if(State.ShowCalendar) DrawPanel(g,Layout.Calendar,new PointF(282,16),delegate {
    var full=new RectangleF(282,16,Layout.Calendar.Width,Layout.Calendar.Height);
    Card(g,full);

    DrawHeader(g);if(IsWeek)DrawWeek(g);else DrawMonth(g);
   });
   UiFont.SceneScale=1;UiFont.SceneMinimumPixels=10;
   GlassWindow.Component="widget";if(State.ShowWidget) DrawRestoredComponent(g,ComponentRects["widget"],true);
   GlassWindow.Component="plans";if(State.ShowPlans) DrawRestoredComponent(g,ComponentRects["plans"],false);
   foreach(var item in ComponentRects.Where(x=>ComponentVisible(x.Key))) {
    var r=item.Value;
    var grip=new RectangleF(r.X+r.Width/2-22,r.Y+8,44,14);
    Round(g,new RectangleF(grip.X+10,grip.Y+4,24,3),1,Color.FromArgb(90,Ink));
    Hits.Add(new Hit{Action="layoutmove",Id=item.Key,Box=grip});
    var close=new RectangleF(r.Right-32,r.Top+10,22,22);
    Text(g,"×",close.X,close.Y,22,22,12,Muted);
    Hits.Add(new Hit{Action="hide"+item.Key,Box=close});
    // Continuous invisible edges, with larger corner targets. No editing mode required.
    foreach(string edge in new[]{"n","s","w","e","nw","ne","sw","se"}) {
     RectangleF hit;
     if(edge=="n"||edge=="s")hit=new RectangleF(r.Left+12,edge=="n"?r.Top:r.Bottom-7,r.Width-24,7);
     else if(edge=="w"||edge=="e")hit=new RectangleF(edge=="w"?r.Left:r.Right-7,r.Top+12,7,r.Height-24);
     else hit=new RectangleF(edge.Contains("w")?r.Left:r.Right-12,edge.Contains("n")?r.Top:r.Bottom-12,12,12);
     Hits.Add(new Hit{Action="layoutresize",Id=item.Key+":"+edge,Box=hit});
    }
   }
  }
  void DrawRestoredComponent(Graphics g,RectangleF r,bool sidebar) {
   if(!sidebar){DrawPixelComponent(g,r,DrawPlanViewport,260);return;}
   DrawPixelComponent(g,r,DrawScrollingWidget,180);
  }
  void DrawPixelComponent(Graphics g,RectangleF r,Action<Graphics,RectangleF> draw,float minHeight=180) {
   var state=g.Save();int start=Hits.Count;
   float scale=Math.Max(1,ScreenDpi/96f);
   g.SetClip(r,CombineMode.Intersect);g.TranslateTransform(r.X,r.Y);g.ScaleTransform(scale,scale);
   UiFont.SceneScale=scale;UiFont.SceneMinimumPixels=10;
   draw(g,new RectangleF(0,0,r.Width/scale,r.Height/scale));g.Restore(state);
   for(int i=start;i<Hits.Count;i++){var b=Hits[i].Box;Hits[i].Box=new RectangleF(r.X+b.X*scale,r.Y+b.Y*scale,b.Width*scale,b.Height*scale);}
   UiFont.SceneScale=1;
  }
  void DrawComponentFooter(Graphics g) {
   float y=FooterY+6,end=1226+WideX;
   string[] labels={L.T("星座"),L.T("占卜"),L.T("每日计划")};float[] widths=new float[3];
   using(var font=UiFont.Scene(10.5f))for(int i=0;i<3;i++)widths[i]=Math.Max(82,g.MeasureString(labels[i],font).Width+62);
   float total=widths.Sum()+30+84,x=end-total;
   if(x>430)Text(g,L.T(IsWeek?"双击时间段添加":"双击日期添加日程"),306,y+5,Math.Max(0,x-320),25,9,Ink);
   FooterEntry(g,"widgetzodiac","menustars","☆",labels[0],x,y,widths[0]);x+=widths[0]+10;
   FooterEntry(g,"widgetfortune","menulot","☯",labels[1],x,y,widths[1]);x+=widths[1]+10;
   FooterEntry(g,"footerplans","menuplans","☑",labels[2],x,y,widths[2]);x+=widths[2]+10;
   Button(g,"new",L.T("＋ 日程"),new RectangleF(x,y,84,32));
  }
  void FooterEntry(Graphics g,string action,string menu,string icon,string text,float x,float y,float width) {
   var main=new RectangleF(x,y,width-24,32);var arrow=new RectangleF(x+width-24,y,24,32);
   Round(g,new RectangleF(x,y,width,32),10,Color.FromArgb(Skins.Glass?45:90,Skins.Button));
   if(HoverAction==action)Round(g,main,9,Skins.Button);
   if(HoverAction==menu||OpenComponentMenu==menu)Round(g,arrow,9,Skins.Button);
   using(var font=UiFont.Scene(10.5f)){
    float tw=g.MeasureString(text,font).Width;float start=main.X+(main.Width-tw-24)/2;
    Centered(g,icon,new RectangleF(start,y,16,32),11,Ink,"Segoe UI Symbol");
    Centered(g,text,new RectangleF(start+24,y,tw,32),10.5f,Ink,"Microsoft YaHei UI");
   }
   using(var pen=new Pen(Ink,1.4f)){pen.StartCap=pen.EndCap=LineCap.Round;g.DrawLines(pen,new[]{new PointF(arrow.X+8,y+14),new PointF(arrow.X+12,y+18),new PointF(arrow.X+16,y+14)});}
   Hits.Add(new Hit{Action=action,Box=main});Hits.Add(new Hit{Action=menu,Box=arrow});
  }
  void DrawFlexibleWidget(Graphics g,RectangleF r) {
   Card(g,r);float x=r.X+16,y=r.Y+16,w=r.Width-32;
   float tab=Math.Min(110,(w-32)/2);
   Button(g,"leftzodiac",L.T("星座"),new RectangleF(x,y,tab,30),State.Widget!="占卜");
   Button(g,"leftfortune",L.T("占卜"),new RectangleF(x+tab+6,y,tab,30),State.Widget=="占卜");

   bool lot=State.Widget=="占卜";
   if(lot){DrawCompactFortune(g,new RectangleF(x,y+38,w,r.Bottom-y-46));return;}
   string title,body;
   if(lot){title=L.T("数字占卜");body=L.T("详细解读");if(State.Divination!=null){var result=Divination.Calculate(State.Divination);body=L.T(result.Name)+" · "+result.Meaning+"\n"+result.Advice;}}
   else {var reading=Reading.Daily(State.Zodiac,Today);title=L.T(State.Zodiac);body=reading.Outlook;}
   ContentGlass(g,new RectangleF(x-5,y+36,w+10,Math.Max(30,r.Height-96)));
   Text(g,title,x,y+40,w,30,16,Ink);
   ScrollText(g,body,new RectangleF(x,y+77,w,Math.Max(12,r.Height-144)),lot?"widgetfortune":"widgetzodiac");
   Button(g,lot?"widgetfortune":"widgetzodiac",L.T("详细解读"),new RectangleF(x,r.Bottom-44,Math.Min(w,160),28));
  }
  void DrawFlexiblePlans(Graphics g,RectangleF r) {
   Card(g,r);float x=r.X+16,y=r.Y+12,w=r.Width-32;
   Text(g,L.T("每日计划"),x,y,w-24,30,17,Ink);
   foreach(var pair in new[]{new[]{"plandaily","今日"},new[]{"planweek","周"},new[]{"planmonth","月"}}) {
    int n=pair[0]=="plandaily"?0:pair[0]=="planweek"?1:2;
    Button(g,pair[0],L.T(pair[1]),new RectangleF(x+n*w/3,y+38,w/3-4,28),(n==0&&PlanMode=="day")||(n==1&&PlanMode=="week")||(n==2&&PlanMode=="month"));
   }
   if(PlanMode!="day") {
    Button(g,"planprev","‹",new RectangleF(x,y+68,28,26));Button(g,"plannext","›",new RectangleF(r.Right-44,y+68,28,26));
    Text(g,PlanReview.Start(PlanMode,PlanAnchor).ToString("yyyy-MM-dd"),x+34,y+70,w-68,24,10,Muted);
   }
   ContentGlass(g,new RectangleF(x-5,y+76,w+10,Math.Max(28,r.Height-135)));
   var items=PlanItems();int count=Math.Max(1,(int)((r.Height-(PlanMode=="day"?154:184))/48)),pages=Math.Max(1,(items.Count+count-1)/count),page=Math.Min(Math.Max(State.TodoPage,0),pages-1);
   for(int i=0;i<count&&page*count+i<items.Count;i++) {
    var todo=items[page*count+i];float yy=y+(PlanMode=="day"?80:110)+i*48;
    using(var pen=new Pen(Muted,1))g.DrawEllipse(pen,x,yy+3,15,15);
    if(todo.Done)Text(g,"✓",x,yy-2,22,25,11,Ink);
    Text(g,todo.Title,x+25,yy,w-26,40,11,todo.Done?Muted:Ink,"Microsoft YaHei UI",todo.Done?FontStyle.Strikeout:FontStyle.Regular);
    Hits.Add(new Hit{Action="todo",Id=todo.Id,Box=new RectangleF(x,yy,22,35)});
    Hits.Add(new Hit{Action="planedit",Id=todo.Id,Box=new RectangleF(x+25,yy,w-25,40)});
   }
   if(items.Count==0)Text(g,L.T("暂无今日待办"),x,y+82,w,30,11,Muted);
   float bottom=r.Bottom-45;
   Button(g,PlanMode=="day"?"planadd":"planexport",PlanMode=="day"?"＋":"↗",new RectangleF(x,bottom,35,28));
   Button(g,"openplans",L.T("全部记录"),new RectangleF(x+42,bottom,Math.Max(55,w-130),28));
   if(pages>1) {Button(g,"todoprev","‹",new RectangleF(r.Right-80,bottom,28,28));Button(g,"todonext","›",new RectangleF(r.Right-46,bottom,28,28));}
  }
  internal static Color HighlightInk(Entry e) {
   var c=ColorTranslator.FromHtml(Skins.HighlightOf(e));return c.R*.2126+c.G*.7152+c.B*.0722>145?Color.FromArgb(28,32,38):Color.White;
  }
  void DrawAlignedEvent(Graphics g,Entry e,RectangleF box) {
   string text=(e.Important?"★ ":"")+(e.ShowTime&&!e.AllDay?e.Start.ToString("HH:mm")+" ":"")+e.Title+(String.IsNullOrWhiteSpace(e.ClassName)?"":" · "+e.ClassName);
   float icon=e.UseEmoji?18:8,x=box.X+2,cy=box.Y+box.Height/2;
   using(var f=UiFont.Scene(10)) {
    if(e.Highlight){float width=Math.Min(box.Width,g.MeasureString(text,f).Width+(e.UseEmoji?25:15));BrushHighlight.Draw(g,new RectangleF(box.X,cy-9,width,18),Skins.HighlightOf(e),true);}
    if(e.UseEmoji)EmojiArt.Draw(g,e.Emoji,new RectangleF(x,cy-icon/2,icon,icon),e.EmojiOriginal,e.Highlight&&!e.EmojiTint?ColorTranslator.ToHtml(HighlightInk(e)):Skins.EmojiInk(e));
    else Dot(g,x,cy-4,8,ColorTranslator.FromHtml(e.Color));
    x+=icon+5;
    using(var brush=new SolidBrush(e.Highlight?HighlightInk(e):Ink))using(var format=new StringFormat{LineAlignment=StringAlignment.Center,Trimming=StringTrimming.EllipsisCharacter,FormatFlags=StringFormatFlags.NoWrap})
      g.DrawString(text,f,brush,new RectangleF(x,box.Y,Math.Max(1,box.Right-x),box.Height),format);
   }
  }
 }
}
