using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WinFontSwap.Wpf
{
    /// <summary>预设管理：保存/加载/重命名/删除；编辑模式下可批量删除，悬停可预览预设内容。</summary>
    internal sealed class PresetDialog : Window
    {
        readonly ListBox _list = new ListBox();
        readonly TextBlock _summary = new TextBlock();
        readonly TextBlock _selInfo = new TextBlock();
        readonly CheckBox _selectAll = new CheckBox();
        readonly List<CheckBox> _checks = new List<CheckBox>();
        List<PresetStore.Entry> _entries = new List<PresetStore.Entry>();
        readonly Func<Dictionary<string, string>> _collect;
        readonly Action<string, Dictionary<string, string>> _load;
        bool _editMode;
        Panel _batchBar;
        Button _editBtn, _loadBtn, _renameBtn, _delBtn, _saveBtn;

        public PresetDialog(Func<Dictionary<string, string>> collect, Action<string, Dictionary<string, string>> load) : this(collect, load, false) { }

        public PresetDialog(Func<Dictionary<string, string>> collect, Action<string, Dictionary<string, string>> load, bool editMode)
        {
            _collect = collect;
            _load = load;
            Title = L.T("preset.title");
            Width = 720;
            Height = 560;
            MinWidth = 720; MinHeight = 560; MaxWidth = 720; MaxHeight = 560;
            ResizeMode = ResizeMode.NoResize;   // 这个窗口只保留关闭按钮，不给最小化/最大化
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Foreground = T.Text;
            FontSize = 13;

            var root = new Grid { Margin = new Thickness(16, 52, 16, 16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 概览
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 批量工具条
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 按钮

            _summary.FontSize = 12;
            _summary.Foreground = T.TextDim;
            _summary.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetRow(_summary, 0);
            root.Children.Add(_summary);

            // 批量工具条（仅编辑模式显示）
            var tools = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var allBox = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _selectAll.Margin = new Thickness(0, 0, 8, 0);
            _selectAll.Click += delegate { foreach (var cb in _checks) cb.IsChecked = _selectAll.IsChecked == true; UpdateSelInfo(); };
            allBox.Children.Add(_selectAll);
            allBox.Children.Add(T.Label(L.T("preset.select.all"), 12, T.Text, false));
            tools.Children.Add(allBox);
            _selInfo.FontSize = 12;
            _selInfo.Foreground = T.TextDim;
            _selInfo.HorizontalAlignment = HorizontalAlignment.Right;
            _selInfo.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(_selInfo, 1);
            tools.Children.Add(_selInfo);
            Grid.SetRow(tools, 1);
            root.Children.Add(tools);
            _batchBar = tools;

            _list.SelectionMode = SelectionMode.Single;
            _list.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            _list.Padding = new Thickness(4);
            Grid.SetRow(_list, 2);
            root.Children.Add(_list);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            _saveBtn = T.Btn2(L.T("preset.save"), true, 13);
            _saveBtn.Margin = new Thickness(0, 0, 8, 0);
            _saveBtn.Click += delegate { SaveCurrent(); };
            _loadBtn = T.Btn2(L.T("preset.load"), false, 13);
            _loadBtn.Margin = new Thickness(0, 0, 8, 0);
            _loadBtn.Click += delegate { LoadSelected(); };
            _renameBtn = T.Btn2(L.T("preset.rename"), false, 13);
            _renameBtn.Margin = new Thickness(0, 0, 8, 0);
            _renameBtn.Click += delegate { RenameSelected(); };
            _delBtn = T.Btn2(L.T("preset.delete"), false, 13);
            _delBtn.Margin = new Thickness(0, 0, 8, 0);
            _delBtn.Click += delegate { DeleteSelected(); };
            _editBtn = T.Btn2(L.T("preset.edit.mode"), false, 13);
            _editBtn.Margin = new Thickness(0, 0, 8, 0);
            _editBtn.Click += delegate { SetEditMode(!_editMode); };
            var open = T.Btn2(L.T("preset.open.folder"), false, 13);
            open.Margin = new Thickness(0, 0, 8, 0);
            open.Click += delegate { OpenDir(); };
            var close = T.Btn2(L.T("preset.close"), false, 13);
            close.Click += delegate { Close(); };
            bar.Children.Add(_saveBtn);
            bar.Children.Add(_loadBtn);
            bar.Children.Add(_renameBtn);
            bar.Children.Add(_delBtn);
            bar.Children.Add(_editBtn);
            bar.Children.Add(open);
            bar.Children.Add(close);
            Grid.SetRow(bar, 3);
            root.Children.Add(bar);

            Content = Styles.DialogFrame(this, root, Title);   // 和主页面一样：无系统标题栏，自绘 ×
            SourceInitialized += delegate { Styles.DarkTitle(this); };
            SetEditMode(editMode);
        }

        PresetStore.Entry Selected
        {
            get { return _list.SelectedIndex >= 0 && _list.SelectedIndex < _entries.Count ? _entries[_list.SelectedIndex] : null; }
        }

        void SetEditMode(bool on)
        {
            _editMode = on;
            if (_batchBar != null) _batchBar.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            if (_loadBtn != null) _loadBtn.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
            if (_renameBtn != null) _renameBtn.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
            if (_delBtn != null) _delBtn.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            if (_saveBtn != null) _saveBtn.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
            if (_editBtn != null) _editBtn.Content = L.T(on ? "preset.edit.done" : "preset.edit.mode");
            Load();
        }

        void UpdateSelInfo()
        {
            int n = 0;
            foreach (var cb in _checks) if (cb.IsChecked == true) n++;
            _selInfo.Text = L.T("preset.selected") + n + L.T("preset.selected.unit");
            if (_delBtn != null) _delBtn.Content = L.T("preset.delete.selected") + (n > 0 ? " (" + n + ")" : "");
        }

        void Load()
        {
            _entries = PresetStore.List();
            _list.Items.Clear();
            _checks.Clear();
            _selectAll.IsChecked = false;
            foreach (var e in _entries)
            {
                var row = new Grid { Background = Brushes.Transparent };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                if (_editMode)
                {
                    var cb = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
                    cb.Click += delegate { UpdateSelInfo(); };
                    row.Children.Add(cb);
                    _checks.Add(cb);
                }
                var entry = e;
                var label = T.Label(e.Name + "    " + e.Date.ToString("yyyy-MM-dd HH:mm")
                    + "    " + e.Slots + L.T("preset.slots") + " · " + e.Shell + L.T("preset.shells"), 12, T.Text, false);
                label.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(label, 1);
                row.Children.Add(label);
                row.ToolTip = " ";
                row.ToolTipOpening += delegate { row.ToolTip = BuildTip(entry); };
                ToolTipService.SetInitialShowDelay(row, 350);
                ToolTipService.SetShowDuration(row, 60000);
                ToolTipService.SetPlacement(row, System.Windows.Controls.Primitives.PlacementMode.Mouse);
                ToolTipService.SetHorizontalOffset(row, 12);
                ToolTipService.SetVerticalOffset(row, 16);
                _list.Items.Add(row);
            }
            UpdateSelInfo();
            _summary.Text = _entries.Count == 0 ? L.T("preset.none") : L.T("preset.total") + _entries.Count + L.T("preset.items");
            if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        }

        /// <summary>悬停预览：这份预设里的系统字体、窗口字体和开关。</summary>
        string BuildTip(PresetStore.Entry e)
        {
            var sb = new StringBuilder();
            sb.AppendLine(e.Name + "    " + e.Date.ToString("yyyy-MM-dd HH:mm"));
            var v = PresetStore.Read(e.Name);
            string t;
            sb.AppendLine(L.T("preset.tip.fonts"));
            bool anyFont = false;
            for (int i = 0; i < Installer.TargetFiles.Length; i++)
            {
                if (!v.TryGetValue("slot." + i + ".on", out t) || t == "0") continue;
                string family;
                if (!v.TryGetValue("slot." + i + ".family", out family) || string.IsNullOrEmpty(family)) continue;
                sb.AppendLine("   " + SlotLabel(i) + "  ←  " + family);
                anyFont = true;
            }
            if (!anyFont) sb.AppendLine("   " + L.T("preset.tip.empty"));
            sb.AppendLine(L.T("preset.tip.shell"));
            bool anyShell = false;
            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                if (!v.TryGetValue("shell." + i + ".on", out t) || t == "0") continue;
                string face, pt;
                if (!v.TryGetValue("shell." + i + ".face", out face) || string.IsNullOrEmpty(face)) continue;
                v.TryGetValue("shell." + i + ".pt", out pt);
                sb.AppendLine("   " + Win32Fonts.Names[i] + "  ←  " + face + "  " + (pt ?? "9") + " pt");
                anyShell = true;
            }
            if (!anyShell) sb.AppendLine("   " + L.T("preset.tip.empty"));
            sb.AppendLine(L.T("preset.tip.switches"));
            string aw, ae;
            v.TryGetValue("auto.weight", out aw);
            v.TryGetValue("auto.explorer", out ae);
            sb.AppendLine("   " + L.T("preset.tip.auto.weight") + "：" + (aw == "0" ? L.T("preset.tip.off") : L.T("preset.tip.on")));
            sb.AppendLine("   " + L.T("preset.tip.auto.explorer") + "：" + (ae == "0" ? L.T("preset.tip.off") : L.T("preset.tip.on")));
            return sb.ToString().TrimEnd();
        }

        /// <summary>预设预览里系统字体槽的名字（避免 SegUIVar 和 segoeui 都叫「英文数字」）。</summary>
        static string SlotLabel(int i)
        {
            switch (i)
            {
                case 0: return L.T("font.regular");
                case 1: return L.T("font.bold");
                case 2: return L.T("font.light");
                case 3: return L.T("font.latin.digits");
                default: return "Segoe UI";
            }
        }

        void SaveCurrent()
        {
            var values = _collect != null ? _collect() : null;
            if (values == null || values.Count == 0) return;
            var def = L.T("preset.default.name") + (_entries.Count + 1);
            var name = PromptDialog.Ask(this, L.T("preset.name.title"), L.T("preset.name.prompt"), def);
            if (name == null) return;
            var safe = PresetStore.Sanitize(name);
            if (PresetStore.Exists(safe))
            {
                var d = new ConfirmDialog(L.T("preset.save"), L.T("preset.overwrite") + "\n" + safe, L.T("dlg.yes"), L.T("dlg.no")) { Owner = this };
                d.ShowDialog();
                if (!d.Result) return;
            }
            PresetStore.Save(safe, values);
            Load();
        }

        void LoadSelected()
        {
            var e = Selected;
            if (e == null) return;
            var values = PresetStore.Read(e.Name);
            if (_load != null) _load(e.Name, values);
            Close();
        }

        void RenameSelected()
        {
            var e = Selected;
            if (e == null) return;
            var name = PromptDialog.Ask(this, L.T("preset.name.rename"), L.T("preset.name.rename.prompt"), e.Name);
            if (name == null) return;
            try { PresetStore.Rename(e.Name, name); }
            catch (Exception ex) { ConfirmDialog.Info(this, L.T("preset.rename"), ex.Message); }
            Load();
        }

        void DeleteSelected()
        {
            var picked = new List<PresetStore.Entry>();
            if (_editMode)
            {
                for (int i = 0; i < _checks.Count && i < _entries.Count; i++)
                    if (_checks[i].IsChecked == true) picked.Add(_entries[i]);
            }
            else
            {
                var e = Selected;
                if (e != null) picked.Add(e);
            }
            if (picked.Count == 0) return;
            string names = "";
            foreach (var e in picked) names += "\n" + e.Name;
            var d = new ConfirmDialog(L.T("preset.delete"), L.T("preset.delete.prompt") + names, L.T("dlg.yes"), L.T("dlg.no")) { Owner = this };
            d.ShowDialog();
            if (!d.Result) return;
            int ok = 0, fail = 0;
            foreach (var e in picked)
            {
                try { PresetStore.Delete(e.Name); ok++; } catch { fail++; }
            }
            ConfirmDialog.Info(this, L.T("preset.delete"), L.T("preset.deleted") + ok + (fail > 0 ? "  " + L.T("backup.delete.failed") + fail : ""));
            Load();
        }

        void OpenDir()
        {
            try
            {
                Directory.CreateDirectory(PresetStore.Root);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + PresetStore.Root + "\"");
            }
            catch { }
        }
    }
}
