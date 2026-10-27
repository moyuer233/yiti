using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WinFontSwap.Wpf
{
    /// <summary>截取窗口背后的桌面，交给 WPF 用 GPU 模糊（BlurEffect 走 D3D），观感与 WinForms 版一致。</summary>
    internal static class Backdrop
    {
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleDC(IntPtr hdc);
        [DllImport("gdi32.dll")] static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);
        [DllImport("gdi32.dll")] static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool BitBlt(IntPtr dst, int x, int y, int w, int h, IntPtr src, int sx, int sy, int rop);
        [DllImport("gdi32.dll")] static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] static extern bool DeleteDC(IntPtr hdc);
        [DllImport("user32.dll")] static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("user32.dll")] static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);
        [DllImport("user32.dll")] static extern bool SetWindowDisplayAffinity(IntPtr hwnd, uint affinity);

        public static void HideFromCapture(IntPtr hwnd)
        {
            try { SetWindowDisplayAffinity(hwnd, 0x00000011); } catch { }
        }

        public static void ShowInCapture(IntPtr hwnd)
        {
            try { SetWindowDisplayAffinity(hwnd, 0); } catch { }
        }

        /// <summary>抓一块屏幕，缩到 1/4 再交给 WPF 放大模糊（模糊用 GPU，代价很低）。</summary>
        public static BitmapSource Capture(Rectangle area)
        {
            try
            {
                if (area.Width <= 0 || area.Height <= 0) return null;
                IntPtr screenDc = GetDC(IntPtr.Zero);
                IntPtr memDc = CreateCompatibleDC(screenDc);
                IntPtr bmp = CreateCompatibleBitmap(screenDc, area.Width, area.Height);
                IntPtr old = SelectObject(memDc, bmp);
                BitBlt(memDc, 0, 0, area.Width, area.Height, screenDc, area.Left, area.Top, 0x00CC0020);
                SelectObject(memDc, old);
                using (var raw = Image.FromHbitmap(bmp))
                {
                    DeleteObject(bmp);
                    DeleteDC(memDc);
                    ReleaseDC(IntPtr.Zero, screenDc);

                    // 降到 1/4：模糊本来就要丢掉细节，传输量小 → 每帧更快
                    int sw = Math.Max(16, area.Width / 4);
                    int sh = Math.Max(16, area.Height / 4);
                    using (var small = new Bitmap(sw, sh, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        using (var g = Graphics.FromImage(small))
                        {
                            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                            g.DrawImage(raw, new Rectangle(0, 0, sw, sh));
                        }
                        var src = ToBitmapSource(small);
                        src.Freeze();
                        return src;
                    }
                }
            }
            catch { return null; }
        }

        static BitmapSource ToBitmapSource(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                return BitmapSource.Create(bmp.Width, bmp.Height, 96, 96,
                    PixelFormats.Bgra32, null, data.Scan0, data.Stride * bmp.Height, data.Stride);
            }
            finally { bmp.UnlockBits(data); }
        }
    }
}
