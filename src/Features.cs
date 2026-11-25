using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;

namespace PaperDays
{
    public class Skin
    {
        public string Id, Name, Emoji, Background, Paper, Decor, Accent, Field, ButtonTone;
        public string[] Colors, Highlighters;
    }

    public static class Skins
    {
        public static readonly Skin[] All =
        {
            new Skin
            {
                Id = "cream",
                Name = "奶白",
                Emoji = "",
                Background = "#FFFEFC",
                Paper = "#FFFFFF",
                Decor = "#F4EBD9",
                Accent = "#766852",
                Field = "#FFFEFC",
                ButtonTone = "#F4EBD9",
                Colors = new[]
                {
                    "#FF7899",
                    "#FFCAB8",
                    "#F2E96F",
                    "#88D5A2",
                    "#90B8F8",
                    "#B89AEF",
                    "#EE8DCE",
                    "#62C5C9"
                },
                Highlighters = new[]
                {
                    "#FFDCE8",
                    "#FFE8DF",
                    "#FFFFB8",
                    "#DEF5E3",
                    "#DFEAFF",
                    "#EBDDFF",
                    "#FBE0F4",
                    "#D2F3F1"
                }
            },
            new Skin
            {
                Id = "sage",
                Name = "薄荷绿",
                Emoji = "",
                Background = "#EFFBF6",
                Paper = "#FFFFFF",
                Decor = "#CDEFE0",
                Accent = "#3C866B",
                Field = "#EFFBF6",
                ButtonTone = "#CDEFE0",
                Colors = new[]
                {
                    "#FF7897",
                    "#FFCAB8",
                    "#F2E96F",
                    "#88D5A2",
                    "#90B8F8",
                    "#B89AEF",
                    "#F18AC3",
                    "#68BFC3"
                },
                Highlighters = new[]
                {
                    "#FFD8E5",
                    "#FFE8DF",
                    "#FFFFB8",
                    "#DEF5E3",
                    "#DFEAFF",
                    "#EBDDFF",
                    "#FFDBF0",
                    "#D2F3F1"
                }
            },
            new Skin
            {
                Id = "blue",
                Name = "冰蓝",
                Emoji = "",
                Background = "#F1F8FC",
                Paper = "#FFFFFF",
                Decor = "#D6EAF6",
                Accent = "#487FA3",
                Field = "#F1F8FC",
                ButtonTone = "#D6EAF6",
                Colors = new[]
                {
                    "#F8ADD1",
                    "#FFCAB8",
                    "#F2E96F",
                    "#88D5A2",
                    "#90B8F8",
                    "#B89AEF",
                    "#F4779E",
                    "#66BFB7"
                },
                Highlighters = new[]
                {
                    "#FFE0EE",
                    "#FFE8DF",
                    "#FFFFB8",
                    "#DEF5E3",
                    "#DFEAFF",
                    "#EBDDFF",
                    "#FFD6E4",
                    "#D3F4EB"
                }
            },
            new Skin
            {
                Id = "rose",
                Name = "樱花粉",
                Emoji = "",
                Background = "#FFF3F8",
                Paper = "#FFFFFF",
                Decor = "#F8DCEB",
                Accent = "#A65C7F",
                Field = "#FFF3F8",
                ButtonTone = "#F8DCEB",
                Colors = new[]
                {
                    "#90B8F8",
                    "#FFCAB8",
                    "#F2E96F",
                    "#88D5A2",
                    "#B89AEF",
                    "#F06BA9",
                    "#D78ED1",
                    "#62C3BB"
                },
                Highlighters = new[]
                {
                    "#DFEAFF",
                    "#FFE8DF",
                    "#FFFFB8",
                    "#DEF5E3",
                    "#EBDDFF",
                    "#FFD7E9",
                    "#F4DFF7",
                    "#D1F4EB"
                }
            },
            new Skin
            {
                Id = "lilac",
                Name = "淡紫",
                Emoji = "",
                Background = "#F5F1FF",
                Paper = "#FFFFFF",
                Decor = "#E7DDF8",
                Accent = "#8462A8",
                Field = "#F5F1FF",
                ButtonTone = "#E7DDF8",
                Colors = new[]
                {
                    "#88D5A2",
                    "#FFCAB8",
                    "#F2E96F",
                    "#90B8F8",
                    "#FF8BC6",
                    "#B89AEF",
                    "#F37F91",
                    "#62C3BB"
                },
                Highlighters = new[]
                {
                    "#DEF5E3",
                    "#FFE8DF",
                    "#FFFFB8",
                    "#DFEAFF",
                    "#FFD9F0",
                    "#EBDDFF",
                    "#FFDBE1",
                    "#D1F4EB"
                }
            },
            new Skin
            {
                Id = "stone",
                Name = "柔黑",
                Emoji = "",
                Background = "#18191D",
                Paper = "#24262C",
                Decor = "#454A54",
                Accent = "#DCE1E8",
                Field = "#2C2E35",
                ButtonTone = "#41454D",
                Colors = new[]
                {
                    "#FFF4DB",
                    "#FDFD97",
                    "#FF9588",
                    "#68DCE7",
                    "#C4ACFF",
                    "#8EE0AA",
                    "#FFCAB8",
                    "#FFA3D2"
                },
                Highlighters = new[]
                {
                    "#6C6653",
                    "#7A7948",
                    "#824F4D",
                    "#376D77",
                    "#635680",
                    "#47725C",
                    "#805F55",
                    "#7E506D"
                }
            }
        };
        static Skins()
        {
            var source = All[All.Length - 1];
            All = All.Concat(new[] { new Skin { Id = "glass", Name = "玻璃·深色", Emoji = "", Background = "#18191D", Paper = "#24262C", Decor = "#454A54", Accent = "#FFFFFF", Field = "#2C2E35", ButtonTone = "#41454D", Colors = (string[])source.Colors.Clone(), Highlighters = (string[])source.Highlighters.Clone() } }).ToArray();
            All = All.Concat(new[] { new Skin { Id="glass-light", Name="玻璃·浅色", Emoji="", Background="#EDF2F5", Paper="#F6F8FA", Decor="#CBD5DE", Accent="#263442", Field="#E4EBF0", ButtonTone="#D6E0E8", Colors=(string[])source.Colors.Clone(), Highlighters=(string[])source.Highlighters.Clone() } }).ToArray();
        }
        public static bool IsGlass(string id) { return id == "glass" || id == "glass-light"; }
        public static bool Glass { get { return IsGlass(Current.Id); } }
        static Skin EventPalette = All[0];
        public static Skin Current = All[0];
        public static bool Dark
        {
            /// <summary>
            /// 读取Dark属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return Current.Id == "stone" || Current.Id == "glass";
            }
        }

