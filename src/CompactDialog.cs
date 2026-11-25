using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace PaperDays {
 public class CompactDialog : Form {
  public bool CenterOnShow=true;
  public readonly Panel Body=new Panel{Dock=DockStyle.Fill,AutoScroll=true};
  readonly Panel header=new Panel{Dock=DockStyle.Top,Height=40};
  readonly FlowLayoutPanel footer=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,FlowDirection=FlowDirection.RightToLeft,Padding=new Padding(10,6,10,6),WrapContents=true,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(0,52),AutoScroll=true};
  readonly Label heading=new Label{Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(14,0,0,0)};
  readonly Button minimize=new SoftButton{Text="−",Width=38,Dock=DockStyle.Right,TabStop=false,AccessibleName="最小化"};
  readonly Button close=new SoftButton{Text="×",Width=38,Dock=DockStyle.Right,TabStop=false,AccessibleName="关闭窗口"};
  [DllImport("user32.dll")]static extern bool ReleaseCapture();
  [DllImport("user32.dll")]static extern IntPtr SendMessage(IntPtr h,int msg,IntPtr w,IntPtr l);
  public CompactDialog(string title,int width,int height) {
   Text=title;heading.Text=title;Font=UiFont.New(10);heading.Font=UiFont.New(11,FontStyle.Bold);
   FormBorderStyle=FormBorderStyle.None;AutoScaleMode=AutoScaleMode.Dpi;AutoScaleDimensions=new SizeF(96,96);
   StartPosition=FormStartPosition.Manual;ClientSize=new Size(width,height);Padding=new Padding(1);ShowInTaskbar=true;
   Controls.Add(Body);Controls.Add(footer);Controls.Add(header);header.Controls.Add(heading);header.Controls.Add(minimize);header.Controls.Add(close);
   MinimizeBox=true;Icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath);minimize.Click+=delegate{WindowState=FormWindowState.Minimized;};
   close.Click+=delegate{Close();};heading.MouseDown+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Left){ReleaseCapture();SendMessage(Handle,0xA1,new IntPtr(2),IntPtr.Zero);}};
   ApplyTheme();Shown+=delegate{Activate();};MinimumSize=new Size(250,140);
  }
  protected override void SetVisibleCore(bool value){if(value&&!Visible){PerformLayout();if(CenterOnShow)FitToScreen();}base.SetVisibleCore(value);}
  protected override void OnSizeChanged(EventArgs e){base.OnSizeChanged(e);if(Width<2||Height<2)return;var old=Region;using(var path=Chrome.Round(new RectangleF(0,0,Width,Height),16))Region=new Region(path);if(old!=null)old.Dispose();}
  public void ScrollActionsWith(FlowLayoutPanel page){var actions=new FlowLayoutPanel{AutoSize=true,WrapContents=true,Margin=new Padding(0,10,0,8)};foreach(Control c in footer.Controls.Cast<Control>().ToArray()){footer.Controls.Remove(c);actions.Controls.Add(c);}page.Controls.Add(actions);WholePageScroll.Install(page);page.SizeChanged+=delegate{actions.MaximumSize=new Size(Math.Max(180,page.ClientSize.Width-page.Padding.Horizontal-24),0);};footer.Visible=false;}
  public void HideFooter(){footer.Visible=false;}
  public void FitToScreen(){Bounds=WindowPlacement.Fit(Size,WindowPlacement.WorkArea);}
  public Button AddButton(string label,Action action,bool primary=false) {
   var button=new SoftButton{Text=label,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(88,32),Padding=new Padding(10,3,10,3),Margin=new Padding(6,0,0,0),Tag=primary?"primary":null};
   button.Click+=delegate{action();};footer.Controls.Add(button);ThemeControl(button);return button;
  }
  public void ApplyTheme(){ThemeControl(this);Invalidate(true);}
  void ThemeControl(Control control) {
   if(control.BackColor!=Palette.Paper)control.BackColor=Palette.Paper;if(control.ForeColor!=Palette.Ink)control.ForeColor=Palette.Ink;
   if(control is Button){control.BackColor=(string)control.Tag=="primary"?Skins.Accent:Skins.Button;control.ForeColor=(string)control.Tag=="primary"?Skins.OnAccent:Palette.Ink;}
   foreach(Control child in control.Controls)ThemeControl(child);
  }
  protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);e.Graphics.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var path=Chrome.Round(new RectangleF(.5f,.5f,Width-1,Height-1),16))using(var pen=new Pen(Skins.Line))e.Graphics.DrawPath(pen,path);}
  protected override void WndProc(ref Message m){
   if(m.Msg==0x84){base.WndProc(ref m);if(m.Result.ToInt32()==1){var point=PointToClient(new Point(unchecked((short)(m.LParam.ToInt64()&65535)),unchecked((short)((m.LParam.ToInt64()>>16)&65535))));bool l=point.X<5,r=point.X>=ClientSize.Width-5,t=point.Y<5,b=point.Y>=ClientSize.Height-5;m.Result=new IntPtr(t?(l?13:r?14:12):b?(l?16:r?17:15):l?10:r?11:1);}return;}
   base.WndProc(ref m);
   if(m.Msg==0x2E0 && Visible)FitToScreen();
  }
 }
}

