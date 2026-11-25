using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace PaperDays
{
    public static class DailyArchive
    {
        /// <summary>
        /// 处理逐日归档中的 Key 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Key(Todo t)
        {
            return String.IsNullOrEmpty(t.TaskId) ? t.Id : t.TaskId;
        }

        /// <summary>
        /// 启用逐日归档并补齐可恢复的历史快照。
        /// </summary>
        public static void Enable(State s, DateTime day)
        {
            s.ArchiveEnabled = true;
            Sync(s, day);
        }

        /// <summary>
        /// 按日期同步待办快照，保留过去内容和删除记录。
        /// </summary>
        public static void Sync(State s, DateTime day)
        {
            if (!s.ArchiveEnabled)
                return;
            day = day.Date;
            if (s.TodoArchive == null)
                s.TodoArchive = new List<Todo>();
            if (s.DeletedArchive == null)
                s.DeletedArchive = new List<string>();
            var deleted = new HashSet<string>(s.DeletedArchive);
            var existing = s.TodoArchive.ToDictionary(x => x.Id);
            var last = s.TodoArchive.GroupBy(Key).ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Date).First());
            foreach (var t in s.Todos)
            {
                DateTime origin = DateTime.ParseExact(t.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                if (origin > day)
                    continue;
                Todo previous;
                last.TryGetValue(t.Id, out previous);
                DateTime end = t.Done ? (t.CompletedAt.HasValue ? t.CompletedAt.Value.Date : origin) : day;
                if (end > day)
                    end = day;
                DateTime start = previous == null ? origin : DateTime.ParseExact(previous.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1);
                if (previous != null && previous.Done && !t.Done)
                    start = day;
                for (var d = start; d <= end; d = d.AddDays(1))
                {
                    string id = t.Id + "@" + d.ToString("yyyy-MM-dd");
                    if (existing.ContainsKey(id) || deleted.Contains(id))
                        continue;
                    bool complete = t.Done && (t.CompletedAt.HasValue ? d == t.CompletedAt.Value.Date : d == origin);
                    var row = new Todo
                    {
                        Id = id,
                        TaskId = t.Id,
                        OriginalDate = t.Date,
                        Date = d.ToString("yyyy-MM-dd"),
                        Title = previous != null && d < day ? previous.Title : t.Title,
                        Done = complete,
                        CompletedAt = complete ? t.CompletedAt : null,
                        Reconstructed = d < day
                    };
                    s.TodoArchive.Add(row);
                    existing[id] = row;
                }

                string todayId = t.Id + "@" + day.ToString("yyyy-MM-dd");
                Todo current;
                if (end >= day && existing.TryGetValue(todayId, out current))
                {
                    current.Title = t.Title;
                    current.Done = t.Done;
                    current.CompletedAt = t.CompletedAt;
                    current.Reconstructed = false;
                }
            }
        }

        /// <summary>
        /// 删除逐日归档中的 Delete 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static bool Delete(State s, string id)
        {
            if (s.TodoArchive == null)
                return false;
            if (s.TodoArchive.RemoveAll(t => t.Id == id) == 0)
                return false;
            if (!s.DeletedArchive.Contains(id))
                s.DeletedArchive.Add(id);
            return true;
        }

        /// <summary>
        /// 打开归档详情的只读视图。
        /// </summary>
        public static Form View(Todo t)
        {
            var f = new Form();
            UI.Style(f, "每日计划归档 · " + t.Date, 640, 460);
            UI.Label(f, "原计划：" + t.OriginalDate + " · " + (t.Done ? "已完成" : "未完成") + (t.Reconstructed ? " · 补录，历史状态未核实" : ""), 24, 22, 590);
            var text = new TextBox
            {
                Text = t.Title,
                ReadOnly = true,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                BorderStyle = BorderStyle.None,
                BackColor = Palette.Cream,
                ForeColor = Palette.Ink,
                Location = new Point(24, 62),
                Size = new Size(590, 315)
            };
            f.Controls.Add(text);
            UI.Label(f, "历史归档只读；修改今日计划不会覆盖此记录。", 24, 389, 590);
            UI.Button(f, "关闭", 494, 419, 120, delegate
            {
                f.Close();
            });
            return f;
        }
    }
}