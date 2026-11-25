using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace PaperDays {
 public static class WindowIcons {
  static Icon components;
  [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr icon);
  public static Icon Components {get{
   if(components!=null)return components;
   using(var bitmap=new Bitmap(64,64))using(var g=Graphics.FromImage(bitmap)){
    g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.Transparent);
    using(var path=Chrome.Round(new RectangleF(2,2,60,60),15))using(var b=new SolidBrush(Color.FromArgb(29,93,108)))g.FillPath(b,path);
    for(int i=0;i<4;i++)using(var path=Chrome.Round(new RectangleF(13+(i%2)*22,13+(i/2)*22,16,16),4))using(var b=new SolidBrush(i==0?Color.FromArgb(138,230,207):Color.White))g.FillPath(b,path);
    IntPtr h=bitmap.GetHicon();try{using(var icon=Icon.FromHandle(h))components=(Icon)icon.Clone();}finally{DestroyIcon(h);}
   }return components;
  }}
 }
 // Noninteractive border only. No dimming overlay steals desktop clicks.
 public class GuideHighlight:Form {
  public GuideHighlight(){FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;StartPosition=FormStartPosition.Manual;BackColor=Color.FromArgb(33,174,157);TopMost=true;}
  protected override bool ShowWithoutActivation{get{return true;}}
  protected override CreateParams CreateParams{get{var cp=base.CreateParams;cp.ExStyle|=0x08000020;return cp;}}
  public void Outline(Rectangle bounds){
   bounds.Inflate(4,4);Bounds=bounds;var old=Region;
   using(var outer=Chrome.Round(new RectangleF(0,0,Width,Height),12))using(var inner=Chrome.Round(new RectangleF(3,3,Width-6,Height-6),9)){var region=new Region(outer);region.Exclude(inner);Region=region;}
   if(old!=null)old.Dispose();if(!Visible)Show();
  }
 }
 public class GuideDialog:CompactDialog {
  public int Step=-1;
  readonly Label title,description,progress;
  readonly Button previous,next;
  readonly GuideHighlight highlight=new GuideHighlight();
  readonly Func<int,Rectangle?> target;
  public GuideDialog(Func<int,Rectangle?> target):base(L.T("新手引导"),390,280){
   this.target=target;Icon=WindowIcons.Components;CenterOnShow=false;
   var page=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(18)};Body.Controls.Add(page);
   progress=new Label{AutoSize=true};title=new Label{AutoSize=true,Font=UiFont.New(15,FontStyle.Bold),Margin=new Padding(0,10,0,10)};description=new Label{AutoSize=true,MaximumSize=new Size(340,0)};
   page.Controls.Add(progress);page.Controls.Add(title);page.Controls.Add(description);
   next=AddButton(L.T("开始引导"),Next,true);AddButton(L.T("跳过"),()=>Close());previous=AddButton(L.T("上一步"),Back);
   Resize+=delegate{if(WindowState==FormWindowState.Minimized)highlight.Hide();else if(Visible)Place();};
   FormClosed+=delegate{highlight.Close();highlight.Dispose();};UpdateStep();
  }
  public void Next(){if(Step==4){Close();return;}Step++;UpdateStep();}
  public void Back(){if(Step>0){Step--;UpdateStep();}}
  void UpdateStep(){
   string[] titles={"认识你的桌面","添加你的第一条日程","皮肤与透明度","移动与缩放","关闭组件后，也能找回来"};
   string[] descriptions={"日历、星座占卜和每日计划可以分别显示。引导不会修改你的日程或布局。","双击一个日期，即可添加日程。现在可以先了解用法，不必实际添加。","从工具打开外观与皮肤。在皮肤上方选择显示方式，点击玻璃皮肤后调整透明度。","拖动标题空白处移动；鼠标放到边缘或四角即可缩放。工具中的显示屏幕可以切换显示器。","点击右上角关闭只会隐藏组件。右键点击托盘图标，选择桌面组件，即可重新显示。"};
   progress.Text=Step<0?"PaperDays":L.T("新手引导")+" · "+(Step+1)+"/5";
   title.Text=L.T(Step<0?"欢迎使用 PaperDays":titles[Step]);description.Text=L.T(Step<0?"用五步熟悉你的桌面日历。可以随时跳过，之后从工具的帮助与诊断重新打开。":descriptions[Step]);
   previous.Enabled=Step>0;next.Text=L.T(Step<0?"开始引导":Step==4?"完成":"下一步");ApplyTheme();Place();
  }
  void Place(){
   if(WindowState==FormWindowState.Minimized)return;
   var area=WindowPlacement.WorkArea;var size=WindowPlacement.Fit(Size,area).Size;Rectangle? anchor=Step<0?null:target(Step);
   var location=WindowPlacement.Fit(size,area).Location;
   if(anchor.HasValue){var a=anchor.Value;int x=a.Right+14;if(x+size.Width>area.Right)x=a.Left-size.Width-14;int y=a.Top;
    if(x<area.Left){x=Math.Max(area.Left,Math.Min(area.Right-size.Width,a.Left));y=a.Bottom+14;if(y+size.Height>area.Bottom)y=a.Top-size.Height-14;}
    location=new Point(Math.Max(area.Left,Math.Min(area.Right-size.Width,x)),Math.Max(area.Top,Math.Min(area.Bottom-size.Height,y)));
    highlight.Outline(a);
   }else highlight.Hide();
   Bounds=new Rectangle(location,size);description.MaximumSize=new Size(Math.Max(100,Body.ClientSize.Width-40),0);title.MaximumSize=description.MaximumSize;
  }
 }
 public sealed partial class Controller {
  GuideDialog guide;
  CompactDialog firstLanguage;
  void OpenGuide(){
   if(stopping)return;if(!state.LanguageConfirmed){ChooseFirstLanguage();return;}if(guide!=null&&!guide.IsDisposed){guide.WindowState=FormWindowState.Normal;guide.Activate();return;}
   guide=new GuideDialog(step=>{
    var surface=surfaces.FirstOrDefault(s=>s.Connected);if(surface==null)return null;
    string action=step==1?"day":step==2?"tools":step==3?"layoutmove":step==4?"hidecalendar":"viewmonth";
    var hit=scene.Hits.FirstOrDefault(h=>h.Action==action);
    if(hit==null)return (Rectangle?)null;return surface.RectangleToScreen(Rectangle.Ceiling(hit.Box));
   });
   guide.FormClosed+=delegate{guide=null;Change(delegate{state.GuideVersion=2;});};if(!isolatedWindows)guide.Show();
  }
  void ChooseFirstLanguage(){
   if(firstLanguage!=null&&!firstLanguage.IsDisposed){firstLanguage.Activate();return;}
   var form=new CompactDialog("语言 / Language",340,370);firstLanguage=form;string selected=state.Language;
   var panel=new System.Windows.Forms.FlowLayoutPanel{Dock=System.Windows.Forms.DockStyle.Fill,FlowDirection=System.Windows.Forms.FlowDirection.TopDown,AutoScroll=true,WrapContents=false,Padding=new System.Windows.Forms.Padding(20)};form.Body.Controls.Add(panel);
   foreach(var pair in new[]{new[]{"zh-CN","简体中文"},new[]{"zh-TW","繁體中文"},new[]{"en","English"},new[]{"ko","한국어"},new[]{"ja","日本語"}}){var code=pair[0];var button=new System.Windows.Forms.RadioButton{Text=pair[1],Checked=code==selected,AutoSize=true,Margin=new System.Windows.Forms.Padding(4,8,4,8)};button.CheckedChanged+=delegate{if(button.Checked)selected=code;};panel.Controls.Add(button);}
   form.AddButton("继续 / Continue",delegate{if(!Change(delegate{state.Language=selected;state.LanguageConfirmed=true;}))return;L.Language=selected;form.Close();ReloadCalendars();RefreshSurface();OpenGuide();},true);
   form.FormClosed+=delegate{firstLanguage=null;};form.ApplyTheme();if(!isolatedWindows)form.Show();
  }
  void RecoverOffscreen(){
   Change(delegate{
    WidgetPlacement[] rows;if(!state.WidgetLayouts.TryGetValue(scene.PlacementKey,out rows)||rows==null)return;
    foreach(var r in rows){r.W=Math.Min(1,Math.Max(.05f,r.W));r.H=Math.Min(1,Math.Max(.05f,r.H));r.X=Math.Max(0,Math.Min(1-r.W,r.X));r.Y=Math.Max(0,Math.Min(1-r.H,r.Y));}
   });scene.DragPreview=null;RefreshSurface();
  }
 }
}
