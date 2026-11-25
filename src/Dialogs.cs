using System.Collections.Generic;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaperDays
{
    // Render the selected value ourselves so it remains legible in native windows
    // and Windows' bitmap preview path; the menu still uses normal mouse/keyboard input.
    public class ChoiceButton : SoftButton
    {
        public System.Collections.Generic.List<object> Items = new System.Collections.Generic.List<object>();
        int selected = -1;
        public ComboBoxStyle DropDownStyle { 
            /// <summary>
            /// 读取DropDownStyle属性，供日程及设置对话框使用。
            /// </summary>
  get; 
            /// <summary>
            /// 更新DropDownStyle属性，供日程及设置对话框使用。
            /// </summary>
  set; }

        public int SelectedIndex
        {
            /// <summary>
            /// 读取SelectedIndex属性，供日程及设置对话框使用。
            /// </summary>
            get
            {
                return selected;
            }

            /// <summary>
            /// 更新SelectedIndex属性，供日程及设置对话框使用。
            /// </summary>
            set
            {
                selected = value >= 0 && value < Items.Count ? value : -1;
                Text = (selected >= 0 ? L.T(Items[selected].ToString()) : L.T("请选择")) + "   ▾";
            }
        }

        public object SelectedItem
        {
            /// <summary>
            /// 读取SelectedItem属性，供日程及设置对话框使用。
            /// </summary>
            get
            {
                return selected >= 0 && selected < Items.Count ? Items[selected] : null;
            }

            /// <summary>
            /// 更新SelectedItem属性，供日程及设置对话框使用。
            /// </summary>
            set
            {
                SelectedIndex = Items.IndexOf(value);
            }
        }

        /// <summary>
        /// 初始化日程及设置对话框的ChoiceButton，准备状态、布局与必要的事件绑定。
        /// </summary>
        public ChoiceButton()
        {
            Font = UiFont.New(10);
            Height = 31;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderColor = Color.FromArgb(211, 210, 203);
            BackColor = Skins.Button;
            ForeColor = Palette.Ink;
            TextAlign = ContentAlignment.MiddleLeft;
            Cursor = Cursors.Default;
        }

        /// <summary>
        /// 依据最长选项测量选择框宽度，并保留箭头和边距。
        /// </summary>
        public void FitContent(int maximum)
        {
            Width = Math.Min(maximum, Math.Max(86, Items.Select(x => TextRenderer.MeasureText(x.ToString(), Font).Width + 52).DefaultIfEmpty(86).Max()));
        }

        /// <summary>
        /// 响应Click事件，同步日程及设置对话框状态并保留基类行为。
        /// </summary>
        protected override void OnClick(EventArgs e)
        {
            base.OnClick(e);
            var menu = new ThemedMenu();
            for (int i = 0; i < Items.Count; i++)
            {
                int index = i;
                var item = new ToolStripMenuItem(L.T(Items[i].ToString()))
                {
                    Checked = i == selected
                };
                item.Click += delegate
                {
                    SelectedIndex = index;
                };
                menu.Items.Add(item);
            }

            menu.Closed += delegate
            {
                BeginInvoke((Action)delegate
                {
                    menu.Dispose();
                });
            };
            menu.Show(this, new Point(0, Height));
        }

        /// <summary>
        /// 响应KeyDown事件，同步日程及设置对话框状态并保留基类行为。
        /// </summary>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                SelectedIndex = Math.Min(Items.Count - 1, selected + 1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                SelectedIndex = Math.Max(0, selected - 1);
                e.Handled = true;
            }
            else
                base.OnKeyDown(e);
        }
    }

    public static class UI
    {
        /// <summary>
        /// 处理日程及设置对话框中的 Style 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static Icon appIcon;

        public static void Style(Form f, string title, int w, int h)
        {
            f.SuspendLayout();
            if (appIcon == null)
                appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            f.Icon = appIcon;
            f.Text = title;
            f.ClientSize = new Size(w, h);
            f.Font = UiFont.New(10);
            f.BackColor = Palette.Paper;
            f.ForeColor = Palette.Ink;
            f.StartPosition = FormStartPosition.CenterScreen;
            // 边框样式必须在窗口句柄创建前定好，之后再改会重建全部控件，打开窗口要多等一秒多。
            f.FormBorderStyle = FormBorderStyle.None;
            f.ControlBox = true;
            f.MaximizeBox = true;
            f.MinimizeBox = true;
            f.ShowInTaskbar = true;
            f.AutoScaleMode = AutoScaleMode.Dpi;
            WindowPlacement.Prepare(f);
            // 句柄刚创建时子控件还没有句柄，此时换字体和排版几乎没有开销。
            EventHandler install = null;
            install = delegate
            {
                f.HandleCreated -= install;
                f.ResumeLayout(false);
                Chrome.Install(f);
            };
            f.HandleCreated += install;
            f.Shown += delegate
            {
                f.Activate();
            };
        }

        /// <summary>
        /// 处理日程及设置对话框中的 Label 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static Label Label(Control p, string text, int x, int y, int width = 150)
        {
            var l = new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 25),
                ForeColor = Palette.Ink
            };
            p.Controls.Add(l);
            return l;
        }

        /// <summary>
        /// 绘制或创建可操作按钮，保留选中反馈与点击区域。
        /// </summary>
        public static Button Button(Control p, string text, int x, int y, int w, EventHandler click)
        {
            var b = new SoftButton
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Skins.Button,
                ForeColor = Palette.Ink,
                Cursor = Cursors.Default
            };
            b.FlatAppearance.BorderColor = Color.FromArgb(220, 218, 209);
            b.Click += click;
            p.Controls.Add(b);
            return b;
        }

        /// <summary>
        /// 处理日程及设置对话框中的 Input 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static TextBox Input(Control p, string value, int x, int y, int w)
        {
            var t = new TextBox
            {
                Text = value,
                Location = new Point(x, y),
                Width = w
            };
            p.Controls.Add(t);
            return t;
        }

        /// <summary>
        /// 处理日程及设置对话框中的 Prompt 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Prompt(string title, string initial = "")
        {
            using (var f = new Form())
            {
                Style(f, title, 440, 145);
                var t = Input(f, initial, 24, 24, 390);
                t.MaxLength = 120;
                var ok = Button(f, L.T("保存"), 322, 82, 92, delegate
                {
                    if (!String.IsNullOrWhiteSpace(t.Text))
                    {
                        f.DialogResult = DialogResult.OK;
                        f.Close();
                    }
                });
                f.AcceptButton = ok;
                return f.ShowDialog() == DialogResult.OK ? t.Text.Trim() : null;
            }
        }
    }

    public class ColorDot : Button
    {
        public Color DotColor;
        public bool Selected;
        /// <summary>
        /// 初始化日程及设置对话框的ColorDot，准备状态、布局与必要的事件绑定。
        /// </summary>
        public ColorDot()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Default;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        /// <summary>
        /// 绘制控件当前状态，保持主题配色和内容可见。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.Clear(Parent == null ? Palette.Paper : Parent.BackColor);
            using (var b = new SolidBrush(DotColor))
                e.Graphics.FillEllipse(b, Width * .17f, Height * .17f, Width * .66f - 1, Height * .66f - 1);
            if (Selected || Focused)
                using (var pen = new Pen(Palette.Ink, 1.4f))
                    e.Graphics.DrawEllipse(pen, 2, 2, Width - 5, Height - 5);
        }
    }

    public class Editor : Form
    {
        public Entry Result;
        public bool Deleted;
        public string ChosenColor;
        public TextBox TitleInput, EmojiInput, NotesInput;
        public DateField StartDate, EndDate;
        public TimeField StartTime, EndTime;
        public CheckBox AllDay, ShowTime, Important, Highlight;
        public RadioButton DotMode, EmojiMode;
        public ChoiceButton Zone, Reminder, Calendar;
        public Button SaveButton;
        public Func<Entry,bool> Commit;
        public Button[] ColorButtons;
        public string ChosenHighlight;
        public int HighlightSlot;
        public ChoiceButton EmojiTone;
        public CheckBox SameCourse;
        readonly List<EmojiButton> emojiButtons = new List<EmojiButton>();
        EmojiButton emojiPreview;
        public Button[] HighlightButtons;
        Panel highlightPreview;
        bool colorChosen, fixedColor;
        Entry original;
        State state;
        int[] offsets =
        {
            -1,
            0,
            5,
            15,
            30,
            60,
            1440
        };
        /// <summary>
        /// 初始化日程及设置对话框的Editor，准备状态、布局与必要的事件绑定。
        /// </summary>
        public Editor(State state, Entry entry, DateTime day, bool timeSlot = false, bool styleOnly = false)
        {
            this.state = state;
            original = entry;
            UI.Style(this, entry == null ? L.T("添加日程") : L.T("编辑日程"), 650, 826);
            Entry v = entry ?? new Entry
            {
                Start = timeSlot ? day : day.Date.AddHours(9),
                End = timeSlot ? day.AddHours(1) : day.Date.AddHours(10),
                Color = Palette.Hex[state.Events.Count % Palette.Hex.Length]
            };
            ChosenColor = v.Color;
            HighlightSlot = entry == null ? 0 : v.HighlightSlot;
            ChosenHighlight = entry == null ? Skins.HighlightAt(0) : Skins.HighlightOf(v);
            UI.Label(this, L.T("日程"), 32, 22, 580).ForeColor = Palette.Muted;
            TitleInput = UI.Input(this, v.Title, 32, 58, 586);
            TitleInput.Font = UiFont.New(11, FontStyle.Bold);
            TitleInput.BorderStyle = BorderStyle.FixedSingle;
            TitleInput.MaxLength = 100;
            UI.Label(this, L.T("开始"), 32, 123, 70);
            UI.Label(this, L.T("结束"), 32, 174, 70);
            StartDate = Date(this, v.Start, 109, 119);
            EndDate = Date(this, v.End, 109, 170);
            StartTime = Clock(this, v.Start, 322, 119);
            EndTime = Clock(this, v.End, 322, 170);
            AllDay = new CheckBox
            {
                Text = L.T("全天"),
                Checked = v.AllDay,
                Location = new Point(109, 204),
                AutoSize = true
            };
            ShowTime = new CheckBox
            {
                Text = L.T("显示时间"),
                Checked = v.ShowTime,
                Location = new Point(218, 204),
                AutoSize = true
            };
            Controls.Add(AllDay);
            Controls.Add(ShowTime);
            AllDay.CheckedChanged += delegate
            {
                UpdateTimeFields();
            };
            UpdateTimeFields();
            UI.Label(this, L.T("时区"), 32, 245, 70);
            Zone = new ChoiceButton
            {
                Location = new Point(109, 239),
                Width = 509
            };
            foreach (var z in TimeZoneInfo.GetSystemTimeZones())
                Zone.Items.Add(new ZoneItem { Zone = z });
            Zone.SelectedItem = Zone.Items.Cast<ZoneItem>().FirstOrDefault(z => z.Zone.Id == v.TimeZone);
            Controls.Add(Zone);
            UI.Label(this, L.T("日历"), 32, 277, 70);
            Calendar = new ChoiceButton
            {
                Location = new Point(109, 271),
                Width = 240
            };
            foreach (var c in state.Calendars)
                Calendar.Items.Add(new CalendarItem { Calendar = c });
            Calendar.SelectedItem = Calendar.Items.Cast<CalendarItem>().FirstOrDefault(c => c.Calendar.Id == v.CalendarId);
            if (Calendar.SelectedIndex < 0)
                Calendar.SelectedIndex = 0;
            Calendar.FitContent(270);
            Controls.Add(Calendar);
            Important = new CheckBox
            {
                Text = L.T("重要日程"),
                Checked = v.Important,
                Location = new Point(475, 278),
                AutoSize = true
            };
            Controls.Add(Important);
            var marks = new PaletteSection
            {
                Bounds = new Rectangle(26, 313, 598, 183),
                Caption = L.T("标记样式与颜色")
            };
            Controls.Add(marks);
            DotMode = new RadioButton
            {
                Text = L.T("圆点"),
                Bounds = new Rectangle(16, 32, 72, 28),
                Checked = !v.UseEmoji
            };
            EmojiMode = new RadioButton
            {
                Text = "emoji",
                Bounds = new Rectangle(98, 32, 80, 28),
                Checked = v.UseEmoji
            };
            marks.Controls.Add(DotMode);
            marks.Controls.Add(EmojiMode);
            EmojiInput = new TextBox
            {
                Text = v.Emoji,
                Bounds = new Rectangle(190, 32, 82, 28),
                Font = UiFont.New("Segoe UI Symbol", 13),
                MaxLength = 32
            };
            // 系统文本框只能把 emoji 画成一团，这里只保存值，界面上显示线条图标预览。
            FormClosed += delegate
            {
                EmojiInput.Dispose();
            };
            emojiPreview = new EmojiButton
            {
                Bounds = new Rectangle(190, 28, 36, 36),
                AccessibleName = L.T("当前 emoji，点击输入其他 emoji")
            };
            emojiPreview.Click += async delegate
            {
                string typed = await IndependentWindows.Prompt(L.T("输入其他 emoji"), EmojiInput.Text);
                if (typed != null && !IsDisposed)
                {
                    EmojiInput.Text = typed.Length > 32 ? typed.Substring(0, 32) : typed;
                    EmojiMode.Checked = true;
                }
            };
            marks.Controls.Add(emojiPreview);
            EmojiInput.TextChanged += delegate
            {
                RefreshEmojiPreview();
            };
            EmojiTone = new ChoiceButton
            {
                Bounds = new Rectangle(290, 32, 190, 28)
            };
            EmojiTone.Items.AddRange(new object[] { L.T("黑白 emoji"), L.T("标记色 emoji"), L.T("原彩 emoji") });
            EmojiTone.SelectedIndex = v.EmojiOriginal ? 2 : v.EmojiTint ? 1 : 0;
            EmojiTone.TextChanged += delegate
            {
                RefreshEmojiPreview();
            };
            marks.Controls.Add(EmojiTone);
            ColorButtons = new Button[Palette.Hex.Length];
            var colorGrid = new SwatchGrid { Bounds = new Rectangle(13, 71, 570, 36), BackColor = marks.BackColor };
            marks.Controls.Add(colorGrid);
            for (int i = 0; i < Palette.Hex.Length; i++)
            {
                int index = i;
                var dot = new ColorDot
                {
                    Bounds = new Rectangle(13 + i * 54, 71, 36, 36),
                    DotColor = ColorTranslator.FromHtml(Palette.Hex[i]),
                    AccessibleName = L.T("标记颜色 ") + (i + 1)
                };
                dot.Click += delegate
                {
                    colorChosen = true;
                    fixedColor = false;
                    TintEmoji();
                    SelectColor(Palette.Hex[index]);
                };
                colorGrid.Controls.Add(dot);
                ColorButtons[i] = dot;
            }

            var pickMark = UI.Button(marks, L.T("自选"), 470, 4, 94, delegate
            {
                using (var picker = new HexColorDialog
                {
                    Color = ColorTranslator.FromHtml(ChosenColor)
                }

                )
                    if (picker.ShowDialog(this) == DialogResult.OK)
                    {
                        colorChosen = true;
                        fixedColor = true;
                        TintEmoji();
                        SelectColor(ColorTranslator.ToHtml(picker.Color));
                    }
            });
            pickMark.Tag = "corner";
            bool lesson = styleOnly || entry != null && entry.Id.StartsWith("course-");
            string[] symbols = lesson ? CourseIcons.Emoji : new[] { "🌿", "☕", "🎂", "💻", "📚", "✈️", "🏃", "🌙", "❤️", "🎨" };
            var emojiGrid = new SwatchGrid { Bounds = new Rectangle(14, 126, 570, 40), BackColor = marks.BackColor };
            marks.Controls.Add(emojiGrid);
            for (int i = 0; i < symbols.Length; i++)
            {
                string emoji = symbols[i];
                var button = new EmojiButton
                {
                    Emoji = emoji,
                    Bounds = new Rectangle(14 + i * 55, 126, 44, 40),
                    BackColor = Skins.Button
                };
                button.Click += delegate
                {
                    EmojiInput.Text = emoji;
                    EmojiMode.Checked = true;
                };
                emojiGrid.Controls.Add(button);
                emojiButtons.Add(button);
            }

            var high = new PaletteSection
            {
                Bounds = new Rectangle(26, 506, 598, 108),
                Caption = L.T("高亮颜色（独立选择）")
            };
            Controls.Add(high);
            Highlight = new CheckBox
            {
                Text = L.T("笔刷高亮"),
                Checked = v.Highlight,
                Bounds = new Rectangle(15, 30, 130, 28)
            };
            high.Controls.Add(Highlight);
            highlightPreview = new Panel
            {
                Bounds = new Rectangle(170, 30, 160, 28)
            };
            highlightPreview.Paint += delegate (object sender, PaintEventArgs args)
            {
                if (Highlight.Checked)
                    BrushHighlight.Draw(args.Graphics, new RectangleF(0, 0, highlightPreview.Width - 4, highlightPreview.Height - 1), ChosenHighlight, true);
                TextRenderer.DrawText(args.Graphics, Highlight.Checked ? L.T("日程高亮示例") : L.T("勾选后预览"), highlightPreview.Font, new Rectangle(0, 0, highlightPreview.Width, highlightPreview.Height), Palette.Ink, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            high.Controls.Add(highlightPreview);
            Highlight.CheckedChanged += delegate
            {
                highlightPreview.Invalidate();
            };
            HighlightButtons = new Button[8];
            var highlightGrid = new SwatchGrid { Bounds = new Rectangle(13, 64, 570, 34), BackColor = high.BackColor };
            high.Controls.Add(highlightGrid);
            for (int i = 0; i < 8; i++)
            {
                int slot = i;
                var dot = new ColorDot
                {
                    Bounds = new Rectangle(13 + i * 54, 64, 34, 34),
                    DotColor = ColorTranslator.FromHtml(Skins.HighlightAt(i)),
                    Selected = HighlightSlot == i,
                    AccessibleName = L.T("高亮颜色 ") + (i + 1)
                };
                dot.Click += delegate
                {
                    SelectHighlight(slot);
                };
                highlightGrid.Controls.Add(dot);
                HighlightButtons[i] = dot;
            }

            var pickHighlight = UI.Button(high, L.T("自选"), 470, 4, 94, delegate
            {
                using (var picker = new HexColorDialog
                {
                    Color = ColorTranslator.FromHtml(ChosenHighlight)
                }

                )
                    if (picker.ShowDialog(this) == DialogResult.OK)
                    {
                        HighlightSlot = -1;
                        ChosenHighlight = ColorTranslator.ToHtml(picker.Color);
                        foreach (ColorDot dot in HighlightButtons)
                        {
                            dot.Selected = false;
                            dot.Invalidate();
                        }

                        highlightPreview.Invalidate();
                    }
            });
            pickHighlight.Tag = "corner";
            SelectColor(v.Color);
            Shown += delegate
            {
                UpdateTimeFields();
            };
            UI.Label(this, L.T("提醒"), 32, 631, 70);
            Reminder = new ChoiceButton
            {
                Location = new Point(109, 625),
                Width = 164
            };
            Reminder.Items.AddRange(new object[] { L.T("不提醒"), L.T("准时"), L.T("提前 5 分钟"), L.T("提前 15 分钟"), L.T("提前 30 分钟"), L.T("提前 1 小时"), L.T("提前 1 天") });
            Reminder.SelectedIndex = Math.Max(0, Array.IndexOf(offsets, v.Reminder));
            Reminder.FitContent(210);
            Controls.Add(Reminder);
            if (entry != null && !styleOnly && entry.Id.StartsWith("course-"))
            {
                SameCourse = new CheckBox
                {
                    Text = L.T("样式应用到这门课全部课次"),
                    Checked = true,
                    Location = new Point(300, 631),
                    AutoSize = true
                };
                Controls.Add(SameCourse);
            }

            UI.Label(this, L.T("备注"), 32, 681, 70);
            NotesInput = UI.Input(this, v.Notes, 109, 675, 509);
            NotesInput.Multiline = true;
            NotesInput.Height = 63;
            NotesInput.MaxLength = 2000;
            NotesInput.BorderStyle = BorderStyle.FixedSingle;
            SaveButton = UI.Button(this, L.T("保存日程"), 478, 765, 140, delegate
            {
                TrySave();
            });
            SaveButton.BackColor = Palette.Ink;
            SaveButton.ForeColor = Palette.Paper;
            SaveButton.FlatAppearance.BorderSize = 0;
            AcceptButton = SaveButton;
            var cancel = UI.Button(this, L.T("取消"), 364, 765, 95, delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            });
            CancelButton = cancel;
            if (entry != null)
                UI.Button(this, L.T("删除日程"), 32, 765, 110, delegate
                {
                    if (Notice.Show(this, L.T("删除这条日程？"), L.T("确认"), MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        Deleted = true;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                });
            RefreshEmojiPreview();
            if (styleOnly)
            {
                Text = L.T("课程样式 · ") + v.Title;
                var hidden = Controls.Cast<Control>().Where(c => c != marks && c != high && c != SaveButton && c != cancel).ToArray();
                foreach (var c in hidden)
                    Controls.Remove(c);
                FormClosed += delegate
                {
                    foreach (var c in hidden)
                        c.Dispose();
                };
                SaveButton.Text = L.T("应用到这门课");
                ClientSize = new Size(650, 540);
            }
        }

        /// <summary>
        /// 选了颜色就让 emoji 跟随这个颜色；圆点模式不受影响。
        /// </summary>
        void TintEmoji()
        {
            if (EmojiMode.Checked && EmojiTone.SelectedIndex != 1)
                EmojiTone.SelectedIndex = 1;
        }

        /// <summary>
        /// 让 emoji 选择按钮显示实际效果。
        /// </summary>
        void RefreshEmojiPreview()
        {
            string ink = Skins.EmojiInk(new Entry { Color = ChosenColor, EmojiTint = EmojiTone.SelectedIndex == 1 }), chosen = EmojiArt.Key(EmojiInput.Text);
            emojiPreview.Emoji = EmojiInput.Text;
            emojiPreview.Original = EmojiTone.SelectedIndex == 2;
            emojiPreview.Ink = ink;
            emojiPreview.Invalidate();
            foreach (var button in emojiButtons)
            {
                button.Original = EmojiTone.SelectedIndex == 2;
                button.Ink = ink;
                button.Selected = EmojiMode.Checked && EmojiArt.Key(button.Emoji) == chosen;
                button.Invalidate();
            }
        }

        /// <summary>
        /// 更新日程及设置对话框中的 UpdateTimeFields 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void UpdateTimeFields()
        {
            foreach (var c in new Control[]
            {
                StartTime,
                EndTime
            }

            )
            {
                if (c.Parent is FieldShell)
                    c.Parent.Visible = !AllDay.Checked;
                c.Visible = !AllDay.Checked;
            }

            ShowTime.Enabled = !AllDay.Checked;
        }

        /// <summary>
        /// 仅更新标记颜色与当前选择状态，不修改高亮颜色。
        /// </summary>
        public void SelectColor(string color)
        {
            ChosenColor = color;
            RefreshEmojiPreview();
            if (highlightPreview != null)
                highlightPreview.Invalidate();
            for (int i = 0; i < ColorButtons.Length; i++)
                if (ColorButtons[i] != null)
                {
                    var dot = (ColorDot)ColorButtons[i];
                    dot.Selected = String.Equals(color, Palette.Hex[i], StringComparison.OrdinalIgnoreCase);
                    dot.Invalidate();
                }
        }

        /// <summary>
        /// 切换高亮色槽并刷新预览，不修改圆点或 emoji 颜色。
        /// </summary>
        public void SelectHighlight(int slot)
        {
            HighlightSlot = slot;
            ChosenHighlight = Skins.HighlightAt(slot);
            for (int i = 0; i < HighlightButtons.Length; i++)
            {
                var dot = (ColorDot)HighlightButtons[i];
                dot.Selected = i == slot;
                dot.Invalidate();
            }

            highlightPreview.Invalidate();
        }

        /// <summary>
        /// 校验标题、日期、时区和时间后生成日程，保留独立标记及高亮设置。
        /// </summary>
        public bool TrySave()
        {
            if (!StartDate.CommitInput() || !EndDate.CommitInput())
            {
                Notice.Show(this, L.T("请输入有效日期，例如 2026-11-04。"));
                return false;
            }

            if (String.IsNullOrWhiteSpace(TitleInput.Text))
            {
                Notice.Show(this, L.T("请填写日程标题"));
                return false;
            }

            TimeSpan st, et;
            if (!AllDay.Checked && (!TryClock(StartTime.Text, out st) || !TryClock(EndTime.Text, out et)))
            {
                Notice.Show(this, L.T("请输入有效的24小时时间，例如 08:30 或 18:05。"));
                return false;
            }

            TryClock(StartTime.Text, out st);
            TryClock(EndTime.Text, out et);
            DateTime start = StartDate.Value.Date.Add(AllDay.Checked ? TimeSpan.FromHours(9) : st);
            DateTime end = EndDate.Value.Date.Add(AllDay.Checked ? new TimeSpan(23, 59, 0) : et);
            if (end < start)
            {
                Notice.Show(this, L.T("结束时间不能早于开始时间"));
                return false;
            }

            if (Zone.SelectedItem == null)
            {
                Notice.Show(this, L.T("请选择时区"));
                return false;
            }

            var zone = ((ZoneItem)Zone.SelectedItem).Zone;
            if (zone.IsInvalidTime(start) || zone.IsAmbiguousTime(start) || zone.IsInvalidTime(end) || zone.IsAmbiguousTime(end))
            {
                Notice.Show(this, L.T("该时间处于夏令时切换区间，请选择明确时间"));
                return false;
            }

            Result = new Entry
            {
                Id = original == null ? Guid.NewGuid().ToString() : original.Id,
                Title = TitleInput.Text.Trim(),
                Start = start,
                End = end,
                TimeZone = zone.Id,
                AllDay = AllDay.Checked,
                ShowTime = ShowTime.Checked,
                Important = Important.Checked,
                Highlight = Highlight.Checked,
                HighlightColor = ChosenHighlight,
                HighlightSlot = HighlightSlot,
                EmojiOriginal = EmojiTone.SelectedIndex == 2,
                EmojiTint = EmojiTone.SelectedIndex == 1,
                Color = ChosenColor,
                ColorSlot = colorChosen ? (fixedColor ? -1 : Array.IndexOf(Palette.Hex, ChosenColor)) : original == null ? Array.IndexOf(Palette.Hex, ChosenColor) : original.ColorSlot,
                ClassName = original == null ? "" : original.ClassName,
                UseEmoji = EmojiMode.Checked,
                Emoji = EmojiInput.Text,
                Notes = NotesInput.Text,
                Reminder = offsets[Reminder.SelectedIndex],
                CalendarId = ((CalendarItem)Calendar.SelectedItem).Calendar.Id
            };
            Result.Notified = original != null && original.Notified && original.Start == Result.Start && original.Reminder == Result.Reminder && original.TimeZone == Result.TimeZone;
            if(Commit!=null&&!Commit(Result))return false;
            DialogResult = DialogResult.OK;
            Close();
            return true;
        }

        /// <summary>
        /// 处理日程及设置对话框中的 Date 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static DateField Date(Control p, DateTime value, int x, int y)
        {
            var d = new DateField
            {
                Value = value,
                Location = new Point(x, y),
                Width = 190
            };
            p.Controls.Add(d);
            return d;
        }

        /// <summary>
        /// 按24小时格式解析时间，兼容中文与英文冒号。
        /// </summary>
        public static bool TryClock(string text, out TimeSpan value)
        {
            DateTime t;
            bool ok = DateTime.TryParseExact((text ?? "").Trim().Replace('：', ':'), new[] { "H:mm", "HH:mm" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out t);
            value = ok ? t.TimeOfDay : TimeSpan.Zero;
            return ok;
        }

        /// <summary>
        /// 处理日程及设置对话框中的 Clock 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static TimeField Clock(Control p, DateTime value, int x, int y)
        {
            var c = new TimeField
            {
                Location = new Point(x, y),
                Width = 150
            };
            for (int i = 0; i < 1440; i += 15)
                c.Items.Add((i / 60).ToString("00") + ":" + (i % 60).ToString("00"));
            c.Text = value.ToString("HH:mm");
            p.Controls.Add(c);
            return c;
        }

        class ZoneItem
        {
            public TimeZoneInfo Zone;
            /// <summary>
            /// 返回供界面显示的名称或摘要。
            /// </summary>
            public override string ToString()
            {
                return Zone.DisplayName;
            }
        }

        class CalendarItem
        {
            public UserCalendar Calendar;
            /// <summary>
            /// 返回供界面显示的名称或摘要。
            /// </summary>
            public override string ToString()
            {
                return Calendar.Name;
            }
        }
    }

    public class SettingsDialog : CompactDialog
    {
        public State Result;
        public bool Startup;
        public System.Collections.Generic.List<Entry> AddedEntries = new System.Collections.Generic.List<Entry>();
        public System.Collections.Generic.List<CalendarMark> AddedMarks = new System.Collections.Generic.List<CalendarMark>();
        ChoiceList countries, personal;
        public HashSet<string> DeletedCalendarIds=new HashSet<string>();
        CheckBox lunar;
        Label message;
        State draft;
        bool loading;
        DateTime browseMonth;
        /// <summary>
        /// 初始化日程及设置对话框的SettingsDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public SettingsDialog(State state, bool startup, DateTime? viewMonth = null, string startupError = "") : base(L.T("日历管理"),660,650)
        {
            browseMonth=viewMonth??DateTime.Today;Startup=startup;
            draft=Data.Json().Deserialize<State>(Data.Json().Serialize(state));
            var page=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(18)};Body.Controls.Add(page);
            var groups=new FlowLayoutPanel{AutoSize=true,WrapContents=true,Margin=new Padding(0)};page.Controls.Add(groups);
            var left=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0,0,14,0)};
            var right=new FlowLayoutPanel{AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Margin=new Padding(0)};groups.Controls.Add(left);groups.Controls.Add(right);
            left.Controls.Add(new Label{Text=L.T("国家 / 地区假日（可多选）"),AutoSize=true,Margin=new Padding(0,0,0,8)});
            countries=new ChoiceList{Size=new Size(280,250),Name="countries"};left.Controls.Add(countries);
            for(int i=0;i<Calendars.Codes.Length;i++)countries.Items.Add(L.Country(Calendars.Codes[i],Calendars.Names[i])+" / "+Calendars.Codes[i],draft.Countries.Contains(Calendars.Codes[i]));
            right.Controls.Add(new Label{Text=L.T("你的日历（勾选显示）"),AutoSize=true,Margin=new Padding(0,0,0,8)});
            personal=new ChoiceList{Size=new Size(280,250),Name="personal"};right.Controls.Add(personal);RefreshPersonal();
            Action<FlowLayoutPanel,string,Action> add=(parent,label,action)=>{var button=new SoftButton{Text=L.T(label),Width=270,Height=36,Margin=new Padding(0,6,0,0)};button.Click+=delegate{action();};parent.Controls.Add(button);};
            add(left,"刷新所选国家假日",async()=>await RefreshOnline());
            add(right,"＋ 新建日历",async()=>{string name=await IndependentWindows.Prompt(L.T("新建日历名称"));if(IsDisposed||String.IsNullOrWhiteSpace(name))return;SyncPersonal();draft.Calendars.Add(new UserCalendar{Name=name,Color=Palette.Hex[draft.Calendars.Count%8]});RefreshPersonal();});
            add(right,"导入 ICS…",ImportIcs);add(right,"导入 JSON 日历…",ImportJson);
            var delete=new SoftButton{Text=L.T("删除所选日历"),Width=270,Height=36,Enabled=false,Name="delete-calendar",Margin=new Padding(0,6,0,0)};right.Controls.Add(delete);
            personal.SelectionChanged+=delegate{delete.Enabled=personal.SelectedIndex>=0&&personal.SelectedIndex<draft.Calendars.Count&&draft.Calendars[personal.SelectedIndex].Id!="personal";};
            delete.Click+=delegate{
             int index=personal.SelectedIndex;if(index<0||index>=draft.Calendars.Count)return;var cal=draft.Calendars[index];
             int count=draft.Events.Count(e=>e.CalendarId==cal.Id);
             if(Notice.Show(this,L.T("删除日历并删除其中的日程？")+"\n\n"+cal.Name+" · "+count+L.T(" 条日程"),L.T("删除所选日历"),MessageBoxButtons.YesNo)==DialogResult.Yes)DeleteCalendar(cal.Id);
            };
            lunar=new CheckBox{Text=L.T("显示农历日期"),Checked=draft.Lunar,AutoSize=true,Margin=new Padding(0,12,0,8)};page.Controls.Add(lunar);
            message=new Label{Text=L.T("假日按所查看的年份联网更新，并保存离线缓存。\n中国先显示节日日期；官方放假、补班待公告更新。"),AutoSize=true,MaximumSize=new Size(580,0),Margin=new Padding(0,10,0,12)};page.Controls.Add(message);
            AddButton(L.T("应用"),delegate{if(loading){Notice.Show(this,L.T("正在刷新日历，请稍候"));return;}SyncPersonal();draft.Countries=countries.CheckedIndices.Select(i=>Calendars.Codes[i]).ToList();draft.Lunar=lunar.Checked;Result=draft;DialogResult=DialogResult.OK;Close();},true);
            CancelButton=AddButton(L.T("取消"),()=>{if(!loading)Close();});
            Action arrange=()=>{int width=Math.Max(220,page.ClientSize.Width-50);groups.Width=width;groups.MaximumSize=new Size(width,0);int col=width>=560?(width-22)/2:width-8;left.MaximumSize=right.MaximumSize=new Size(col,0);left.Width=right.Width=col;countries.Width=personal.Width=col;foreach(var panel in new[]{left,right})foreach(Control c in panel.Controls)if(c is Button)c.Width=col;message.MaximumSize=new Size(width,0);};
            ScrollActionsWith(page);page.SizeChanged+=delegate{arrange();};arrange();ApplyTheme();
        }
        internal bool DeleteCalendar(string id) {
            if(id=="personal")return false;SyncPersonal();var calendar=draft.Calendars.FirstOrDefault(c=>c.Id==id);if(calendar==null)return false;
            draft.Calendars.Remove(calendar);draft.Events.RemoveAll(e=>e.CalendarId==id);AddedEntries.RemoveAll(e=>e.CalendarId==id);DeletedCalendarIds.Add(id);RefreshPersonal();return true;
        }

        /// <summary>
        /// 处理日程及设置对话框中的 SyncPersonal 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void SyncPersonal()
        {
            for (int i = 0; i < personal.Items.Count; i++)
                draft.Calendars[i].Enabled = personal.GetItemChecked(i);
        }

        /// <summary>
        /// 刷新日程及设置对话框中的 RefreshPersonal 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void RefreshPersonal()
        {
            personal.Items.Clear();
            foreach (var c in draft.Calendars)
                personal.Items.Add(c.Name, c.Enabled);
        }

        /// <summary>
        /// 导入明确日期的 ICS 日程；不支持的重复规则明确报错。
        /// </summary>
        async void ImportIcs()
        {
            using (var f = new OpenFileDialog
            {
                Filter = "iCalendar (*.ics)|*.ics"
            }

            )
                if (WindowPlacement.Show(f, this) == DialogResult.OK)
                {
                    try
                    {
                        string name = await IndependentWindows.Prompt(L.T("导入日历名称"), Path.GetFileNameWithoutExtension(f.FileName));
                        if (name == null || IsDisposed)
                            return;
                        var cal = new UserCalendar
                        {
                            Name = name,
                            Color = Palette.Hex[draft.Calendars.Count % Palette.Hex.Length]
                        };
                        var entries = Calendars.ImportIcs(File.ReadAllText(f.FileName), cal.Id, cal.Color);
                        SyncPersonal();
                        draft.Calendars.Add(cal);
                        draft.Events.AddRange(entries);
                        AddedEntries.AddRange(entries);
                        RefreshPersonal();
                        message.Text = L.T("已导入 ") + entries.Count + L.T(" 条日程。点击「应用」保存。");
                    }
                    catch (Exception ex)
                    {
                        Notice.Show(this, ex.Message, L.T("导入未完成"));
                    }
                }
        }

        /// <summary>
        /// 导入日程及设置对话框中的 ImportJson 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void ImportJson()
        {
            using (var f = new OpenFileDialog
            {
                Filter = L.T("日历标注 (*.json)|*.json")
            }

            )
                if (WindowPlacement.Show(f, this) == DialogResult.OK)
                    try
                    {
                        var marks = Data.Json().Deserialize<System.Collections.Generic.List<CalendarMark>>(File.ReadAllText(f.FileName));
                        if (marks == null)
                            throw new Exception(L.T("日历为空"));
                        var check = new State();
                        check.Marks = marks;
                        Data.Validate(check);
                        foreach (var mark in marks)
                        {
                            draft.Marks.RemoveAll(x => x.Date == mark.Date && x.Source == mark.Source && x.Label == mark.Label);
                            draft.Marks.Add(mark);
                            AddedMarks.Add(mark);
                        }

                        message.Text = L.T("已导入 ") + marks.Count + L.T(" 条日历标注。点击「应用」保存。");
                    }
                    catch (Exception ex)
                    {
                        Notice.Show(this, ex.Message);
                    }
        }

        /// <summary>
        /// 刷新日程及设置对话框中的 RefreshOnline 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        async Task RefreshOnline()
        {
            if (loading)
                return;
            loading = true;
            var codes = countries.CheckedIndices.Cast<int>().Select(i => Calendars.Codes[i]).ToArray();
            message.Text = L.T("正在更新 ") + browseMonth.Year + L.T(" 年所选国家假日…");
            try
            {
                string errors = await HolidayUpdates.UpdateAsync(codes, browseMonth, true);
                if (!IsDisposed)
                    message.Text = String.IsNullOrEmpty(errors) ? L.T("该年份已更新，点击应用显示。") : L.T("已保留可用缓存。") + errors;
            }
            finally
            {
                loading = false;
            }
        }
    }
}

