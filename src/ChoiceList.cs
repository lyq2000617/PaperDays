using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
namespace PaperDays {
 public class ChoiceList:ScrollableControl {
  public class Rows {
   readonly ChoiceList owner;internal List<string> Names=new List<string>();internal List<bool> Checked=new List<bool>();
   public Rows(ChoiceList owner){this.owner=owner;}
   public int Count{get{return Names.Count;}}
   public void Add(string name,bool check){Names.Add(name);Checked.Add(check);owner.UpdateRows();}
   public void Clear(){Names.Clear();Checked.Clear();owner.SelectedIndex=-1;owner.UpdateRows();}
  }
  public readonly Rows Items;int selected=-1;
  public event EventHandler SelectionChanged;
  public int SelectedIndex{get{return selected;}set{selected=value;Invalidate();if(SelectionChanged!=null)SelectionChanged(this,EventArgs.Empty);}}
  public IEnumerable<int> CheckedIndices{get{return Enumerable.Range(0,Items.Count).Where(i=>Items.Checked[i]);}}
  public bool GetItemChecked(int i){return Items.Checked[i];}
  public void SetItemChecked(int i,bool value){Items.Checked[i]=value;Invalidate();}
  public ChoiceList(){Items=new Rows(this);AutoScroll=true;TabStop=true;SetStyle(ControlStyles.UserPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.Selectable,true);}
  int RowHeight{get{return Math.Max(34,Font.Height+16);}}
  void UpdateRows(){AutoScrollMinSize=new Size(0,Items.Count*RowHeight+12);Invalidate();}
  protected override void OnFontChanged(EventArgs e){base.OnFontChanged(e);if(Items!=null)UpdateRows();}
  protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);Focus();int row=(e.Y-AutoScrollPosition.Y-6)/RowHeight;if(row<0||row>=Items.Count)return;SelectedIndex=row;if(e.X<36)SetItemChecked(row,!GetItemChecked(row));}
  protected override bool IsInputKey(Keys key){return key==Keys.Up||key==Keys.Down||base.IsInputKey(key);}
  protected override void OnKeyDown(KeyEventArgs e){base.OnKeyDown(e);if(e.KeyCode==Keys.Down)SelectedIndex=Math.Min(Items.Count-1,selected+1);if(e.KeyCode==Keys.Up)SelectedIndex=Math.Max(0,selected-1);if(e.KeyCode==Keys.Space&&selected>=0)SetItemChecked(selected,!GetItemChecked(selected));}
  protected override void OnPaint(PaintEventArgs e){
   base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.Clear(Palette.Paper);
   for(int i=0;i<Items.Count;i++){
    int y=6+i*RowHeight+AutoScrollPosition.Y;if(y+RowHeight<0||y>Height)continue;int width=ClientSize.Width-12;
    if(i==selected)using(var path=Chrome.Round(new RectangleF(4,y,width,RowHeight-2),9))using(var b=new SolidBrush(Skins.Button))g.FillPath(b,path);
    var box=new RectangleF(13,y+(RowHeight-16)/2f,16,16);using(var path=Chrome.Round(box,4))using(var pen=new Pen(Palette.Muted,1)){g.DrawPath(pen,path);}
    if(Items.Checked[i])using(var pen=new Pen(Palette.Ink,1.8f))g.DrawLines(pen,new[]{new PointF(box.X+3,box.Y+8),new PointF(box.X+7,box.Y+12),new PointF(box.X+13,box.Y+4)});
    TextRenderer.DrawText(g,Items.Names[i],Font,new Rectangle(39,y,width-39,RowHeight-2),Palette.Ink,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
   }
  }
 }
}
