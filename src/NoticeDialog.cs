using System;
using System.Drawing;
using System.Windows.Forms;
namespace PaperDays {
 public class NoticeDialog : CompactDialog {
  public Button ConfirmButton,DismissButton;
  static int MessageHeight(string message) {
   using(var font=UiFont.New(10))return Math.Max(30,TextRenderer.MeasureText(message,font,new Size(374,10000),TextFormatFlags.WordBreak|TextFormatFlags.TextBoxControl).Height+8);
  }
  public NoticeDialog(string message,string title,MessageBoxButtons buttons) : base(L.T(title),420,Math.Min(360,MessageHeight(message))+120) {
   bool three=buttons==MessageBoxButtons.YesNoCancel,ask=buttons==MessageBoxButtons.YesNo||three;
   string details=null;
   if(!ask && (title=="桌面连接结果" || message.Contains("StackTrace") || message.Contains("System.Exception"))) {details=message;message=L.T("暂时无法连接桌面。可查看详情，或稍后重新选择显示屏幕。");}
   int measured=MessageHeight(message);ClientSize=new Size(420,Math.Min(360,measured)+120);
   Body.Padding=new Padding(18,8,18,8);
   var label=new Label{Text=message,UseMnemonic=false,AutoSize=true,MaximumSize=new Size(374,0),Location=new Point(18,8),Font=Font,ForeColor=Palette.Ink};Body.Controls.Add(label);
   Body.SizeChanged+=delegate{label.MaximumSize=new Size(Math.Max(80,Body.ClientSize.Width-40),0);};
   ConfirmButton=AddButton(L.T(three?"应用":ask?"确认":"知道了"),delegate{DialogResult=ask?DialogResult.Yes:DialogResult.OK;Close();},true);AcceptButton=ConfirmButton;
   if(ask){DismissButton=AddButton(L.T(three?"放弃":"取消"),delegate{DialogResult=DialogResult.No;Close();});CancelButton=DismissButton;
    if(three)CancelButton=AddButton(L.T("继续编辑"),delegate{DialogResult=DialogResult.Cancel;Close();});
   }else CancelButton=ConfirmButton;
   if(details!=null){string full=details;AddButton(L.T("查看详情"),delegate{label.Text=full;ClientSize=new Size(520,Math.Min(480,MessageHeight(full)+120));FitToScreen();});}
   ApplyTheme();
  }
 }
}
