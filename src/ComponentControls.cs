using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace PaperDays {
 public sealed partial class Controller {
  bool DispatchComponent(Hit hit) {
   switch(hit.Action) {
    case "headermore":
     var more=new ThemedMenu();more.Items.Add(L.T("日历设置"),null,delegate{Settings();});more.Items.Add(L.T("国家日历"),null,delegate{CalendarSummary();});
     more.Closed+=delegate{dispatcher.BeginInvoke((Action)(()=>more.Dispose()));};more.Show(Cursor.Position);return true;
    case "menustars":case "menulot":case "menuplans":
     scene.OpenComponentMenu=hit.Action;RequestRender();
     var menu=new ThemedMenu();string id=hit.Action=="menuplans"?"plans":"widget";
     var visible=new ToolStripMenuItem(L.T("在桌面显示")){Checked=scene.ComponentVisible(id)};
     visible.Click+=delegate {SetComponentVisible(id,!scene.ComponentVisible(id));};menu.Items.Add(visible);
     menu.Closed+=delegate {scene.OpenComponentMenu=null;RequestRender();dispatcher.BeginInvoke((Action)(()=>menu.Dispose()));};ShowFooterMenu(menu,hit);return true;
    case "hidecalendar":SetComponentVisible("calendar",false);return true;
    case "hidewidget":SetComponentVisible("widget",false);return true;
    case "hideplans":SetComponentVisible("plans",false);return true;
    case "openplans":case "footerplans":OpenPlanManager();return true;
    case "planadd":AddTodo();return true;
    case "planedit":TodoFull(hit.Id);return true;
   }
   return false;
  }
  void ShowFooterMenu(ThemedMenu menu,Hit hit) {
   var owner=surfaces.FirstOrDefault(s=>s.RectangleToScreen(s.ClientRectangle).Contains(Cursor.Position));
   Rectangle anchor=owner==null?new Rectangle(Cursor.Position,new Size(1,1)):owner.RectangleToScreen(Rectangle.Ceiling(hit.Box));
   var area=Screen.FromPoint(anchor.Location).WorkingArea;var size=menu.GetPreferredSize(Size.Empty);
   int x=Math.Max(area.Left,Math.Min(area.Right-size.Width,anchor.Right-size.Width));
   int y=anchor.Bottom+5;if(y+size.Height>area.Bottom)y=anchor.Top-size.Height-5;
   menu.Show(new Point(x,Math.Max(area.Top,y)));
  }
  void SetComponentVisible(string id,bool show) {
   foreach(var surface in surfaces)if(!surface.FinishInline())return;
   if(show && !scene.ComponentVisible(id) && id=="widget" && state.Widget=="占卜")scene.ClearFortune();
   if(!Change(delegate {if(id=="calendar")state.ShowCalendar=show;else if(id=="plans")state.ShowPlans=show;else state.ShowWidget=show;}))return;
   if(show){hidden=false;if(surfaces.Count==0)Connect();else RefreshSurface();}
   else {RefreshSurface();if(!state.ShowCalendar&&!state.ShowWidget&&!state.ShowPlans){
     foreach(var surface in surfaces)surface.Dispose();surfaces.Clear();scene.Hits.Clear();
     if(fallback!=null){fallback.Dispose();fallback=null;}
     tray.ShowBalloonTip(3500,L.T("桌面组件"),L.T("所有组件已隐藏，可从托盘右键的桌面组件恢复。"),ToolTipIcon.Info);
   }}
  }
  void AddComponentPreferences(ThemedMenu menu) {menu.Items.Add(L.T("桌面组件"),null,delegate{OpenComponentManager();});}

  void OpenPlanManager() {
   var form=new Form{Text=L.T("每日计划"),Size=new Size(650,540),MinimumSize=new Size(420,320),StartPosition=FormStartPosition.CenterScreen,BackColor=Palette.Paper,ForeColor=Palette.Ink};
   var list=new ListView{Dock=DockStyle.Fill,View=View.Details,CheckBoxes=true,FullRowSelect=true,HideSelection=false,BackColor=Palette.Paper,ForeColor=Palette.Ink,Font=UiFont.New(11)};
   list.Columns.Add(L.T("日期"),105);list.Columns.Add(L.T("每日计划"),450);
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=48,Padding=new Padding(6),BackColor=Palette.Paper};
   Action<string,Action> add=(label,action)=>{var b=new Button{Text=L.T(label),AutoSize=true,Height=32};b.Click+=delegate{action();};buttons.Controls.Add(b);};
   add("＋ 添加待办",()=>AddTodo());add("编辑",()=>{if(list.SelectedItems.Count>0)TodoFull((string)list.SelectedItems[0].Tag);});
   add("删除",()=>{if(list.SelectedItems.Count>0){string id=(string)list.SelectedItems[0].Tag;Change(delegate{state.DeleteTodo(id);});}});
   add("全部记录",()=>TodoRecords());add("关闭",()=>form.Close());form.Controls.Add(list);form.Controls.Add(buttons);
   bool loading=false;string previous=null;
   Action refresh=delegate {
    string fingerprint=Data.Json().Serialize(state.Todos);
    if(previous==fingerprint)return;previous=fingerprint;
    string selection=list.SelectedItems.Count>0?(string)list.SelectedItems[0].Tag:null;
    loading=true;list.BeginUpdate();list.Items.Clear();
    foreach(var todo in state.Todos.OrderByDescending(t=>t.Date)) {var item=new ListViewItem(todo.Date){Tag=todo.Id,Checked=todo.Done};item.SubItems.Add(todo.Title);list.Items.Add(item);if(todo.Id==selection)item.Selected=true;}
    list.EndUpdate();loading=false;
   };
   list.ItemChecked+=delegate(object sender,ItemCheckedEventArgs e){if(!loading){string id=(string)e.Item.Tag;bool done=e.Item.Checked;Change(delegate{var todo=state.Todos.FirstOrDefault(t=>t.Id==id);if(todo!=null&&todo.Done!=done)state.ToggleTodo(id);});}};
   list.DoubleClick+=delegate{if(list.SelectedItems.Count>0)TodoFull((string)list.SelectedItems[0].Tag);};
   var timer=new Timer{Interval=500};timer.Tick+=delegate{refresh();};form.FormClosed+=delegate{timer.Dispose();};refresh();timer.Start();form.Show();
  }
 }
 public partial class SceneView {
  public Action LayoutSaved;
  string moving,edge;
  Point dragOrigin;
  RectangleF original;
  bool LayoutMessage(int msg,Point point) {
   if(msg==0x201 || msg==0x203) {
    var hit=scene.At(point,ClientSize);
    if(hit!=null&&(hit.Action=="layoutmove"||hit.Action=="layoutresize")) {
     var parts=hit.Id.Split(':');moving=parts[0];edge=parts.Length>1?parts[1]:"";dragOrigin=point;original=scene.ComponentRects[moving];
     scene.DragPreview=new System.Collections.Generic.Dictionary<string,RectangleF>(scene.ComponentRects);
     Capture=true;return true;
    }
   }
   if(moving==null)return false;
   if(msg==0x200) {
    float dx=point.X-dragOrigin.X,dy=point.Y-dragOrigin.Y;var r=original;
    if(edge==""){r.X+=dx;r.Y+=dy;}else {
     if(edge.Contains("e"))r.Width+=dx;if(edge.Contains("s"))r.Height+=dy;
     if(edge.Contains("w")){r.X+=dx;r.Width-=dx;}if(edge.Contains("n")){r.Y+=dy;r.Height-=dy;}
     float minW=moving=="calendar"?Math.Min(580,ClientSize.Width):220,minH=moving=="calendar"?Math.Min(430,ClientSize.Height):180;
     if(r.Width<minW){if(edge.Contains("w"))r.X=original.Right-minW;r.Width=minW;}
     if(r.Height<minH){if(edge.Contains("n"))r.Y=original.Bottom-minH;r.Height=minH;}
    }
    r.Width=Math.Min(r.Width,ClientSize.Width);r.Height=Math.Min(r.Height,ClientSize.Height);r.X=Math.Max(0,Math.Min(ClientSize.Width-r.Width,r.X));r.Y=Math.Max(0,Math.Min(ClientSize.Height-r.Height,r.Y));
    scene.DragPreview[moving]=r;Invalidate();Update();return true;
   }
   if(msg==0x202) {scene.PrepareComponents(ClientSize);scene.SaveComponents(ClientSize);moving=null;edge=null;Capture=false;if(LayoutSaved!=null)LayoutSaved();Invalidate();return true;}
   if(msg==0x215 || msg==0x1F) {moving=null;scene.DragPreview=null;Invalidate();return true;}
   return false;
  }
 }
}
