using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PaperDays
{
    public class MonitorDialog : Form
    {
        public string SelectedDevice;
        public Button[] ScreenButtons;
        FlowLayoutPanel list;
        Label summary;
        DisplayOption[] options;
        bool live;
        /// <summary>
        /// 处理MonitorDialog中的 NameOf 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string NameOf(string device)
        {
            var options = DisplayOption.Current();
            int index = Array.FindIndex(options, o => o.Device == device);
            return index < 0 ? L.T("主屏幕（自动跟随）") : L.T("屏幕 ") + (index + 1) + (options[index].Primary ? L.T(" · 主屏幕") : "");
        }

        /// <summary>
        /// 初始化MonitorDialog的MonitorDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public MonitorDialog(string selected) : this(selected, DisplayOption.Current())
        {
            live = true;
        }

        /// <summary>
        /// 初始化MonitorDialog的MonitorDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public MonitorDialog(string selected, DisplayOption[] displays)
        {
            SelectedDevice = selected;
            options = displays;
            UI.Style(this, L.T("显示屏幕"), 660, 560);
            summary = UI.Label(this, "", 28, 22, 604);
            summary.Height = 32;
            UI.Label(this, L.T("显示 Windows 当前识别的屏幕；点击卡片即可切换。"), 28, 60, 604).ForeColor = Palette.Muted;
            list = new FlowLayoutPanel
            {
                Location = new Point(28, 108),
                Size = new Size(604, 330),
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true
            };
            Controls.Add(list);
            UI.Button(this, L.T("刷新屏幕"), 28, 458, 130, delegate
            {
                if (live)
                    options = DisplayOption.Current();
                Populate();
            });
            UI.Button(this, L.T("识别屏幕"), 174, 458, 130, delegate
            {
                Identify();
            });
            UI.Button(this, L.T("跟随主屏幕"), 320, 458, 160, delegate
            {
                SelectedDevice = "";
                DialogResult = DialogResult.OK;
                Close();
            });
            CancelButton = UI.Button(this, L.T("取消"), 500, 458, 130, delegate
            {
                Close();
            });
            UI.Label(this, "复制显示模式可能合并桌面区域；可在 Windows 显示设置中检查。", 28, 514, 604).Font = UiFont.New(9);
            Populate();
        }

        /// <summary>
        /// 处理MonitorDialog中的 Populate 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void Populate()
        {
            while (list.Controls.Count > 0)
                list.Controls[0].Dispose();
            summary.Text = "检测到 " + options.Length + L.T(" 块屏幕 · 不限制屏幕数量");
            ScreenButtons = new Button[options.Length];
            var active = DisplayOption.Resolve(SelectedDevice, options);
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                bool selected = option.Device == active.Device;
                string caption = L.T("屏幕 ") + (i + 1) + (option.Primary ? L.T(" · 主屏幕") : "") + (selected ? L.T(" · 当前") : "") + "    " + option.Bounds.Width + " × " + option.Bounds.Height + "\n" + option.Device + "  /  " + (option.Bounds.Height > option.Bounds.Width ? L.T("竖屏") : L.T("横屏")) + L.T("  /  缩放 ") + Math.Round(IconArea.Dpi(option.Bounds) * 100) + "%";
                var button = new SoftButton
                {
                    Text = caption,
                    Size = new Size(575, 88),
                    Margin = new Padding(0, 0, 0, 12),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(16, 0, 0, 0),
                    BackColor = selected ? Palette.Ink : Palette.Cream,
                    ForeColor = selected ? Palette.Paper : Palette.Ink
                };
                button.Click += delegate
                {
                    SelectedDevice = option.Device;
                    DialogResult = DialogResult.OK;
                    Close();
                };
                list.Controls.Add(button);
                ScreenButtons[i] = button;
            }
        }

        /// <summary>
        /// 处理MonitorDialog中的 Identify 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void Identify()
        {
            for (int i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var overlay = new Form
                {
                    FormBorderStyle = FormBorderStyle.None,
                    ShowInTaskbar = false,
                    TopMost = true,
                    BackColor = Palette.Ink,
                    StartPosition = FormStartPosition.Manual,
                    Bounds = new Rectangle(option.Bounds.Left + 30, option.Bounds.Top + 30, 220, 120)
                };
                overlay.Controls.Add(new Label { Dock = DockStyle.Fill, Text = L.T("屏幕 ") + (i + 1), TextAlign = ContentAlignment.MiddleCenter, ForeColor = Palette.Paper, Font = UiFont.New(28) });
                var timer = new Timer
                {
                    Interval = 2500
                };
                timer.Tick += delegate
                {
                    timer.Stop();
                    overlay.Close();
                    timer.Dispose();
                };
                overlay.Show();
                timer.Start();
            }
        }
    }
}