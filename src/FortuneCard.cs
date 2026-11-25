using System;
using System.Drawing;
using System.Linq;

namespace PaperDays
{
    public partial class Scene
    {
        /// <summary>桌面卡片上的校验提示；空字符串表示没有错误。</summary>
        public string FortuneError = "";
        /// <summary>校验失败的数字格，只有这些格子变色。</summary>
        public bool[] FortuneBad = new bool[3];

        /// <summary>当前选中的运势类别。</summary>
        public string FortuneCategory
        {
            /// <summary>读取类别，旧数据缺失时回到“日常”。</summary>
            get
            {
                return Array.IndexOf(Divination.Categories, State.FortuneCategory) >= 0 ? State.FortuneCategory : "日常";
            }
        }

        /// <summary>
        /// 校验桌面卡片的三个数字；通过时返回新记录，失败时标出错误的格子。
        /// </summary>
        public DivinationRecord ReadFortune()
        {
            int[] values = new int[3];
            FortuneBad = new bool[3];
            for (int i = 0; i < 3; i++)
                FortuneBad[i] = !Int32.TryParse(FortuneNumbers[i], out values[i]) || values[i] < 1 || values[i] > Divination.InputMaximum;
            if (FortuneBad.Any(b => b))
            {
                FortuneError = L.T("标红的格子请填 1–") + Divination.InputMaximum + L.T(" 的整数");
                return null;
            }

            FortuneError = "";
            return new DivinationRecord { A = values[0], B = values[1], C = values[2], Category = FortuneCategory, Created = DateTime.UtcNow };
        }

        /// <summary>
        /// 清空三个数字和校验提示，同时清除已保存的结果。
        /// </summary>
        public void ClearFortune()
        {
            FortuneNumbers = new[] { "", "", "" };
            FortuneBad = new bool[3];
            FortuneError = "";
            State.Divination=null;
        }

        /// <summary>
        /// 把已保存的结果填回数字格；旧版三位数超出输入范围时只保留结果，不填入格子。
        /// </summary>
        public void LoadFortune()
        {
            var d = State.Divination;
            FortuneError = "";
            FortuneBad = new bool[3];
            if (d != null && new[] { d.A, d.B, d.C }.All(n => n >= 1 && n <= Divination.InputMaximum))
                FortuneNumbers = new[] { d.A.ToString(), d.B.ToString(), d.C.ToString() };
        }

        /// <summary>
        /// 绘制桌面小六壬卡片：输入、类别、查看运势和简短结果都在卡片内完成。
        /// </summary>
        void DrawFortuneCard(Graphics g)
        {
            const float x = 38, w = 207;
            Text(g, L.T("数字小六壬"), x, 396, w - 52, 32, 12, Ink);
            var clear = new RectangleF(x + w - 46, 398, 46, 28);
            if (HoverAction == "fortuneclear")
                Round(g, clear, 8, Skins.Button);
            Centered(g, L.T("清空"), clear, 10, Muted, "Microsoft YaHei UI");
            Hits.Add(new Hit { Action = "fortuneclear", Box = clear });
            Wrap(g,L.T("请在下面三个框内输入数字，每个数字为 1–99 的整数。"),new RectangleF(x,430,w,48),9.5f,Ink);
            for (int i = 0; i < 3; i++)
            {
                var box = new RectangleF(x + i * 72.5f, 482, 62, 46);
                bool bad = FortuneBad[i];
                Round(g, box, 11, bad ? FortuneColors.BadFill : FortuneColors.Slot);
                bool empty = String.IsNullOrEmpty(FortuneNumbers[i]);
                Centered(g, empty ? L.T("数") + (i + 1) : FortuneNumbers[i], box, empty ? 10.5f : 16, bad ? FortuneColors.BadInk : empty ? Muted : Ink, empty ? "Microsoft YaHei UI" : "Segoe UI");
                Hits.Add(new Hit { Action = "fortuneinput", Id = i.ToString(), Box = box });
            }

            string chosen = FortuneCategory;
            float chipGap = 3, chip = (w - chipGap * 4) / 5;
            for (int i = 0; i < 5; i++)
            {
                string name = Divination.Categories[i];
                var box = new RectangleF(x + i * (chip + chipGap), 538, chip, 28);
                bool on = name == chosen;
                if (on || HoverAction == "fortunecat" + i)
                    Round(g, box, 9, Skins.Button);
                Centered(g, L.T(name), box, 10, on ? Ink : Muted, "Microsoft YaHei UI");
                if (on)
                    using (var pen = new Pen(Skins.Accent, 1.6f))
                        g.DrawLine(pen, box.X + 9, box.Bottom - 3, box.Right - 9, box.Bottom - 3);
                Hits.Add(new Hit { Action = "fortunecat" + i, Id = name, Box = box });
            }

            Round(g, new RectangleF(x, 576, w, 38), 10, HoverAction == "drawfortune" ? Skins.Accent : Skins.Button);
            Centered(g, L.T("开始占卜"), new RectangleF(x, 576, w, 38), 11, HoverAction == "drawfortune" ? Palette.Paper : Ink, "Microsoft YaHei UI");
            Hits.Add(new Hit { Action = "drawfortune", Box = new RectangleF(x, 576, w, 38) });
            var r = Divination.Read(State.Divination);
            float y = 624;
            if (!String.IsNullOrEmpty(FortuneError))
            {
                Wrap(g, FortuneError, new RectangleF(x, y, w, 50), 10.5f, FortuneColors.BadInk);
                y += 56;
            }

            if (r == null)
            {
                if (String.IsNullOrEmpty(FortuneError))
                    Wrap(g, L.T("选好类别后，点击开始占卜。"), new RectangleF(x, y, w, 70), 11, Muted);
            }
            else
            {
                string category = Array.IndexOf(Divination.Categories, State.Divination.Category) >= 0 ? State.Divination.Category : chosen;
                Text(g, L.T(r.Name), x, y - 2, w, 30, L.IsChinese ? 23 : 17, Ink);
                Text(g, L.T(r.Grade), x, y + 27, w, 18, 9, Muted);
                ScrollText(g,r.Meaning+"\n\n"+L.T(category)+L.T("运势：")+r.Advice,new RectangleF(x,y+46,w,Math.Max(30,800+TallY-y-46)),"widgetfortune",10.5f);
            }

            Button(g, "reading", L.T("详细解读"), new RectangleF(37, 808 + TallY, 207, 35));
            Text(g, L.T("民俗娱乐参考"), x, 850 + TallY, w, 23, 9.5f, Muted);
        }