        public static Color OnAccent
        {
            /// <summary>
            /// 读取OnAccent属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return Dark ? Color.FromArgb(28, 27, 33) : Color.White;
            }
        }

        public static Color Line
        {
            /// <summary>
            /// 读取Line属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return ColorTranslator.FromHtml(Current.Decor);
            }
        }

        public static Color Decor
        {
            /// <summary>
            /// 读取Decor属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return Line;
            }
        }

        public static Color Button
        {
            /// <summary>
            /// 读取Button属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return ColorTranslator.FromHtml(Current.ButtonTone);
            }
        }

        public static Color Field
        {
            /// <summary>
            /// 读取Field属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return ColorTranslator.FromHtml(Current.Field);
            }
        }

        public static Color Accent
        {
            /// <summary>
            /// 读取Accent属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return ColorTranslator.FromHtml(Current.Accent);
            }
        }

        /// <summary>
        /// 处理皮肤与课表导入中的 Highlight 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static Color Highlight(string hex)
        {
            var c = ColorTranslator.FromHtml(hex);
            var p = Glass ? ColorTranslator.FromHtml(EventPalette.Paper) : Palette.Paper;
            return Color.FromArgb((c.R * 32 + p.R * 68) / 100, (c.G * 32 + p.G * 68) / 100, (c.B * 32 + p.B * 68) / 100);
        }

        /// <summary>
        /// 按当前皮肤取得独立高亮预设。
        /// </summary>
        public static string HighlightAt(int slot)
        {
            return EventPalette.Highlighters[Math.Max(0, slot) % 8];
        }

        /// <summary>
        /// 取得单色 emoji 的颜色：默认黑白；标记色太浅时加深，保证在纸面上看得清。
        /// </summary>
        public static string EmojiInk(Entry entry)
        {
            if (!entry.EmojiTint)
                return ColorTranslator.ToHtml(Palette.Ink);
            if (Glass) return entry.Color;
            Color c = ColorTranslator.FromHtml(entry.Color);
            double light = EmojiArt.Brightness(c);
            if (!Dark && light > .62)
                c = Color.FromArgb((int)(c.R * .62 / light), (int)(c.G * .62 / light), (int)(c.B * .62 / light));
            else if (Dark && light < .45)
                c = Color.FromArgb(c.R + (255 - c.R) / 2, c.G + (255 - c.G) / 2, c.B + (255 - c.B) / 2);
            return ColorTranslator.ToHtml(c);
        }

        /// <summary>
        /// 取得日程独立高亮色，旧数据无此字段时沿用原来的显示效果。
        /// </summary>
        public static string HighlightOf(Entry entry)
        {
            return entry.HighlightSlot >= 0 ? HighlightAt(entry.HighlightSlot) : String.IsNullOrEmpty(entry.HighlightColor) ? ColorTranslator.ToHtml(Highlight(entry.Color)) : entry.HighlightColor;
        }

        /// <summary>
        /// 按颜色槽取得当前皮肤的标记色。
        /// </summary>
        public static string ColorAt(int slot)
        {
            slot = Math.Max(0, slot);
            Color c = ColorTranslator.FromHtml(EventPalette.Colors[slot % 8]);
            int cycle = slot / 8;
            if (cycle == 0)
                return ColorTranslator.ToHtml(c);
            double f = Math.Max(.5, 1 - cycle * .065);
            return ColorTranslator.ToHtml(Color.FromArgb((int)(c.R * f), (int)(c.G * f), (int)(c.B * f)));
        }

        /// <summary>
        /// 应用配置到当前对象并刷新相关状态。
        /// </summary>
        public static void Apply(State state)
        {
            Current = All.FirstOrDefault(x => x.Id == state.Skin) ?? All[0];
            EventPalette = Glass ? All.FirstOrDefault(x => x.Id == state.GlassBaseSkin && !IsGlass(x.Id)) ?? All[0] : Current;
            Palette.Ink = ColorTranslator.FromHtml(Dark ? "#EEEFF3" : "#30343A");
            Palette.Muted = ColorTranslator.FromHtml(Dark ? "#B8BBC5" : "#696D76");
            Palette.Cream = ColorTranslator.FromHtml(Current.Background);
            Palette.Paper = ColorTranslator.FromHtml(Current.Paper);
            Array.Copy(EventPalette.Colors, Palette.Hex, 8);
            if (Glass) { Palette.Ink = ColorTranslator.FromHtml(Dark ? "#F7FAFC" : "#17232D"); Palette.Muted = ColorTranslator.FromHtml(Dark ? "#EDF3F7" : "#26333F"); return; }
            foreach (var e in state.Events)
                if (e.ColorSlot >= 0)
                    e.Color = ColorAt(e.ColorSlot);
            foreach (var c in state.Calendars)
                if (c.ColorSlot >= 0)
                    c.Color = ColorAt(c.ColorSlot);
        }
    }

    public class SkinDialog : Form
    {
        public string Selected;
        /// <summary>
        /// 初始化皮肤与课表导入的SkinDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public SkinDialog(string selected)
        {
            Selected = selected;
            UI.Style(this, L.T("更换皮肤"), 650, 91 + ((Skins.All.Length + 1) / 2) * 146 + 18);
            UI.Label(this, L.T("选择皮肤"), 28, 20, 590);
            UI.Label(this, L.T("背景 · 卡片 · 日程配色"), 28, 53, 590).ForeColor = Palette.Muted;
            for (int i = 0; i < Skins.All.Length; i++)
            {
                var skin = Skins.All[i];
                var tile = new SkinTile
                {
                    Theme = skin,
                    Chosen = skin.Id == selected,
                    Text = L.T(skin.Name),
                    AccessibleName = skin.Name,
                    Location = new Point(28 + i % 2 * 300, 91 + i / 2 * 146),
                    Size = new Size(284, 132),
                    Font = UiFont.New(11)
                };
                tile.Click += delegate
                {
                    Selected = skin.Id;
                    DialogResult = DialogResult.OK;
                    Close();
                };
                Controls.Add(tile);
            }
        }
    }

    public class SheetData
    {
        public bool CachedFormulas;
        public string Name;
        public List<List<string>> Rows = new List<List<string>>();
        public List<string> Merges = new List<string>();
    }

    public static class XlsxReader
    {
        static XNamespace S = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        /// <summary>
        /// 处理皮肤与课表导入中的 Xml 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static XDocument Xml(ZipArchive archive, string path)
        {
            var entry = archive.GetEntry(path);
            if (entry == null)
                throw new Exception("工作簿缺少 " + path);
            if (entry.Length > 20 * 1024 * 1024)
                throw new Exception("工作表过大，请拆分后导入。");
            using (var stream = entry.Open())
            using (var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 20000000 }))
            {
                var doc = XDocument.Load(reader);
                foreach (var element in doc.Descendants().Where(e => e.Name.NamespaceName == "http://purl.oclc.org/ooxml/spreadsheetml/main").ToList())
                    element.Name = S + element.Name.LocalName;
                foreach (var element in doc.Descendants())
                    foreach (var attr in element.Attributes().Where(a => a.Name.NamespaceName == "http://purl.oclc.org/ooxml/officeDocument/relationships").ToList())
                    {
                        element.SetAttributeValue(XName.Get(attr.Name.LocalName, "http://schemas.openxmlformats.org/officeDocument/2006/relationships"), attr.Value);
                        attr.Remove();
                    }

                return doc;
            }
        }

        /// <summary>
        /// 读取并解析本地或系统数据，调用方负责处理失败。
        /// </summary>
        public static List<SheetData> Read(string path)
        {
            if (String.Equals(Path.GetExtension(path), ".xls", StringComparison.OrdinalIgnoreCase))
                return LegacyExcel.Read(path);
            if (!String.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new Exception("请先在 Excel 中另存为 .xlsx 文件。暂不读取 .xls、加密文件或宏工作簿。");
            if (new FileInfo(path).Length > 15 * 1024 * 1024)
                throw new Exception("请使用小于 15 MB 的课表文件。");
            using (var archive = ZipFile.OpenRead(path))
            {
                var strings = new List<string>();
                if (archive.GetEntry("xl/sharedStrings.xml") != null)
                    foreach (var si in Xml(archive, "xl/sharedStrings.xml").Descendants(S + "si"))
                        strings.Add(String.Concat(si.Descendants(S + "t").Select(x => x.Value)));
                var relations = Xml(archive, "xl/_rels/workbook.xml.rels").Root.Elements().ToDictionary(x => (string)x.Attribute("Id"), x => (string)x.Attribute("Target"));
                var result = new List<SheetData>();
                foreach (var sheet in Xml(archive, "xl/workbook.xml").Descendants(S + "sheet"))
                {
                    string id = (string)sheet.Attribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"));
                    string target = relations[id].Replace('\\', '/');
                    string member = target.StartsWith("/") ? target.TrimStart('/') : "xl/" + target;
                    if (member.Contains(".."))
                        throw new Exception("不支持此工作表路径。");
                    var doc = Xml(archive, member);
                    var data = new SheetData
                    {
                        Name = (string)sheet.Attribute("name")
                    };
                    foreach (var row in doc.Descendants(S + "row"))
                    {
                        int ri = (int? )row.Attribute("r") ?? data.Rows.Count + 1;
                        if (ri > 3000)
                            throw new Exception("课表超过 3000 行。");
                        while (data.Rows.Count < ri)
                            data.Rows.Add(new List<string>());
                        foreach (var cell in row.Elements(S + "c"))
                        {
                            string reference = (string)cell.Attribute("r") ?? "A1";
                            int col = 0;
                            foreach (char ch in reference.TakeWhile(Char.IsLetter))
                                col = col * 26 + Char.ToUpperInvariant(ch) - 'A' + 1;
                            if (col > 100)
                                throw new Exception("课表超过 100 列，请只保留课表区域。");
                            while (data.Rows[ri - 1].Count < col)
                                data.Rows[ri - 1].Add("");
                            string type = (string)cell.Attribute("t"), value = (string)cell.Element(S + "v") ?? "";
                            if (type == "s")
                            {
                                int n;
                                if (!Int32.TryParse(value, out n) || n < 0 || n >= strings.Count)
                                    throw new Exception("共享文本索引无效。");
                                value = strings[n];
                            }
                            else if (type == "inlineStr")
                                value = String.Concat(cell.Descendants(S + "t").Select(t => t.Value));
                            if (cell.Element(S + "f") != null)
                            {
                                data.CachedFormulas = true;
                                if (String.IsNullOrWhiteSpace(value))
                                    throw new Exception("公式没有计算结果，请在 Excel 中计算并保存后重试，无需修改布局。");
                            }

                            data.Rows[ri - 1][col - 1] = value.Trim();
                        }
                    }

                    data.Merges = doc.Descendants(S + "mergeCell").Select(x => (string)x.Attribute("ref")).ToList();
                    result.Add(data);
                }

                return result;
            }
        }
    }

    public class CourseRow
    {
        /// <summary>课程样式：颜色之外的高亮和 emoji 设置，为空时用默认。</summary>
        public Entry Style;
        /// <summary>提前提醒的分钟数。</summary>
        public int Reminder = 15;
        public int Day;
        public DateTime? FirstDate;
        public bool Teacher;
        public TimeSpan Start, End;
        public string Class, Title, Room, Emoji, Period, EndPeriod, ChosenColor;
    }

    public static class CourseImporter
    {
        /// <summary>
        /// 处理皮肤与课表导入中的 ColorKey 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string ColorKey(CourseRow c)
        {
            return CourseKey(c.Teacher && !String.IsNullOrWhiteSpace(c.Class) ? c.Class : c.Title);
        }

        /// <summary>
        /// 处理皮肤与课表导入中的 CourseKey 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string CourseKey(string title)
        {
            return Regex.Replace(title ?? "", @"\s+", " ").Trim().ToLowerInvariant();
        }

        /// <summary>
        /// 处理皮肤与课表导入中的 Cell 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Cell(List<string> row, int i)
        {
            return i >= 0 && i < row.Count ? row[i].Trim() : "";
        }

        /// <summary>
        /// 清理文本中的无效或多余字符。
        /// </summary>
        static string Clean(string x)
        {
            return x.Replace(" ", "").Replace("\n", "").Trim();
        }

        /// <summary>
        /// 处理皮肤与课表导入中的 Day 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static int Day(string s)
        {
            s = Clean(s);
            int n;
            if (Int32.TryParse(s, out n) && n >= 1 && n <= 7)
                return n;
            string[] names =
            {
                "一",
                "二",
                "三",
                "四",
                "五",
                "六",
                "日"
            };
            for (int i = 0; i < 7; i++)
                if (s == "星期" + names[i] || s == "周" + names[i])
                    return i + 1;
            if (s == "星期天" || s == "周天")
                return 7;
            throw new Exception("星期应为周一至周日，或数字 1–7。");
        }

        /// <summary>
        /// 处理皮肤与课表导入中的 Time 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static TimeSpan Time(string s)
        {
            s = s.Trim().Replace('：', ':');
            double serial;
            if (Double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out serial) && serial >= 0 && serial < 1)
                return TimeSpan.FromMinutes(Math.Round(serial * 1440));
            DateTime time;
            if (DateTime.TryParseExact(s, new[] { "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out time))
                return time.TimeOfDay;
            throw new Exception("时间需为 08:00 格式；仅有节次时，请在课程这一行填写开始和结束时间。");
        }

        /// <summary>
        /// 解析输入为业务对象，并拒绝格式错误的数据。
        /// </summary>
        public static List<CourseRow> Parse(SheetData sheet)
        {
            var result = new List<CourseRow>();
            string[][] aliases =
            {
                new[]
                {
                    "星期",
                    "周几",
                    "星期几"
                },
                new[]
                {
                    "开始时间",
                    "开始",
                    "上课时间"
                },
                new[]
                {
                    "结束时间",
                    "结束",
                    "下课时间"
                },
                new[]
                {
                    "班级",
                    "授课班级"
                },
                new[]
                {
                    "课程",
                    "科目",
                    "课程名称"
                },
                new[]
                {
                    "教室",
                    "地点"
                },
                new[]
                {
                    "emoji",
                    "标记"
                }
            };
            int header = -1;
            int[] map = null;
            for (int i = 0; i < Math.Min(30, sheet.Rows.Count); i++)
            {
                var row = sheet.Rows[i];
                var found = aliases.Select(a => row.FindIndex(v => a.Any(k => String.Equals(Clean(v), k, StringComparison.OrdinalIgnoreCase)))).ToArray();
                if (found.Take(5).All(v => v >= 0))
                {
                    header = i;
                    map = found;
                    break;
                }
            }

            if (header >= 0)
            {
                RejectMerges(sheet, header + 1);
                for (int r = header + 1; r < sheet.Rows.Count; r++)
                {
                    var row = sheet.Rows[r];
                    if (row.All(String.IsNullOrWhiteSpace))
                        continue;
                    try
                    {
                        result.Add(Validate(new CourseRow { Day = Day(Cell(row, map[0])), Start = Time(Cell(row, map[1])), End = Time(Cell(row, map[2])), Class = Cell(row, map[3]), Title = Cell(row, map[4]), Room = Cell(row, map[5]), Emoji = Cell(row, map[6]) }));
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("第 " + (r + 1) + " 行：" + ex.Message);
                    }
                }
            }
            else
            {
                int[] days = null;
                for (int r = 0; r < Math.Min(30, sheet.Rows.Count); r++)
                {
                    int[] candidate = sheet.Rows[r].Select(v =>
                    {
                        try
                        {
                            return Day(v);
                        }
                        catch
                        {
                            return 0;
                        }
                    }).ToArray();
                    if (candidate.Count(d => d > 0) >= 3)
                    {
                        header = r;
                        days = candidate;
                        break;
                    }
                }

                if (header < 0)
                    throw new Exception("未找到课表标题。明细表需要：星期、开始时间、结束时间、班级、课程；周课表需要星期列和每行的时间范围。");
                RejectMerges(sheet, header + 1);
                for (int r = header + 1; r < sheet.Rows.Count; r++)
                {
                    var row = sheet.Rows[r];
                    if (row.All(String.IsNullOrWhiteSpace))
                        continue;
                    bool hasCourses = Enumerable.Range(0, days.Length).Any(c => days[c] > 0 && !String.IsNullOrWhiteSpace(Cell(row, c)));
                    if (!hasCourses)
                        continue;
                    var timeCell = row.Select(v => Regex.Match(v, @"(\d{1,2}[:：]\d{2})\s*[-—–~～至]\s*(\d{1,2}[:：]\d{2})")).FirstOrDefault(m => m.Success);
                    if (timeCell == null)
                        throw new Exception("第 " + (r + 1) + " 行缺少上课时间范围，例如 08:00-08:45。");
                    for (int c = 0; c < days.Length; c++)
                    {
                        string value = Cell(row, c);
                        if (days[c] == 0 || String.IsNullOrWhiteSpace(value))
                            continue;
                        string[] parts = value.Split(new[] { '|', '｜', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
                        if (parts.Length < 2 || parts.Length > 4)
                            throw new Exception("第 " + (r + 1) + " 行第 " + (c + 1) + " 列：单元格请写成 班级｜课程｜教室（教室可省略）。");
                        result.Add(Validate(new CourseRow { Day = days[c], Start = Time(timeCell.Groups[1].Value), End = Time(timeCell.Groups[2].Value), Class = parts[0], Title = parts[1], Room = parts.Length > 2 ? parts[2] : "", Emoji = parts.Length > 3 ? parts[3] : "" }));
                    }
                }
            }

            if (result.Count == 0)
                throw new Exception("没有可导入的课程。");
            return result;
        }

        /// <summary>
        /// 处理皮肤与课表导入中的 RejectMerges 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static void RejectMerges(SheetData sheet, int headerRow)
        {
            foreach (var merge in sheet.Merges)
            {
                var nums = Regex.Matches(merge, @"\d+");
                if (nums.Count > 0 && Int32.Parse(nums[nums.Count - 1].Value) > headerRow)
                    throw new Exception("课程区域存在合并单元格。请先取消合并并填全班级、课程和时间；未导入任何课程。");
            }
        }

        /// <summary>
        /// 校验数据范围和必填内容，异常时拒绝继续处理。
        /// </summary>
        public static CourseRow Validate(CourseRow c)
        {
            if (c.Day < 1 || c.Day > 7 || c.Start < TimeSpan.Zero || c.End >= TimeSpan.FromDays(1) || c.End <= c.Start)
                throw new Exception("星期或起止时间无效（不支持跨午夜课程）。");
            if (String.IsNullOrWhiteSpace(c.Title) && String.IsNullOrWhiteSpace(c.Class))
                throw new Exception("课程名或班级至少填写一项。");
            if (String.IsNullOrWhiteSpace(c.Title))
                c.Title = c.Class.Trim();
            if (String.IsNullOrWhiteSpace(c.Class))
                c.Class = "";
            return c;
        }

        /// <summary>
        /// 计算稳定标识，用于重复检测而非密码存储。
        /// </summary>
        static string Hash(string s)
        {
            using (var h = SHA256.Create())
                return BitConverter.ToString(h.ComputeHash(Encoding.UTF8.GetBytes(s))).Replace("-", "").Substring(0, 24);
        }

        /// <summary>
        /// 校验并生成课程日程，利用稳定标识避免重复导入。
        /// </summary>
        public static int Commit(State state, List<CourseRow> courses, DateTime start, int weeks, bool useEmoji)
        {
            if (weeks < 1 || weeks > 52)
                throw new Exception("重复周数需为 1–52。");
            if (courses.Count * weeks > 15000)
                throw new Exception("单次最多导入 15000 条日程，请缩短周数。");
            foreach (var c in courses)
                Validate(c);
            int added = 0;
            string batch = Guid.NewGuid().ToString("N");
            foreach (string name in courses.Select(c => ColorKey(c)).Distinct().OrderBy(x => x))
            {
                string id = "course-type-" + Hash(name);
                if (!state.Calendars.Any(c => c.Id == id))
                {
                    int slot = state.Calendars.Where(c => c.Id.StartsWith("course-type-")).Select(c => c.ColorSlot).DefaultIfEmpty(-1).Max() + 1;
                    state.Calendars.Add(new UserCalendar { Id = id, Name = name, ColorSlot = slot, Color = Skins.ColorAt(slot), Emoji = CourseIcons.For(name) });
                }
            }

            foreach (var c in courses)
            {
                var calendar = state.Calendars.First(x => x.Id == "course-type-" + Hash(ColorKey(c)));
                int selectedDay = ((int)start.DayOfWeek + 6) % 7 + 1;
                int offset = (c.Day - selectedDay + 7) % 7;
                for (int w = 0; w < weeks; w++)
                {
                    DateTime date = c.FirstDate.HasValue ? c.FirstDate.Value.Date.AddDays(7 * w) : start.Date.AddDays(offset + 7 * w);
                    string id = "course-" + Hash(calendar.Id + "|" + c.Class.Trim() + "|" + c.Title.Trim() + "|" + date.ToString("yyyy-MM-dd") + "|" + c.Start + "|" + c.End + "|" + c.Room);
                    if (state.Events.Any(e => e.Id == id))
                        continue;
                    state.Events.Add(new Entry { Id = id, ImportBatch = batch, Title = c.Teacher && !String.IsNullOrWhiteSpace(c.Class) ? c.Class.Trim() : c.Title.Trim(), ClassName = c.Teacher ? "" : c.Class.Trim(), Start = date + c.Start, End = date + c.End, CalendarId = calendar.Id, ColorSlot = String.IsNullOrEmpty(c.ChosenColor) ? calendar.ColorSlot : Array.IndexOf(Palette.Hex, c.ChosenColor), Color = String.IsNullOrEmpty(c.ChosenColor) ? calendar.Color : c.ChosenColor, Emoji = String.IsNullOrWhiteSpace(c.Emoji) ? CourseIcons.For(c.Title) : c.Emoji, UseEmoji = useEmoji && c.Emoji != "无", Notes = (c.Teacher ? "课程：" + c.Title + "\r\n" : "") + (String.IsNullOrWhiteSpace(c.Room) ? "" : "教室：" + c.Room), Reminder = c.Reminder });
                    if (c.Style != null)
                    {
                        var lesson = state.Events[state.Events.Count - 1];
                        lesson.EmojiOriginal = c.Style.EmojiOriginal;
                        lesson.EmojiTint = c.Style.EmojiTint;
                        lesson.Highlight = c.Style.Highlight;
                        lesson.HighlightColor = c.Style.HighlightColor;
                        lesson.HighlightSlot = c.Style.HighlightSlot;
                    }

                    added++;
                }
            }

            return added;
        }
    }
}

namespace PaperDays
{
    public partial class CourseImportDialog : Form, IResponsiveDialog
    {
        public List<CourseRow> Courses;
        public DateTime StartDate;
        public int Weeks;
        public bool UseEmoji;
        State existing;
        Func<State> latest;
        Func<Action, bool> apply;
        DataGridView periodGrid;
        List<SheetData> sheets;
        ComboBox sheetList;
        DataGridView grid;
        DateTimePicker start;
        NumericUpDown weeks;
        CheckBox emoji;
        Label status;
        bool updating;
        /// <summary>
        /// 初始化皮肤与课表导入的CourseImportDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public CourseImportDialog(State state, Func<State> latest = null, Func<Action, bool> apply = null)
        {
            existing = state;
            this.latest = latest ?? (() => existing);
            this.apply = apply;
            UI.Style(this, "课表管理", 960, 650);
            BuildManager();
        }

        internal DateTimePicker StartControl
        {
            /// <summary>
            /// 读取StartControl属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return start;
            }
        }

        internal DataGridView PreviewGrid
        {
            /// <summary>
            /// 读取PreviewGrid属性，供皮肤与课表导入使用。
            /// </summary>
            get
            {
                return grid;
            }
        }

        /// <summary>
        /// 载入皮肤与课表导入中的 LoadFile 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void LoadFile()
        {
            using (var file = new OpenFileDialog
            {
                Filter = "Excel 课表 (*.xlsx;*.xls)|*.xlsx;*.xls"
            }

            )
                if (WindowPlacement.Show(file, this) == DialogResult.OK)
                    LoadWorkbook(file.FileName);
        }

        /// <summary>
        /// 读取工作簿并准备课程预览，错误时保留现有日程。
        /// </summary>
        internal void LoadWorkbook(string path)
        {
            updating = true;
            grid.Rows.Clear();
            sheetList.Items.Clear();
            sheets = null;
            updating = false;
            try
            {
                sheets = XlsxReader.Read(path);
                foreach (var sheet in sheets)
                    sheetList.Items.Add(sheet.Name);
                if (sheets.Count > 0)
                    sheetList.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                status.Text = "未加载：" + ex.Message;
                Notice.Show(this, ex.Message, "课表未导入");
            }
        }

        /// <summary>
        /// 载入皮肤与课表导入中的 LoadSheet 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void LoadSheet()
        {
            if (sheets == null || sheetList.SelectedIndex < 0)
                return;
            updating = true;
            grid.Rows.Clear();
            try
            {
                var courses = SmartCourses.Parse(sheets[sheetList.SelectedIndex]);
                foreach (var c in courses)
                {
                    int n = grid.Rows.Add("周" + "一二三四五六日"[c.Day - 1], c.Start < TimeSpan.Zero ? "" : c.Start.ToString(@"hh\:mm"), c.End < TimeSpan.Zero ? "" : c.End.ToString(@"hh\:mm"), c.Class, c.Title, c.Room, c.Emoji, CourseDates.First(c, DateTime.Today).ToString("yyyy-MM-dd"));
                    grid.Rows[n].Tag = c;
                }

                LoadPeriods(courses);
            }
            catch (Exception ex)
            {
                status.Text = "未加载：" + ex.Message;
                Notice.Show(this, ex.Message, "课表识别提示");
                updating = false;
                return;
            }

            updating = false;
            UpdateStatus();
            if (sheets[sheetList.SelectedIndex].CachedFormulas)
                status.Text += "  含公式，使用上次保存的计算结果，请核对预览。";
        }

        /// <summary>
        /// 读取课程预览行并逐行校验，错误消息包含行号。
        /// </summary>
        List<CourseRow> ReadRows()
        {
            var rows = new List<CourseRow>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                Func<int, string> value = i => Convert.ToString(row.Cells[i].Value).Trim();
                try
                {
                    rows.Add(CourseImporter.Validate(new CourseRow { Day = CourseDates.Optional(value(7)).HasValue ? ((int)CourseDates.Optional(value(7)).Value.DayOfWeek + 6) % 7 + 1 : CourseImporter.Day(value(0)), Start = CourseImporter.Time(value(1)), End = CourseImporter.Time(value(2)), Class = value(3), Title = value(4), Room = value(5), Emoji = value(6), Teacher = teacherMode.Checked, FirstDate = CourseDates.Optional(value(7)), ChosenColor = value(9), Style = StyleOf(row), Reminder = CourseBook.Minutes[importReminder.SelectedIndex] }));
                }
                catch (Exception ex)
                {
                    throw new Exception("预览第 " + (row.Index + 1) + " 行：" + ex.Message);
                }
            }

            if (rows.Count == 0)
                throw new Exception("请先载入有课程的工作表。");
            return rows;
        }

        /// <summary>
        /// 更新皮肤与课表导入中的 UpdateStatus 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void UpdateStatus()
        {
            if (updating || status == null)
                return;
            existing = latest();
            try
            {
                var rows = ReadRows();
                var names = rows.Select(c => CourseImporter.ColorKey(c)).Distinct().OrderBy(n => n).ToList();
                var colors = new Dictionary<string, string>();
                int count = existing.Calendars.Where(c => c.Id.StartsWith("course-type-")).Select(c => c.ColorSlot).DefaultIfEmpty(-1).Max() + 1;
                foreach (string name in names)
                {
                    var cal = existing.Calendars.FirstOrDefault(c => c.Id.StartsWith("course-type-") && c.Name == name);
                    int slot = cal == null ? count++ : Math.Max(0, cal.ColorSlot);
                    colors[name] = cal == null ? Skins.ColorAt(slot) : cal.Color;
                }

                for (int i = 0; i < rows.Count; i++)
                {
                    var color = ColorTranslator.FromHtml(String.IsNullOrEmpty(rows[i].ChosenColor) ? colors[CourseImporter.ColorKey(rows[i])] : rows[i].ChosenColor);
                    grid.Rows[i].Cells[9].Tag = ColorTranslator.ToHtml(color);
                    grid.Rows[i].Cells[4].Style.BackColor = Color.FromArgb((color.R + 255) / 2, (color.G + 255) / 2, (color.B + 255) / 2);
                    // 自动选的图标随课程名更新；手动选过或 Excel 里填过的不覆盖。
                    if (String.IsNullOrWhiteSpace(rows[i].Emoji) || grid.Rows[i].Cells[6].Tag as string == "auto")
                    {
                        grid.Rows[i].Cells[6].Value = CourseIcons.For(rows[i].Title);
                        grid.Rows[i].Cells[6].Tag = "auto";
                    }

                    int dow = ((int)start.Value.DayOfWeek + 6) % 7 + 1;
                    grid.Rows[i].Cells[7].ToolTipText = "留空时自动从今天起安排下一次课程；可以输入或双击选择日期。";
                }

                status.Text = "范围：" + rows.Min(c => CourseDates.First(c, DateTime.Today)).ToString("yyyy-MM-dd") + " 至 " + rows.Max(c => CourseDates.First(c, DateTime.Today).AddDays(((int)weeks.Value - 1) * 7)).ToString("yyyy-MM-dd") + "；" + rows.Select(c => CourseImporter.ColorKey(c)).Distinct().Count() + " 门课程／内容，预计 " + rows.Count * (int)weeks.Value + " 条（重复项会跳过）。\n可直接编辑上方内容；按周重复，不自动处理单双周、节假日停课或调课。";
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
            }
        }

        /// <summary>
        /// 结束表格编辑并校验课程，成功后才提交导入结果。
        /// </summary>
        internal void ConfirmImport()
        {
            grid.EndEdit();
            try
            {
                var rows = ReadRows();
                var old = new HashSet<string>(latest().Events.Where(e => e.Id.StartsWith("course-")).Select(e => e.Title));
                var again = rows.Select(c => c.Teacher && !String.IsNullOrWhiteSpace(c.Class) ? c.Class.Trim() : c.Title.Trim()).Where(old.Contains).Distinct().ToList();
                if (again.Count > 0 && Notice.Show(this, "这些课程已经导入过：" + String.Join("、", again.Take(5)) + (again.Count > 5 ? " 等" : "") + "。\n时间相同的课次会跳过；如果在已导入课程里改过时间，这次会按表格时间再加一份。继续导入吗？", "重复课程", MessageBoxButtons.YesNo) != DialogResult.Yes)
                    return;
                Courses = rows;
                StartDate = DateTime.Today;
                Weeks = (int)weeks.Value;
                UseEmoji = emoji.Checked;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                Notice.Show(this, ex.Message, "请检查预览");
            }
        }
    }
}

namespace PaperDays
{

#if TESTS
    public static class FeatureTests
    {
        /// <summary>
        /// 执行FeatureTests中的Run验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        public static void Run(Action<bool, string> check, string directory)
        {
            // Minimal Open XML fixture for the importer. Never touches the user's workbook.
            string path = Path.Combine(directory, "course-fixture.xlsx");
            if (File.Exists(path))
                File.Delete(path);
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Create))
            {
                Put(zip, "xl/workbook.xml", "<workbook xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships'><sheets><sheet name='课程' sheetId='1' r:id='r1'/></sheets></workbook>");
                Put(zip, "xl/_rels/workbook.xml.rels", "<Relationships xmlns='http://schemas.openxmlformats.org/package/2006/relationships'><Relationship Id='r1' Target='worksheets/sheet1.xml'/></Relationships>");
                string[] values =
                {
                    "星期",
                    "开始时间",
                    "结束时间",
                    "班级",
                    "课程",
                    "教室",
                    "周一",
                    "08:00",
                    "08:45",
                    "一班",
                    "数学",
                    "A101",
                    "周三",
                    "09:00",
                    "09:45",
                    "二班",
                    "语文",
                    "A102"
                };
                var xml = new StringBuilder("<worksheet xmlns='http://schemas.openxmlformats.org/spreadsheetml/2006/main'><sheetData>");
                for (int r = 0; r < 3; r++)
                {
                    xml.Append("<row r='" + (r + 1) + "'>");
                    for (int c = 0; c < 6; c++)
                        xml.Append("<c r='" + (char)('A' + c) + (r + 1) + "' t='inlineStr'><is><t>" + values[r * 6 + c] + "</t></is></c>");
                    xml.Append("</row>");
                }

                xml.Append("</sheetData></worksheet>");
                Put(zip, "xl/worksheets/sheet1.xml", xml.ToString());
            }

            var sheets = XlsxReader.Read(path);
            var courses = CourseImporter.Parse(sheets[0]);
            check(courses.Count == 2 && courses[0].Class == "一班", "xlsx inline strings and structured timetable parsed");
            check(CourseImporter.Time("0.375") == TimeSpan.FromHours(9), "Excel fractional time read correctly");
            var state = new State();
            Skins.Apply(state);
            int count = CourseImporter.Commit(state, courses, new DateTime(2026, 11, 4), 3, true);
            check(count == 6 && state.Events.Min(e => e.Start.Date) == new DateTime(2026, 11, 4) && state.Events.Where(e => e.ClassName == "一班").Min(e => e.Start.Date) == new DateTime(2026, 11, 9), "midweek start excludes earlier dates and repeats correct weekdays");
            check(state.Events.GroupBy(e => e.ClassName).All(g => g.Select(e => e.Color).Distinct().Count() == 1) && state.Events.Select(e => e.Color).Distinct().Count() == 2, "class colors remain consistent and differ between classes");
            check(CourseImporter.Commit(state, courses, new DateTime(2026, 11, 4), 3, true) == 0, "reimport skips identical lessons");
            string before = state.Events[0].Color;
            state.Skin = "rose";
            Skins.Apply(state);
            check(state.Events[0].Color != before && state.Events.All(e => e.UseEmoji && !String.IsNullOrEmpty(e.Emoji)), "skin changes imported class colors and preserves emoji");
            Data.Write(state);
            check(Data.Read().Skin == "rose" && Data.Read().Events.Count == 6, "skin and imported events persist");
            var matrix = new SheetData
            {
                Name = "周表",
                Rows = new List<List<string>>
                {
                    new List<string>
                    {
                        "时间",
                        "星期一",
                        "星期二",
                        "星期三"
                    },
                    new List<string>
                    {
                        "08:00-08:45",
                        "一班｜数学｜A101",
                        "",
                        "二班｜语文"
                    }
                }
            };
            check(CourseImporter.Parse(matrix).Count == 2, "weekly matrix with explicit times parsed");
            matrix.Merges.Add("B2:B3");
            bool rejected = false;
            try
            {
                CourseImporter.Parse(matrix);
            }
            catch
            {
                rejected = true;
            }

            check(rejected, "merged course cells are rejected instead of guessed");
            matrix.Merges.Clear();
            matrix.Rows[1][0] = "第一节";
            rejected = false;
            try
            {
                CourseImporter.Parse(matrix);
            }
            catch
            {
                rejected = true;
            }

            check(rejected, "missing period times are rejected");
            foreach (var skin in Skins.All)
            {
                state.Skin = skin.Id;
                Skins.Apply(state);
                check(Palette.Hex.SequenceEqual(skin.Colors) && Palette.Paper == ColorTranslator.FromHtml(skin.Paper), "theme palette applies: " + skin.Name);
            }

            Skins.Apply(new State());
            using (var dialog = new CourseImportDialog(new State()))
            {
                dialog.Show();
                dialog.LoadWorkbook(path);
                check(!dialog.StartControl.Visible && dialog.PreviewGrid.Rows.Count == 2, "import UI automatically chooses today and shows editable preview");
                dialog.PreviewGrid.Rows[0].Cells[4].Value = "数学复习";
                Application.DoEvents();
                using (var image = new Bitmap(dialog.Width, dialog.Height))
                {
                    dialog.DrawToBitmap(image, new Rectangle(Point.Empty, dialog.Size));
                    image.Save(Path.Combine(directory, "import-preview.png"));
                }

                dialog.ConfirmImport();
                check(dialog.DialogResult == DialogResult.OK && dialog.StartDate == DateTime.Today && dialog.Courses[0].Title == "数学复习", "import UI commits automatic date and edited course preview");
            }

            using (var dialog = new SkinDialog("cream"))
            {
                dialog.Show();
                Application.DoEvents();
                using (var image = new Bitmap(dialog.Width, dialog.Height))
                {
                    dialog.DrawToBitmap(image, new Rectangle(Point.Empty, dialog.Size));
                    image.Save(Path.Combine(directory, "skin-picker-preview.png"));
                }

                dialog.Close();
            }
        }

        /// <summary>
        /// 执行FeatureTests中的Put验证；使用隔离测试数据，不修改用户的真实记录。
        /// </summary>
        static void Put(ZipArchive zip, string name, string content)
        {
            using (var stream = zip.CreateEntry(name).Open())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
                writer.Write(content);
        }
    }
#endif

}