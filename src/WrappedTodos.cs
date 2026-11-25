using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace PaperDays
{
    public class TodoBlock
    {
        public Todo Todo;
        public string Text;
        public float Height;
        public bool Continued;
    }

    public partial class Scene
    {
        public int TodoPageCount = 1;
        public bool InlineTodo;
        public float InlineHeight = 45;
        public string InlineId;
        /// <summary>
        /// 处理WrappedTodos中的 TodoPages 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static List<List<TodoBlock>> TodoPages(Graphics g, List<Todo> todos, float width, float height)
        {
            var pages = new List<List<TodoBlock>>
            {
                new List<TodoBlock>()
            };
            float used = 0;
            using (var font = UiFont.Scene(11))
            {
                float lineHeight = font.GetHeight(g) + 3;
                int capacity = Math.Max(1, (int)((height - 22) / lineHeight));
                foreach (var todo in todos)
                {
                    var lines = new List<string>();
                    string line = "";
                    var iter = StringInfo.GetTextElementEnumerator(todo.Title.Replace("\r\n", "\n").Replace("\r", "\n"));
                    while (iter.MoveNext())
                    {
                        string c = iter.GetTextElement();
                        if (c == "\n")
                        {
                            lines.Add(line);
                            line = "";
                            continue;
                        }

                        if (line.Length > 0 && g.MeasureString(line + c, font).Width > width)
                        {
                            lines.Add(line);
                            line = "";
                        }

                        line += c;
                    }

                    lines.Add(line);
                    for (int i = 0; i < lines.Count; i += capacity)
                    {
                        var chunk = lines.Skip(i).Take(capacity).ToArray();
                        float h = chunk.Length * lineHeight + 22;
                        if (used + h > height && pages.Last().Count > 0)
                        {
                            pages.Add(new List<TodoBlock>());
                            used = 0;
                        }

                        pages.Last().Add(new TodoBlock { Todo = todo, Text = String.Join("\n", chunk), Height = h, Continued = i > 0 });
                        used += h;
                    }
                }
            }

            return pages;
        }

        /// <summary>
        /// 绘制WrappedTodos中的 DrawDaily 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void DrawDaily(Graphics g, bool compact)
        {
            float x = compact ? 25 : 1290, y = compact ? 112 : 215, w = compact ? 920 : 270, h = compact ? 240 : 490 + TallY;
            Card(g, compact ? new RectangleF(0, 0, 970, 440) : new RectangleF(1268, 16, 316, Layout.Plans.Height));
            ContentGlass(g,new RectangleF(x-8,y-10,w+16,h+20));
            Text(g, L.T("今日计划"), x, compact ? 19 : 35, w, 43, 20, Ink);
            float tabsY = compact ? 65 : 102;
            Button(g, "plandaily", L.T("今日"), new RectangleF(x, tabsY, 82, 35), true);
            Button(g, "planweek", L.T("周"), new RectangleF(x + 94, tabsY, 82, 35));
            Button(g, "planmonth", L.T("月"), new RectangleF(x + 188, tabsY, 82, 35));
            if (!compact)
                Text(g, Today.ToString("MM.dd dddd", L.Culture), x, 158, w, 29, 11, Muted);
            var items = PlanItems().Where(t => !InlineTodo || t.Id != InlineId).ToList();
            var pages = TodoPages(g, items, w - 65, h - (InlineTodo ? Math.Min(InlineHeight + 35, h - 70) : 0));
            TodoPageCount = pages.Count;
            int page = Math.Max(0, Math.Min(State.TodoPage, pages.Count - 1));
            State.TodoPage = page;
            foreach (var block in pages[page])
            {
                var todo = block.Todo;
                using (var pen = new Pen(todo.Done ? Skins.Accent : Muted, 1.3f))
                    g.DrawEllipse(pen, x + 1, y + 4, 15, 15);
                if (todo.Done)
                    Text(g, "✓", x, y - 1, 22, 24, 10, Skins.Accent);
                Hits.Add(new Hit { Action = "todo", Id = todo.Id, Box = new RectangleF(x - 2, y - 2, 27, 30) });
                using (var font = UiFont.Scene(11, todo.Done ? FontStyle.Strikeout : FontStyle.Regular))
                using (var brush = new SolidBrush(todo.Done ? Muted : Ink))
                    g.DrawString(block.Text, font, brush, new PointF(x + 28, y));
                Hits.Add(new Hit { Action = "todofull", Id = todo.Id, Box = new RectangleF(x + 28, y, w - 65, block.Height - 16) });
                Button(g, "tododelete", "×", new RectangleF(x + w - 31, y, 31, 30));
                Hits.Last().Id = todo.Id;
                using (var pen = new Pen(Line))
                    g.DrawLine(pen, x, y + block.Height - 8, x + w, y + block.Height - 8);
                y += block.Height;
            }

            if (InlineTodo)
            {
                float ih = Math.Min(InlineHeight, h - 105);
                using (var pen = new Pen(Muted, 1.3f))
                    g.DrawEllipse(pen, x + 1, y + 4, 15, 15);
                Hits.Add(new Hit { Action = "todoeditor", Box = new RectangleF(x + 28, y, w - 35, ih) });
                Text(g, L.T("Enter 换行 · 点击其他位置保存"), x + 28, y + ih + 5, w - 28, 22, 9, Muted);
            }
            else if (items.Count == 0)
                Text(g, L.T("暂无今日待办"), x, y, w, 40, 12, Muted);
            float bottom = compact ? 374 : 774 + TallY;
            Button(g, "addtodo", L.T("＋ 添加待办"), new RectangleF(x, bottom, compact ? 240 : w, 40));
            Button(g, "todohistory", L.T("全部记录"), new RectangleF(compact ? 300 : x, compact ? 374 : 721 + TallY, compact ? 170 : w, 35));
            float foot = compact ? 380 : 837 + TallY;
            Text(g, items.Count(t => t.Done) + " / " + items.Count + L.T(" 已完成") + (pages.Count > 1 ? " · " + (page + 1) + "/" + pages.Count : ""), compact ? 490 : x, foot, 190, 28, 10, Muted);
            if (pages.Count > 1)
            {
                Button(g, "todoprev", "‹", new RectangleF(compact ? 838 : 1486, foot - 3, 32, 32));
                Button(g, "todonext", "›", new RectangleF(compact ? 893 : 1528, foot - 3, 32, 32));
            }
        }
    }
}