using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace PaperDays
{
    public static class GlassWindow
    {
        [StructLayout(LayoutKind.Sequential, Pack=1)] struct Blend { public byte Operation, Flags, ConstantAlpha, Format; }
        [DllImport("user32.dll",SetLastError=true)] static extern bool UpdateLayeredWindow(IntPtr window, IntPtr screen, IntPtr position, ref Size size, IntPtr source, ref Point origin, uint key, ref Blend blend, uint flags);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr dc, IntPtr value);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr value);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr dc);
        public static void Present(IntPtr window, Bitmap bitmap)
        {
            IntPtr dc = CreateCompatibleDC(IntPtr.Zero), image = IntPtr.Zero, prior = IntPtr.Zero;
            try
            {
                if (dc == IntPtr.Zero) throw new Win32Exception();
                image = bitmap.GetHbitmap(Color.FromArgb(0));
                prior = SelectObject(dc,image);
                Size size = bitmap.Size; Point origin = Point.Empty;
                // Constant alpha is always 255: only the background pixels are translucent.
                var blend = new Blend { ConstantAlpha = 255, Format = 1 };
                if (!UpdateLayeredWindow(window,IntPtr.Zero,IntPtr.Zero,ref size,dc,ref origin,0,ref blend,2))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                if (prior != IntPtr.Zero) SelectObject(dc,prior);
                if (image != IntPtr.Zero) DeleteObject(image);
                if (dc != IntPtr.Zero) DeleteDC(dc);
            }
        }
        [ThreadStatic] public static Bitmap Backdrop;
        [ThreadStatic] public static State Preferences;
        [ThreadStatic] public static string Component;
        public static void Card(Graphics g, RectangleF r, int relief=0, bool header=false, int strengthOverride=-1, bool content=true)
        {
            if (r.Width<=0 || r.Height<=0) return;
            var pref=Preferences==null?new GlassPreference():GlassOptions.Get(Preferences,Skins.Current.Id,Component??"calendar");
            float opacity=1-Math.Max(0,Math.Min(100,content?pref.Content:pref.Outer))/100f;
            using (var path = Chrome.Round(r, Math.Min(14,r.Height/3)))
            {
                // Replace this material region instead of accumulating prior skin/layer alpha.
                var mode=g.CompositingMode;g.CompositingMode=CompositingMode.SourceCopy;
                using(var clear=new SolidBrush(Color.Transparent))g.FillPath(clear,path);
                g.CompositingMode=mode;
                if(opacity<=0)return;
                // Sample the static wallpaper at the exact device location, never stretch a crop per card.
                if (Backdrop != null) {
                    var state=g.Save();
                    using(var device=(GraphicsPath)path.Clone()) {
                        using(var matrix=g.Transform) device.Transform(matrix);
                        g.ResetTransform();g.SetClip(device,CombineMode.Intersect);
                        using(var attributes=new System.Drawing.Imaging.ImageAttributes()) {
                            var alpha=new System.Drawing.Imaging.ColorMatrix();alpha.Matrix33=opacity;
                            attributes.SetColorMatrix(alpha);
                            var crop=Rectangle.Intersect(Rectangle.Ceiling(device.GetBounds()),new Rectangle(Point.Empty,Backdrop.Size));
                            if(crop.Width>0 && crop.Height>0) g.DrawImage(Backdrop,crop,crop.X,crop.Y,crop.Width,crop.Height,GraphicsUnit.Pixel,attributes);
                        }
                    }
                    g.Restore(state);
                }
                Color tint=Skins.Dark?Color.FromArgb(24,38,48):Color.FromArgb(243,247,250);
                int strength=(int)((Backdrop==null?255:255*.72f)*opacity);
                // The fallback uses identical tint strength: skin history cannot change opacity.
                using(var fill=new SolidBrush(Color.FromArgb(strength,tint))) g.FillPath(fill,path);
                // Broad soft lighting, no perimeter stroke. Relief never changes event colors.
                Color top=Color.FromArgb((int)((relief<0?24:relief>0?30:10)*opacity),relief<0?Color.Black:Color.White);
                Color bottom=Color.FromArgb((int)((relief<0?21:relief>0?13:2)*opacity),relief<0?Color.White:Color.Black);
                using(var light=new LinearGradientBrush(r,top,bottom,90f)) g.FillPath(light,path);
            }
        }
    }
}
