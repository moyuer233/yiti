using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFontSwap
{
    /// <summary>把「窗口背后的桌面」截下来做模糊+压暗，子控件按屏幕坐标取对应块，做出亚克力/Mica 观感。</summary>
    internal static class Backdrop
    {
        static Bitmap _bmp;
        static Rectangle _bounds;   // 截图覆盖的屏幕区域

        public static void Capture()
        {
            try
            {
                var vs = SystemInformation.VirtualScreen;
                if (vs.Width <= 0 || vs.Height <= 0) return;

                using (var raw = new Bitmap(vs.Width, vs.Height))
                {
                    using (var g = Graphics.FromImage(raw))
                    {
                        g.CopyFromScreen(vs.Left, vs.Top, 0, 0, new Size(vs.Width, vs.Height), CopyPixelOperation.SourceCopy);
                    }

                    int sw = Math.Max(16, vs.Width / 12);
                    int sh = Math.Max(16, vs.Height / 12);
                    using (var small = new Bitmap(sw, sh))
                    {
                        using (var g = Graphics.FromImage(small))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(raw, new Rectangle(0, 0, sw, sh));
                        }
                        var result = new Bitmap(sw, sh);
                        using (var g = Graphics.FromImage(result))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(small, new Rectangle(0, 0, sw, sh));
                            using (var dark = new SolidBrush(Color.FromArgb(60, 8, 10, 14)))
                                g.FillRectangle(dark, 0, 0, sw, sh);
                        }
                        if (_bmp != null) _bmp.Dispose();
                        _bmp = result;
                        _bounds = vs;
                    }
                }
            }
            catch { }
        }

        public static bool Ready { get { return _bmp != null; } }

        /// <summary>按控件的屏幕位置，从模糊背景里取一块画进它的客户区。</summary>
        public static void Paint(Graphics g, Control c)
        {
            if (_bmp == null) { g.Clear(Theme.Current.Bg); return; }
            Rectangle cr = c.ClientRectangle;
            if (cr.Width <= 0 || cr.Height <= 0) return;

            Point sp = c.PointToScreen(Point.Empty);
            float k = (float)_bmp.Width / Math.Max(1, _bounds.Width);
            int sx = (int)((sp.X - _bounds.Left) * k);
            int sy = (int)((sp.Y - _bounds.Top) * k);
            int sw = Math.Max(1, (int)(cr.Width * k));
            int sh = Math.Max(1, (int)(cr.Height * k));

            g.InterpolationMode = InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            g.DrawImage(_bmp, cr, new Rectangle(sx, sy, sw, sh), GraphicsUnit.Pixel);
        }
    }
}
