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
        const int W = 900, H = 700, PadX = 24;
        const int InnerW = W - PadX * 2;

        string _demo;
        internal static bool _openPicker;

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
            if (Theme.Glass) Opacity = 0.94;   // 整体轻微透明：边缘抗锯齿不会被色键切出暗边
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint, true);

            BuildHeader();
            BuildFontCard();
            BuildPreviewCard();
            BuildTargetCard();
            BuildActions();

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
            e.Graphics.Clear(Theme.BackPaint);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            EnableGlass();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var b = new SolidBrush(Theme.BackPaint)) e.Graphics.FillRectangle(b, ClientRectangle);
        }

        void BuildHeader()
        {
            _title = new Label
            {
                Text = "字体替换",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                AutoSize = false,
                Location = new Point(PadX, 16),
                Size = new Size(500, 34)
            };
            _subtitle = new Label
            {
                Text = "选一个字体，粗体细体自动匹配；重启后设置、开始菜单一起变。",
                Tag = "dim",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Location = new Point(PadX, 52),
                Size = new Size(W - PadX * 2, 22)
            };
            Controls.Add(_title);
            Controls.Add(_subtitle);
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
            var card = new CardPanel { Location = new Point(PadX, 86), Size = new Size(InnerW, 200) };
            Controls.Add(card);
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
            var card = new CardPanel { Location = new Point(PadX, 298), Size = new Size(InnerW, 118) };
            Controls.Add(card);
            card.Controls.Add(Header("预览", 16, 10));
            _preview = new PreviewBox { Location = new Point(16, 38), Size = new Size(InnerW - 32, 68) };
            card.Controls.Add(_preview);
        }

        void BuildTargetCard()
        {
            var card = new CardPanel { Location = new Point(PadX, 428), Size = new Size(InnerW, 168) };
            Controls.Add(card);
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
            _applyBtn = new RoundedButton { Text = "应用并重启生效", Primary = true, Location = new Point(PadX, 608), Size = new Size(168, 34) };
            _genBtn = new RoundedButton { Text = "仅生成", Location = new Point(PadX + 180, 608), Size = new Size(90, 34) };
            _rollbackBtn = new RoundedButton { Text = "回滚", Location = new Point(PadX + 282, 608), Size = new Size(80, 34) };
            _cancelBtn = new RoundedButton { Text = "取消排队", Visible = false, Location = new Point(PadX + 374, 608), Size = new Size(96, 34) };

            _applyBtn.Click += async delegate (object s, EventArgs e) { await RunApply(false); };
            _genBtn.Click += async delegate (object s, EventArgs e) { await RunApply(true); };
            _rollbackBtn.Click += async delegate (object s, EventArgs e) { await RunRollback(); };
            _cancelBtn.Click += async delegate (object s, EventArgs e) { await RunCancel(); };

            Controls.Add(_applyBtn);
            Controls.Add(_genBtn);
            Controls.Add(_rollbackBtn);
            Controls.Add(_cancelBtn);

            _status = new Label
            {
                Text = "",
                Tag = "dim",
                Font = new Font("Segoe UI", 9f),
                AutoSize = false,
                Location = new Point(PadX, 648),
                Size = new Size(InnerW, 22)
            };
            Controls.Add(_status);
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
            if (!string.IsNullOrEmpty(_demo)) PickByPath(_demo);
            if (_openPicker) { _openPicker = false; PickFont(); }
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
            using (var dlg = new OpenFileDialog())
            {
                dlg.Filter = "字体文件 (*.ttf;*.ttc)|*.ttf;*.ttc";
                dlg.Title = "指定" + new[] { "常规", "粗体", "细体" }[index] + "字重";
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                var entries = FontLibrary.ReadFile(dlg.FileName);
                if (entries.Count == 0) { Status("这个文件读不出字体名", true); return; }
                _slots[index] = entries[0];
                UpdateSlotLabels();
                UpdateToggles();
                Log("手动指定" + new[] { "常规", "粗体", "细体" }[index] + "：" + entries[0].FileName);
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
