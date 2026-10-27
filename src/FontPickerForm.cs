using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WinFontSwap
{
    /// <summary>从系统已安装字体里挑一个家族（带搜索和字体本身预览）。</summary>
    internal sealed class FontPickerForm : Form
    {
        internal sealed class FamilyInfo
        {
            public string Name;
            public List<FontEntry> Entries = new List<FontEntry>();
            public Font Preview;
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
            ClientSize = new Size(480, 560);
            BackColor = p.Bg;
            Font = new Font("Segoe UI", 9.5f);

            var title = new FlatLabel
            {
                Text = "系统已安装字体",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = p.Text,
                AutoSize = false,
                Location = new Point(20, 16),
                Size = new Size(420, 28)
            };
            _search = new TextBox
            {
                Location = new Point(20, 52),
                Size = new Size(440, 26),
                BackColor = p.Surface,
                ForeColor = p.Text,
                BorderStyle = BorderStyle.FixedSingle
            };
            _search.TextChanged += delegate { ApplyFilter(); };

            _list = new ListBox
            {
                Location = new Point(20, 88),
                Size = new Size(440, 400),
                BackColor = p.Surface,
                ForeColor = p.Text,
                BorderStyle = BorderStyle.FixedSingle,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 34,
                IntegralHeight = false
            };
            _list.DrawItem += DrawItem;
            _list.SelectedIndexChanged += delegate { UpdatePreview(); };
            _list.DoubleClick += delegate { Accept(); };

            _hint = new FlatLabel
            {
                Text = "选一个字体家族，粗体/细体会按字重自动匹配。",
                ForeColor = p.TextDim,
                AutoSize = false,
                Location = new Point(20, 494),
                Size = new Size(440, 20)
            };

            _ok = new RoundedButton { Text = "确定", Primary = true, Location = new Point(268, 516), Size = new Size(92, 32), Enabled = false };
            _cancel = new RoundedButton { Text = "取消", Location = new Point(368, 516), Size = new Size(92, 32) };
            _ok.Click += delegate { Accept(); };
            _cancel.Click += delegate { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(title);
            Controls.Add(_search);
            Controls.Add(_list);
            Controls.Add(_hint);
            Controls.Add(_ok);
            Controls.Add(_cancel);

            LoadFamilies();
            Shown += delegate { _search.Focus(); };
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
            if (_list.Items.Count > 0) _list.SelectedIndex = 0;
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
                try { f = new Font(info.Name, 11f); }
                catch { f = new Font("Segoe UI", 11f); }
                _fontCache[info.Name] = f;
            }

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            var textRect = new RectangleF(e.Bounds.X + 10, e.Bounds.Y + 3, e.Bounds.Width - 120, 20);
            using (var brush = new SolidBrush(selected ? p.Accent : p.Text))
                e.Graphics.DrawString(info.Name, f, brush, textRect);

            var subRect = new RectangleF(e.Bounds.X + 10, e.Bounds.Y + 19, e.Bounds.Width - 20, 14);
            using (var f2 = new Font("Segoe UI", 8f))
            using (var brush2 = new SolidBrush(p.TextDim))
                e.Graphics.DrawString(info.Entries.Count + " 个字重", f2, brush2, subRect);
        }

        void UpdatePreview()
        {
            var info = _list.SelectedItem as FamilyInfo;
            _ok.Enabled = info != null;
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
