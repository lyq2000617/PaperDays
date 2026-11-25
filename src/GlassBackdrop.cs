using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
namespace PaperDays {
    // Reads wallpaper files only. Does not capture the desktop or private windows.
    public sealed class GlassBackdropCache : IDisposable {
        Bitmap image;
        public Bitmap Wallpaper { get; private set; }
        string identity;
        DateTime next;
        public void Invalidate() { next=DateTime.MinValue; }
        public Bitmap Get(Rectangle target) {
            if(target.Width<=0 || target.Height<=0) return null;
            if(DateTime.UtcNow<next) return image;
            next=DateTime.UtcNow.AddSeconds(10);
            try {
                var monitor=Screen.FromRectangle(target).Bounds;
                string file;
                try { using(var backend=new WallpaperBackend()) file=backend.Get(backend.Find(monitor)); }
                catch(System.Runtime.InteropServices.COMException) { file=PaperDays.Wallpaper.Current(); }
                string style="10",tile="0";
                using(var key=Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop")) {
                    if(key!=null) { style=Convert.ToString(key.GetValue("WallpaperStyle","10"));tile=Convert.ToString(key.GetValue("TileWallpaper","0")); }
                }
                string id=file+"|"+target+"|"+monitor+"|"+style+tile+"|"+(File.Exists(file)?File.GetLastWriteTimeUtc(file).Ticks:0);
                if(id==identity) return image;
                if(Wallpaper!=null){Wallpaper.Dispose();Wallpaper=null;}
                Bitmap result=null;
                if(File.Exists(file)) using(var source=Image.FromFile(file)) {
                    var canvas=new Bitmap(target.Width,target.Height);
                    using(var g=Graphics.FromImage(canvas)) {
                        g.Clear(Color.Black);
                        Rectangle area=style=="22"?SystemInformation.VirtualScreen:monitor;
                        area.Offset(-target.X,-target.Y);
                        if(tile=="1") {
                            using(var brush=new TextureBrush(source,WrapMode.Tile)) { brush.TranslateTransform(area.X,area.Y);g.FillRectangle(brush,0,0,canvas.Width,canvas.Height); }
                        } else {
                            RectangleF dest=area;
                            if(style!="2") {
                                float scale=style=="0"?1:style=="6"?Math.Min((float)area.Width/source.Width,(float)area.Height/source.Height):Math.Max((float)area.Width/source.Width,(float)area.Height/source.Height);
                                dest=new RectangleF(area.X+(area.Width-source.Width*scale)/2,area.Y+(area.Height-source.Height*scale)/2,source.Width*scale,source.Height*scale);
                            }
                            g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(source,dest);
                        }
                    }
                    result=Blur(canvas);if(Wallpaper!=null)Wallpaper.Dispose();Wallpaper=canvas;
                }
                if(image!=null) image.Dispose();image=result;identity=id;
            } catch(Exception ex) {
                if(!(ex is IOException || ex is System.Runtime.InteropServices.COMException || ex is ArgumentException || ex is UnauthorizedAccessException || ex is System.Runtime.InteropServices.ExternalException)) throw;
                if(image!=null) image.Dispose();image=null;identity=null;if(Wallpaper!=null)Wallpaper.Dispose();Wallpaper=null;
            }
            return image;
        }
        public static Bitmap Blur(Bitmap input) {
            var output=new Bitmap(input.Width,input.Height);
            using(var small=new Bitmap(Math.Max(1,input.Width/12),Math.Max(1,input.Height/12))) {
                using(var g=Graphics.FromImage(small)) {g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(input,new Rectangle(Point.Empty,small.Size));}
                using(var g=Graphics.FromImage(output)) {g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(small,new Rectangle(Point.Empty,output.Size));}
            }
            return output;
        }
        public void Dispose() {if(image!=null)image.Dispose();image=null;if(Wallpaper!=null)Wallpaper.Dispose();Wallpaper=null;}
    }
    public partial class Scene {
        internal static float EventDotTop(Graphics g,float y) {
            // Measure the same font used for the time: GDI+ top padding is included in the glyph box.
            using(var font=UiFont.Scene(10)) using(var format=(StringFormat)StringFormat.GenericDefault.Clone()) {
                format.SetMeasurableCharacterRanges(new[] {new CharacterRange(0,5)});
                var ranges=g.MeasureCharacterRanges("18:00",font,new RectangleF(0,0,200,60),format);
                try {var bounds=ranges[0].GetBounds(g);return y+bounds.Top+bounds.Height*.43f-4;}
                finally {foreach(var region in ranges) region.Dispose();}
            }
        }
    }
}
