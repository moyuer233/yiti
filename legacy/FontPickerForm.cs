using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFontSwap
{
    /// <summary>从系统已安装字体里挑一个家族：搜索 + 按字体本身渲染 + 底部样张。</summary>
    internal sealed class FontPickerForm : Form
    {
        internal sealed class FamilyInfo
        {
            public string Name;
            public List<FontEntry> Entries = new List<FontEntry>();
        }

        readonly TextBox _search;
        readonly ListBox _list;
        readonly Label _hint;
        readonly RoundedButton _ok;
        readonly RoundedButton _cancel;
        readonly List<FamilyInfo> _all = new List<FamilyInfo>();
        readonly Dictionary<string, Font> _fontCache = new Dictionary<string, Font>(StringComparer.OrdinalIgnoreCase);

        public string SelectedFamily;

        public FontPickerForm()
        {
            var p = Theme.Current;
            Text = "选择字体";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 620);
            BackColor = Theme.KeyColor;
            TransparencyKey = Theme.KeyColor;
            Font = new Font("Segoe UI", 9.5f);
            Opacity = 1;

            // 整页铺一张不透明卡片：按钮四角只要和卡片同色就能干净贴合，
            // 不用去猜窗体背景（它可能被 MicaForEveryone 做成亚克力）
            var root = new CardPanel { Location = new Point(0, 0), Size = new Size(520, 620), Radius = 8 };
            Controls.Add(root);

            var title = new Label
            {
                Text = "系统已安装字体",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = p.Text,
                AutoSize = false,
                Location = new Point(20, 14),
                Size = new Size(300, 26)
            };
            _hint = new Label
            {
                Text = "",
                Tag = "dim",
                ForeColor = p.TextDim,
                AutoSize = false,
                Location = new Point(20, 40),
                Size = new Size(480, 18)
            };
            var searchHost = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(480, 34),
                BackColor = p.Surface
            };
            searchHost.Paint += delegate (object s, PaintEventArgs e)
            {
                using (var pen = new Pen(p.Line)) e.Graphics.DrawRectangle(pen, 0, 0, searchHost.Width - 1, searchHost.Height - 1);
            };
            _search = new TextBox
            {
                Location = new Point(10, 8),
                Size = new Size(458, 20),
                BackColor = p.Surface,
                ForeColor = p.Text,
                BorderStyle = BorderStyle.None
            };
            _search.TextChanged += delegate { ApplyFilter(); };
            searchHost.Controls.Add(_search);

            _list = new ListBox
            {
                Location = new Point(20, 106),
                Size = new Size(480, 430),
                BackColor = p.Surface,
                ForeColor = p.Text,
                BorderStyle = BorderStyle.None,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 38,
                IntegralHeight = false
            };
            _list.DrawItem += DrawItem;
            _list.SelectedIndexChanged += delegate { UpdatePreview(); };
            _list.DoubleClick += delegate { Accept(); };

            _ok = new RoundedButton { Text = "确定", Primary = true, Location = new Point(308, 556), Size = new Size(92, 34), Enabled = false, CornerColor = p.Surface };
            _cancel = new RoundedButton { Text = "取消", Location = new Point(408, 556), Size = new Size(92, 34), CornerColor = p.Surface };
            _ok.Click += delegate { Accept(); };
            _cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            root.Controls.Add(title);
            root.Controls.Add(_hint);
            root.Controls.Add(searchHost);
            root.Controls.Add(_list);
            root.Controls.Add(_ok);
            root.Controls.Add(_cancel);

            LoadFamilies();
            Shown += delegate
            {
                _list.ItemHeight = 38;   // 句柄创建后再设一次，否则会被重置
                if (_list.Items.Count > 0) _list.SelectedIndex = 0;
                _search.Focus();
            };
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.KeyColor);
        }

        [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                if (Theme.Current == Theme.Dark) { int dark = 1; DwmSetWindowAttribute(Handle, 20, ref dark, 4); }
                int backdrop = 1;   // DWMSBT_NONE：让弹窗保持纯色，否则四角底色永远对不上
                DwmSetWindowAttribute(Handle, 38, ref backdrop, 4);
                // 搜索框占位提示
                SendMessage(_search.Handle, 0x1501, (IntPtr)1, "搜索字体名…");
            }
            catch { }
        }

        void LoadFamilies()
        {
            var map = new Dictionary<string, FamilyInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in FontLibrary.All)
            {
                if (string.IsNullOrEmpty(e.Family)) continue;
                FamilyInfo info;
                if (!map.TryGetValue(e.Family, out info))
                {
                    info = new FamilyInfo { Name = e.Family };
                    map[e.Family] = info;
                }
                info.Entries.Add(e);
            }
            foreach (var kv in map) _all.Add(kv.Value);
            _all.Sort(delegate (FamilyInfo a, FamilyInfo b) { return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase); });
            ApplyFilter();
        }

        void ApplyFilter()
        {
            string q = _search.Text.Trim();
            var keep = _list.SelectedItem as FamilyInfo;
            _list.BeginUpdate();
            _list.Items.Clear();
            int count = 0;
            foreach (var info in _all)
            {
                if (q.Length > 0 && info.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) < 0) continue;
                _list.Items.Add(info);
                count++;
            }
            _list.EndUpdate();
            _hint.Text = count + " 个字体家族" + (q.Length > 0 ? "（过滤：" + q + "）" : "");
            if (_list.Items.Count > 0)
            {
                int idx = keep == null ? 0 : _list.Items.IndexOf(keep);
                _list.SelectedIndex = idx < 0 ? 0 : idx;
            }
            else UpdatePreview();
        }

        void DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var info = _list.Items[e.Index] as FamilyInfo;
            if (info == null) return;

            var p = Theme.Current;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            using (var b = new SolidBrush(selected ? p.Hover : p.Surface))
                e.Graphics.FillRectangle(b, e.Bounds);

            Font f;
            if (!_fontCache.TryGetValue(info.Name, out f))
            {
                try { f = new Font(info.Name, 12f); }
                catch { f = new Font("Segoe UI", 12f); }
                _fontCache[info.Name] = f;
            }

            // 家族名直接用该字体自己渲染（用户更喜欢这个设计）
            var nameRect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 2, e.Bounds.Width - 24, 22);
            TextRenderer.DrawText(e.Graphics, info.Name, f, nameRect, selected ? p.Accent : p.Text,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            var subRect = new Rectangle(e.Bounds.X + 12, e.Bounds.Y + 21, e.Bounds.Width - 24, 14);
            using (var f2 = new Font("Segoe UI", 8f))
                TextRenderer.DrawText(e.Graphics, info.Entries.Count + " 个字重", f2, subRect, p.TextDim,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
        }

        void UpdatePreview()
        {
            _ok.Enabled = _list.SelectedItem != null;
        }

        void Accept()
        {
            var info = _list.SelectedItem as FamilyInfo;
            if (info == null) return;
            SelectedFamily = info.Name;
            DialogResult = DialogResult.OK;
            Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var f in _fontCache.Values) f.Dispose();
                _fontCache.Clear();
            }
            base.Dispose(disposing);
        }
    }
}
