using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace WinFontSwap
{
    internal static class Draw
    {
        public static GraphicsPath Rounded(Rectangle r, int radius)
        {
            var path = new GraphicsPath();
            int d = Math.Max(2, radius * 2);
            if (d > r.Height) d = r.Height;
            if (d > r.Width) d = r.Width;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        /// <summary>控件该用哪种底色擦除自己：卡片里用卡片色，直接挂在窗体上用窗体底色（透明模式下是色键）。</summary>
        public static Color Back(Control c)
        {
            var parent = c.Parent;
            while (parent != null)
            {
                var card = parent as CardPanel;
                if (card != null) return card.Emphasis ? Theme.Current.SurfaceAlt : Theme.Current.Surface;
                var form = parent as Form;
                if (form != null) return form.BackColor;   // 一路往上找窗体的真实底色
                if ((parent.Tag as string) == "surface") return Theme.Current.Surface;
                parent = parent.Parent;
            }
            return Theme.Current.Bg;
        }

        public static Color Mix(Color a, Color b, float t)
        {
            return Color.FromArgb(
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }
    }

    /// <summary>标签：分层窗口没法用 ClearType，统一走 GDI+ 灰度抗锯齿（不要彩色次像素）。</summary>
    internal sealed class FlatLabel : Label
    {
        public FlatLabel() { UseCompatibleTextRendering = true; }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            base.OnPaint(e);
        }
    }

    /// <summary>圆角卡片。</summary>
    internal sealed class CardPanel : Panel
    {
        public int Radius = 8;
        public bool Emphasis;

        public CardPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Padding = new Padding(14, 12, 14, 12);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // 卡片外圈跟窗体真实底色一致（主窗纯色、弹窗色键）
            e.Graphics.Clear(Draw.Back(this));
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Current;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Draw.Rounded(rect, Radius))
            {
                using (var b = new SolidBrush(Emphasis ? p.SurfaceAlt : p.Surface)) g.FillPath(b, path);
                using (var pen = new Pen(p.Line)) g.DrawPath(pen, path);
            }
        }
    }

    /// <summary>Win11 风格按钮：Primary 强调色填充，次级白底细边。</summary>
    internal sealed class RoundedButton : Button
    {
        public bool Primary;
        public int Radius = 6;
        bool _hover;

        public RoundedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Height = 34;
            Font = new Font("Segoe UI", 9.5f);
            Cursor = Cursors.Hand;
            UseVisualStyleBackColor = false;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        /// <summary>四角外侧填什么颜色。不设则按父级推断；弹窗/不透明容器应显式给。</summary>
        public Color? CornerColor;

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Current;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            // 先用底色把整块擦干净，再画圆角；路径收到 W-1/H-1，四边抗锯齿才均匀
            g.Clear(CornerColor.HasValue ? CornerColor.Value : Draw.Back(this));
            var fillRect = new Rectangle(0, 0, Width - 1, Height - 1);
            var borderRect = new Rectangle(0, 0, Width - 1, Height - 1);

            Color fill = Primary ? p.Accent : (Parent is CardPanel ? p.SurfaceAlt : p.Surface);
            Color text = Primary ? p.AccentText : p.Text;
            if (!Enabled) { fill = p.SurfaceAlt; text = p.TextDim; }
            else if (_hover) { fill = Primary ? Draw.Mix(p.Accent, Color.White, 0.14f) : p.Hover; }

            using (var path = Draw.Rounded(fillRect, Radius))
            {
                using (var b = new SolidBrush(fill)) g.FillPath(b, path);
                if (!Primary && Enabled)
                    using (var pen = new Pen(p.Line)) g.DrawPath(pen, Draw.Rounded(borderRect, Radius));
            }
            TextRenderer.DrawText(g, Text, Font, fillRect, text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>Win11 开关。</summary>
    internal sealed class ToggleSwitch : Control
    {
        bool _on, _hover;

        public bool On
        {
            get { return _on; }
            set { if (_on != value) { _on = value; Invalidate(); } }
        }

        public ToggleSwitch()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(44, 24);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnClick(EventArgs e) { if (Enabled) On = !On; base.OnClick(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Current;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Draw.Back(this));

            int h = 20, w = 40, top = (Height - h) / 2, left = (Width - w) / 2;
            var track = new Rectangle(left, top, w, h);

            Color fill;
            if (!Enabled) fill = Draw.Mix(p.Surface, p.Line, 0.5f);
            else if (_on) fill = p.Accent;
            else fill = _hover ? Draw.Mix(p.TextDim, p.Surface, 0.5f) : Draw.Mix(p.TextDim, p.Surface, 0.35f);

            using (var path = Draw.Rounded(track, h / 2))
            using (var b = new SolidBrush(fill)) g.FillPath(b, path);

            int knob = 12;
            int kx = _on ? track.Right - knob - 4 : track.Left + 4;
            int ky = track.Top + (h - knob) / 2;
            using (var b = new SolidBrush(Color.White)) g.FillEllipse(b, kx, ky, knob, knob);
            using (var pen = new Pen(Color.FromArgb(0xD0, 0xD0, 0xD0)))
                g.DrawEllipse(pen, kx, ky, knob - 1, knob - 1);
        }
    }

    /// <summary>左侧导航项（选中：浅底 + 左侧强调条）。</summary>
    internal sealed class NavButton : Button
    {
        bool _selected, _hover;

        public bool Selected
        {
            get { return _selected; }
            set { _selected = value; Invalidate(); }
        }

        public NavButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Height = 38;
            Font = new Font("Segoe UI", 10f);
            Cursor = Cursors.Hand;
            TextAlign = ContentAlignment.MiddleLeft;
            UseVisualStyleBackColor = false;
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaintBackground(PaintEventArgs e) { }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Current;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            if (_selected || _hover)
            {
                using (var path = Draw.Rounded(rect, 6))
                using (var b = new SolidBrush(_selected ? p.Hover : Draw.Mix(p.Bg, p.Hover, 0.6f)))
                    g.FillPath(b, path);
            }
            if (_selected)
            {
                using (var b = new SolidBrush(p.Accent))
                using (var path = Draw.Rounded(new Rectangle(0, 10, 3, Height - 20), 2))
                    g.FillPath(b, path);
            }

            var textRect = new Rectangle(14, 0, Width - 20, Height);
            TextRenderer.DrawText(g, Text, Font, textRect, _selected ? p.Accent : p.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
        }
    }

    /// <summary>标题栏按钮（最小化 / 关闭）。</summary>
    internal sealed class CaptionButton : Control
    {
        public string Glyph = "\uE921";
        public bool Close;
        bool _hover;

        public CaptionButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            Size = new Size(46, 32);
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Current;
            var g = e.Graphics;
            g.Clear(Draw.Back(this));
            if (_hover)
            {
                using (var b = new SolidBrush(Close ? Color.FromArgb(0xC4, 0x2B, 0x1C) : p.Hover))
                    g.FillRectangle(b, ClientRectangle);
            }
            using (var f = new Font("Segoe MDL2 Assets", 9f))
            {
                TextRenderer.DrawText(g, Glyph, f, ClientRectangle,
                    _hover && Close ? Color.White : p.Text,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }
    }

    /// <summary>样张预览：用选中的字体画一段中英混排。</summary>
    internal sealed class PreviewBox : Panel
    {
        public FontFamily Family;
        public string Placeholder = "选一个字体，这里会用它预览";

        public PreviewBox()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Current.SurfaceAlt;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Current;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(p.SurfaceAlt);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Draw.Rounded(rect, 8))
            using (var pen = new Pen(p.Line)) g.DrawPath(pen, path);

            var sf = new StringFormat();
            sf.Trimming = StringTrimming.EllipsisCharacter;
            sf.FormatFlags = StringFormatFlags.NoWrap;

            if (Family == null)
            {
                TextRenderer.DrawText(g, Placeholder, new Font("Segoe UI", 9.5f),
                    new Rectangle(18, 0, Width - 36, Height), p.TextDim,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
                return;
            }

            // 预览单独用「像素单位 + 网格对齐 + 整数坐标」渲染，避免样张发虚
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            using (var f1 = new Font(Family, 24f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var f2 = new Font(Family, 13f, FontStyle.Regular, GraphicsUnit.Pixel))
            using (var b1 = new SolidBrush(p.Text))
            using (var b2 = new SolidBrush(p.TextDim))
            {
                g.DrawString("清晨的第一缕阳光，照在安静的街道上。", f1, b1, 18f, 6f);
                g.DrawString("Microsoft YaHei UI  0123456789  AaBbCc", f2, b2, 18f, 42f);
            }
        }
    }
}
