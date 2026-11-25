using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PaperDays
{
    // A real child window of Explorer's desktop view. Mouse messages are delivered
    // directly to SceneView; wallpaper ownership and global input hooks are irrelevant.
    public sealed class DesktopSurface : SceneView
    {
        readonly IntPtr desktop;
        readonly Scene scene;
        Rectangle targetBounds;
        int paints;
        string regionKey;
        readonly GlassBackdropCache backdrop = new GlassBackdropCache();
        delegate bool EnumWindowsProc(IntPtr window, IntPtr data);
        /// <summary>
        /// 调用 Windows 原生 EnumWindows；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool EnumWindows(EnumWindowsProc callback, IntPtr data);
        /// <summary>
        /// 调用 Windows 原生 FindWindowEx；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string name, string title);
        /// <summary>
        /// 调用 Windows 原生 IsWindow；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool IsWindow(IntPtr window);
        /// <summary>
        /// 调用 Windows 原生 IsWindowVisible；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr window);
        /// <summary>
        /// 调用 Windows 原生 GetParent；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern IntPtr GetParent(IntPtr window);
        /// <summary>
        /// 调用 Windows 原生 MapWindowPoints；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll")]
        static extern int MapWindowPoints(IntPtr from, IntPtr to, ref Point point, uint count);
        /// <summary>
        /// 调用 Windows 原生 SetWindowPos；保持声明参数与系统接口一致，调用方负责检查结果。
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);
        /// <summary>
        /// 枚举可见桌面宿主窗口，未就绪时返回空句柄。
        /// </summary>
        public static IntPtr FindDesktop()
        {
            IntPtr found = IntPtr.Zero;
            EnumWindows(delegate (IntPtr top, IntPtr unused)
            {
                IntPtr view = FindWindowEx(top, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (view != IntPtr.Zero && IsWindowVisible(view))
                {
                    found = view;
                    return false;
                }

                return true;
            }, IntPtr.Zero);
            return found;
        }

        /// <summary>
        /// 初始化DesktopSurface的DesktopSurface，准备状态、布局与必要的事件绑定。
        /// </summary>
        public DesktopSurface(Scene scene, IntPtr parent) : base(scene)
        {
            if (parent == IntPtr.Zero)
                throw new Exception("没有找到 Windows 桌面窗口。");
            desktop = parent;
            this.scene = scene;
            TabStop = false;
        }

        protected override CreateParams CreateParams
        {
            /// <summary>
            /// 读取CreateParams属性，供DesktopSurface使用。
            /// </summary>
            get
            {
                var cp = base.CreateParams;
                cp.Parent = desktop;
                cp.Style = unchecked((int)0x56000000);
                cp.ExStyle = 0; // Ordinary region-clipped child: alpha never controls mouse delivery.
                return cp;
            }
        }

        public bool Connected
        {
            /// <summary>
            /// 读取Connected属性，供DesktopSurface使用。
            /// </summary>
            get
            {
                return IsHandleCreated && IsWindow(desktop) && GetParent(Handle) == desktop;
            }
        }

        /// <summary>
        /// 把日历挂载到指定宿主并验证首帧已绘制。
        /// </summary>
        public void Attach(Rectangle bounds)
        {
            targetBounds = bounds;
            Point origin = bounds.Location;
            MapWindowPoints(IntPtr.Zero, desktop, ref origin, 1);
            Bounds = new Rectangle(origin, bounds.Size);
            IntPtr h = Handle;
            if (!Connected)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows 没有允许连接交互桌面。");
            scene.PrepareComponents(ClientSize);
            UpdateRegion();
            if (!SetWindowPos(h, IntPtr.Zero, origin.X, origin.Y, bounds.Width, bounds.Height, 0x0050))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            Visible = true;
            Invalidate();
            Update();
            if ((scene.State.ShowCalendar || scene.State.ShowPlans || scene.State.ShowWidget) && (!IsWindowVisible(h) || paints == 0))
                throw new Exception("日历窗口未能显示或绘制，未判定为连接成功。 可见=" + IsWindowVisible(h) + "；宿主可见=" + IsWindowVisible(desktop) + "；绘制=" + paints + "；位置=" + Bounds);
        }

        /// <summary>
        /// 绘制控件当前状态，保持主题配色和内容可见。
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            PaintDesktop(e.Graphics); PlaceInlineEditor();
            paints++;
        }

        /// <summary>
        /// 生成不含日程正文的窗口状态诊断。
        /// </summary>
        void PaintDesktop(Graphics graphics)
        {
            if(ClientSize.Width<1 || ClientSize.Height<1) return;
            scene.GlassBackdrop = Skins.Glass ? backdrop.Get(targetBounds) : null;
            using (var frame = scene.Render(ClientSize)) {
                graphics.Clear(Skins.Glass ? SystemColors.Desktop : Palette.Paper);
                if(Skins.Glass && backdrop.Wallpaper!=null) graphics.DrawImageUnscaled(backdrop.Wallpaper,0,0);
                graphics.DrawImageUnscaled(frame,0,0);
                UpdateRegion();
            }
        }

        protected override void Dispose(bool disposing) {
            if(disposing) { scene.GlassBackdrop=null;backdrop.Dispose(); }
            base.Dispose(disposing);
        }

        public void RefreshGlassWallpaper() { backdrop.Invalidate(); Invalidate(); }
        public void SynchronizeLayout() { scene.PrepareComponents(ClientSize); UpdateRegion(); }
        public string Describe()
        {
            return "屏幕 " + targetBounds + "；连接=" + Connected + "；可见=" + (IsHandleCreated && IsWindowVisible(Handle)) + "；绘制=" + paints + "；点击=" + ClickCount + "；双击=" + DoubleClickCount;
        }

        /// <summary>
        /// 处理当前控件需要的 Windows 消息，其余消息交给基类。
        /// </summary>
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==0x84) {
                var p=PointToClient(new Point(unchecked((short)(m.LParam.ToInt64()&65535)),unchecked((short)((m.LParam.ToInt64()>>16)&65535))));
                m.Result=new IntPtr(System.Linq.Enumerable.Any(scene.ComponentRects,x=>{var bounds=x.Value;if(scene.EditingLayout)bounds.Inflate(10,10);return scene.ComponentVisible(x.Key)&&bounds.Contains(p);})?1:-1);return;
            }
            if (m.Msg == 0x21)
            {
                m.Result = new IntPtr(1);
                return;
            }

            base.WndProc(ref m);
        }

        /// <summary>
        /// 维护日历子窗口的显示位置，不抢占用户焦点。
        /// </summary>
        public void KeepVisible()
        {
            if (Connected)
                SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, 0x0053);
        }

        /// <summary>
        /// 更新桌面留白裁切区域并释放旧区域对象。
        /// </summary>
        public void UpdateRegion()
        {
            string key=scene.State.FillScreen+"|"+ClientSize+"|"+scene.EditingLayout+"|"+String.Join(";",System.Linq.Enumerable.Select(scene.ComponentRects,x=>x.Key+":"+scene.ComponentVisible(x.Key)+":"+x.Value));
            if(key==regionKey)return;regionKey=key;
            var old = Region;
            Region = DesktopAppearance.CreateRegion(scene, ClientSize);
            if (old != null) old.Dispose();
        }
    }
}