namespace PaperDays
{
    public class ZodiacDialog : Form
    {
        public string Selected;
        public Button[] SignButtons = new Button[12];
        /// <summary>
        /// 初始化日程及设置对话框的ZodiacDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public ZodiacDialog(string current)
        {
            Selected = current;
            UI.Style(this, L.T("选择你的星座"), 536, 398);
            UI.Label(this, L.T("选择星座"), 28, 23, 470).ForeColor = Palette.Muted;
            UI.Label(this, L.T("选择后查看对应的今日运势"), 28, 58, 470);
            for (int i = 0; i < Reading.Signs.Length; i++)
            {
                string sign = Reading.Signs[i];
                var button = UI.Button(this, L.T(sign), 28 + (i % 3) * 163, 111 + (i / 3) * 61, 151, delegate
                {
                    Selected = sign;
                    DialogResult = DialogResult.OK;
                    Close();
                });
                button.Height = 46;
                if (sign == current)
                {
                    button.BackColor = Palette.Ink;
                    button.ForeColor = Palette.Paper;
                }

                SignButtons[i] = button;
            }

            var cancel = UI.Button(this, L.T("取消"), 392, 359, 116, delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            });
            CancelButton = cancel;
        }
    }
}

namespace PaperDays
{
    public class DisplayOption
    {
        /// <summary>
        /// 根据用户选择解析目标对象，缺失时使用明确的回退值。
        /// </summary>
        public static DisplayOption Resolve(string device, DisplayOption[] options)
        {
            return options.FirstOrDefault(o => o.Device == device) ?? options.FirstOrDefault(o => o.Primary) ?? options.First();
        }

        public string Device;
        public Rectangle Bounds;
        public Rectangle WorkArea;
        public bool Primary;
        /// <summary>
        /// 处理日程及设置对话框中的 Current 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static DisplayOption[] Current()
        {
            return Screen.AllScreens.Select(s => new DisplayOption { Device = s.DeviceName, Bounds = s.Bounds, WorkArea = s.WorkingArea, Primary = s.Primary }).ToArray();
        }
    }
}