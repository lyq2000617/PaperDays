#if TESTS
using System;using System.IO;using System.Collections.Generic;using System.Linq;
namespace PaperDays {public static class Core54Tests {public static int Run(string directory){Directory.CreateDirectory(directory);Data.Root=directory;OnlineData.Enabled=false;var lines=new List<string>();int failures=0;Action<bool,string> check=(ok,n)=>{lines.Add((ok?"PASS ":"FAIL ")+n);if(!ok)failures++;};try{
            var state = new State();
            var entry = new Entry
            {
                Title = "测试日程 🌿",
                Start = new DateTime(2026, 10, 30, 9, 0, 0),
                End = new DateTime(2026, 10, 31, 10, 0, 0),
                TimeZone = "Korea Standard Time"
            };
            state.Events.Add(entry);
            state.Todos.Add(new Todo { Title = "测试待办", Done = true });
            Data.Write(state);
            State loaded = Data.Read();
            check(loaded.Events[0].Title == entry.Title && loaded.Todos[0].Done, "events and completed todos persist");
            check(entry.UtcStart() == new DateTime(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc), "timezone conversion");
            check(!Data.Due(entry, entry.UtcStart().AddMinutes(-16)) && Data.Due(entry, entry.UtcStart().AddMinutes(-15)), "reminder boundary");
            entry.Notified = true;
            check(!Data.Due(entry, entry.UtcStart().AddHours(2)), "reminder deduplication");
            check(entry.On(new DateTime(2026, 10, 31)) && !entry.On(new DateTime(2026, 11, 1)), "multi-day event range");
            check(Data.Lunar(new DateTime(2026, 2, 17)) == "正月", "lunar new year");
            Data.Write(state);
            check(File.Exists(Path.Combine(directory, "calendar.json.bak")), "atomic backup");
            string valid = File.ReadAllText(Path.Combine(directory, "calendar.json"));
            File.WriteAllText(Path.Combine(directory, "calendar.json"), "broken");
            bool rejected = false;
            try
            {
                Data.Read();
            }
            catch
            {
                rejected = true;
            }

            check(rejected, "corrupted data is not overwritten");
            File.WriteAllText(Path.Combine(directory, "calendar.json"), valid);
            check(Calendars.China().Count(x => x.Label != "补班") == 33 && Calendars.China().Count(x => x.Label == "补班") == 6, "2026 official Chinese holiday dates");
            state.Countries = new List<string>
            {
                "KR",
                "JP",
                "US"
            };
            string status;
            var marks = Calendars.Load(state, new DateTime(2026, 10, 1), out status);
            check(marks.Any(x => x.Source.StartsWith("KR")) && marks.Any(x => x.Source.StartsWith("JP")) && marks.Any(x => x.Source.StartsWith("US")), "multiple foreign calendars load offline");
            string ics = "BEGIN:VCALENDAR\r\nVERSION:2.0\r\nBEGIN:VEVENT\r\nUID:test\r\nSUMMARY:旅行\r\nDTSTART;VALUE=DATE:20261023\r\nDTEND;VALUE=DATE:20261026\r\nEND:VEVENT\r\nEND:VCALENDAR";
            var imported = Calendars.ImportIcs(ics, "trip", Palette.Hex[1]);
            check(imported.Count == 1 && imported[0].On(new DateTime(2026, 10, 25)) && !imported[0].On(new DateTime(2026, 10, 26)), "ICS exclusive end date");
            rejected = false;
            try
            {
                Calendars.ImportIcs(ics.Replace("UID:test", "RRULE:FREQ=DAILY\r\nUID:test"), "trip", Palette.Hex[1]);
            }
            catch
            {
                rejected = true;
            }

            check(rejected, "unsupported recurrence is rejected explicitly");

using(var backdrop=new System.Drawing.Bitmap(1920,1080)) {using(var g=System.Drawing.Graphics.FromImage(backdrop))g.Clear(System.Drawing.Color.Teal);foreach(var skin in Skins.All){var viewState=new State{Skin=skin.Id};Skins.Apply(viewState);var scene=new Scene{State=viewState,GlassBackdrop=backdrop};using(var warm=scene.Render(backdrop.Size)){}var durations=new List<long>();for(int i=0;i<5;i++){var timer=System.Diagnostics.Stopwatch.StartNew();using(var frame=scene.Render(backdrop.Size)){}durations.Add(timer.ElapsedMilliseconds);}lines.Add("TIMING 1920x1080 offscreen frame "+skin.Id+": median="+durations.OrderBy(x=>x).ElementAt(2)+"ms max="+durations.Max()+"ms");}}
}catch(Exception ex){lines.Add("ERROR "+ex);failures++;}File.WriteAllLines(Path.Combine(directory,"test-results.txt"),lines);return failures==0?0:1;}}}
#endif
