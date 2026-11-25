using System;using System.Drawing;using System.Linq;
namespace PaperDays {
 public partial class SceneView {
  Hit scrollingTrack;
  bool ScrollMessage(int message,Point point){
   if(message==0x201){var hit=scene.Hits.LastOrDefault(h=>h.Action=="scrolltrack"&&h.Box.Contains(point));if(hit==null)return false;scrollingTrack=hit;Capture=true;SetScroll(point);return true;}
   if(scrollingTrack==null)return false;
   if(message==0x200){SetScroll(point);return true;}
   if(message==0x202){SetScroll(point);scrollingTrack=null;Capture=false;return true;}
   if(message==0x215){scrollingTrack=null;return false;}
   return false;
  }
  void SetScroll(Point p){float limit;if(!scene.ScrollLimits.TryGetValue(scrollingTrack.Id,out limit))return;scene.ScrollOffsets[scrollingTrack.Id]=Math.Max(0,Math.Min(limit,(p.Y-scrollingTrack.Box.Top)/Math.Max(1,scrollingTrack.Box.Height)*limit));Invalidate();}
 }
 public partial class Scene {
  void DrawPlanViewport(Graphics g,RectangleF r){
   if(r.Height>=350){DrawScrollingPlans(g,r);return;}
   string key="planviewport";float total=350,offset=ScrollOffset(key,total-r.Height);int start=Hits.Count;var saved=g.Save();g.SetClip(r,System.Drawing.Drawing2D.CombineMode.Intersect);g.TranslateTransform(0,-offset);DrawScrollingPlans(g,new RectangleF(r.X,r.Y,r.Width,total));g.Restore(saved);
   for(int i=Hits.Count-1;i>=start;i--){var b=Hits[i].Box;b.Y-=offset;Hits[i].Box=RectangleF.Intersect(b,r);if(Hits[i].Box.Width<=0||Hits[i].Box.Height<=0)Hits.RemoveAt(i);}
   ScrollBar(g,key,r,offset,total);var wheel=Hits.LastOrDefault(h=>h.Action=="contentscroll"&&h.Id==key);if(wheel!=null){Hits.Remove(wheel);Hits.Insert(start,wheel);}
  }
 }
}
