using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace PaperDays {
 public class PearlButton:SoftButton {
  bool hover;
  protected override void OnMouseEnter(EventArgs e){hover=true;base.OnMouseEnter(e);}
  protected override void OnMouseLeave(EventArgs e){hover=false;base.OnMouseLeave(e);}
  protected override void OnPaint(PaintEventArgs e){
   var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent==null?Palette.Paper:Parent.BackColor);
   bool selected=(string)Tag=="primary";
   Color top=Skins.Dark?Color.FromArgb(63,83,97):Color.FromArgb(253,255,255),bottom=Skins.Dark?Color.FromArgb(38,55,69):Color.FromArgb(225,235,242);
   if(selected||hover){top=Skins.Dark?Color.FromArgb(55,123,124):Color.FromArgb(205,245,235);bottom=Skins.Dark?Color.FromArgb(37,89,100):Color.FromArgb(155,225,214);}
   var r=new RectangleF(2,2,Math.Max(1,Width-5),Math.Max(1,Height-6));
   using(var shadow=Chrome.Round(new RectangleF(r.X,r.Y+2,r.Width,r.Height),12))using(var brush=new SolidBrush(Color.FromArgb(18,Color.Black)))g.FillPath(brush,shadow);
   using(var path=Chrome.Round(r,12))using(var brush=new LinearGradientBrush(r,top,bottom,90))using(var pen=new Pen(selected?Color.FromArgb(94,182,172):Skins.Line,Focused?1.5f:1f)){g.FillPath(brush,path);g.DrawPath(pen,path);}
   TextRenderer.DrawText(g,Text,Font,new Rectangle(8,2,Width-16,Height-5),Enabled?Palette.Ink:Palette.Muted,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
  }
 }
}
