using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinFontSwap.Wpf
{
    /// <summary>备份管理：列出历史备份，可多选删除 / 还原到某一次；顶部可设自动备份与保留数量、清理暂存目录。</summary>
    internal sealed class BackupDialog : Window
    {
        readonly ListBox _list = new ListBox();
        readonly TextBlock _summary = new TextBlock();
        readonly TextBlock _stage = new TextBlock();
        List<Installer.BackupEntry> _entries = new List<Installer.BackupEntry>();
        readonly List<CheckBox> _checks = new List<CheckBox>();
        readonly TextBlock _selInfo = new TextBlock();
        Button _delBtn;
        Button _regBtn;
        readonly CheckBox _selectAll = new CheckBox();
        bool _editMode;
        Panel _batchBar;
        Button _editBtn;
        Button _restoreBtn;

        public BackupDialog() : this(false) { }

        public BackupDialog(bool editMode)
        {
            Title = L.T("backup.backups");
            Width = 760;
            Height = 600;
            MinWidth = 760; MinHeight = 600; MaxWidth = 760; MaxHeight = 600;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = T.BDrawer;
            Foreground = T.Text;
            FontSize = 13;

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 0 概览
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 1 设置
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 2 批量工具条
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });   // 3 列表（占满，出滚动条）
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 4 底部按钮

            var head = new Grid();
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            head.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var info = new StackPanel();
            _summary.FontSize = 12;
            _summary.Foreground = T.TextDim;
            info.Children.Add(_summary);
            _stage.FontSize = 12;
            _stage.Foreground = T.TextFaint;
            _stage.Margin = new Thickness(0, 4, 0, 0);
            info.Children.Add(_stage);
            head.Children.Add(info);
            var headBtns = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 0, 0) };
            _editBtn = T.Btn2(L.T("backup.edit.mode"), false, 12);
            _editBtn.Margin = new Thickness(0, 0, 8, 0);
            _editBtn.Click += delegate { SetEditMode(!_editMode); };
            headBtns.Children.Add(_editBtn);
            var clear = T.Btn2(L.T("backup.clear.staging"), false, 12);
            clear.Click += delegate { ClearStageAction(); };
            headBtns.Children.Add(clear);
            Grid.SetColumn(headBtns, 1);
            head.Children.Add(headBtns);
            head.Margin = new Thickness(0, 0, 0, 8);
            Grid.SetRow(head, 0);
            root.Children.Add(head);

            var settings = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 10) };
            var autoLabel = T.Label(L.T("backup.auto"), 12, T.Text, false);
            autoLabel.VerticalAlignment = VerticalAlignment.Center;
            autoLabel.Margin = new Thickness(0, 0, 8, 0);
            settings.Children.Add(autoLabel);
            var autoSw = T.Switch(Installer.AutoBackup);
            autoSw.VerticalAlignment = VerticalAlignment.Center;
            autoSw.Margin = new Thickness(0, 0, 20, 0);
            autoSw.Click += delegate
            {
                Installer.AutoBackup = autoSw.IsChecked == true;
                Load();
            };
            settings.Children.Add(autoSw);
            var keepLabel = T.Label(L.T("backup.keep"), 12, T.Text, false);
            keepLabel.VerticalAlignment = VerticalAlignment.Center;
            keepLabel.Margin = new Thickness(0, 0, 8, 0);
            settings.Children.Add(keepLabel);
            var keepBox = new TextBox
            {
                Text = Installer.MaxAutoBackups.ToString(),
                Width = 56,
                MinHeight = 30,
                FontSize = 12,
                Padding = new Thickness(6, 4, 6, 4),
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            keepBox.LostFocus += delegate { ApplyKeep(keepBox); };
            keepBox.KeyDown += delegate (object s, KeyEventArgs e) { if (e.Key == Key.Enter) { ApplyKeep(keepBox); e.Handled = true; } };
            settings.Children.Add(keepBox);
            var keepSuffix = T.Label(L.T("backup.keep.unit"), 12, T.TextDim, false);
            keepSuffix.VerticalAlignment = VerticalAlignment.Center;
            keepSuffix.Margin = new Thickness(6, 0, 0, 0);
            settings.Children.Add(keepSuffix);
            Grid.SetRow(settings, 1);
            root.Children.Add(settings);

            // 批量工具条：全选 + 已选计数
            var tools = new Grid { Margin = new Thickness(0, 0, 0, 6) };
            tools.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var allBox = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            _selectAll.VerticalAlignment = VerticalAlignment.Center;
            _selectAll.Margin = new Thickness(0, 0, 8, 0);
            _selectAll.Click += delegate { foreach (var cb in _checks) cb.IsChecked = _selectAll.IsChecked == true; UpdateSelInfo(); };
            allBox.Children.Add(_selectAll);
            var allLabel = T.Label(L.T("backup.select.all"), 12, T.Text, false);
            allLabel.VerticalAlignment = VerticalAlignment.Center;
            allBox.Children.Add(allLabel);
            tools.Children.Add(allBox);
            _selInfo.FontSize = 12;
            _selInfo.Foreground = T.TextDim;
            _selInfo.VerticalAlignment = VerticalAlignment.Center;
            _selInfo.HorizontalAlignment = HorizontalAlignment.Right;
            Grid.SetColumn(_selInfo, 1);
            tools.Children.Add(_selInfo);
            Grid.SetRow(tools, 2);
            root.Children.Add(tools);
            _batchBar = tools;

            _list.SelectionMode = SelectionMode.Single;
            _list.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            _list.Padding = new Thickness(4);
            Grid.SetRow(_list, 3);
            root.Children.Add(_list);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var open = T.Btn2(L.T("backup.open.folder"), false, 13);
            open.Margin = new Thickness(0, 0, 8, 0);
            open.Click += delegate { OpenDir(); };
            var del = T.Btn2(L.T("backup.delete.selected"), false, 13);
            del.Margin = new Thickness(0, 0, 8, 0);
            del.Click += delegate { DeleteSelected(); };
            _delBtn = del;
            var restore = T.Btn2(L.T("backup.restore.this.backup"), true, 13);
            restore.Margin = new Thickness(0, 0, 8, 0);
            restore.Click += delegate { RestoreSelected(); };
            _restoreBtn = restore;
            var regBtn = T.Btn2(L.T("backup.restore.user.registry"), false, 13);
            regBtn.Margin = new Thickness(0, 0, 8, 0);
            regBtn.Click += delegate { RestoreUserRegistry(); };
            _regBtn = regBtn;
            var close = T.Btn2(L.T("backup.close"), false, 13);
            close.Click += delegate { Close(); };
            bar.Children.Add(open);
            bar.Children.Add(del);
            bar.Children.Add(restore);
            bar.Children.Add(regBtn);
            bar.Children.Add(close);
            Grid.SetRow(bar, 4);
            root.Children.Add(bar);

            Content = root;
            SourceInitialized += delegate { Styles.DarkTitle(this); };
            SetEditMode(editMode);
        }

        void ApplyKeep(TextBox box)
        {
            int n;
            if (!int.TryParse((box.Text ?? "").Trim(), out n) || n < 1 || n > 200)
            {
                box.Text = Installer.MaxAutoBackups.ToString();
                return;
            }
            Installer.MaxAutoBackups = n;
            try { Installer.PruneAutoBackups(n, null, null); } catch { }
            Load();
        }

        Installer.BackupEntry Selected
        {
            get { return _list.SelectedIndex >= 0 && _list.SelectedIndex < _entries.Count ? _entries[_list.SelectedIndex] : null; }
        }

        void Load()
        {
            _entries = Installer.ListBackups();
            _list.Items.Clear();
            _checks.Clear();
            _selectAll.IsChecked = false;
            long total = 0;
            foreach (var e in _entries)
            {
                total += e.Bytes;
                var name = Path.GetFileName(e.Dir);
                var row = new Grid { Background = Brushes.Transparent };   // 整行可命中，悬停才稳
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                if (_editMode)
                {
                    var cb = new CheckBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
                    cb.Click += delegate { UpdateSelInfo(); };
                    row.Children.Add(cb);
                    _checks.Add(cb);
                }
                var txt = T.Label(e.Date.ToString("yyyy-MM-dd HH:mm") + "   " + Installer.HumanSize(e.Bytes).PadLeft(9)
                    + "   " + e.Files.Count + L.T("backup.files") + "   " + name, 12, T.Text, false);
                txt.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(txt, 1);
                row.Children.Add(txt);
                // 悬停时才现算：算一次要读 5 个字体文件，列表一打开就把 20 份全算会卡住界面
                var entry = e;
                row.ToolTip = " ";
                row.ToolTipOpening += delegate { row.ToolTip = BuildTip(entry); };
                ToolTipService.SetInitialShowDelay(row, 350);
                ToolTipService.SetShowDuration(row, 60000);
                ToolTipService.SetPlacement(row, System.Windows.Controls.Primitives.PlacementMode.Mouse);   // 跟着鼠标走
                ToolTipService.SetHorizontalOffset(row, 12);
                ToolTipService.SetVerticalOffset(row, 16);
                _list.Items.Add(row);
            }
            UpdateSelInfo();
            if (_regBtn != null)
            {
                bool hasReg = Installer.FindNewestUserFontReg() != null;
                _regBtn.IsEnabled = hasReg;
                _regBtn.Opacity = hasReg ? 1.0 : 0.45;
                _regBtn.ToolTip = hasReg ? L.T("backup.restore.user.registry.desc") : L.T("backup.no.user.registry.backup");
                ToolTipService.SetShowOnDisabled(_regBtn, true);
            }
            _summary.Text = _entries.Count == 0
                ? L.T("backup.no.backups.yet")
                : L.T("backup.total") + _entries.Count + L.T("backup.backups.using") + Installer.HumanSize(total);
            int sdirs; long sbytes;
            Installer.StageUsage(out sdirs, out sbytes);
            _stage.Text = L.T("backup.staging") + sdirs + L.T("backup.using") + Installer.HumanSize(sbytes);
        }

        /// <summary>备份悬停提示：当时写入了哪些字体、现在还有几个文件在用、备份里能还原什么。</summary>
        string BuildTip(Installer.BackupEntry e)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine(e.Date.ToString("yyyy-MM-dd HH:mm") + "   " + Installer.HumanSize(e.Bytes) + "   " + Path.GetFileName(e.Dir));
            if (!e.HasState)
            {
                sb.AppendLine(L.T("backup.tip.nostate"));
                return sb.ToString().TrimEnd();
            }
            sb.AppendLine(L.T("backup.tip.written"));
            int inUse = 0, total = 0, changedCount = 0;
            foreach (var f in Installer.TargetFiles)
            {
                if (!e.Families.ContainsKey(f) && !e.Shas.ContainsKey(f)) continue;
                // 只列这份备份里真的有文件的条目：没有原文件就没法说“从哪个字体改成了哪个”
                if (e.Files.Count > 0 && !e.Files.Contains(f)) continue;
                total++;
                string fam;
                e.Families.TryGetValue(f, out fam);
                if (string.IsNullOrEmpty(fam)) fam = "—";
                string want;
                bool used = false;
                if (e.Shas.TryGetValue(f, out want) && !string.IsNullOrEmpty(want))
                {
                    try
                    {
                        var now = Installer.Sha256(Path.Combine(Installer.FontsDir, f));
                        used = string.Equals(now, want, StringComparison.OrdinalIgnoreCase);
                    }
                    catch { }
                }
                // 这次备份到底改没改这个文件：备份里的原文件 与 这次写进去的内容 不同才算改过
                bool changed = true;
                var orig = Path.Combine(e.Dir, "orig", f);
                if (!File.Exists(orig)) orig = Path.Combine(e.Dir, f);
                if (File.Exists(orig) && e.Shas.ContainsKey(f))
                {
                    try { changed = !string.Equals(Installer.Sha256(orig), want, StringComparison.OrdinalIgnoreCase); }
                    catch { }
                }
                if (used) inUse++;
                if (changed) changedCount++;
                // 改之前是什么字体：优先按版权/字形反查备份里的原文件，认不出就退回它的内部族名
                string before = Installer.DetectFamilyOfPath(orig, null);
                if (string.IsNullOrEmpty(before)) before = Installer.InternalFamilyOfPath(orig);
                if (string.IsNullOrEmpty(before)) before = "?";
                bool same = string.Equals((before ?? "").Replace(" ", ""), (fam ?? "").Replace(" ", ""), StringComparison.OrdinalIgnoreCase);
                string line = "   " + f.PadRight(13) + " ";
                if (changed && !same)
                {
                    // ✎ 放在“改成了什么”那一侧：一眼看出和之前不一样的是新字体
                    line += before + " → " + L.T("backup.tip.changed") + " " + fam;
                }
                else
                {
                    line += fam + "  " + L.T("backup.tip.same");
                }
                if (used) line += "  " + L.T("backup.tip.inuse");
                sb.AppendLine(line);
            }
            if (total > 0)
            {
                sb.AppendLine(L.T("backup.tip.result") + L.T("backup.tip.changed.count") + changedCount + L.T("backup.tip.items")
                    + "，" + inUse + "/" + total + L.T("backup.tip.still.in.use"));
            }
            sb.AppendLine(L.T("backup.tip.restorable") + e.Files.Count + L.T("backup.files"));
            return sb.ToString().TrimEnd();
        }

        /// <summary>编辑模式：显示勾选框、全选和删除；普通模式只留查看与还原。</summary>
        void SetEditMode(bool on)
        {
            _editMode = on;
            if (_batchBar != null) _batchBar.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            if (_delBtn != null) _delBtn.Visibility = on ? Visibility.Visible : Visibility.Collapsed;
            if (_editBtn != null) _editBtn.Content = L.T(on ? "backup.edit.done" : "backup.edit.mode");
            // 编辑模式只做批量管理：两个还原入口都收起来，避免"勾着多份还能点还原"的歧义
            if (_restoreBtn != null) _restoreBtn.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
            if (_regBtn != null) _regBtn.Visibility = on ? Visibility.Collapsed : Visibility.Visible;
            Load();
        }

        void UpdateSelInfo()
        {
            int n = 0;
            foreach (var cb in _checks) if (cb.IsChecked == true) n++;
            _selInfo.Text = L.T("backup.selected") + n + L.T("backup.selected.unit");
            if (_delBtn != null) _delBtn.Content = L.T("backup.delete.selected") + (n > 0 ? " (" + n + ")" : "");
        }

        bool Ask(string question, string title)
        {
            var dlg = new ConfirmDialog(title, question, L.T("dlg.yes"), L.T("dlg.no")) { Owner = this };
            dlg.ShowDialog();
            return dlg.Result;
        }

        void OpenDir()
        {
            try
            {
                Directory.CreateDirectory(Installer.BackupRoot);
                System.Diagnostics.Process.Start("explorer.exe", "\"" + Installer.BackupRoot + "\"");
            }
            catch { }
        }

        void ClearStageAction()
        {
            if (!Ask(L.T("backup.clear.the.staging.directory") + "\n" + L.T("backup.refused.while.the.reboot.queue.still.holds.o"), L.T("backup.clear.staging"))) return;
            string log = "";
            int n = 0;
            try { n = Installer.ClearStage(delegate (string s) { log += s + "; "; }); }
            catch (Exception ex) { log += ex.Message; }
            ConfirmDialog.Info(this, L.T("backup.clear.staging"), L.T("backup.cleared") + n + L.T("backup.folders") + (string.IsNullOrEmpty(log) ? "" : "\n" + log));
            Load();
        }

        void RestoreUserRegistry()
        {
            var e = Selected;
            string from = e == null ? null : e.Dir;
            string reg = Installer.FindUserFontRegIn(from) ?? Installer.FindNewestUserFontReg();
            if (reg == null) return;
            if (!Ask(L.T("backup.restore.user.registry.prompt") + "\n" + Path.GetFileName(reg), L.T("backup.restore.user.registry"))) return;
            string msg;
            try { msg = Installer.RestoreUserFontsFrom(from); }
            catch (Exception ex) { msg = ex.Message; }
            ConfirmDialog.Info(this, L.T("backup.restore.user.registry"), msg);
        }

        void DeleteSelected()
        {
            var picked = new List<Installer.BackupEntry>();
            for (int i = 0; i < _checks.Count && i < _entries.Count; i++)
                if (_checks[i].IsChecked == true) picked.Add(_entries[i]);
            if (picked.Count == 0) return;
            string names = "";
            foreach (var e in picked) names += "\n" + Path.GetFileName(e.Dir);
            if (!Ask(L.T("backup.delete.selected.prompt") + names, L.T("backup.delete.selected"))) return;
            int ok = 0, fail = 0;
            foreach (var e in picked)
            {
                try { Installer.DeleteBackup(e.Dir); ok++; }
                catch { fail++; }
            }
            ConfirmDialog.Info(this, L.T("backup.delete.selected"),
                L.T("backup.deleted") + ok + (fail > 0 ? "  " + L.T("backup.delete.failed") + fail : ""));
            Load();
        }

        void RestoreSelected()
        {
            // 编辑模式下还原按钮是隐藏的，正常模式没有勾选框，所以这里只看高亮行
            var e = Selected;
            if (e == null)
            {
                ConfirmDialog.Info(this, L.T("backup.restore.this.backup"), L.T("backup.restore.pick.one"));
                return;
            }
            if (!Ask(L.T("backup.restore.this.backup.it.takes.effect.after.a.") + "\n" + Path.GetFileName(e.Dir), L.T("backup.restore.this.backup"))) return;
            try
            {
                string log = "";
                Installer.RollbackFrom(e.Dir, delegate (string s) { log += s + "; "; });
                ConfirmDialog.Info(this, L.T("backup.restore.this.backup"), L.T("backup.queued.for.the.next.reboot") + log);
                Close();
            }
            catch (Exception ex) { ConfirmDialog.Info(this, L.T("backup.restore.failed"), ex.Message); }
        }
    }
}
