using System;
using System.Drawing;
using System.Windows.Forms;

namespace PaperDays
{
    public class ThemedMenu : ContextMenuStrip
    {
        /// <summary>
        /// 初始化ThemeMenu的ThemedMenu，准备状态、布局与必要的事件绑定。
        /// </summary>
        public ThemedMenu()
        {
            Renderer = new ThemeMenuRenderer();
            Padding = new Padding(7);
            ShowImageMargin = true;
            ShowCheckMargin = false;
            Font = UiFont.New(10);
            BackColor = Palette.Paper;
            ForeColor = Palette.Ink;
            Opening += delegate
            {
                StyleDropDown(this);
                BackColor = Palette.Paper;
                ForeColor = Palette.Ink;
                foreach (ToolStripItem i in Items)
                    i.ForeColor = Palette.Ink;
            };
        }

        /// <summary>
        /// 响应Opened事件，同步ThemeMenu状态并保留基类行为。
        /// </summary>
        static void RoundMenu(object sender,EventArgs e) {
            var menu=(ToolStrip)sender;if(menu.Width<2||menu.Height<2)return;
            var old=menu.Region;using(var path=Chrome.Round(new RectangleF(0,0,menu.Width,menu.Height),12))menu.Region=new Region(path);if(old!=null)old.Dispose();
        }
        static void StyleDropDown(ToolStripDropDown menu) {
            menu.Renderer=new ThemeMenuRenderer();menu.BackColor=Palette.Paper;menu.ForeColor=Palette.Ink;menu.Font=UiFont.New(10);menu.Padding=new Padding(7);
            menu.SizeChanged-=RoundMenu;menu.SizeChanged+=RoundMenu;menu.Opened-=RoundMenu;menu.Opened+=RoundMenu;
            foreach(ToolStripItem item in menu.Items){item.ForeColor=Palette.Ink;if(!(item is ToolStripSeparator))item.Padding=new Padding(6,7,6,7);var parent=item as ToolStripMenuItem;if(parent!=null&&parent.HasDropDownItems)StyleDropDown(parent.DropDown);}
            RoundMenu(menu,EventArgs.Empty);
        }
        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            var old = Region;
            using (var path = Chrome.Round(new RectangleF(0, 0, Width, Height), 12))
                Region = new Region(path);
            if (old != null)
                old.Dispose();
        }
    }

    public class ThemeMenuRenderer : ToolStripProfessionalRenderer
    {
        /// <summary>
        /// 按当前皮肤绘制ToolStripBackground，保留悬停、选中和文字可读性。
        /// </summary>
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(Palette.Paper);
        }

        /// <summary>
        /// 按当前皮肤绘制ImageMargin，保留悬停、选中和文字可读性。
        /// </summary>
        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            using (var b = new SolidBrush(Palette.Paper))
                e.Graphics.FillRectangle(b, e.AffectedBounds);
        }

        /// <summary>
        /// 按当前皮肤绘制MenuItemBackground，保留悬停、选中和文字可读性。
        /// </summary>
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed)
                return;
            using (var p = Chrome.Round(new RectangleF(5, 3, e.Item.Width - 10, e.Item.Height - 6), 9))
            using (var b = new SolidBrush(Skins.Glass ? (Skins.Dark ? Color.FromArgb(45,85,94) : Color.FromArgb(205,237,231)) : Skins.Button))
                e.Graphics.FillPath(b, p);
        }

        /// <summary>
        /// 按当前皮肤绘制ItemText，保留悬停、选中和文字可读性。
        /// </summary>
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextRectangle=new Rectangle(e.TextRectangle.X,0,e.TextRectangle.Width,e.Item.Height);
            e.TextFormat=(e.TextFormat & ~(TextFormatFlags.Top|TextFormatFlags.Bottom))|TextFormatFlags.VerticalCenter;
            e.TextColor = e.Item.Enabled ? Palette.Ink : Palette.Muted;
            base.OnRenderItemText(e);
            DrawIcon(e.Graphics,e.Item,e.TextColor);
        }

        static void DrawIcon(Graphics g,ToolStripItem item,Color color)
        {
            var check=item as ToolStripMenuItem;if(check!=null&&check.Checked)return;
            string text=item.Text;int x=11,y=(item.Height-14)/2;
            using(var p=new Pen(color,1.3f)) {
                g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if(text==L.T("显示屏幕")){g.DrawRectangle(p,x,y,15,10);g.DrawLine(p,x+7,y+10,x+7,y+14);g.DrawLine(p,x+3,y+14,x+11,y+14);}
                else if(text==L.T("桌面组件")||text==L.T("布局")){for(int i=0;i<4;i++)g.DrawRectangle(p,x+(i%2)*8,y+(i/2)*8,5,5);}
                else if(text==L.T("开机自启动")||text==L.T("退出")){g.DrawArc(p,x,y+2,14,12,310,280);g.DrawLine(p,x+7,y-1,x+7,y+7);}
                else if(text=="语言 / Language"){g.DrawEllipse(p,x,y,14,14);g.DrawEllipse(p,x+4,y,6,14);g.DrawLine(p,x,y+7,x+14,y+7);}
                else if(text==L.T("外观与皮肤")){g.DrawLine(p,x+3,y+10,x+12,y+1);g.DrawLine(p,x+6,y+13,x+15,y+4);g.DrawLine(p,x+12,y+1,x+15,y+4);g.DrawArc(p,x,y+8,7,7,0,280);}
                else if(text==L.T("日历与数据")){g.DrawRectangle(p,x,y+2,14,12);g.DrawLine(p,x,y+6,x+14,y+6);g.DrawLine(p,x+4,y,x+4,y+4);g.DrawLine(p,x+10,y,x+10,y+4);}
                else if(text==L.T("帮助与诊断")){g.DrawEllipse(p,x,y,14,14);g.DrawLine(p,x+7,y+6,x+7,y+11);g.DrawEllipse(p,x+6.5f,y+3,1,1);}
            }
        }

        /// <summary>
        /// 按当前皮肤绘制ItemCheck，保留悬停、选中和文字可读性。
        /// </summary>
        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            var r = e.ImageRectangle;
            using (var p = new Pen(Skins.Accent, 2))
            {
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.DrawLines(p, new[] { new PointF(r.Left + 2, r.Top + r.Height * .5f), new PointF(r.Left + r.Width * .42f, r.Bottom - 3), new PointF(r.Right - 1, r.Top + 2) });
            }
        }

        /// <summary>
        /// 按当前皮肤绘制Separator，保留悬停、选中和文字可读性。
        /// </summary>
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            using (var p = new Pen(Skins.Line))
                e.Graphics.DrawLine(p, 8, e.Item.Height / 2, e.Item.Width - 8, e.Item.Height / 2);
        }

        /// <summary>
        /// 按当前皮肤绘制ToolStripBorder，保留悬停、选中和文字可读性。
        /// </summary>
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var path = Chrome.Round(new RectangleF(.5f, .5f, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1), 12))
            using (var p = new Pen(Skins.Line))
                e.Graphics.DrawPath(p, path);
        }

        /// <summary>
        /// 按当前皮肤绘制Arrow，保留悬停、选中和文字可读性。
        /// </summary>
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Palette.Ink;
            base.OnRenderArrow(e);
        }
    }
}
