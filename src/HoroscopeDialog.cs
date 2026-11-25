using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    public class ReadingDialog : CompactDialog
    {
        Timer dailyTimer;
        bool refreshing;
        DateTime nextRefresh = DateTime.MinValue;
        Reading current;
        FlowLayoutPanel content;
        Label metadata;
        Button refresh;
        string sign;
        FlowLayoutPanel page;
        bool building;
        public ReadingDialog(Reading reading,string title) : base(title,580,620) {
            SuspendLayout();Body.SuspendLayout();
            current=reading;
            sign=reading!=null&&Array.IndexOf(Reading.Signs,reading.Title)>=0?reading.Title:"天秤座";
            page=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,TabStop=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(22)};
            page.SuspendLayout();Body.Controls.Add(page);
            page.Controls.Add(new Label{Text=L.T(sign)+L.T(" · 今日运势"),AutoSize=true,Font=UiFont.New(18),Margin=new Padding(0,0,0,12)});
            metadata=new Label{AutoSize=true,Font=UiFont.New(9),Margin=new Padding(0,0,0,12)};page.Controls.Add(metadata);
            content=new FlowLayoutPanel{AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0)};page.Controls.Add(content);
            AddButton(L.T("换星座"),()=>{DialogResult=DialogResult.Retry;Close();});
            refresh=AddButton(L.T("刷新今日"),async()=>{await RefreshData(true);});
            AddButton(L.T("来源与时间"),()=>Notice.Show(this,current==null?L.T("暂无数据"):current.SourceName+"\n"+L.T("内容日期：")+current.SourceDate+"\n"+L.T("获取时间：")+current.FetchedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")+"\n"+current.SourceUrl,L.T("运势来源")));
            CancelButton=AddButton(L.T("关闭"),()=>Close(),true);WholePageScroll.Install(page);
            Populate(reading);page.SizeChanged+=delegate{FitText();};
            page.ResumeLayout(false);Body.ResumeLayout(false);ResumeLayout(true);FitText();ApplyTheme();
            Shown+=async delegate{ActiveControl=page;page.Focus();page.AutoScrollPosition=Point.Empty;if(OnlineData.Enabled)await RefreshData(false);};
            dailyTimer=new Timer{Interval=60000};dailyTimer.Tick+=async delegate{if(OnlineData.Enabled&&DateTime.UtcNow>=nextRefresh)await RefreshData(false);};dailyTimer.Start();
            FormClosed+=delegate{dailyTimer.Stop();dailyTimer.Dispose();};
        }
        void FitText(){
            if(building||page==null)return;building=true;page.SuspendLayout();content.SuspendLayout();
            try{int width=Math.Max(130,page.ClientSize.Width-page.Padding.Horizontal-SystemInformation.VerticalScrollBarWidth-8);
                foreach(var label in page.Controls.OfType<Label>())if(label.MaximumSize.Width!=width)label.MaximumSize=new Size(width,0);
                content.MaximumSize=new Size(width,0);foreach(var label in content.Controls.OfType<Label>())if(label.MaximumSize.Width!=width-6)label.MaximumSize=new Size(width-6,0);
            }finally{content.ResumeLayout(true);page.ResumeLayout(true);building=false;}
        }

        /// <summary>
        /// 处理HoroscopeDialog中的 Section 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void Section(string title, string text)
        {
            var h = new Label
            {
                Text = title,
                AutoSize = true,
                MaximumSize = new Size(560, 0),
                Font = UiFont.New(12, FontStyle.Bold),
                ForeColor = Palette.Ink,
                Margin = new Padding(3, 12, 3, 8)
            };
            var p = new Label
            {
                Text = String.IsNullOrWhiteSpace(text) ? L.T("来源未提供这项详细内容。") : text,
                AutoSize = true,
                MaximumSize = new Size(560, 0),
                Font = UiFont.New(11),
                ForeColor = Palette.Ink,
                Margin = new Padding(3, 0, 3, 14)
            };
            content.Controls.Add(h);
            content.Controls.Add(p);
        }

        /// <summary>
        /// 处理HoroscopeDialog中的 Populate 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void DetailedSection(string title,string text,string key) {
            Section(title,CompleteHoroscopes.Clean(text));
            if(!String.IsNullOrWhiteSpace(text)) Section(InterpretationNotes.Heading,InterpretationNotes.For(key));
        }
        void Populate(Reading r)
        {
            current = r;
            if (r != null && !String.IsNullOrEmpty(r.TranslationStatus))
                Text = L.T(sign) + " · " + L.T("机器翻译 · MyMemory");
            page.SuspendLayout();content.SuspendLayout();
            while (content.Controls.Count > 0)
                content.Controls[0].Dispose();
            if (r == null || !r.IsOnline)
            {
                metadata.Text = DateTime.Today.ToString("yyyy-MM-dd") + L.T(" · 尚未获取来源数据");
                Section(L.T("未获取今日运势"), L.T("请联网后点击刷新。不会用预设文本、昨天的内容或来源的降级生成结果替代今日数据。"));
            }
            else
            {
                metadata.Text = r.SourceDate + " · " + r.SourceName + "\n" + L.T("获取于 ") + r.FetchedUtc.ToLocalTime().ToString("MM-dd HH:mm") + " · " + L.T("娱乐解读，不保证实际预测");
                DetailedSection(L.T("综合运势"), r.Outlook + (String.IsNullOrWhiteSpace(r.Interpretation)||r.Interpretation==r.Outlook?"":"\n\n"+r.Interpretation),"overall");
                DetailedSection(L.T("事业／工作"), r.CareerText,"work");
                DetailedSection(L.T("桃花／感情"), r.LoveText,"love");
                DetailedSection(L.T("财运"), r.WealthText,"money");
                DetailedSection(L.T("健康／状态"), r.HealthText,"health");
                DetailedSection(L.T("出行"), r.TravelText,"travel");
                Section(L.T("今日宜忌"), r.Caution);
                Section(L.T("幸运信息"), L.T("颜色：") + (r.LuckyName ?? L.T("未提供")) + L.T("    数字：") + (r.Number == 0 ? L.T("未提供") : r.Number.ToString()));
            }


            content.ResumeLayout(false);FitText();page.ResumeLayout(true);
        }

        /// <summary>
        /// 刷新HoroscopeDialog中的 RefreshData 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async System.Threading.Tasks.Task RefreshData(bool force)
        {
            if (refreshing)
                return;
            refreshing = true;
            nextRefresh = DateTime.UtcNow.AddMinutes(30);
            if (DateTime.Now.AddMinutes(30).Date != DateTime.Today)
                nextRefresh = DateTime.Today.AddDays(1).ToUniversalTime();
            refresh.Enabled = false;
            try
            {
                var value = await Horoscopes.UpdateAsync(sign, DateTime.Today, force);
                if (!IsDisposed)
                    Populate(value);
            }
            catch (Exception ex)
            {
                if (!IsDisposed)
                {
                    Populate(Horoscopes.Cached(sign, DateTime.Today));
                    metadata.Text += "\n更新失败：" + ex.Message;
                }
            }
            finally
            {
                refreshing = false;
                if (!IsDisposed)
                    refresh.Enabled = true;
            }
        }
    }
}