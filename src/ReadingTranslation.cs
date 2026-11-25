using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace PaperDays
{
    public class TranslationData { public string translatedText; }
    public class TranslationResponse { public int responseStatus; public bool quotaFinished; public string responseDetails; public TranslationData responseData; }
    public static class ReadingTranslation
    {
        static readonly object gate = new object();
        static readonly object translationGate = new object();
        static readonly Dictionary<string,DateTime> failed = new Dictionary<string,DateTime>();
        internal static Func<string,string> Download = OnlineData.Download;
        static string Hash(string text)
        {
            using (var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-","");
        }
        static string[] Fields(Reading r) { return new[] { r.Outlook,r.Interpretation,r.CareerText,r.LoveText,r.WealthText,r.HealthText,r.TravelText,r.Caution,r.LuckyName }; }
        static void SetFields(Reading r,string[] v) { r.Outlook=v[0];r.Interpretation=v[1];r.CareerText=v[2];r.LoveText=v[3];r.WealthText=v[4];r.HealthText=v[5];r.TravelText=v[6];r.Caution=v[7];r.LuckyName=v[8]; }
        static Reading Copy(Reading r) { return Data.Json().Deserialize<Reading>(Data.Json().Serialize(r)); }
        static string PathFor(Reading r,string language)
        {
            return Path.Combine(Data.Root,"horoscope-translations",language,Hash(r.Title+"|"+r.SourceDate+"|"+String.Join("|",Fields(r)))+".json");
        }
        public static Reading Cached(Reading raw,string language)
        {
            if (raw==null || language=="zh-CN") return raw;
            var r=Copy(raw);
            r.Grade=L.T("来源评分 ",language)+raw.Overall+" / 5";
            if (language=="zh-TW") { SetFields(r,Fields(raw).Select(L.Traditional).ToArray()); return r; }
            if (!raw.IsOnline)
            {
                r.Grade=L.T("未获取今日数据",language);r.Outlook=L.T("请联网刷新。不会用预设文案或旧日期内容代替。",language);
                r.Caution=L.T("运势属于娱乐内容，不保证实际预测。",language);r.LuckyName=L.T("未提供",language);return r;
            }
            string path=PathFor(raw,language);
            try
            {
                if (File.Exists(path))
                {
                    var values=Data.Json().Deserialize<string[]>(File.ReadAllText(path));
                    if (values!=null && values.Length==9) { SetFields(r,values); r.TranslationStatus="ready";return r; }
                }
            }
            catch (IOException) { }
            catch (ArgumentException) { }
            bool error;
            lock(gate) error=failed.ContainsKey(path);
            SetFields(r,new[] { L.T(error ? "翻译暂不可用，请稍后刷新。" : "翻译处理中，请稍候。",language),"","","","","","","","" });
            r.TranslationStatus=error?"failed":"pending";
            return r;
        }
        public static Reading Translate(Reading raw,string language,bool force=false)
        {
            var cached=Cached(raw,language);
            if (raw==null || !raw.IsOnline || language=="zh-CN" || language=="zh-TW" || cached.TranslationStatus=="ready") return cached;
            string path=PathFor(raw,language);
            lock(translationGate)
            {
                DateTime last;
                lock(gate)
                    if (!force && failed.TryGetValue(path,out last) && DateTime.UtcNow-last<TimeSpan.FromMinutes(5)) return Cached(raw,language);
                try
                {
                    // Only public horoscope fields enter this path. No events, notes, or user task text.
                    var values=Fields(raw).Select(text=>TranslateText(text,language)).ToArray();
                    OnlineData.Save(path,Data.Json().Serialize(values));
                    lock(gate) failed.Remove(path);
                }
                catch (Exception ex)
                {
                    if (!(ex is WebException || ex is IOException || ex is ArgumentException || ex is InvalidOperationException)) throw;
                    lock(gate) failed[path]=DateTime.UtcNow;
                }
            }
            return Cached(raw,language);
        }
        internal static IEnumerable<string> Chunks(string text)
        {
            var part=new StringBuilder();int bytes=0;
            for (int i=0;i<text.Length;i++)
            {
                string piece=Char.IsHighSurrogate(text[i]) && i+1<text.Length && Char.IsLowSurrogate(text[i+1]) ? text.Substring(i++,2) : text[i].ToString();
                int count=Encoding.UTF8.GetByteCount(piece);
                if (bytes+count>480) { yield return part.ToString();part.Clear();bytes=0; }
                part.Append(piece);bytes+=count;
                if (bytes>=240 && "。！？\n".Contains(piece)) { yield return part.ToString();part.Clear();bytes=0; }
            }
            if (part.Length>0) yield return part.ToString();
        }
        static string TranslateText(string text,string language)
        {
            if (String.IsNullOrWhiteSpace(text)) return text ?? "";
            string known=L.T(text,language);
            if (known!=text) return known;
            var translated=new List<string>();
            foreach (string part in Chunks(text))
            {
                string path=Path.Combine(Data.Root,"translation-segments",language,Hash(part)+".txt");
                if (File.Exists(path)) { translated.Add(File.ReadAllText(path));continue; }
                string url="https://api.mymemory.translated.net/get?langpair="+Uri.EscapeDataString("zh-CN|"+language)+"&mt=1&q="+Uri.EscapeDataString(part);
                var response=Data.Json().Deserialize<TranslationResponse>(Download(url));
                if (response==null || response.responseStatus!=200 || response.quotaFinished || response.responseData==null || String.IsNullOrWhiteSpace(response.responseData.translatedText)) throw new IOException("Translation service unavailable");
                string result=WebUtility.HtmlDecode(response.responseData.translatedText);
                if (result==part || result.IndexOf("MYMEMORY WARNING",StringComparison.OrdinalIgnoreCase)>=0) throw new IOException("Translation not returned");
                OnlineData.Save(path,result);translated.Add(result);
            }
            return String.Join(" ",translated);
        }
    }
}
