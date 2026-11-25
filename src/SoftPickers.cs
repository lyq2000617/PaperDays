using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace PaperDays
{
    // Owned popups: closing never disposes a ToolStrip while its click stack is active.
    public class PaperPopup : ToolStripDropDown
    {
        /// <summary>
        /// 初始化日期时间选择器的PaperPopup，准备状态、布局与必要的事件绑定。
        /// </summary>
        public PaperPopup(Control content)
        {
            AutoSize = false;
            Font = content.Font;
            Padding = new Padding(6);
            ClientSize = new Size(content.Width + 12, content.Height + 12);
            BackColor = Palette.Paper;
            Renderer = new ThemeMenuRenderer();
            DropShadowEnabled = true;
            var host = new ToolStripControlHost(content)
            {
                AutoSize = false,
                Size = content.Size,
                Font = content.Font,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            Items.Add(host);
            using (var p = Chrome.Round(new RectangleF(0, 0, Width, Height), 14))
                Region = new Region(p);
        }

        /// <summary>
        /// 响应Closed事件，同步日期时间选择器状态并保留基类行为。
        /// </summary>
        protected override void OnClosed(ToolStripDropDownClosedEventArgs e)
        {
            base.OnClosed(e);
            var timer = new Timer
            {
                Interval = 1
            };
            timer.Tick += delegate
            {
                timer.Stop();
                timer.Dispose();
                if (!IsDisposed)
                    Dispose();
            };
            timer.Start();
        }
    }

    public abstract class PaperField : UserControl
    {
        public readonly TextBox Input;
        public readonly Button Arrow;
        public PaperPopup Popup { 
            /// <summary>
            /// 读取Popup属性，供日期时间选择器使用。
            /// </summary>
  get; 
            /// <summary>
            /// 更新Popup属性，供日期时间选择器使用。
            /// </summary>
  private set; }

        /// <summary>
        /// 初始化日期时间选择器的PaperField，准备状态、布局与必要的事件绑定。
        /// </summary>
        protected PaperField()
        {
            DoubleBuffered = true;
            Height = 34;
            BackColor = Palette.Paper;
            Input = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Skins.Field,
                ForeColor = Palette.Ink
            };
            Arrow = new SoftButton
            {
                Text = "⌄",
                TabStop = false,
                BackColor = Skins.Field,
                ForeColor = Palette.Ink
            };
            Controls.Add(Input);
            Controls.Add(Arrow);
            Arrow.Click += delegate
            {
                OpenPicker();
            };
            Input.KeyDown += delegate (object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.F4 || (e.Alt && e.KeyCode == Keys.Down))
                {
                    OpenPicker();
                    e.SuppressKeyPress = true;
                }
            };
            Input.TextChanged += delegate
            {
                OnTextChanged(EventArgs.Empty);
            };
            SizeChanged += delegate
            {
                Place();
            };
            FontChanged += delegate
            {
                Place();
            };
            Place();
        }

        /// <summary>
        /// 处理日期时间选择器中的 Place 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void Place()
        {
            if (Input == null)
                return;
            Input.SetBounds(10, Math.Max(5, (Height - Input.PreferredHeight) / 2), Math.Max(12, Width - 43), Input.PreferredHeight);
            Arrow.SetBounds(Width - 31, 3, 27, Height - 6);
        }

        public override string Text
        {
            /// <summary>
            /// 读取Text属性，供日期时间选择器使用。
            /// </summary>
            get
            {
                return Input == null ? "" : Input.Text;
            }

            /// <summary>
            /// 更新Text属性，供日期时间选择器使用。
            /// </summary>
            set
            {
                if (Input != null)
                    Input.Text = value;
            }
        }

        /// <summary>
        /// 绘制控件当前状态，保持主题配色和内容可见。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using (var p = Chrome.Round(new RectangleF(0, 0, Width - 1, Height - 1), 10))
            using (var b = new SolidBrush(Skins.Field))
            {
                e.Graphics.FillPath(b, p);
            }
        }

        /// <summary>
        /// 声明 OpenPicker 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        public abstract void OpenPicker();
        /// <summary>
        /// 显示日期时间选择器中的 ShowPicker 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        protected void ShowPicker(Control body)
        {
            if (Popup != null && !Popup.IsDisposed)
            {
                Popup.Close();
                Popup = null;
            }

            var popup = new PaperPopup(body);
            Popup = popup;
            popup.Closed += delegate
            {
                if (Popup == popup)
                    Popup = null;
            };
            var area = Screen.FromControl(this).WorkingArea;
            Point below = PointToScreen(new Point(0, Height+5));
            if(below.Y+popup.Height>area.Bottom) below.Y=PointToScreen(Point.Empty).Y-popup.Height-5;
            below.X=Math.Max(area.Left,Math.Min(below.X,area.Right-popup.Width));
            below.Y=Math.Max(area.Top,Math.Min(below.Y,area.Bottom-popup.Height));
            popup.Show(below);
        }

        /// <summary>
        /// 关闭选择面板并将焦点返回输入框。
        /// </summary>
        protected void ClosePicker()
        {
            if (Popup != null && !Popup.IsDisposed)
                Popup.Close();
            Input.Focus();
        }

        /// <summary>
        /// 释放控件、计时器、图形和系统资源，并调用基类清理。
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && Popup != null && !Popup.IsDisposed)
                Popup.Close();
            base.Dispose(disposing);
        }
    }

    public class DateField : PaperField
    {
        DateTime value = DateTime.Today;
        public event EventHandler ValueChanged;
        public DateTime Value
        {
            /// <summary>
            /// 读取Value属性，供日期时间选择器使用。
            /// </summary>
            get
            {
                return value;
            }

            /// <summary>
            /// 更新Value属性，供日期时间选择器使用。
            /// </summary>
            set
            {
                var next = value.Date;
                if (this.value == next)
                {
                    Text = next.ToString("yyyy-MM-dd");
                    return;
                }

                this.value = next;
                Text = next.ToString("yyyy-MM-dd");
                if (ValueChanged != null)
                    ValueChanged(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 初始化日期时间选择器的DateField，准备状态、布局与必要的事件绑定。
        /// </summary>
        public DateField()
        {
            Width = 160;
            Text = value.ToString("yyyy-MM-dd");
            Input.Leave += delegate
            {
                if (!CommitInput())
                    Input.ForeColor = Color.FromArgb(170, 65, 65);
            };
            Input.TextChanged += delegate
            {
                Input.ForeColor = Palette.Ink;
            };
        }

        /// <summary>
        /// 校验手动输入并提交有效值，失败不覆盖原值。
        /// </summary>
        public bool CommitInput()
        {
            DateTime parsed;
            if (!DateTime.TryParseExact(Text.Trim(), new[] { "yyyy-MM-dd", "yyyy-M-d", "yyyy/MM/dd", "yyyy/M/d" }, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
                return false;
            Value = parsed;
            return true;
        }

        /// <summary>
        /// 打开当前字段的选择面板，保留手动输入。
        /// </summary>
        public override void OpenPicker()
        {
            CommitInput();
            ShowPicker(new CalendarCard(Value, delegate (DateTime day)
            {
                Value = day;
                ClosePicker();
            }));
        }
    }

    public class CalendarCard : Panel
    {
        DateTime month, selected;
        Action<DateTime> choose;
        Label heading;
        Panel days;
        /// <summary>
        /// 初始化日期时间选择器的CalendarCard，准备状态、布局与必要的事件绑定。
        /// </summary>
        public CalendarCard(DateTime date, Action<DateTime> choose)
        {
            this.choose = choose;
            selected = date;
            month = new DateTime(date.Year, date.Month, 1);
            Size = new Size(302, 342);
            BackColor = Palette.Paper;
            Font = UiFont.New(10);
            heading = new Label
            {
                Bounds = new Rectangle(60, 14, 182, 30),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(heading);
            UI.Button(this, "‹", 12, 12, 36, delegate
            {
                if (month.Year > 1 || month.Month > 1)
                {
                    month = month.AddMonths(-1);
                    DrawDays();
                }
            });
            UI.Button(this, "›", 254, 12, 36, delegate
            {
                if (month.Year < 9999 || month.Month < 12)
                {
                    month = month.AddMonths(1);
                    DrawDays();
                }
            });
            for (int i = 0; i < 7; i++)
                Controls.Add(new Label { Text = L.ShortDay(i), Bounds = new Rectangle(12 + i * 40, 55, 38, 25), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Palette.Muted });
            days = new Panel
            {
                Bounds = new Rectangle(12, 84, 280, 216)
            };
            Controls.Add(days);
            UI.Button(this, L.T("回到今天"), 76, 303, 150, delegate
            {
                choose(DateTime.Today);
            });
            DrawDays();
        }

        /// <summary>
        /// 按当前月份重建日期选择按钮并标记选中日期。
        /// </summary>
        void DrawDays()
        {
            while (days.Controls.Count > 0)
            {
                var old = days.Controls[0];
                days.Controls.Remove(old);
                old.Dispose();
            }

            heading.Text = L.Date(month, "yyyy 年 M 月", "MMMM yyyy");
            int offset = ((int)month.DayOfWeek + 6) % 7;
            for (int n = 1; n <= DateTime.DaysInMonth(month.Year, month.Month); n++)
            {
                DateTime date = month.AddDays(n - 1);
                int index = offset + n - 1;
                var b = UI.Button(days, n.ToString(), index % 7 * 40, index / 7 * 36, 36, delegate
                {
                    choose(date);
                });
                b.Height = 32;
                b.AccessibleName = date.ToString("yyyy-MM-dd");
                b.BackColor = date == selected ? Skins.Highlight(Palette.Hex[0]) : Palette.Paper;
                b.ForeColor = date == DateTime.Today ? Skins.Accent : Palette.Ink;
            }
        }
    }

    public class TimeField : PaperField
    {
        public List<string> Items = new List<string>();
        public ComboBoxStyle DropDownStyle
        {
            /// <summary>
            /// 读取DropDownStyle属性，供日期时间选择器使用。
            /// </summary>
            get
            {
                return ComboBoxStyle.DropDown;
            }
        }

        /// <summary>
        /// 初始化日期时间选择器的TimeField，准备状态、布局与必要的事件绑定。
        /// </summary>
        public TimeField()
        {
            Width = 142;
            Input.MaxLength = 5;
        }

        /// <summary>
        /// 打开当前字段的选择面板，保留手动输入。
        /// </summary>
        public override void OpenPicker()
        {
            int rowHeight = Math.Max(32, TextRenderer.MeasureText("23:55", Font).Height + 12);
            int cellWidth = Math.Max(70, TextRenderer.MeasureText("23:55", Font).Width + 24);
            int popupWidth = Math.Min(Screen.FromControl(this).WorkingArea.Width-32, (cellWidth+6)*3+40);
            int listTop = Math.Max(43, Font.Height+24);
            int popupHeight = Math.Min(Screen.FromControl(this).WorkingArea.Height-40, (rowHeight+6)*6+listTop+8);
            var panel = new Panel
            {
                Size = new Size(popupWidth, popupHeight),
                BackColor = Palette.Paper,
                Font = Font
            };
            var heading = UI.Label(panel, L.T("选择时间 · 可直接输入"), 12, 10, popupWidth-24);
            heading.ForeColor = Palette.Muted; heading.Height = Font.Height+8;
            var list = new FlowLayoutPanel
            {
                Bounds = new Rectangle(8, listTop, popupWidth-16, popupHeight-listTop-8),
                AutoScroll = true,
                WrapContents = true,
                BackColor = Palette.Paper
            };
            panel.Controls.Add(list);
            Control selected = null;
            foreach (string item in Items)
            {
                string time = item;
                var b = UI.Button(list, time, 0, 0, cellWidth, delegate
                {
                    Text = time;
                    ClosePicker();
                });
                b.Margin = new Padding(3);
                b.Font = Font;
                b.Height = rowHeight;
                if (time == Text.Replace('：', ':'))
                {
                    b.BackColor = Skins.Highlight(Palette.Hex[0]);
                    selected = b;
                }
            }

            ShowPicker(panel);
            if (selected != null)
                list.AutoScrollPosition = new Point(0, Math.Max(0, ((selected.Top-3)/(rowHeight+6)-2)*(rowHeight+6)));
        }
    }
}