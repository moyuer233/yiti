using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinFontSwap
{
    /// <summary>整窗背景：当前壁纸模糊 + 压暗。子控件按自己在窗体里的位置取同一张图的一块。</summary>
    internal static class Backdrop
    {
        static Bitmap _bmp;
        static Size _size;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern bool SystemParametersInfo(int action, int param, System.Text.StringBuilder buffer, int winIni);

        static void Build(Size size)
        {
            if (size.Width <= 0 || size.Height <= 0) return;
            if (_bmp != null && _size == size) return;
            Bitmap src = null;
            try
            {
                src = LoadWallpaper();
                if (src == null) { Swap(Fallback(size), size); return; }

                int sw = Math.Max(8, size.Width / 24);
                int sh = Math.Max(8, size.Height / 24);
                using (var small = new Bitmap(sw, sh))
                {
                    using (var g = Graphics.FromImage(small))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        float scale = Math.Max((float)sw / src.Width, (float)sh / src.Height);
                        int dw = (int)(src.Width * scale), dh = (int)(src.Height * scale);
                        g.DrawImage(src, new Rectangle((sw - dw) / 2, (sh - dh) / 2, dw, dh));
                    }
                    var result = new Bitmap(size.Width, size.Height);
                    using (var g = Graphics.FromImage(result))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(small, new Rectangle(0, 0, size.Width, size.Height));
                        using (var dark = new SolidBrush(Color.FromArgb(92, 8, 10, 14)))
                            g.FillRectangle(dark, 0, 0, size.Width, size.Height);
                        using (var veil = new LinearGradientBrush(new Rectangle(0, 0, size.Width, size.Height),
                                   Color.FromArgb(14, 0, 0, 0), Color.FromArgb(54, 0, 0, 0), 90f))
                            g.FillRectangle(veil, 0, 0, size.Width, size.Height);
                    }
                    Swap(result, size);
                }
            }
            catch { Swap(Fallback(size), size); }
            finally { if (src != null) src.Dispose(); }
        }

        static void Swap(Bitmap b, Size size)
        {
            if (_bmp != null) _bmp.Dispose();
            _bmp = b;
            _size = size;
        }

        static Bitmap Fallback(Size size)
        {
            var b = new Bitmap(size.Width, size.Height);
            using (var g = Graphics.FromImage(b))
            using (var lg = new LinearGradientBrush(new Rectangle(0, 0, size.Width, size.Height),
                       Color.FromArgb(24, 28, 36), Color.FromArgb(10, 12, 16), 90f))
                g.FillRectangle(lg, 0, 0, size.Width, size.Height);
            return b;
        }

        static Bitmap LoadWallpaper()
        {
            foreach (var path in Candidates())
            {
                try
                {
                    if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) continue;
                    using (var fs = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    using (var img = Image.FromStream(fs))
                        return new Bitmap(img);
                }
                catch { }
            }
            return null;
        }

        static System.Collections.Generic.List<string> Candidates()
        {
            var list = new System.Collections.Generic.List<string>();
            try
            {
                var sb = new System.Text.StringBuilder(1024);
                if (SystemParametersInfo(0x0073, sb.Capacity, sb, 0)) list.Add(sb.ToString());
            }
            catch { }
            try
            {
                string themes = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    @"Microsoft\Windows\Themes");
                list.Add(System.IO.Path.Combine(themes, "TranscodedWallpaper"));
                string cached = System.IO.Path.Combine(themes, @"CachedFiles");
                if (System.IO.Directory.Exists(cached))
                    foreach (var f in System.IO.Directory.GetFiles(cached))
                        if (f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) list.Add(f);
            }
            catch { }
            return list;
        }

        /// <summary>按控件在窗体中的位置，把整窗背景的一块画进它的客户区。</summary>
        public static void Paint(Graphics g, Control c)
        {
            var form = c.FindForm();
            if (form == null) { g.Clear(c.BackColor); return; }
            Build(form.ClientSize);
            if (_bmp == null) { g.Clear(c.BackColor); return; }

            Rectangle cr = c.ClientRectangle;
            if (cr.Width <= 0 || cr.Height <= 0) return;
            Point origin = form.PointToClient(c.PointToScreen(Point.Empty));
            var src = new Rectangle(origin.X, origin.Y, cr.Width, cr.Height);
            src.Intersect(new Rectangle(0, 0, _bmp.Width, _bmp.Height));
            if (src.Width <= 0 || src.Height <= 0) { g.Clear(Color.Black); return; }
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_bmp, cr, src, GraphicsUnit.Pixel);
        }
    }
}