        /// <summary>
        /// 在区域内水平和垂直居中绘制一行文字。
        /// </summary>
        void DrawCompactFortune(Graphics g,RectangleF area)
        {
            ContentGlass(g,area);float x=area.X+6,y=area.Y+5,w=area.Width-12;
            bool compact=area.Height<250;
            Text(g,L.T("数字小六壬"),x,y,w,24,12,Ink);y+=26;
            if(!compact){Wrap(g,L.T("请在下面三个框内输入数字，每个数字为 1–99 的整数。"),new RectangleF(x,y,w,42),9.5f,Ink);y+=44;}
            float cell=(w-12)/3;
            for(int i=0;i<3;i++){
                var box=new RectangleF(x+i*(cell+6),y,cell,34);Round(g,box,9,FortuneBad[i]?FortuneColors.BadFill:FortuneColors.Slot);
                Centered(g,String.IsNullOrEmpty(FortuneNumbers[i])?"1–99":FortuneNumbers[i],box,12,Ink,"Segoe UI");Hits.Add(new Hit{Action="fortuneinput",Id=i.ToString(),Box=box});
            }
            y+=40;
            Button(g,"drawfortune",L.T("开始占卜"),new RectangleF(x,y,w*.66f,30));Button(g,"fortuneclear",L.T("清空"),new RectangleF(x+w*.66f+6,y,w*.34f-6,30));y+=36;
            float bottom=area.Bottom-34;
            if(bottom>y+18){var result=Divination.Read(State.Divination);string text=!String.IsNullOrEmpty(FortuneError)?FortuneError:result==null?L.T("选好类别后，点击开始占卜。"):L.T(result.Name)+" · "+result.Meaning+"\n"+result.Advice;ScrollText(g,text,new RectangleF(x,y,w,bottom-y-4),"widgetfortune",10.5f);}
            if(area.Height>=180)Button(g,"reading",L.T("详细解读"),new RectangleF(x,Math.Max(y,area.Bottom-30),w,28));
        }

        static void Centered(Graphics g, string text, RectangleF box, float size, Color color, string family)
        {
            using (var font = UiFont.Scene(family, size))
            using (var brush = new SolidBrush(color))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
                g.DrawString(text, font, brush, box, format);
        }
    }

    /// <summary>
    /// 小六壬输入的错误配色；只用于被判定错误的数字格。
    /// </summary>
    public static class FortuneColors
    {
        /// <summary>数字格平时的底色：比输入框底色略深，不用边框也能看清格子。</summary>
        public static Color Slot
        {
            /// <summary>取输入框和按钮底色的中间值。</summary>
            get
            {
                Color a = Skins.Field, b = Skins.Button;
                return Color.FromArgb((a.R + b.R) / 2, (a.G + b.G) / 2, (a.B + b.B) / 2);
            }
        }

        /// <summary>错误格底色。</summary>
        public static Color BadFill
        {
            /// <summary>按皮肤明暗取底色。</summary>
            get
            {
                return Skins.Dark ? Color.FromArgb(84, 44, 48) : Color.FromArgb(253, 233, 231);
            }
        }

        /// <summary>错误提示文字色。</summary>
        public static Color BadInk
        {
            /// <summary>按皮肤明暗取文字色。</summary>
            get
            {
                return Skins.Dark ? Color.FromArgb(244, 170, 166) : Color.FromArgb(176, 58, 52);
            }
        }
    }
}
