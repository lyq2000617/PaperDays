using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
namespace PaperDays {
 public partial class Scene {
  internal readonly Dictionary<string,float> ScrollOffsets=new Dictionary<string,float>(),ScrollLimits=new Dictionary<string,float>();
  float ScrollOffset(string key,float limit) {float offset;ScrollOffsets.TryGetValue(key,out offset);ScrollLimits[key]=Math.Max(0,limit);offset=Math.Max(0,Math.Min(offset,ScrollLimits[key]));ScrollOffsets[key]=offset;return offset;}
  void ScrollBar(Graphics g,string key,RectangleF area,float offset,float total) {
   Hits.Add(new Hit{Action="contentscroll",Id=key,Box=area});
   if(total<=area.Height)return;
   float thumb=Math.Max(18,area.Height*area.Height/total),y=area.Y+(area.Height-thumb)*offset/(total-area.Height);
   Round(g,new RectangleF(area.Right-4,y,3,thumb),1,Color.FromArgb(130,Muted));
   Hits.Add(new Hit{Action="scrolltrack",Id=key,Box=new RectangleF(area.Right-9,area.Top,9,area.Height)});
  }
  void ScrollText(Graphics g,string text,RectangleF area,string key,float fontSize=11) {
   if(area.Height<=0||area.Width<10)return;
   using(var font=UiFont.Scene(fontSize)){
    float total=g.MeasureString(text,font,(int)Math.Max(1,area.Width-10)).Height+6,offset=ScrollOffset(key,total-area.Height);
    var saved=g.Save();g.SetClip(area,CombineMode.Intersect);
    using(var brush=new SolidBrush(Ink))g.DrawString(text,font,brush,new RectangleF(area.X,area.Y-offset,area.Width-10,Math.Max(total,area.Height)));
    g.Restore(saved);ScrollBar(g,key,area,offset,total);
   }
  }
  void DrawScrollingPlans(Graphics g,RectangleF r) {
   Card(g,r);float x=r.X+16,w=r.Width-32,y=r.Y+20;
   Text(g,L.T("每日计划"),x,y,w-25,32,17,Ink);
   for(int i=0;i<3;i++){string[] actions={"plandaily","planweek","planmonth"},labels={"今日","周","月"},modes={"day","week","month"};Button(g,actions[i],L.T(labels[i]),new RectangleF(x+i*w/3,y+38,w/3-4,28),PlanMode==modes[i]);}
   string date=PlanMode=="day"?Today.ToString("MM.dd dddd",L.Culture):PlanReview.Start(PlanMode,PlanAnchor).ToString("yyyy-MM-dd");
   Text(g,date,x+(PlanMode=="day"?0:27),y+75,w-(PlanMode=="day"?0:54),24,10,Muted);
   if(PlanMode!="day"){Button(g,"planprev","‹",new RectangleF(x,y+73,24,26));Button(g,"plannext","›",new RectangleF(r.Right-40,y+73,24,26));}
   var area=new RectangleF(x,y+105,w,Math.Max(25,r.Height-230));ContentGlass(g,new RectangleF(area.X-5,area.Y-6,area.Width+10,area.Height+12));
   var items=PlanItems().Where(t=>!InlineTodo||t.Id!=InlineId).ToList();string key="plans"+PlanMode;var heights=new List<float>();
   using(var font=UiFont.Scene(11))foreach(var item in items)heights.Add(Math.Max(40,g.MeasureString(item.Title,font,(int)Math.Max(30,w-50)).Height+16));
   float editorHeight=InlineTodo?Math.Min(area.Height,Math.Max(45,InlineHeight)):0;
   float total=heights.Sum()+editorHeight,offset=ScrollOffset(key,total-area.Height),top=area.Y-offset;
   int hitStart=Hits.Count;var saved=g.Save();g.SetClip(area,CombineMode.Intersect);
   for(int i=0;i<items.Count;i++){
    var todo=items[i];float h=heights[i];
    if(top+h>=area.Top&&top<area.Bottom){
     using(var pen=new Pen(Muted,1))g.DrawEllipse(pen,x,top+4,14,14);
     if(todo.Done)Text(g,"✓",x,top,18,22,11,Ink);
     using(var font=UiFont.Scene(11,todo.Done?FontStyle.Strikeout:FontStyle.Regular))using(var brush=new SolidBrush(todo.Done?Muted:Ink))g.DrawString(todo.Title,font,brush,new RectangleF(x+24,top,w-50,h));
     Hits.Add(new Hit{Action="todo",Id=todo.Id,Box=new RectangleF(x,top,22,h)});
     Hits.Add(new Hit{Action="planedit",Id=todo.Id,Box=new RectangleF(x+24,top,w-50,h)});
     Button(g,"tododelete","×",new RectangleF(x+w-24,top,24,28));Hits[Hits.Count-1].Id=todo.Id;
     using(var pen=new Pen(Line,1))g.DrawLine(pen,x,top+h-5,x+w-10,top+h-5);
    }top+=h;
   }
   if(InlineTodo){
    using(var pen=new Pen(Muted,1))g.DrawEllipse(pen,x,top+4,14,14);
    var editor=new RectangleF(x+24,top,w-50,editorHeight);Hits.Add(new Hit{Action="todoeditor",Box=editor});
    Button(g,"todocancel","×",new RectangleF(x+w-24,top,24,28));
   }
   g.Restore(saved);
   for(int i=Hits.Count-1;i>=hitStart;i--){Hits[i].Box=RectangleF.Intersect(Hits[i].Box,area);if(Hits[i].Box.IsEmpty)Hits.RemoveAt(i);}
   ScrollBar(g,key,area,offset,total);
   // Scrolling area is a wheel target, not a click target; keep row actions above it.
   if(Hits.Count>hitStart){var wheel=Hits.FirstOrDefault(h=>h.Action=="contentscroll"&&h.Id==key);if(wheel!=null){Hits.Remove(wheel);Hits.Insert(hitStart,wheel);}}
   if(items.Count==0&&!InlineTodo)Text(g,L.T(PlanMode=="day"?"暂无今日待办":"此时段没有完成事项"),x,area.Y+4,w,40,11,Muted);
   ContentGlass(g,new RectangleF(x-5,r.Bottom-94,w+10,34));
   ContentGlass(g,new RectangleF(x-5,r.Bottom-46,w+10,34));
   Button(g,"openplans",L.T("全部记录"),new RectangleF(x,r.Bottom-92,w,30));
   Button(g,PlanMode=="day"?"addtodo":"planexport",L.T(PlanMode=="day"?"＋ 添加待办":"导出"),new RectangleF(x,r.Bottom-44,w,30));
  }
 }
 public partial class SceneView {
  protected override void OnMouseWheel(MouseEventArgs e) {
   base.OnMouseWheel(e);var target=scene.Hits.LastOrDefault(h=>h.Action=="contentscroll"&&h.Box.Contains(e.Location));
   if(target==null)return;float offset,limit;scene.ScrollOffsets.TryGetValue(target.Id,out offset);scene.ScrollLimits.TryGetValue(target.Id,out limit);
   scene.ScrollOffsets[target.Id]=Math.Max(0,Math.Min(limit,offset-e.Delta/120f*55));Invalidate();
  }
 }
}
