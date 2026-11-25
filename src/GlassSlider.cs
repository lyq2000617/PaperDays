using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace PaperDays {
 // A neutral recessed track; value is conveyed by position, never by a progress fill.
 public class GlassSlider : Control {
  int value;
  public event EventHandler ValueChanged;
  public int Value {get{return value;}set{int next=Math.Max(0,Math.Min(100,value));if(next==this.value)return;this.value=next;Invalidate();if(ValueChanged!=null)ValueChanged(this,EventArgs.Empty);}}
  public GlassSlider(){Height=66;TabStop=true;AccessibleRole=AccessibleRole.Slider;SetStyle(ControlStyles.Selectable|ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);}
  protected override bool IsInputKey(Keys key){return key==Keys.Left||key==Keys.Right||key==Keys.Home||key==Keys.End||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Left)Value--;if(e.KeyCode==Keys.Right)Value++;if(e.KeyCode==Keys.Home)Value=0;if(e.KeyCode==Keys.End)Value=100;}
  void MoveKnob(int x){Value=(int)Math.Round((x-18)*100.0/Math.Max(1,Width-36));}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);if(e.Button==MouseButtons.Left){Focus();Capture=true;MoveKnob(e.X);}}
  protected override void OnMouseMove(MouseEventArgs e){base.OnMouseMove(e);if(Capture)MoveKnob(e.X);}
  protected override void OnMouseUp(MouseEventArgs e){Capture=false;base.OnMouseUp(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(BackColor);
   var track=new RectangleF(18,16,Math.Max(1,Width-36),12);
   using(var path=Chrome.Round(track,6))using(var brush=new LinearGradientBrush(track,Skins.Dark?Color.FromArgb(29,39,48):Color.FromArgb(174,188,198),Skins.Dark?Color.FromArgb(74,91,104):Color.FromArgb(237,242,246),90))g.FillPath(brush,path);
   float x=18+(Width-36)*Value/100f;
   using(var b=new SolidBrush(Color.FromArgb(25,Color.Black)))g.FillEllipse(b,x-12,12,26,26);
   var knob=new RectangleF(x-12,9,24,24);
   using(var b=new LinearGradientBrush(knob,Color.White,Color.FromArgb(218,229,235),90))g.FillEllipse(b,knob);
   using(var p=new Pen(Focused?Skins.Accent:Color.FromArgb(185,200,208),1))g.DrawEllipse(p,knob);
   using(var b=new SolidBrush(Palette.Muted))for(int i=0;i<=10;i++)g.FillEllipse(b,17+(Width-36)*i/10f,40,3,3);
   using(var f=UiFont.New(8))for(int i=0;i<=4;i++)TextRenderer.DrawText(g,(i*25).ToString(),f,new Rectangle((int)(18+(Width-36)*i/4f)-18,47,36,18),Palette.Ink,TextFormatFlags.HorizontalCenter);
  }
 }
}
