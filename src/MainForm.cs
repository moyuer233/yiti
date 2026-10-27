using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFontSwap
{
    public sealed class MainForm : Form
    {
        const int W = 1080, H = 720, PadX = 24, RailW = 184;
        const int InnerW = W - RailW - PadX * 2;

        string _demo;
        internal static bool _openPicker;
        internal static string ShotPath;
        internal static int StartPage = -1;

        Panel _header, _page;
        Panel[] _pages = new Panel[4];
        NavRail _rail;
        Label _title, _subtitle;
        RoundedButton _pickBtn;
        Label _pickedLabel;
        Label[] _slotLabels = new Label[3];
        RoundedButton[] _slotBtns = new RoundedButton[3];
        PreviewBox _preview;
        ToggleSwitch[] _toggles = new ToggleSwitch[3];
        Label[] _targetMeta = new Label[3];
        RoundedButton _applyBtn, _genBtn, _rollbackBtn, _cancelBtn;
        Label _status;

        // 系统元素页（Win32 字体）
        Label[] _winCurrent = new Label[6];
        NumericUpDown[] _winSize = new NumericUpDown[6];
        string[] _winFace = new string[6];
        Label _winStatus;

        // 中文回退页
        ComboBox _linkKeys;
        TextBox _linkText;
        Label _linkStatus;

        // 预设页
        TextBox _presetName;
        ListBox _presetList;
        Label _presetStatus;

        FontEntry _picked;
        List<FontEntry> _pool = new List<FontEntry>();
        FontEntry[] _slots = new FontEntry[3];
        PrivateFontCollection _pfc;

        public MainForm() : this(null) { }

        public MainForm(string demoFont)
        {
            _demo = demoFont;
            Text = "字体替换";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(W, H);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.Current.Bg;
            if (Theme.Glass) Opacity = 0.90;   // 整体轻微透明：边缘抗锯齿不会被色键切出暗边
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);

            BuildHeader();
            BuildShell();
            BuildFontCard();
            BuildPreviewCard();
            BuildTargetCard();
            BuildActions();
            BuildWin32Page();
            BuildFontLinkPage();
            BuildPresetPage();

            Shown += OnShown;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MARGINS { public int Left, Top, Right, Bottom; }

        [DllImport("dwmapi.dll")]
        static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS margins);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref uint value, int size);

        [StructLayout(LayoutKind.Sequential)]
        struct ACCENTPOLICY { public int AccentState; public int AccentFlags; public int GradientColor; public int AnimationId; }

        [StructLayout(LayoutKind.Sequential)]
        struct WINCOMPATTRDATA { public int Attribute; public IntPtr Data; public int SizeOfData; }

        [DllImport("user32.dll")]
        static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINCOMPATTRDATA data);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr h, int idx);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr h, int idx, int value);

        [DllImport("user32.dll")]
        static extern bool SetLayeredWindowAttributes(IntPtr h, uint key, byte alpha, uint flags);

        void EnableGlass()
        {
            if (!Theme.Glass) return;
            try
            {
                int dark = 1;
                DwmSetWindowAttribute(Handle, 20, ref dark, 4);   // 深色标题栏
                int backdrop = 1;                                  // DWMSBT_NONE：不要 Mica/亚克力背景
                DwmSetWindowAttribute(Handle, 38, ref backdrop, 4);
            }
            catch { }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Theme.Current.Bg);   // 主窗用整体透明度，不再用色键
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            EnableGlass();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var b = new SolidBrush(Theme.Current.Bg)) e.Graphics.FillRectangle(b, ClientRectangle);
        }

        void BuildHeader()
        {
            _title = new Label
            {
                Text = "字体替换",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = false,
                Location = new Point(PadX, 14),
                Size = new Size(600, 34)
            };
            _subtitle = new Label
            {
                Text = "选一个字体，粗体细体自动匹配；重启后设置、开始菜单一起变。",
                Tag = "dim",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Location = new Point(PadX + 2, 50),
                Size = new Size(W - PadX * 2, 22)
            };
        }

        void BuildShell()
        {
            _header = new Panel();
            _header.Location = new Point(0, 0);
            _header.Size = new Size(W, 84);
            _header.BackColor = Theme.Current.Bg;
            _header.Controls.Add(_title);
            _header.Controls.Add(_subtitle);

            _rail = new NavRail();
            _rail.Location = new Point(0, 84);
            _rail.Size = new Size(RailW, H - 84);
            _rail.Add("字体替换");
            _rail.Add("系统元素");
            _rail.Add("中文回退");
            _rail.Add("预设");
            _rail.Selected += delegate { ShowPage(_rail.SelectedIndex); };

            _page = new Panel();
            _page.Location = new Point(RailW, 84);
            _page.Size = new Size(W - RailW, H - 84);
            _page.BackColor = Theme.Current.Bg;

            for (int i = 0; i < 4; i++)
            {
                var pg = new Panel();
                pg.Location = new Point(0, 0);
                pg.Size = _page.Size;
                pg.BackColor = Theme.Current.Bg;
                pg.Visible = i == 0;
                _pages[i] = pg;
                _page.Controls.Add(pg);
            }

            Controls.Add(_page);
            Controls.Add(_rail);
            Controls.Add(_header);
        }

        void ShowPage(int index)
        {
            if (index < 0 || index >= _pages.Length) return;
            for (int i = 0; i < _pages.Length; i++) _pages[i].Visible = i == index;
            if (index == 1) LoadWin32();
            if (index == 2) LoadFontLinkKeys();
            if (index == 3) RefreshPresets();
        }

        void BuildWin32Page()
        {
            var card = new CardPanel { Location = new Point(PadX, 10), Size = new Size(InnerW, 344) };
            card.Controls.Add(Header("界面元素字体", 16, 12));
            card.Controls.Add(new Label
            {
                Text = "改的是 Win32 窗口的标题栏、菜单等（HKCU\\Control Panel\\Desktop\\WindowMetrics）。注销或重启资源管理器后生效。",
                Tag = "dim",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Location = new Point(16, 38),
                Size = new Size(InnerW - 32, 20)
            });

            for (int i = 0; i < 6; i++)
            {
                int y = 68 + i * 42;
                var name = new Label
                {
                    Text = Win32Fonts.Names[i],
                    Font = new Font("Segoe UI", 9.5f),
                    AutoSize = false,
                    Location = new Point(16, y),
                    Size = new Size(90, 28),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var cur = new Label
                {
                    Text = "读取中…",
                    Tag = "dim",
                    Font = new Font("Segoe UI", 9f),
                    AutoSize = false,
                    AutoEllipsis = true,
                    Location = new Point(110, y),
                    Size = new Size(InnerW - 300, 28),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var size = new NumericUpDown
                {
                    Minimum = 6, Maximum = 32, Value = 9,
                    Location = new Point(InnerW - 170, y + 2),
                    Size = new Size(56, 24),
                    BorderStyle = BorderStyle.FixedSingle,
                    BackColor = Theme.Current.Surface,
                    ForeColor = Theme.Current.Text,
                    Font = new Font("Segoe UI", 9f)
                };
                int idx = i;
                var btn = new RoundedButton
                {
                    Text = "选择",
                    Font = new Font("Segoe UI", 8.5f),
                    Location = new Point(InnerW - 104, y),
                    Size = new Size(72, 28)
                };
                btn.Click += delegate { PickWin32Face(idx); };

                card.Controls.Add(name);
                card.Controls.Add(cur);
                card.Controls.Add(size);
                card.Controls.Add(btn);
                _winCurrent[i] = cur;
                _winSize[i] = size;
            }
            _pages[1].Controls.Add(card);

            var apply = new RoundedButton { Text = "应用到系统", Primary = true, Location = new Point(PadX, 366), Size = new Size(120, 34) };
            apply.Click += delegate { ApplyWin32(); };
            var restore = new RoundedButton { Text = "还原系统默认", Location = new Point(PadX + 132, 366), Size = new Size(120, 34) };
            restore.Click += delegate { RestoreWin32(); };
            _pages[1].Controls.Add(apply);
            _pages[1].Controls.Add(restore);

            _winStatus = new Label
            {
                Text = "",
                Tag = "dim",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Location = new Point(PadX, 408),
                Size = new Size(InnerW, 22)
            };
            _pages[1].Controls.Add(_winStatus);
        }

        void BuildFontLinkPage()
        {
            var card = new CardPanel { Location = new Point(PadX, 10), Size = new Size(InnerW, 470) };
            card.Controls.Add(Header("中文字体回退", 16, 12));
            card.Controls.Add(new Label
            {
                Text = "字体缺字时按这里的顺序找替代字体。每行一条「文件名,字体名」，例如 msyh.ttc,Microsoft YaHei。",
                Tag = "dim",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Location = new Point(16, 38),
                Size = new Size(InnerW - 32, 20)
            });

            _linkKeys = new ComboBox
            {
                Location = new Point(16, 66),
                Size = new Size(InnerW - 32, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Theme.Current.Surface,
                ForeColor = Theme.Current.Text,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f)
            };
            _linkKeys.SelectedIndexChanged += delegate { LoadFontLinkText(); };
            card.Controls.Add(_linkKeys);

            _linkText = new TextBox
            {
                Location = new Point(16, 102),
                Size = new Size(InnerW - 32, 292),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                AcceptsReturn = true,
                WordWrap = false,
                BackColor = Theme.Current.Surface,
                ForeColor = Theme.Current.Text,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Consolas", 9f)
            };
            card.Controls.Add(_linkText);

            var save = new RoundedButton { Text = "保存回退链", Primary = true, Location = new Point(16, 406), Size = new Size(110, 32) };
            save.Click += delegate { SaveFontLinkText(); };
            var reload = new RoundedButton { Text = "重新读取", Location = new Point(134, 406), Size = new Size(96, 32) };
            reload.Click += delegate { LoadFontLinkText(); };
            var clear = new RoundedButton { Text = "删除该键", Location = new Point(238, 406), Size = new Size(96, 32) };
            clear.Click += delegate { ClearFontLink(); };
            card.Controls.Add(save);
            card.Controls.Add(reload);
            card.Controls.Add(clear);

            _pages[2].Controls.Add(card);

            _linkStatus = new Label
            {
                Text = "选一个字体键，编辑后保存；注销或重启资源管理器后生效。",
                Tag = "dim",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Location = new Point(PadX, 492),
                Size = new Size(InnerW, 22)
            };
            _pages[2].Controls.Add(_linkStatus);
        }

        void LoadFontLinkKeys()
        {
            if (_linkKeys == null) return;
            string keep = _linkKeys.SelectedItem as string;
            _linkKeys.Items.Clear();
            foreach (var k in FontLink.Keys()) _linkKeys.Items.Add(k);
            if (_linkKeys.Items.Count == 0) return;
            int idx = keep == null ? -1 : _linkKeys.Items.IndexOf(keep);
            _linkKeys.SelectedIndex = idx >= 0 ? idx : 0;
            LoadFontLinkText();
        }

        void LoadFontLinkText()
        {
            if (_linkKeys == null) return;
            string key = _linkKeys.SelectedItem as string;
            if (key == null) { _linkText.Text = ""; return; }
            var vals = FontLink.Read(key);
            var sb = new System.Text.StringBuilder();
            foreach (var v in vals) sb.AppendLine(v);
            _linkText.Text = sb.ToString();
        }

        void SaveFontLinkText()
        {
            string key = _linkKeys.SelectedItem as string;
            if (key == null) return;
            try
            {
                FontLink.WriteAll(key, _linkText.Text);
                _linkStatus.Text = "已保存「" + key + "」的回退链，注销后生效。";
                _linkStatus.ForeColor = Theme.Current.TextDim;
                Log("回退链已保存：" + key);
            }
            catch (Exception ex)
            {
                _linkStatus.Text = "保存失败：" + ex.Message;
                _linkStatus.ForeColor = Theme.Current.Err;
            }
        }

        void ClearFontLink()
        {
            string key = _linkKeys.SelectedItem as string;
            if (key == null) return;
            try
            {
                FontLink.Write(key, new string[0]);
                _linkText.Text = "";
                _linkStatus.Text = "已删除「" + key + "」的回退链（系统会用自己的默认）。";
                _linkStatus.ForeColor = Theme.Current.TextDim;
            }
            catch (Exception ex)
            {
                _linkStatus.Text = "删除失败：" + ex.Message;
                _linkStatus.ForeColor = Theme.Current.Err;
            }
        }

        void BuildPresetPage()
        {
            var card = new CardPanel { Location = new Point(PadX, 10), Size = new Size(InnerW, 300) };
            card.Controls.Add(Header("预设", 16, 12));
            card.Controls.Add(new Label
            {
                Text = "把「选中的字体 + 各界面元素的字体字号」存成一套，随时切回来。",
                Tag = "dim",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Location = new Point(16, 38),
                Size = new Size(InnerW - 32, 20)
            });

            _presetName = new TextBox
            {
                Location = new Point(16, 66),
                Size = new Size(InnerW - 32 - 120, 26),
                BackColor = Theme.Current.Surface,
                ForeColor = Theme.Current.Text,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9f)
            };
            card.Controls.Add(_presetName);

            var save = new RoundedButton { Text = "保存当前", Primary = true, Location = new Point(InnerW - 28 - 104, 64), Size = new Size(104, 30) };
            save.Click += delegate { SavePresetNow(); };
            card.Controls.Add(save);

            _presetList = new ListBox
            {
                Location = new Point(16, 106),
                Size = new Size(InnerW - 32, 120),
                BackColor = Theme.Current.Surface,
                ForeColor = Theme.Current.Text,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };
            card.Controls.Add(_presetList);

            var load = new RoundedButton { Text = "加载", Location = new Point(16, 236), Size = new Size(90, 30) };
            load.Click += delegate { LoadPresetNow(); };
            var del = new RoundedButton { Text = "删除", Location = new Point(118, 236), Size = new Size(90, 30) };
            del.Click += delegate { DeletePresetNow(); };
            card.Controls.Add(load);
            card.Controls.Add(del);

            _pages[3].Controls.Add(card);

            _presetStatus = new Label
            {
                Text = "加载只会填好界面，点「应用」才真正生效。",
                Tag = "dim",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Location = new Point(PadX, 322),
                Size = new Size(InnerW, 22)
            };
            _pages[3].Controls.Add(_presetStatus);
        }

        void RefreshPresets()
        {
            if (_presetList == null) return;
            _presetList.Items.Clear();
            foreach (var n in Presets.List()) _presetList.Items.Add(n);
        }

        void SavePresetNow()
        {
            string name = (_presetName.Text ?? "").Trim();
            if (name.Length == 0) { _presetStatus.Text = "先起个名字。"; return; }
            var faces = new string[6];
            var sizes = new decimal[6];
            for (int i = 0; i < 6; i++) { faces[i] = _winFace[i]; sizes[i] = _winSize[i].Value; }
            try
            {
                Presets.Save(name, _picked == null ? "" : _picked.Family, faces, sizes);
                RefreshPresets();
                _presetStatus.Text = "已保存：「" + name + "」。";
                _presetStatus.ForeColor = Theme.Current.TextDim;
            }
            catch (Exception ex)
            {
                _presetStatus.Text = "保存失败：" + ex.Message;
                _presetStatus.ForeColor = Theme.Current.Err;
            }
        }

        void LoadPresetNow()
        {
            string name = _presetList.SelectedItem as string;
            if (name == null) { _presetStatus.Text = "先选一套预设。"; return; }
            string family; string[] faces; decimal[] sizes;
            if (!Presets.Load(name, out family, out faces, out sizes)) { _presetStatus.Text = "读不到这套预设。"; return; }
            if (!string.IsNullOrEmpty(family)) PickByFamily(family);
            for (int i = 0; i < 6; i++)
            {
                if (!string.IsNullOrEmpty(faces[i])) _winFace[i] = faces[i];
                if (sizes[i] >= 6 && sizes[i] <= 32) _winSize[i].Value = sizes[i];
                if (!string.IsNullOrEmpty(_winFace[i]))
                    _winCurrent[i].Text = _winFace[i] + "  ·  " + _winSize[i].Value + " pt";
            }
            _presetStatus.Text = "已填入「" + name + "」，点对应页的「应用」才会生效。";
            _presetStatus.ForeColor = Theme.Current.TextDim;
        }

        void DeletePresetNow()
        {
            string name = _presetList.SelectedItem as string;
            if (name == null) return;
            Presets.Delete(name);
            RefreshPresets();
            _presetStatus.Text = "已删除：" + name;
            _presetStatus.ForeColor = Theme.Current.TextDim;
        }

        void LoadWin32()
        {
            for (int i = 0; i < 6; i++)
            {
                var info = Win32Fonts.Read(Win32Fonts.Keys[i]);
                if (info == null)
                {
                    _winFace[i] = "Segoe UI";
                    _winCurrent[i].Text = "未设置（系统默认）";
                    continue;
                }
                _winFace[i] = info.Face;
                double pt = Math.Abs(info.HeightPx) * 72.0 / 96.0;
                decimal d = (decimal)Math.Round(pt);
                if (d < _winSize[i].Minimum) d = _winSize[i].Minimum;
                if (d > _winSize[i].Maximum) d = _winSize[i].Maximum;
                _winSize[i].Value = d;
                _winCurrent[i].Text = info.Face + "  ·  " + d + " pt" + (info.Weight >= 700 ? "  · 粗体" : "");
            }
        }

        void PickWin32Face(int index)
        {
            using (var dlg = new FontPickerForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                _winFace[index] = dlg.SelectedFamily;
                _winCurrent[index].Text = dlg.SelectedFamily + "  ·  " + _winSize[index].Value + " pt";
            }
        }

        void ApplyWin32()
        {
            try
            {
                Win32Fonts.BackupOnce();
                int n = 0;
                for (int i = 0; i < 6; i++)
                {
                    if (string.IsNullOrEmpty(_winFace[i])) continue;
                    Win32Fonts.Write(Win32Fonts.Keys[i], _winFace[i], (double)_winSize[i].Value, 400, 1, false);
                    n++;
                }
                _winStatus.Text = "已写入 " + n + " 项，注销或重启资源管理器后生效；可用「还原系统默认」退回。";
                _winStatus.ForeColor = Theme.Current.TextDim;
                Log("Win32 元素字体已写入 " + n + " 项");
            }
            catch (Exception ex)
            {
                _winStatus.Text = "写入失败：" + ex.Message;
                _winStatus.ForeColor = Theme.Current.Err;
            }
        }

        void RestoreWin32()
        {
            try
            {
                int n = Win32Fonts.Restore();
                LoadWin32();
                _winStatus.Text = n > 0 ? ("已还原 " + n + " 项，注销后生效。") : "没有备份可还原（还没改过）。";
                _winStatus.ForeColor = Theme.Current.TextDim;
            }
            catch (Exception ex)
            {
                _winStatus.Text = "还原失败：" + ex.Message;
                _winStatus.ForeColor = Theme.Current.Err;
            }
        }

        Label Header(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                AutoSize = false,
                Location = new Point(x, y),
                Size = new Size(300, 24)
            };
        }

        void BuildFontCard()
        {
            var card = new CardPanel { Location = new Point(PadX, 10), Size = new Size(InnerW, 200) };
            _pages[0].Controls.Add(card);
            card.Controls.Add(Header("字体", 16, 14));

            _pickBtn = new RoundedButton { Text = "选择字体…", Location = new Point(16, 46), Size = new Size(120, 34) };
            _pickBtn.Click += delegate { PickFont(); };
            _pickedLabel = new Label
            {
                Text = "还没有选择",
                Tag = "dim",
                Font = new Font("Segoe UI", 10f),
                AutoSize = false,
                AutoEllipsis = true,
                Location = new Point(150, 46),
                Size = new Size(InnerW - 32 - 134, 34),
                TextAlign = ContentAlignment.MiddleLeft
            };
            card.Controls.Add(_pickBtn);
            card.Controls.Add(_pickedLabel);

            string[] slotNames = { "常规", "粗体", "细体" };
            for (int i = 0; i < 3; i++)
            {
                int y = 90 + i * 30;
                var name = new Label
                {
                    Text = slotNames[i],
                    Tag = "dim",
                    Font = new Font("Segoe UI", 9f),
                    AutoSize = false,
                    Location = new Point(16, y),
                    Size = new Size(52, 24),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var val = new Label
                {
                    Text = "未匹配",
                    Tag = "dim",
                    Font = new Font("Segoe UI", 9f),
                    AutoSize = false,
                    AutoEllipsis = true,
                    Location = new Point(68, y),
                    Size = new Size(InnerW - 28 - 68 - 72, 24),
                    TextAlign = ContentAlignment.MiddleLeft
                };
                var btn = new RoundedButton
                {
                    Text = "指定",
                    Font = new Font("Segoe UI", 8.5f),
                    Height = 26,
                    Tag = i,
                    Location = new Point(InnerW - 28 - 58, y - 1),
                    Size = new Size(58, 26)
                };
                btn.Click += delegate (object s, EventArgs e) { PickForSlot((int)((RoundedButton)s).Tag); };

                card.Controls.Add(name);
                card.Controls.Add(val);
                card.Controls.Add(btn);
                _slotLabels[i] = val;
                _slotBtns[i] = btn;
            }

            card.Controls.Add(new Label
            {
                Text = "斜体由系统合成，不用单独匹配；没匹配到的字重保留系统原样。",
                Tag = "dim",
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Location = new Point(16, 176),
                Size = new Size(InnerW - 32, 18)
            });
        }

        void BuildPreviewCard()
        {
            var card = new CardPanel { Location = new Point(PadX, 222), Size = new Size(InnerW, 118) };
            _pages[0].Controls.Add(card);
            card.Controls.Add(Header("预览", 16, 10));
            _preview = new PreviewBox { Location = new Point(16, 38), Size = new Size(InnerW - 32, 68) };
            card.Controls.Add(_preview);
        }

        void BuildTargetCard()
        {
            var card = new CardPanel { Location = new Point(PadX, 352), Size = new Size(InnerW, 168) };
            _pages[0].Controls.Add(card);
            card.Controls.Add(Header("替换目标", 16, 12));

            string[] targetNames = { "微软雅黑", "微软雅黑 粗体", "微软雅黑 细体" };
            for (int i = 0; i < 3; i++)
            {
                int y = 42 + i * 40;
                var title = new Label
                {
                    Text = targetNames[i],
                    Font = new Font("Segoe UI", 9.5f),
                    AutoSize = false,
                    Location = new Point(16, y),
                    Size = new Size(260, 20)
                };
                var meta = new Label
                {
                    Text = "检测中…",
                    Tag = "dim",
                    Font = new Font("Segoe UI", 8.5f),
                    AutoSize = false,
                    AutoEllipsis = true,
                    Location = new Point(16, y + 19),
                    Size = new Size(InnerW - 28 - 60, 18)
                };
                var tog = new ToggleSwitch { Location = new Point(InnerW - 28 - 44, y + 4), Enabled = false };

                card.Controls.Add(title);
                card.Controls.Add(meta);
                card.Controls.Add(tog);
                _targetMeta[i] = meta;
                _toggles[i] = tog;
            }
        }

        void BuildActions()
        {
            _applyBtn = new RoundedButton { Text = "应用并重启生效", Primary = true, Location = new Point(PadX, 532), Size = new Size(168, 34) };
            _genBtn = new RoundedButton { Text = "仅生成", Location = new Point(PadX + 180, 532), Size = new Size(90, 34) };
            _rollbackBtn = new RoundedButton { Text = "回滚", Location = new Point(PadX + 282, 532), Size = new Size(80, 34) };
            _cancelBtn = new RoundedButton { Text = "取消排队", Visible = false, Location = new Point(PadX + 374, 532), Size = new Size(96, 34) };

            _applyBtn.Click += async delegate (object s, EventArgs e) { await RunApply(false); };
            _genBtn.Click += async delegate (object s, EventArgs e) { await RunApply(true); };
            _rollbackBtn.Click += async delegate (object s, EventArgs e) { await RunRollback(); };
            _cancelBtn.Click += async delegate (object s, EventArgs e) { await RunCancel(); };

            _pages[0].Controls.Add(_applyBtn);
            _pages[0].Controls.Add(_genBtn);
            _pages[0].Controls.Add(_rollbackBtn);
            _pages[0].Controls.Add(_cancelBtn);

            _status = new Label
            {
                Text = "",
                Tag = "dim",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Location = new Point(PadX, 572),
                Size = new Size(InnerW, 22)
            };
            _pages[0].Controls.Add(_status);
        }

        // ---------------- 行为 ----------------

        async void OnShown(object sender, EventArgs e)
        {
            Theme.Apply("system", this);
            BackColor = Theme.BackPaint;
            _title.ForeColor = Theme.Current.Text;
            Log("程序已以管理员身份运行");
            RefreshTargets();
            await Task.Run(delegate
            {
                try { Installer.FixPendingAcl(delegate (string m) { UiLog(m); }); }
                catch (Exception ex) { UiLog("收尾失败：" + ex.Message); }
            });
            RefreshTargets();
            RecolorInputs();
            if (StartPage >= 0 && _rail != null) _rail.Select(StartPage);
            if (!string.IsNullOrEmpty(_demo)) PickByPath(_demo);
            if (_openPicker) { _openPicker = false; PickFont(); }
            if (!string.IsNullOrEmpty(ShotPath))
            {
                var timer = new System.Windows.Forms.Timer { Interval = 700 };
                timer.Tick += delegate
                {
                    timer.Stop();
                    try
                    {
                        using (var bmp = new Bitmap(Width, Height))
                        {
                            DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height));
                            bmp.Save(ShotPath, System.Drawing.Imaging.ImageFormat.Png);
                        }
                    }
                    catch { }
                    Close();
                };
                timer.Start();
            }
        }

        void RecolorInputs()
        {
            var p = Theme.Current;
            _header.BackColor = p.Bg;
            _rail.BackColor = p.Bg;
            _page.BackColor = p.Bg;
            foreach (var pg in _pages) pg.BackColor = p.Bg;
            for (int i = 0; i < 6; i++)
            {
                if (_winSize[i] != null)
                {
                    _winSize[i].BackColor = p.Surface;
                    _winSize[i].ForeColor = p.Text;
                }
            }
            if (_linkKeys != null) { _linkKeys.BackColor = p.Surface; _linkKeys.ForeColor = p.Text; }
            if (_linkText != null) { _linkText.BackColor = p.Surface; _linkText.ForeColor = p.Text; }
            if (_presetName != null) { _presetName.BackColor = p.Surface; _presetName.ForeColor = p.Text; }
            if (_presetList != null) { _presetList.BackColor = p.Surface; _presetList.ForeColor = p.Text; }
            Refresh();
        }

        void UiLog(string m) { try { BeginInvoke(new Action(delegate { Log(m); })); } catch { } }

        void Log(string m)
        {
            try
            {
                Directory.CreateDirectory(Installer.DataDir);
                File.AppendAllText(Path.Combine(Installer.DataDir, "ui.log"),
                    "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + m + Environment.NewLine);
            }
            catch { }
        }

        void Status(string text, bool error)
        {
            _status.Text = text;
            _status.ForeColor = error ? Theme.Current.Err : Theme.Current.TextDim;
        }

        void PickFont()
        {
            using (var dlg = new FontPickerForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                PickByFamily(dlg.SelectedFamily);
            }
        }

        void PickByFamily(string family)
        {
            var pool = new List<FontEntry>();
            foreach (var e in FontLibrary.All)
                if (string.Equals(e.Family, family, StringComparison.OrdinalIgnoreCase)) pool.Add(e);
            if (pool.Count == 0) { Status("没有找到这个字体的文件", true); return; }

            _picked = FontLibrary.Match(pool, 400);
            if (_picked == null) _picked = pool[0];
            _pool = pool;
            _slots[0] = FontLibrary.Match(pool, 400);
            _slots[1] = FontLibrary.Match(pool, 700);
            _slots[2] = FontLibrary.Match(pool, 300);

            _pickedLabel.Text = family + "   ·   " + pool.Count + " 个字重   ·   " + _picked.FileName;
            _pickedLabel.ForeColor = Theme.Current.Text;
            UpdateSlotLabels();
            UpdateToggles();
            UpdatePreview();
            Log("已选字体：" + family + "，共 " + pool.Count + " 个字重");
        }

        void PickForSlot(int index)
        {
            // 和顶部「选择字体…」用同一个选择器：先选字体家族，再按该字重自动匹配
            using (var dlg = new FontPickerForm())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                var pool = new List<FontEntry>();
                foreach (var e in FontLibrary.All)
                    if (string.Equals(e.Family, dlg.SelectedFamily, StringComparison.OrdinalIgnoreCase)) pool.Add(e);
                if (pool.Count == 0) { Status("没有找到这个字体的文件", true); return; }

                int weight = index == 0 ? 400 : (index == 1 ? 700 : 300);
                var pick = FontLibrary.Match(pool, weight);
                if (pick == null) pick = pool[0];

                _slots[index] = pick;
                UpdateSlotLabels();
                UpdateToggles();
                Log("手动指定" + new[] { "常规", "粗体", "细体" }[index] + "：" + pick.FileName);
            }
        }

        void PickByPath(string path)
        {
            List<FontEntry> entries = FontLibrary.ReadFile(path);
            if (entries.Count == 0) { Status("这个文件不是可用的 TrueType 字体", true); return; }

            _picked = entries[0];
            _pool = FontLibrary.CandidatesFor(_picked);
            _slots[0] = FontLibrary.Match(_pool, 400);
            _slots[1] = FontLibrary.Match(_pool, 700);
            _slots[2] = FontLibrary.Match(_pool, 300);

            _pickedLabel.Text = _picked.Family + "   ·   " + (_picked.Subfamily ?? "") + "   ·   " + _picked.FileName;
            _pickedLabel.ForeColor = Theme.Current.Text;
            UpdateSlotLabels();
            UpdateToggles();
            UpdatePreview();
            Log("已选字体：" + _picked.Family + "（" + _picked.FileName + "），同家族找到 " + _pool.Count + " 个字重");
        }

        void UpdateSlotLabels()
        {
            for (int i = 0; i < 3; i++)
            {
                var e = _slots[i];
                if (e == null)
                {
                    _slotLabels[i].Text = "未匹配 · 保留系统原样";
                    _slotLabels[i].ForeColor = Theme.Current.TextDim;
                }
                else
                {
                    _slotLabels[i].Text = e.FileName + "   w" + e.Weight + "   " + (e.Subfamily ?? "");
                    _slotLabels[i].ForeColor = Theme.Current.Text;
                }
            }
        }

        void UpdateToggles()
        {
            for (int i = 0; i < 3; i++)
            {
                bool has = _slots[i] != null;
                _toggles[i].Enabled = has;
                if (!has) _toggles[i].On = false;
                else if (!_toggles[i].On) _toggles[i].On = true;
            }
        }

        void UpdatePreview()
        {
            if (_pfc != null) { _pfc.Dispose(); _pfc = null; }
            try
            {
                var pfc = new PrivateFontCollection();
                pfc.AddFontFile(_picked.Path);
                if (pfc.Families.Length == 0) throw new InvalidDataException("无法加载");
                _pfc = pfc;
                _preview.Family = pfc.Families[0];
            }
            catch { _preview.Family = null; }
            _preview.Invalidate();
        }

        void RefreshTargets()
        {
            try
            {
                var infos = Installer.Inspect();
                for (int i = 0; i < infos.Count && i < 3; i++)
                {
                    var info = infos[i];
                    var parts = new List<string>();
                    parts.Add(info.Exists ? ("系统 " + (info.Version ?? "?")) : "缺失");
                    if (info.Exists) parts.Add(Installer.HumanSize(info.Size));
                    if (info.Applied) parts.Add("已替换");
                    if (info.Queued) parts.Add("等待重启");
                    _targetMeta[i].Text = string.Join("  ·  ", parts.ToArray());
                }
                bool queued = false;
                foreach (var info in infos) if (info.Queued) queued = true;
                _cancelBtn.Visible = queued;
                if (!queued) Status("选好字体后点「应用」，重启即可生效。", false);
            }
            catch (Exception ex)
            {
                Status("读系统字体状态失败：" + ex.Message, true);
            }
        }

        List<FontSource> CollectSources()
        {
            var list = new List<FontSource>();
            for (int i = 0; i < 3; i++)
            {
                if (_slots[i] == null || !_toggles[i].On) continue;
                var e = _slots[i];
                list.Add(new FontSource
                {
                    TargetFile = Installer.TargetFiles[i],
                    SourcePath = e.Path,
                    FaceIndex = e.FaceIndex,
                    Family = e.Family,
                    Subfamily = e.Subfamily,
                    Data = File.ReadAllBytes(e.Path)
                });
            }
            return list;
        }

        void SetBusy(bool busy, string hint)
        {
            _applyBtn.Enabled = _genBtn.Enabled = _rollbackBtn.Enabled = _cancelBtn.Enabled = !busy;
            _pickBtn.Enabled = !busy;
            foreach (var b in _slotBtns) b.Enabled = !busy;
            UseWaitCursor = busy;
            if (busy) Status(hint, false);
        }

        async Task RunApply(bool generateOnly)
        {
            var list = CollectSources();
            if (list.Count == 0) { Status("先选一个字体。", true); return; }

            if (!generateOnly)
            {
                var msg = "将要替换 " + list.Count + " 个系统字体文件：\r\n\r\n";
                foreach (var s in list) msg += "  · " + Installer.LabelOf(s.TargetFile) + "  " + Path.GetFileName(s.SourcePath) + "\r\n";
                msg += "\r\n原文件会自动备份，重启后生效，可随时回滚。继续？";
                if (MessageBox.Show(this, msg, "确认替换", MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                        MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            }

            SetBusy(true, generateOnly ? "正在生成…" : "正在生成并写入替换队列…");
            try
            {
                var result = await Task.Run(delegate { return Installer.Apply(list, UiLog, generateOnly); });
                if (generateOnly)
                {
                    Status("已生成到 " + result.OutputDir, false);
                    Log("生成目录：" + result.OutputDir);
                }
                else
                {
                    Status("已排入替换队列，重启后生效。", false);
                    Log("备份目录：" + result.BackupDir);
                }
            }
            catch (Exception ex)
            {
                Status(ex.Message, true);
                Log("失败：" + ex);
            }
            finally
            {
                SetBusy(false, "");
                RefreshTargets();
            }
        }

        async Task RunRollback()
        {
            var backups = Installer.Backups();
            if (backups.Count == 0) { Status("没有可用的备份。", true); return; }
            var latest = backups[0];
            if (MessageBox.Show(this, "将从最近一次备份还原：\r\n" + latest.Date.ToString("yyyy-MM-dd HH:mm") +
                    "\r\n\r\n重启后生效。继续？", "回滚", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            SetBusy(true, "正在准备回滚…");
            try
            {
                await Task.Run(delegate { Installer.RollbackFrom(latest.Dir, UiLog); });
                Status("回滚已排入队列，重启后生效。", false);
            }
            catch (Exception ex)
            {
                Status(ex.Message, true);
                Log("回滚失败：" + ex);
            }
            finally
            {
                SetBusy(false, "");
                RefreshTargets();
            }
        }

        async Task RunCancel()
        {
            SetBusy(true, "正在取消…");
            try
            {
                await Task.Run(delegate { Installer.CancelQueue(UiLog); });
                Status("已取消排队。", false);
            }
            catch (Exception ex) { Status(ex.Message, true); }
            finally
            {
                SetBusy(false, "");
                RefreshTargets();
            }
        }
    }
}
