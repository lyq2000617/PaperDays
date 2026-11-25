using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace PaperDays {
 // Viewing a result never enters the new-reading workflow or modifies its saved record.
 public sealed class DivinationResultDialog : CompactDialog {
  public DivinationResultDialog(DivinationRecord record) : base(L.T("详细解读 · 数字小六壬"),540,620) {
   if(record==null)throw new ArgumentNullException("record");
   var page=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,TabStop=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(22)};
   page.SuspendLayout();Body.Controls.Add(page);
   Action<string,string> section=(title,text)=>{
    page.Controls.Add(new Label{Text=title,AutoSize=true,Font=UiFont.New(12,FontStyle.Bold),Margin=new Padding(0,16,0,8)});
    page.Controls.Add(new Label{Text=text,AutoSize=true,Font=UiFont.New(11),Margin=new Padding(0,0,0,8)});
   };
   var read=Divination.Read(record);int last=DivinationDetails.Palaces(record)[2];
   section(L.T(read.Name)+" · "+L.T(read.Grade),record.A+"  /  "+record.B+"  /  "+record.C+"    ·    "+L.T(record.Category));
   section(L.T("总体"),read.Meaning);
   section(L.T(record.Category),read.Advice);
   section(InterpretationNotes.Heading,InterpretationNotes.ForCategory(record.Category));
   foreach(var category in Divination.Categories.Where(c=>c!=record.Category)) {
    section(L.T(category),Divination.Calculate(new DivinationRecord{A=record.A,B=record.B,C=record.C,Category=category}).Advice);
    section(InterpretationNotes.Heading,InterpretationNotes.ForCategory(category));
   }
   section(L.T("本宫详解"),L.T(DivinationDetails.Attributes[last])+"\n\n"+L.T(DivinationDetails.Images[last])+"\n\n"+L.T("适合：")+L.T(DivinationDetails.Suitable[last])+"\n"+L.T("留意：")+L.T(DivinationDetails.Careful[last]));
   section(L.T("起课过程"),DivinationDetails.Process(record));
   section(L.T("起课规则"),L.T(DivinationDetails.Rule));
   section(L.T("说明"),L.T(DivinationDetails.Note));
   HideFooter();WholePageScroll.Install(page);
   Action fit=()=>{int width=Math.Max(130,page.ClientSize.Width-page.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-4);foreach(var label in page.Controls.OfType<Label>())label.MaximumSize=new Size(width,0);};
   page.SizeChanged+=delegate{fit();};Shown+=delegate{fit();ActiveControl=page;page.Focus();page.AutoScrollPosition=Point.Empty;};fit();page.ResumeLayout(true);ApplyTheme();
  }
 }
}
