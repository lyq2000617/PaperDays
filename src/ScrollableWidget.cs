using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
namespace PaperDays {
 public partial class Scene {
  void DrawScrollingWidget(Graphics g,RectangleF r){
   Card(g,r);
   float panelTop=r.Y+r.Height/3f;
   var panel=new RectangleF(r.X+16,panelTop,Math.Max(70,r.Width-32),Math.Max(30,r.Bottom-16-panelTop));
   var headerClip=new RectangleF(r.X+16,r.Y+38,Math.Max(70,r.Width-32),Math.Max(0,panelTop-r.Y-62));
   var headerState=g.Save();g.SetClip(headerClip,CombineMode.Intersect);
   float hx=headerClip.X,hy=headerClip.Y;
   Text(g,L.Date(Month,"M 月","MMM")+"  "+Month.Year,hx,hy,headerClip.Width,34,20,Ink);hy+=46;
   foreach(var cal in State.Calendars.Where(c=>c.Enabled).Take(2)){if(hy+25>panelTop-62)break;Dot(g,hx+2,hy+9,7,ColorTranslator.FromHtml(cal.Color));Text(g,cal.Name,hx+20,hy,headerClip.Width-20,25,11,Ink);hy+=28;}
   if(headerClip.Height>=78)Button(g,"sources",L.T("国家假日")+" · "+State.Countries.Count,new RectangleF(hx,panelTop-54,headerClip.Width,30));
   g.Restore(headerState);
   ContentGlass(g,panel);
   var view=new RectangleF(panel.X+18,panel.Y+64,Math.Max(34,panel.Width-36),Math.Max(12,panel.Height-82));
   float x=view.X,w=Math.Max(24,view.Width-12);
   bool lot=State.Widget=="占卜"||State.Widget=="抽签卡片";string key=lot?"sidebarfortune":"sidebarzodiac";
   string body="",title="",grade="";
   if(lot){var read=Divination.Read(State.Divination);title=L.T("数字小六壬");body=read==null?L.T("选好类别后，点击开始占卜。"):L.T(read.Name)+" · "+L.T(read.Grade)+"\n\n"+read.Meaning+"\n\n"+read.Advice;if(!String.IsNullOrEmpty(FortuneError))body=FortuneError+"\n"+body;}
   else{var read=Reading.Daily(State.Zodiac,Today);title=L.T(State.Zodiac);grade=read.Grade;body=L.T("综合运势 ")+new string('★',read.Overall)+"\n\n"+read.Outlook;if(!String.IsNullOrWhiteSpace(read.Interpretation)&&read.Interpretation!=read.Outlook)body+="\n\n"+read.Interpretation;body+="\n\n"+(read.IsOnline?L.T("幸运色：")+read.LuckyName+L.T(" / 数字 ")+read.Number:L.T("尚未获取幸运信息"))+"\n\n"+L.T("注意：")+read.Caution;}
   float inner=w;float textHeight,hintHeight;using(var font=UiFont.Scene(11)){textHeight=g.MeasureString(body,font,(int)inner).Height+12;hintHeight=g.MeasureString(L.T("请在下面三个框内输入数字，每个数字为 1–99 的整数。"),font,(int)inner).Height+10;}
   int columns=inner>=216?3:2;float catWidth=(inner-8*(columns-1))/columns;
   // Measure category labels so translated text does not get squeezed into tiny pills.
   using(var font=UiFont.Scene(10.5f)){float needed=Divination.Categories.Max(c=>g.MeasureString(L.T(c),font).Width)+20;if(catWidth<needed){columns=Math.Max(1,(int)((inner+8)/(needed+8)));catWidth=(inner-8*(columns-1))/columns;}}
   int rows=(5+columns-1)/columns;
   float actionsHeight=42;
   float bodyStart=lot?33+hintHeight+56+actionsHeight+16+rows*42+8:73,total=bodyStart+textHeight+122;
   float half=(w-8)/2;
   Button(g,"leftzodiac",L.T("星座"),new RectangleF(x,panel.Y+18,half,32),!lot);Button(g,"leftfortune",L.T("占卜"),new RectangleF(x+half+8,panel.Y+18,half,32),lot);
   float offset=ScrollOffset(key,total-view.Height);int start=Hits.Count;var saved=g.Save();g.SetClip(view,CombineMode.Intersect);g.TranslateTransform(0,-offset);float y=view.Y;
   Text(g,title,x,view.Y,w,28,16,Ink);
   if(lot){
    y=view.Y+33;Wrap(g,L.T("请在下面三个框内输入数字，每个数字为 1–99 的整数。"),new RectangleF(x,y,w,hintHeight),11,Ink);y+=hintHeight+8;
    float cell=(w-16)/3;for(int i=0;i<3;i++){var b=new RectangleF(x+i*(cell+8),y,cell,40);Round(g,b,10,FortuneBad[i]?FortuneColors.BadFill:FortuneColors.Slot);Centered(g,String.IsNullOrEmpty(FortuneNumbers[i])?L.T("数"+(i+1)):FortuneNumbers[i],b,13,Ink,"Segoe UI");Hits.Add(new Hit{Action="fortuneinput",Id=i.ToString(),Box=b});}y+=48;
    float clearWidth=Math.Min(64,w*.29f),mainWidth=w-clearWidth-8;
    var primary=new RectangleF(x,y,mainWidth,42);Round(g,primary,10,Skins.Accent);Centered(g,L.T("开始占卜"),primary,11,Skins.OnAccent,"Microsoft YaHei UI");Hits.Add(new Hit{Action="drawfortune",Box=primary});
    Button(g,"fortuneclear",L.T("清空"),new RectangleF(x+mainWidth+8,y,clearWidth,42));
    y+=actionsHeight+16;
    for(int i=0;i<5;i++){Button(g,"fortunecat"+i,L.T(Divination.Categories[i]),new RectangleF(x+(i%columns)*(catWidth+8),y+(i/columns)*42,catWidth,34),FortuneCategory==Divination.Categories[i]);Hits[Hits.Count-1].Id=Divination.Categories[i];}

   }else Text(g,grade,x,view.Y+38,w-4,21,10,Muted);
   y=view.Y+bodyStart;Wrap(g,body,new RectangleF(x,y,w,textHeight),11,Ink);y+=textHeight+14;
   if(lot){if(State.Divination!=null)Button(g,"reading",L.T("详细解读"),new RectangleF(x,y,w,38));else {Round(g,new RectangleF(x,y,w,38),9,Skins.Button);Centered(g,L.T("详细解读"),new RectangleF(x,y,w,38),10.5f,Muted,"Microsoft YaHei UI");}}
   else {Button(g,"zodiacchoose",L.T("换星座"),new RectangleF(x,y,half,32));Button(g,"reading",L.T("详细运势"),new RectangleF(x+half+8,y,half,32));}
   Text(g,L.T(lot?"民俗娱乐参考":"娱乐内容"),x,y+54,w,36,10,Muted);
   g.Restore(saved);
   for(int i=Hits.Count-1;i>=start;i--){var b=Hits[i].Box;b.Y-=offset;Hits[i].Box=RectangleF.Intersect(b,view);if(Hits[i].Box.Width<=0||Hits[i].Box.Height<=0)Hits.RemoveAt(i);}
   int scrollStart=Hits.Count;ScrollBar(g,key,view,offset,total);var wheel=Hits[scrollStart];Hits.RemoveAt(scrollStart);Hits.Insert(start,wheel);
  }
 }
}
