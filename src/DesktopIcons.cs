using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Accessibility;

namespace PaperDays
{
    public class IconSnapshot
    {
        public bool Success;
        public List<Rectangle> Bounds = new List<Rectangle>();
        public string Error, Method;
    }

    public static class DesktopIcons
    {
        /// <summary>
        /// 调用 Windows 原生 FindWindowEx；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        /// <summary>
        /// 调用 Windows 原生 IsWindowVisible；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr window);
        /// <summary>
        /// 调用 Windows 原生 AccessibleObjectFromWindow；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("oleacc.dll")]
        static extern int AccessibleObjectFromWindow(IntPtr window, uint id, ref Guid iid, [MarshalAs(UnmanagedType.Interface)] out IAccessible obj);
        /// <summary>
        /// 调用 Windows 原生 AccessibleChildren；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("oleacc.dll")]
        static extern int AccessibleChildren(IAccessible parent, int start, int count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] object[] children, out int obtained);
        /// <summary>
        /// 读取并解析本地或系统数据，调用方负责处理失败。
        /// </summary>
        public static IconSnapshot Read()
        {
            var shell = ShellIconReader.Read();
            if (shell.Success)
                return shell;
            var accessible = ReadAccessible();
            if (accessible.Success)
                return accessible;
            accessible.Error = shell.Error + "；" + accessible.Error;
            return accessible;
        }

        /// <summary>
        /// 通过系统辅助功能接口读取桌面图标位置。
        /// </summary>
        public static IconSnapshot ReadAccessible()
        {
            var result = new IconSnapshot();
            IAccessible root = null;
            try
            {
                var desktop = DesktopSurface.FindDesktop();
                if (desktop == IntPtr.Zero)
                    throw new Exception("未找到桌面");
                var list = FindWindowEx(desktop, IntPtr.Zero, "SysListView32", null);
                if (list == IntPtr.Zero)
                    throw new Exception("未找到桌面图标列表");
                if (!IsWindowVisible(list))
                    throw new Exception("辅助功能找到的图标列表不可见，不能据此判断没有图标");
                var iid = new Guid("618736E0-3C3D-11CF-810C-00AA00389B71");
                int hr = AccessibleObjectFromWindow(list, 0xFFFFFFFC, ref iid, out root);
                if (hr != 0 || root == null)
                    throw new Exception("辅助功能读取失败 (0x" + hr.ToString("X8") + ")");
                int count = root.accChildCount;
                if (count > 4000)
                    throw new Exception("图标过多，请手动设置留白");
                var children = new object[count];
                int obtained = 0;
                if (count > 0 && AccessibleChildren(root, 0, count, children, out obtained) < 0)
                    throw new Exception("无法读取图标列表");
                if (obtained != count)
                    throw new Exception("图标枚举不完整，保留上次结果");
                for (int i = 0; i < obtained; i++)
                {
                    object child = children[i];
                    if (child == null)
                        continue;
                    IAccessible item = child as IAccessible;
                    try
                    {
                        int x, y, w, h;
                        var owner = item ?? root;
                        object id = item == null ? child : (object)0;
                        int state = Convert.ToInt32(owner.get_accState(id));
                        if ((state & 0x8000) != 0)
                            continue;
                        owner.accLocation(out x, out y, out w, out h, id);
                        if (w > 0 && h > 0)
                            result.Bounds.Add(new Rectangle(x, y, w, h));
                    }
                    finally
                    {
                        if (item != null && Marshal.IsComObject(item))
                            Marshal.ReleaseComObject(item);
                    }
                }

                if (count > 0 && result.Bounds.Count == 0)
                    throw new Exception("图标存在但没有返回位置");
                result.Success = true;
                result.Method = "Windows 辅助功能";
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
            }
            finally
            {
                if (root != null && Marshal.IsComObject(root))
                    Marshal.ReleaseComObject(root);
            }

            return result;
        }

        /// <summary>
        /// 在独立后台线程读取系统数据，避免阻塞界面。
        /// </summary>
        public static Task<IconSnapshot> ReadAsync()
        {
            var completion = new TaskCompletionSource<IconSnapshot>();
            var thread = new Thread(delegate ()
            {
                completion.SetResult(Read());
            })
            {
                IsBackground = true,
                Name = "Desktop icon positions"
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            return completion.Task;
        }

        /// <summary>
        /// 处理桌面图标检测中的 Reserve 业务步骤，保持调用方约定的输入与返回结果。
        /// </summary>
        public static int Reserve(Rectangle screen, IEnumerable<Rectangle> icons, float dpi)
        {
            int right = 0, limit = (int)(screen.Width * .45);
            foreach (var icon in icons)
            {
                if (!screen.IntersectsWith(icon))
                    continue;
                int localLeft = icon.Left - screen.Left, localRight = icon.Right - screen.Left;
                if (icon.Left + icon.Width / 2 < screen.Left || icon.Left + icon.Width / 2 >= screen.Right || localRight > limit)
                    continue;
                right = Math.Max(right, localRight);
            }

            return right == 0 ? 0 : Math.Min(limit, right + (int)(16 * dpi));
        }
    }
}