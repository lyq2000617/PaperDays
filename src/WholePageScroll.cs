using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace PaperDays {
 public static class WholePageScroll {
  public static void Install(FlowLayoutPanel page){
   bool pending=false;
   Action queue=()=>{if(pending||page.IsDisposed||!page.IsHandleCreated)return;pending=true;page.BeginInvoke((Action)(()=>{pending=false;if(page.IsDisposed)return;int height=page.Controls.Cast<Control>().Where(c=>c.Visible).Sum(c=>c.Height+c.Margin.Vertical);var size=new Size(0,height+page.Padding.Vertical+24);if(page.AutoScrollMinSize!=size)page.AutoScrollMinSize=size;}));};
   Action<Control> watch=c=>{c.SizeChanged+=delegate{queue();};c.VisibleChanged+=delegate{queue();};};
   foreach(Control c in page.Controls)watch(c);
   page.ControlAdded+=delegate(object sender,ControlEventArgs e){watch(e.Control);queue();};
   page.Layout+=delegate{queue();};page.HandleCreated+=delegate{queue();};page.VisibleChanged+=delegate{queue();};page.SizeChanged+=delegate{queue();};
  }
 }
}
