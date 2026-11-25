#if TESTS
using System;using System.IO;using System.Diagnostics;using System.Collections.Generic;using System.Windows.Forms;
namespace PaperDays {public static class PopupBench56 {
 public static int Run(string dir){Directory.CreateDirectory(dir);Data.Root=dir;OnlineData.Enabled=false;L.Language="zh-CN";var s=new State();Skins.Apply(s);var lines=new List<string>();
 var factories=new Dictionary<string,Func<Form>>{{"horoscope",()=>new ReadingDialog(Reading.Daily("双子座",DateTime.Today),"运势")},{"event",()=>new Editor(s,null,DateTime.Today)},{"todo",()=>new TodoDetails()},{"calendars",()=>new SettingsDialog(s,false)},{"divination",()=>new DivinationDialog(null)},{"result",()=>new DivinationResultDialog(new DivinationRecord{A=3,B=9,C=27,Category="工作"})}};
 foreach(var pair in factories)for(int i=0;i<3;i++){var watch=Stopwatch.StartNew();using(var f=pair.Value()){long created=watch.ElapsedMilliseconds;f.Show();Application.DoEvents();lines.Add(pair.Key+" run="+i+" construct="+created+"ms open="+watch.ElapsedMilliseconds+"ms");f.Close();Application.DoEvents();}}
 File.WriteAllLines(Path.Combine(dir,"timings.txt"),lines);return 0;
 }
}}
#endif
