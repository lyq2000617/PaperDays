using System;
using System.Drawing;
namespace PaperDays {
    public partial class Scene {
        void DrawGlassCompactWidget(Graphics g) {
            Card(g,new RectangleF(16,16,970,312));
            bool lot=State.Widget=="占卜" || State.Widget=="抽签卡片";
            Button(g,"leftzodiac",L.T("星座"),new RectangleF(40,35,125,38),!lot);
            Button(g,"leftfortune",L.T("占卜"),new RectangleF(177,35,125,38),lot);
            Button(g,"sources",L.T("我的日历"),new RectangleF(755,35,200,38));
            string title,body;
            if(lot) {
                var result=Divination.Calculate(State.Divination ?? new DivinationRecord { A=1,B=1,C=1 });
                title=L.T("数字占卜");
                body=State.Divination==null?L.T("详细解读"):L.T(result.Name)+" · "+result.Meaning+"\n"+result.Advice;
            } else {
                var reading=Reading.Daily(State.Zodiac,Today);title=L.T(State.Zodiac);body=reading.Outlook;
            }
            Text(g,title,40,92,900,36,19,Ink);
            Wrap(g,body,new RectangleF(40,140,880,105),12,Ink);
            Button(g,lot?"widgetfortune":"reading",L.T("详细解读"),new RectangleF(730,266,225,40));
        }
    }
}
