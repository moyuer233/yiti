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
                var colors = src == null ? new Color[0] : Dominant(src, 5);

                var result = new Bitmap(size.Width, size.Height);
                using (var g = Graphics.FromImage(result))
                {
                    // 底色
                    using (var baseBrush = new LinearGradientBrush(new Rectangle(0, 0, size.Width, size.Height),
                               Color.FromArgb(18, 22, 30), Color.FromArgb(8, 10, 14), 90f))
                        g.FillRectangle(baseBrush, 0, 0, size.Width, size.Height);

                    // 参考 Lyricify-Backgrounds（Apache-2.0）的三层结构：一层主色 + 两层辅助，
                    // 各自缩放/旋转后叠加（我们省掉了动画与 GPU 网格，只保留观感）
                    if (colors.Length > 0)
                    {
                        float[] scale = { 1.4f, 0.7f, 0.7f };
                        float[] angle = { -6f, 12f, -20f };
                        int[] alpha = { 170, 110, 95 };
                        float[][] px = new float[][]
                        {
                            new float[] { 0.16f, 0.80f, 0.50f },
                            new float[] { 0.72f, 0.26f, 0.58f },
                            new float[] { 0.34f, 0.86f, 0.62f },
                        };
                        float[][] py = new float[][]
                        {
                            new float[] { 0.20f, 0.22f, 0.74f },
                            new float[] { 0.72f, 0.18f, 0.86f },
                            new float[] { 0.88f, 0.62f, 0.16f },
                        };
                        float[] radFactor = { 0.62f, 0.48f, 0.44f };

                        for (int layer = 0; layer < 3; layer++)
                        {
                            using (var tmp = new Bitmap(size.Width, size.Height))
                            {
                                using (var gt = Graphics.FromImage(tmp))
                                {
                                    gt.SmoothingMode = SmoothingMode.AntiAlias;
                                    gt.TranslateTransform(size.Width / 2f, size.Height / 2f);
                                    gt.RotateTransform(angle[layer]);
                                    gt.ScaleTransform(scale[layer], scale[layer]);
                                    gt.TranslateTransform(-size.Width / 2f, -size.Height / 2f);
                                    for (int i = 0; i < colors.Length && i < 3; i++)
                                    {
                                        float cx = size.Width * px[layer][i];
                                        float cy = size.Height * py[layer][i];
                                        float rad = Math.Max(size.Width, size.Height) * radFactor[layer];
                                        using (var brush = new PathGradientBrush(Oval(cx, cy, rad)))
                                        {
                                            brush.CenterColor = Color.FromArgb(alpha[layer], colors[(i + layer) % colors.Length]);
                                            brush.SurroundColors = new[] { Color.FromArgb(0, colors[(i + layer) % colors.Length]) };
                                            gt.FillEllipse(brush, cx - rad, cy - rad, rad * 2, rad * 2);
                                        }
                                    }
                                }
                                g.DrawImage(tmp, 0, 0, size.Width, size.Height);
                            }
                        }
                    }

                    // 压暗（用户指定 60）
                    using (var dark = new SolidBrush(Color.FromArgb(60, 8, 10, 14)))
                        g.FillRectangle(dark, 0, 0, size.Width, size.Height);
                    using (var veil = new LinearGradientBrush(new Rectangle(0, 0, size.Width, size.Height),
                               Color.FromArgb(8, 0, 0, 0), Color.FromArgb(30, 0, 0, 0), 90f))
                        g.FillRectangle(veil, 0, 0, size.Width, size.Height);
                }
                Swap(result, size);
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

        static GraphicsPath Oval(float cx, float cy, float rad)
        {
            var p = new GraphicsPath();
            p.AddEllipse(cx - rad, cy - rad, rad * 2, rad * 2);
            return p;
        }

        /// <summary>从壁纸里取几个主色（跳过近黑/近灰，饱和度提上来一点）。</summary>
        static Color[] Dominant(Bitmap src, int want)
        {
            var buckets = new System.Collections.Generic.Dictionary<int, int>();
            var list = new System.Collections.Generic.List<Color>();
            using (var tiny = new Bitmap(48, 27))
            {
                using (var g = Graphics.FromImage(tiny))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, 0, 0, 48, 27);
                }
                for (int y = 0; y < 27; y++)
                    for (int x = 0; x < 48; x++)
                    {
                        var c = tiny.GetPixel(x, y);
                        int max = Math.Max(c.R, Math.Max(c.G, c.B));
                        int min = Math.Min(c.R, Math.Min(c.G, c.B));
                        if (max < 40) continue;                    // 近黑
                        if (max - min < 18) continue;              // 近灰
                        int key = ((c.R / 32) << 16) | ((c.G / 32) << 8) | (c.B / 32);
                        int n;
                        buckets.TryGetValue(key, out n);
                        buckets[key] = n + 1;
                    }
            }
            var ordered = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<int, int>>(buckets);
            ordered.Sort(delegate (System.Collections.Generic.KeyValuePair<int, int> a, System.Collections.Generic.KeyValuePair<int, int> b) { return b.Value.CompareTo(a.Value); });
            foreach (var kv in ordered)
            {
                int cr = ((kv.Key >> 16) & 0xFF) * 32 + 16;
                int cg = ((kv.Key >> 8) & 0xFF) * 32 + 16;
                int cb = (kv.Key & 0xFF) * 32 + 16;
                list.Add(Color.FromArgb(Math.Min(255, cr), Math.Min(255, cg), Math.Min(255, cb)));
                if (list.Count >= want) break;
            }
            if (list.Count == 0)
            {
                list.Add(Color.FromArgb(38, 68, 110));
                list.Add(Color.FromArgb(90, 46, 96));
                list.Add(Color.FromArgb(36, 92, 74));
            }
            return list.ToArray();
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
