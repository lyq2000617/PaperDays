using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace PaperDays
{
    public static class ShellIconReader
    {
        [ComImport, Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85"), InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
        interface Windows
        {
            /// <summary>
            /// 声明 FindWindowSW 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            [return: MarshalAs(UnmanagedType.IDispatch)]
            object FindWindowSW([MarshalAs(UnmanagedType.Struct)] ref object location, [MarshalAs(UnmanagedType.Struct)] ref object root, int type, out int hwnd, int options);
        }

        [ComImport, Guid("6D5140C1-7436-11CE-8034-00AA006009FA"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface Services
        {
            /// <summary>
            /// 声明 QueryService 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            [return: MarshalAs(UnmanagedType.Interface)]
            object QueryService(ref Guid service, ref Guid iid);
        }

        [ComImport, Guid("000214E2-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface Browser
        {
            /// <summary>
            /// 声明 GetWindow 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void GetWindow(out IntPtr hwnd);
            /// <summary>
            /// 声明 ContextHelp 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void ContextHelp();
            /// <summary>
            /// 声明 InsertMenus 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void InsertMenus();
            /// <summary>
            /// 声明 SetMenu 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void SetMenu();
            /// <summary>
            /// 声明 RemoveMenus 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void RemoveMenus();
            /// <summary>
            /// 声明 Status 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void Status();
            /// <summary>
            /// 声明 Enable 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void Enable();
            /// <summary>
            /// 声明 Translate 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void Translate();
            /// <summary>
            /// 声明 Browse 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void Browse();
            /// <summary>
            /// 声明 ViewState 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void ViewState();
            /// <summary>
            /// 声明 ControlWindow 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void ControlWindow();
            /// <summary>
            /// 声明 ControlMessage 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void ControlMessage();
            /// <summary>
            /// 声明 QueryActiveShellView 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            [return: MarshalAs(UnmanagedType.Interface)]
            object QueryActiveShellView();
        }

        [ComImport, Guid("00000114-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface OleWindow
        {
            /// <summary>
            /// 声明 GetWindow 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void GetWindow(out IntPtr hwnd);
            /// <summary>
            /// 声明 ContextHelp 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void ContextHelp();
        }

        [ComImport, Guid("CDE725B0-CCC9-4519-917E-325D72FAB4CE"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        interface FolderView
        {
            /// <summary>
            /// 声明 GetCurrentViewMode 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void GetCurrentViewMode(out uint mode);
            /// <summary>
            /// 声明 SetCurrentViewMode 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void SetCurrentViewMode(uint mode);
            /// <summary>
            /// 声明 GetFolder 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void GetFolder(ref Guid iid, out IntPtr folder);
            /// <summary>
            /// 声明 Item 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void Item(int index, out IntPtr item);
            /// <summary>
            /// 声明 ItemCount 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void ItemCount(uint flags, out int count);
            /// <summary>
            /// 声明 Items 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void Items(uint flags, ref Guid iid, out IntPtr items);
            /// <summary>
            /// 声明 GetSelectionMarkedItem 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void GetSelectionMarkedItem(out int index);
            /// <summary>
            /// 声明 GetFocusedItem 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void GetFocusedItem(out int index);
            /// <summary>
            /// 声明 GetItemPosition 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void GetItemPosition(IntPtr item, out Point point);
            /// <summary>
            /// 声明 GetSpacing 系统接口契约；COM 接口的方法顺序必须保持以匹配虚表布局。
            /// </summary>
            void GetSpacing(out Point point);
        }

        /// <summary>
        /// 调用 Windows 原生 FindWindowEx；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string cls, string title);
        /// <summary>
        /// 调用 Windows 原生 ClientToScreen；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool ClientToScreen(IntPtr window, ref Point point);
        /// <summary>
        /// 调用 Windows 原生 IsWindowVisible；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr window);
        /// <summary>
        /// 释放系统或 COM 资源，避免句柄泄漏。
        /// </summary>
        static void Release(object value)
        {
            if (value != null && Marshal.IsComObject(value))
                Marshal.ReleaseComObject(value);
        }

        /// <summary>
        /// 读取并解析本地或系统数据，调用方负责处理失败。
        /// </summary>
        public static IconSnapshot Read()
        {
            var result = new IconSnapshot();
            object windows = null, dispatch = null, browser = null, view = null;
            try
            {
                windows = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39")));
                object location = 0, root = null;
                int handle;
                dispatch = ((Windows)windows).FindWindowSW(ref location, ref root, 8, out handle, 1);
                if (dispatch == null)
                    throw new Exception("桌面服务未返回窗口");
                var service = new Guid("4C96BE40-915C-11CF-99D3-00AA004AE837");
                var iid = typeof(Browser).GUID;
                browser = ((Services)dispatch).QueryService(ref service, ref iid);
                view = ((Browser)browser).QueryActiveShellView();
                var folder = (FolderView)view;
                IntPtr hwnd;
                ((OleWindow)view).GetWindow(out hwnd);
                var list = FindWindowEx(hwnd, IntPtr.Zero, "SysListView32", null);
                if (list != IntPtr.Zero && !IsWindowVisible(list))
                {
                    result.Success = true;
                    result.Method = "Windows 桌面服务（图标已隐藏）";
                    return result;
                }

                Point origin = Point.Empty;
                if (!ClientToScreen(list == IntPtr.Zero ? hwnd : list, ref origin))
                    throw new Exception("无法转换桌面坐标");
                Point spacing;
                folder.GetSpacing(out spacing);
                if (spacing.X < 16 || spacing.X > 1000 || spacing.Y < 16 || spacing.Y > 1000)
                    throw new Exception("图标间距无效");
                int count;
                folder.ItemCount(2, out count);
                if (count < 0 || count > 4000)
                    throw new Exception("图标数量超出范围");
                for (int i = 0; i < count; i++)
                {
                    IntPtr item = IntPtr.Zero;
                    try
                    {
                        folder.Item(i, out item);
                        Point point;
                        folder.GetItemPosition(item, out point);
                        result.Bounds.Add(new Rectangle(origin.X + point.X, origin.Y + point.Y, spacing.X, spacing.Y));
                    }
                    finally
                    {
                        if (item != IntPtr.Zero)
                            Marshal.FreeCoTaskMem(item);
                    }
                }

                result.Success = true;
                result.Method = "Windows 桌面服务";
            }
            catch (Exception ex)
            {
                result.Error = "桌面服务：" + ex.Message + " (0x" + ex.HResult.ToString("X8") + ")";
            }
            finally
            {
                Release(view);
                Release(browser);
                Release(dispatch);
                Release(windows);
            }

            return result;
        }
    }
}