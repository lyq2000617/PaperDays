using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{
    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDesktopWallpaper
    {
        /// <summary>
        /// 声明 SetWallpaper 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        void SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitor, [MarshalAs(UnmanagedType.LPWStr)] string path);
        /// <summary>
        /// 声明 GetWallpaper 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        void GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitor, [MarshalAs(UnmanagedType.LPWStr)] out string path);
        /// <summary>
        /// 声明 GetMonitorDevicePathAt 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        void GetMonitorDevicePathAt(uint index, [MarshalAs(UnmanagedType.LPWStr)] out string path);
        /// <summary>
        /// 声明 GetMonitorDevicePathCount 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        void GetMonitorDevicePathCount(out uint count);
        /// <summary>
        /// 声明 GetMonitorRECT 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        void GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitor, out NativeRect rect);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NativeRect
    {
        public int Left, Top, Right, Bottom;
        public Rectangle Bounds
        {
            /// <summary>
            /// 读取Bounds属性，供多屏壁纸管理使用。
            /// </summary>
            get
            {
                return Rectangle.FromLTRB(Left, Top, Right, Bottom);
            }
        }
    }

    public interface IWallpaperBackend : IDisposable
    {
        /// <summary>
        /// 声明 Find 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        string Find(Rectangle bounds);
        /// <summary>
        /// 声明 Get 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        string Get(string monitor);
        /// <summary>
        /// 声明 Set 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
        /// </summary>
        void Set(string monitor, string path);
    }

    public sealed class WallpaperBackend : IWallpaperBackend
    {
        IDesktopWallpaper api = (IDesktopWallpaper)Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD")));
        /// <summary>
        /// 查找多屏壁纸管理中的 Find 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public string Find(Rectangle bounds)
        {
            uint count;
            api.GetMonitorDevicePathCount(out count);
            for (uint i = 0; i < count; i++)
            {
                string id;
                NativeRect r;
                api.GetMonitorDevicePathAt(i, out id);
                api.GetMonitorRECT(id, out r);
                if (r.Bounds == bounds)
                    return id;
            }

            throw new IOException("没有找到所选屏幕的壁纸接口，未更改背景。");
        }

        /// <summary>
        /// 读取缓存或本地图形资源；缺失时返回空值供调用方回退。
        /// </summary>
        public string Get(string monitor)
        {
            string path;
            api.GetWallpaper(monitor, out path);
            return path ?? "";
        }

        /// <summary>
        /// 设置多屏壁纸管理中的 Set 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public void Set(string monitor, string path)
        {
            if (String.IsNullOrEmpty(monitor))
                throw new ArgumentException("必须指定屏幕，禁止覆盖全部屏幕");
            api.SetWallpaper(monitor, path ?? "");
        }

        /// <summary>
        /// 释放控件、计时器、图形和系统资源，并调用基类清理。
        /// </summary>
        public void Dispose()
        {
            if (api != null)
            {
                Marshal.ReleaseComObject(api);
                api = null;
            }
        }
    }

    public class MonitorBackgroundRecord
    {
        public string Monitor, Original, Owned;
    }

    public sealed class MonitorBackground
    {
        readonly string directory;
        readonly Func<IWallpaperBackend> factory;
        /// <summary>
        /// 初始化多屏壁纸管理的MonitorBackground，准备状态、布局与必要的事件绑定。
        /// </summary>
        public MonitorBackground(string directory, Func<IWallpaperBackend> factory)
        {
            this.directory = directory;
            this.factory = factory;
        }

        string Journal
        {
            /// <summary>
            /// 读取Journal属性，供多屏壁纸管理使用。
            /// </summary>
            get
            {
                return Path.Combine(directory, "monitor-background-v11.json");
            }
        }

        /// <summary>
        /// 读取并解析本地或系统数据，调用方负责处理失败。
        /// </summary>
        List<MonitorBackgroundRecord> Read()
        {
            return File.Exists(Journal) ? Data.Json().Deserialize<List<MonitorBackgroundRecord>>(File.ReadAllText(Journal)) : new List<MonitorBackgroundRecord>();
        }

        /// <summary>
        /// 保存多屏壁纸管理中的 Save 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        void Save(List<MonitorBackgroundRecord> rows)
        {
            Directory.CreateDirectory(directory);
            string temp = Journal + ".tmp";
            File.WriteAllText(temp, Data.Json().Serialize(rows));
            if (File.Exists(Journal))
                File.Replace(temp, Journal, Journal + ".bak");
            else
                File.Move(temp, Journal);
        }

        /// <summary>
        /// 应用配置到当前对象并刷新相关状态。
        /// </summary>
        public void Apply(Rectangle bounds, Color color)
        {
            using (var backend = factory())
            {
                string monitor = backend.Find(bounds), current = backend.Get(monitor);
                var rows = Read();
                var row = rows.FirstOrDefault(r => r.Monitor == monitor);
                if (row == null)
                {
                    row = new MonitorBackgroundRecord
                    {
                        Monitor = monitor
                    };
                    rows.Add(row);
                }

                if (String.IsNullOrEmpty(row.Owned) || !String.Equals(current, row.Owned, StringComparison.OrdinalIgnoreCase))
                {
                    Directory.CreateDirectory(directory);
                    row.Original = current;
                    // 恢复后系统壁纸就是这里的备份；同一张图只留一份，不再每次复制。
                    if (File.Exists(current) && !IsBackup(current))
                    {
                        string backup = Path.Combine(directory, "original-monitor-" + Fingerprint(current) + Path.GetExtension(current));
                        if (!File.Exists(backup))
                            File.Copy(current, backup);
                        row.Original = backup;
                    }
                }

                Directory.CreateDirectory(directory);
                string name = Path.Combine(directory, "background-" + bounds.Width + "x" + bounds.Height + "-" + color.ToArgb().ToString("X8") + ".bmp");
                if (!File.Exists(name))
                    using (var bitmap = new Bitmap(bounds.Width, bounds.Height))
                    {
                        using (var g = Graphics.FromImage(bitmap))
                            g.Clear(color);
                        bitmap.Save(name, ImageFormat.Bmp);
                    }

                string oldOwned = row.Owned;
                row.Owned = name;
                Save(rows);
                try
                {
                    backend.Set(monitor, name);
                }
                catch
                {
                    row.Owned = oldOwned;
                    Save(rows);
                    throw;
                }

                Tidy(backend, rows);
            }
        }

        /// <summary>
        /// 判断文件是否已经是程序目录里的备份。
        /// </summary>
        bool IsBackup(string path)
        {
            return String.Equals(Path.GetDirectoryName(Path.GetFullPath(path)), Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 计算文件内容指纹。
        /// </summary>
        static string Fingerprint(string path)
        {
            using (var sha = System.Security.Cryptography.SHA1.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }

        /// <summary>
        /// 清理缓存：换过皮肤留下的纯色图可随时重建，壁纸备份只删内容完全相同的重复文件。
        /// 只要有一块屏幕读不到当前壁纸就不清理，避免删到正在使用的文件。
        /// </summary>
        void Tidy(IWallpaperBackend backend, List<MonitorBackgroundRecord> rows)
        {
            try
            {
                var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var row in rows)
                {
                    keep.Add(row.Original ?? "");
                    keep.Add(row.Owned ?? "");
                }

                foreach (var screen in Screen.AllScreens)
                    keep.Add(backend.Get(backend.Find(screen.Bounds)) ?? "");
                foreach (string file in Directory.GetFiles(directory, "background-*.bmp"))
                    if (!keep.Contains(file))
                        File.Delete(file);
                var kept = new HashSet<string>(keep.Where(File.Exists).Where(IsBackup).Where(p => !p.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)).Select(Fingerprint));
                foreach (string file in Directory.GetFiles(directory, "original-monitor-*"))
                    if (!keep.Contains(file) && !kept.Add(Fingerprint(file)))
                        File.Delete(file);
            }
            catch (Exception ex)
            {
                StartupTiming.Mark("Background cache not tidied: " + ex.GetType().Name);
            }
        }

        /// <summary>
        /// 依据所有权记录恢复原状态，避免覆盖其他程序的修改。
        /// </summary>
        public void Restore()
        {
            var rows = Read();
            if (rows.Count == 0)
                return;
            using (var backend = factory())
            {
                var pending = new List<MonitorBackgroundRecord>();
                var failures = new List<string>();
                foreach (var row in rows)
                {
                    try
                    {
                        if (!String.IsNullOrEmpty(row.Owned) && String.Equals(backend.Get(row.Monitor), row.Owned, StringComparison.OrdinalIgnoreCase))
                            backend.Set(row.Monitor, row.Original);
                    }
                    catch (Exception ex)
                    {
                        pending.Add(row);
                        failures.Add(ex.Message);
                    }
                }

                Save(pending);
                if (failures.Count > 0)
                    throw new IOException("部分屏幕背景尚未恢复，已保留恢复记录：" + String.Join("；", failures));
            }
        }
    }

    public static class IconArea
    {
        /// <summary>
        /// 调用 Windows 原生 FindWindowEx；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string name, string title);
        /// <summary>
        /// 调用 Windows 原生 SendMessageTimeout；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SendMessageTimeout(IntPtr h, uint msg, IntPtr w, IntPtr l, uint flags, uint timeout, out UIntPtr result);
        /// <summary>
        /// 调用 Windows 原生 MonitorFromPoint；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromPoint(Point point, uint flags);
        /// <summary>
        /// 调用 Windows 原生 GetDpiForMonitor；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("shcore.dll")]
        static extern int GetDpiForMonitor(IntPtr monitor, int type, out uint x, out uint y);
        /// <summary>
        /// 处理多屏壁纸管理中的 Dpi 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static float Dpi(Rectangle bounds)
        {
            try
            {
                uint x, y;
                if (GetDpiForMonitor(MonitorFromPoint(new Point(bounds.Left + 1, bounds.Top + 1), 2), 0, out x, out y) == 0)
                    return x / 96f;
            }
            catch
            {
            }

            return 1;
        }

        /// <summary>
        /// 处理多屏壁纸管理中的 Spacing 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static int Spacing(IntPtr desktop, float dpi)
        {
            IntPtr list = FindWindowEx(desktop, IntPtr.Zero, "SysListView32", null);
            UIntPtr value;
            if (list != IntPtr.Zero && SendMessageTimeout(list, 0x1033, IntPtr.Zero, IntPtr.Zero, 2, 300, out value) != IntPtr.Zero)
            {
                int spacing = (int)(value.ToUInt64() & 65535);
                if (spacing >= 40 && spacing < 600)
                    return spacing;
            }

            return (int)(96 * dpi);
        }

        /// <summary>
        /// 处理多屏壁纸管理中的 Width 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static int Width(int screenWidth, int columns, int spacing, float dpi)
        {
            return Math.Max(0, Math.Min((int)(screenWidth * .45f), columns <= 0 ? 0 : columns * spacing + (int)(16 * dpi)));
        }

        /// <summary>
        /// 处理多屏壁纸管理中的 For 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static int For(DisplayOption monitor, int count, int setting, IntPtr desktop)
        {
            float dpi = Dpi(monitor.Bounds);
            int columns = setting < 0 ? 2 : setting;
            return Width(monitor.Bounds.Width, columns, Spacing(desktop, dpi), dpi);
        }
    }

    public class IconAreaDialog : Form
    {
        public int Columns;
        /// <summary>
        /// 初始化多屏壁纸管理的IconAreaDialog，准备状态、布局与必要的事件绑定。
        /// </summary>
        public IconAreaDialog(int selected, string status = "", Func<string> report = null)
        {
            Columns = selected;
            UI.Style(this, "桌面图标留白", 470, 515);
            UI.Label(this, "为左侧 Windows 图标预留空间", 28, 23, 414);
            UI.Label(this, "启动默认自动识别；手动设置仅本次运行生效。", 28, 63, 414).ForeColor = Palette.Muted;
            string[] names =
            {
                "自动 · 根据桌面图标位置",
                "不预留",
                "一列图标",
                "两列图标",
                "三列图标",
                "四列图标"
            };
            int[] values =
            {
                -1,
                0,
                1,
                2,
                3,
                4
            };
            for (int i = 0; i < names.Length; i++)
            {
                int value = values[i];
                var b = UI.Button(this, names[i] + (value == selected ? "  ✓" : ""), 28, 110 + i * 41, 414, delegate
                {
                    Columns = value;
                    DialogResult = DialogResult.OK;
                    Close();
                });
                b.TextAlign = ContentAlignment.MiddleLeft;
            }

            var note = UI.Label(this, String.IsNullOrEmpty(status) ? "自动读取所选屏幕左侧图标；中央分散图标不会扩大留白。" : status, 28, 365, 414);
            note.Height = 70;
            note.Font = UiFont.New(9);
            if (report != null)
            {
                note.Text = status.Split(new[] { (char)13, (char)10 })[0];
                UI.Button(this, "查看检测结果", 28, 453, 190, async delegate
                {
                    using (var f = new Form())
                    {
                        UI.Style(f, "图标检测结果", 650, 520);
                        var box = new TextBox
                        {
                            Multiline = true,
                            ReadOnly = true,
                            ScrollBars = ScrollBars.Both,
                            WordWrap = false,
                            Location = new Point(24, 24),
                            Size = new Size(600, 410),
                            Text = report(),
                            BackColor = Palette.Paper,
                            ForeColor = Palette.Ink,
                            BorderStyle = BorderStyle.None
                        };
                        f.Controls.Add(box);
                        UI.Button(f, "复制诊断", 24, 456, 160, delegate
                        {
                            try
                            {
                                Clipboard.SetText(report());
                            }
                            catch (Exception ex)
                            {
                                Notice.Show(ex.Message);
                            }
                        });
                        UI.Button(f, "刷新结果", 205, 456, 160, delegate
                        {
                            box.Text = report();
                        });
                        UI.Button(f, "关闭", 480, 456, 144, delegate
                        {
                            f.Close();
                        });
                        await IndependentWindows.Show(f);
                    }
                });
            }
        }
    }
}