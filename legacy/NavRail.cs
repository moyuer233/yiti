using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WinFontSwap
{
    /// <summary>左侧导航栏（DevLink 观感：选中项圆角底色 + 左侧竖条）。</summary>
    internal sealed class NavRail : Control
    {
        sealed class Item
        {
            public string Text;
            public int Y;
            public const int H = 40;
            public Rectangle Bounds(Control owner)
            {
                return new Rectangle(10, Y, owner.Width - 20, H);
            }
        }

        readonly List<Item> _items = new List<Item>();
        Font _bold;
        int _hover = -1;

        public int SelectedIndex = -1;
        public event EventHandler Selected;

        public NavRail()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Theme.Current.Bg;
            _bold = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        }

        public void Add(string text)
        {
            var it = new Item();
            it.Text = text;
            it.Y = 16 + _items.Count * (Item.H + 6);
            _items.Add(it);
            if (SelectedIndex < 0) SelectedIndex = 0;
            Invalidate();
        }

        public void Select(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            SelectedIndex = index;
            Invalidate();
            if (Selected != null) Selected(this, EventArgs.Empty);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int hit = HitTest(e.Location);
            if (hit != _hover) { _hover = hit; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e) { _hover = -1; Invalidate(); base.OnMouseLeave(e); }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int hit = HitTest(e.Location);
            if (hit >= 0) Select(hit);
            base.OnMouseClick(e);
        }

        int HitTest(Point p)
        {
            for (int i = 0; i < _items.Count; i++)
                if (_items[i].Bounds(this).Contains(p)) return i;
            return -1;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = Theme.Current;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Backdrop.Paint(g, this);

            for (int i = 0; i < _items.Count; i++)
            {
                var it = _items[i];
                bool sel = i == SelectedIndex;
                bool hov = i == _hover && !sel;
                var r = it.Bounds(this);

                if (sel)
                {
                    // LF 风格：选中项不铺底，只在最左画一根蓝色竖条
                    using (var bar = new SolidBrush(p.Accent))
                        g.FillRectangle(bar, new Rectangle(2, r.Y + 10, 3, r.Height - 20));
                }
                else if (hov)
                {
                    using (var path = Draw.Rounded(r, 6))
                    using (var b = new SolidBrush(Color.FromArgb(14, 255, 255, 255)))
                        g.FillPath(b, path);
                }

                var textRect = new Rectangle(r.X + 14, r.Y, r.Width - 20, r.Height);
                Font f = sel ? _bold : Font;
                TextRenderer.DrawText(g, it.Text, f, textRect,
                    sel ? p.Text : Color.FromArgb(0xB4, 0xFF, 0xFF, 0xFF),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }
    }
}
