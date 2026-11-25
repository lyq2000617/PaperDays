using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PaperDays
{
    public static class Native
    {
        /// <summary>
        /// 调用 Windows 原生 SetProcessDpiAwarenessContext；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        /// <summary>
        /// 调用 Windows 原生 SetProcessDPIAware；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();
        /// <summary>
        /// 调用 Windows 原生 SetForegroundWindow；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr h);
        /// <summary>
        /// 处理Windows 壁纸与启动中的 Dpi 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        [DllImport("shcore.dll")]static extern int SetProcessDpiAwareness(int value);
        public static void Dpi()
        {
            try{if(SetProcessDpiAwarenessContext(new IntPtr(-4)))return;}catch(EntryPointNotFoundException){}catch(DllNotFoundException){}
            try{if(SetProcessDpiAwareness(2)==0)return;}catch(EntryPointNotFoundException){}catch(DllNotFoundException){}
            try{SetProcessDPIAware();}catch(EntryPointNotFoundException){}
        }

    }

    public class WallpaperBackup
    {
        public string Path, Style, Tile;
    }

    public static class Wallpaper
    {
        /// <summary>
        /// 调用 Windows 原生 SystemParametersInfo；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SystemParametersInfo(uint action, uint count, StringBuilder value, uint flags);
        /// <summary>
        /// 调用 Windows 原生 SystemParametersInfo；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SystemParametersInfo(uint action, uint count, string value, uint flags);
        public static string PathName
        {
            /// <summary>
            /// 读取PathName属性，供Windows 壁纸与启动使用。
            /// </summary>
            get
            {
                return Path.Combine(Data.Root, "paperdays-wallpaper.bmp");
            }
        }

        static string BackupPath
        {
            /// <summary>
            /// 读取BackupPath属性，供Windows 壁纸与启动使用。
            /// </summary>
            get
            {
                return Path.Combine(Data.Root, "wallpaper-backup-v3.json");
            }
        }

        static string acceptedPath;
        static DateTime acceptedWrite;
        /// <summary>
        /// 处理Windows 壁纸与启动中的 Current 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static string Current()
        {
            var b = new StringBuilder(32768);
            if (!SystemParametersInfo(0x73, (uint)b.Capacity, b, 0))
                throw new System.ComponentModel.Win32Exception();
            return b.ToString();
        }

        /// <summary>
        /// 检查当前壁纸是否仍属于本程序设置。
        /// </summary>
        public static bool Owned()
        {
            try
            {
                string value = Current();
                return acceptedPath != null && String.Equals(value, acceptedPath, StringComparison.OrdinalIgnoreCase) && (!File.Exists(value) || File.GetLastWriteTimeUtc(value) == acceptedWrite);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 应用配置到当前对象并刷新相关状态。
        /// </summary>
        public static void Apply(Bitmap bitmap)
        {
            Directory.CreateDirectory(Data.Root);
            string old = Current();
            if (!Owned() && !String.Equals(old, PathName, StringComparison.OrdinalIgnoreCase))
            {
                string copy = old;
                if (!String.IsNullOrEmpty(old))
                {
                    if (!File.Exists(old))
                        throw new IOException("原壁纸文件无法读取，未更换壁纸");
                    copy = Path.Combine(Data.Root, "previous-v3" + Path.GetExtension(old));
                    if (!String.Equals(old, copy, StringComparison.OrdinalIgnoreCase))
                        File.Copy(old, copy, true);
                }

                using (var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                    File.WriteAllText(BackupPath, Data.Json().Serialize(new WallpaperBackup { Path = copy, Style = (string)k.GetValue("WallpaperStyle", "10"), Tile = (string)k.GetValue("TileWallpaper", "0") }));
            }

            string temp = PathName + ".tmp";
            bitmap.Save(temp, ImageFormat.Bmp);
            if (File.Exists(PathName))
                File.Replace(temp, PathName, null);
            else
                File.Move(temp, PathName);
            SetStyle("10", "0");
            if (!SystemParametersInfo(20, 0, PathName, 3))
                throw new System.ComponentModel.Win32Exception();
            acceptedPath = Current();
            acceptedWrite = File.Exists(acceptedPath) ? File.GetLastWriteTimeUtc(acceptedPath) : DateTime.MinValue;
        }

        /// <summary>
        /// 设置Windows 壁纸与启动中的 SetStyle 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        static void SetStyle(string style, string tile)
        {
            bool change;
            using (var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop"))
                change = (string)k.GetValue("WallpaperStyle", "10") != style || (string)k.GetValue("TileWallpaper", "0") != tile;
            if (!change)
                return;
            using (var k = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true))
            {
                k.SetValue("WallpaperStyle", style);
                k.SetValue("TileWallpaper", tile);
            }
        }

        /// <summary>
        /// 依据所有权记录恢复原状态，避免覆盖其他程序的修改。
        /// </summary>
        public static void Restore()
        {
            if (!File.Exists(BackupPath))
                return;
            if (!Owned() && !String.Equals(Current(), PathName, StringComparison.OrdinalIgnoreCase))
                return;
            var b = Data.Json().Deserialize<WallpaperBackup>(File.ReadAllText(BackupPath));
            if (!String.IsNullOrEmpty(b.Path) && !File.Exists(b.Path))
                throw new IOException("原壁纸备份缺失");
            SetStyle(b.Style, b.Tile);
            if (!SystemParametersInfo(20, 0, b.Path ?? "", 3))
                throw new System.ComponentModel.Win32Exception();
            acceptedPath = null;
        }

        /// <summary>
        /// 读取当前用户是否启用了程序自启动。
        /// </summary>
        public static bool Startup()
        {
#if TESTS
            // 测试程序绝不读写真实注册表，否则会把用户的自启动项指向测试程序。
            return TestStartup;
#else
            using (var k = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
                return k != null && k.GetValue("PaperDays") != null;
#endif
        }

#if TESTS
        public static bool TestStartup;
#endif

        /// <summary>
        /// 构造带引号的程序路径与启动参数，兼容路径空格。
        /// </summary>
        public static string StartupCommand(string executable)
        {
            return "\"" + executable + "\" --startup";
        }

        /// <summary>
        /// 写入或移除当前用户自启动项，并验证写入结果。
        /// </summary>
        public static void SetStartup(bool value)
        {
#if TESTS
            TestStartup = value;
            return;
#else
            using (var k = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run"))
            {
                if (value)
                    k.SetValue("PaperDays", StartupCommand(Application.ExecutablePath));
                else
                    k.DeleteValue("PaperDays", false);
                string saved = k.GetValue("PaperDays") as string;
                if (value ? saved != StartupCommand(Application.ExecutablePath) : saved != null)
                    throw new IOException("Windows 未保存自启动设置，请重新尝试。");
            }
#endif
        }
    }

    public class DoubleClickTracker
    {
        Point last;
        uint time;
        bool previous;
        /// <summary>
        /// 处理Windows 壁纸与启动中的 Push 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public bool Push(Point point, uint ticks, int interval, Size tolerance)
        {
            bool twice = previous && unchecked(ticks - time) <= interval && Math.Abs(point.X - last.X) <= tolerance.Width && Math.Abs(point.Y - last.Y) <= tolerance.Height;
            last = point;
            time = ticks;
            previous = !twice;
            return twice;
        }

        /// <summary>
        /// 处理Windows 壁纸与启动中的 Reset 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public void Reset()
        {
            previous = false;
        }
    }
}