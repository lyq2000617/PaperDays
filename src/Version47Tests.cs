#if TESTS
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
namespace PaperDays
{
    public static class Version47Tests
    {
        public static int Run(string directory)
        {
            Directory.CreateDirectory(directory);Data.Root=directory;OnlineData.Enabled=false;
            var lines=new List<string>();int fails=0;
            Action<bool,string> check=(ok,message)=>{lines.Add((ok?"PASS ":"FAIL ")+message);if(!ok)fails++;};
            try
            {
                var old=new State { TransparentDesktop=true,PanelOpacity=35,Language="ko" };Data.Normalize(old);
                check(old.Skin=="glass" && !old.TransparentDesktop && old.Language=="ko","v0.46 transparency migrates to independent glass skin");
                var normal=new State();Data.Normalize(normal);check(normal.Skin=="cream","opaque users keep original skin");
                check(Skins.All.Count(s=>s.Id=="glass")==1,"glass occurs once in skin picker");
                foreach(string lang in new[] { "zh-CN","zh-TW","en","ko","ja" })
                {
                    L.Language=lang;
                    old.Language=lang;Data.Write(old);check(Data.Read().Language==lang,"language persists: "+lang);
                    check(L.Culture.Name.Length>0,"valid date culture: "+lang);
                    var entry=new Entry { Notes="教室：A101",Title="我的日程" };
                    CourseBook.SetRoom(entry,"B202");check(CourseBook.Room(entry)=="B202" && entry.Title=="我的日程","data keys and personal content unchanged: "+lang);
                    for(int n=1;n<=6;n++)
                    {
                        var record=new DivinationRecord { A=1,B=1,C=n,Category="工作" };
                        var result=Divination.Calculate(record);
                        check(!String.IsNullOrEmpty(result.Meaning) && !String.IsNullOrEmpty(result.Advice),"local reading includes body: "+lang+" "+n);
                        if(lang=="en" || lang=="ko") check(!ContainsHan(result.Meaning+result.Advice),"no Chinese fallback in reading: "+lang+" "+n);
                    }
                    check(!String.IsNullOrWhiteSpace(L.T(DivinationDetails.Rule)),"localized rules: "+lang);
                    foreach(string view in new[] { "month","week" })
                    {
                        var state=new State { Skin="glass",Language=lang,CalendarView=view,IconColumns=0,Widget="占卜",Divination=new DivinationRecord { A=3,B=4,C=5 } };
                        state.Todos.Add(new Todo { Title="日程 / Task / 할 일 / 予定" });Skins.Apply(state);
                        var scene=new Scene { State=state,Month=new DateTime(2026,11,1),Selected=new DateTime(2026,11,16) };scene.LoadFortune();
                        using(var bitmap=scene.Render(new Size(1920,1080)))
                        {
                            check(bitmap.GetPixel(0,0).A==0,"outside cards fully transparent: "+lang+" "+view);
                            var data=bitmap.LockBits(new Rectangle(Point.Empty,bitmap.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
                            var bytes=new byte[Math.Abs(data.Stride)*data.Height];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);bitmap.UnlockBits(data);
                            int solid=0,clear=0;for(int i=3;i<bytes.Length;i+=4){if(bytes[i]>=250)solid++;if(bytes[i]<40)clear++;}
                            check(solid>1000,"opaque text pixels remain: "+lang+" "+view);
                            check(clear>bitmap.Width*bitmap.Height*.70,"no solid paper backing: "+lang+" "+view);
                            using(var region=DesktopAppearance.CreateRegion(scene,bitmap.Size)) check(!region.IsVisible(0,0),"outer desktop clicks pass through: "+lang+" "+view);
                            if(lang=="ko" && view=="month")
                            {
                                bitmap.Save(Path.Combine(directory,"glass-alpha.png"));
                                using(var composite=new Bitmap(bitmap.Width,bitmap.Height))
                                using(var g=Graphics.FromImage(composite))
                                {
                                    using(var fill=new LinearGradientBrush(new Rectangle(Point.Empty,bitmap.Size),Color.FromArgb(25,55,89),Color.FromArgb(106,71,117),25f))g.FillRectangle(fill,0,0,bitmap.Width,bitmap.Height);
                                    using(var glow=new SolidBrush(Color.FromArgb(45,170,230,220)))g.FillEllipse(glow,500,-280,1200,1300);
                                    g.DrawImageUnscaled(bitmap,0,0);composite.Save(Path.Combine(directory,"glass-ko-preview.png"));
                                }
                            }
                            using(var host=new Form())
                            using(var surface=new DesktopSurface(scene,host.Handle))
                            {
                                surface.Size=bitmap.Size;GlassWindow.Present(surface.Handle,bitmap);
                                check(surface.Connected,"native per-pixel alpha submission: "+lang+" "+view);
                            }
                        }
                    }
                }
                L.Language="en";Skins.Apply(new State());
                using(var regular=new Scene { State=new State() }.Render(new Size(1366,768))) check(regular.GetPixel(0,0).A==255,"ordinary skin stays opaque");
                var text=String.Concat(Enumerable.Repeat("这是一段公开的星座文字。🌙",100));
                var chunks=ReadingTranslation.Chunks(text).ToArray();
                check(chunks.All(c=>Encoding.UTF8.GetByteCount(c)<=480) && String.Concat(chunks)==text,"translation chunk limits preserve Unicode and all content");
                var raw=new Reading { IsOnline=true,Title="天秤座",SourceDate="2099-01-01",SourceUrl="https://example.com/source",Outlook="公开运势原文",Interpretation="另一段公开解读",Overall=4 };
                int requests=0;
                ReadingTranslation.Download=url=>{requests++;return "{\"responseStatus\":200,\"responseData\":{\"translatedText\":\"오늘의 공개 운세 번역\"}}";};
                var resultKo=ReadingTranslation.Translate(raw,"ko",true);
                check(resultKo.TranslationStatus=="ready" && resultKo.Outlook=="오늘의 공개 운세 번역","horoscope body replaced with provider translation");
                check(raw.Outlook=="公开运势原文" && resultKo.Title==raw.Title && resultKo.SourceUrl==raw.SourceUrl,"original and provenance retained without translating identity");
                int before=requests;ReadingTranslation.Translate(raw,"ko");check(requests==before,"translated reading cached without new requests");
                ReadingTranslation.Download=url=>"{\"responseStatus\":403,\"quotaFinished\":true}";
                var failed=ReadingTranslation.Translate(raw,"ja",true);
                check(failed.TranslationStatus=="failed" && failed.Outlook==L.T("翻译暂不可用，请稍后刷新。","ja"),"quota failure is explicit in selected language");
                check(failed.Interpretation=="" && failed.CareerText=="","failure does not invent horoscope content");
                var traditional=ReadingTranslation.Cached(raw,"zh-TW");check(traditional.Outlook=="公開運勢原文","traditional Chinese body conversion");
                check(L.T("天秤座","ko")=="천칭자리" && L.T("占卜","ja")=="占い","sign and divination labels localized");
            }
            catch(Exception ex){fails++;lines.Add("ERROR "+ex);}
            finally{ReadingTranslation.Download=OnlineData.Download;L.Language="zh-CN";File.WriteAllLines(Path.Combine(directory,"test-results.txt"),lines);}
            return fails==0?0:1;
        }
        static bool ContainsHan(string s){return s.Any(c=>c>='\u4e00'&&c<='\u9fff');}
    }
}
#endif
