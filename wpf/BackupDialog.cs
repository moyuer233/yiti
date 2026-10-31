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

        public BackupDialog()
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
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

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
            var clear = T.Btn2(L.T("backup.clear.staging"), false, 12);
            clear.VerticalAlignment = VerticalAlignment.Center;
            clear.Margin = new Thickness(12, 0, 0, 0);
            clear.Click += delegate { ClearStageAction(); };
            Grid.SetColumn(clear, 1);
            head.Children.Add(clear);
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
                Height = 28,
                FontSize = 12,
                Padding = new Thickness(4),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center
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

            _list.SelectionMode = SelectionMode.Extended;
            _list.HorizontalContentAlignment = HorizontalAlignment.Left;
            _list.Padding = new Thickness(4);
            Grid.SetRow(_list, 2);
            root.Children.Add(_list);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var open = T.Btn2(L.T("backup.open.folder"), false, 13);
            open.Margin = new Thickness(0, 0, 8, 0);
            open.Click += delegate { OpenDir(); };
            var del = T.Btn2(L.T("backup.delete.selected"), false, 13);
            del.Margin = new Thickness(0, 0, 8, 0);
            del.Click += delegate { DeleteSelected(); };
            var restore = T.Btn2(L.T("backup.restore.this.backup"), true, 13);
            restore.Margin = new Thickness(0, 0, 8, 0);
            restore.Click += delegate { RestoreSelected(); };
            var close = T.Btn2(L.T("backup.close"), false, 13);
            close.Click += delegate { Close(); };
            bar.Children.Add(open);
            bar.Children.Add(del);
            bar.Children.Add(restore);
            bar.Children.Add(close);
            Grid.SetRow(bar, 3);
            root.Children.Add(bar);

            Content = root;
            SourceInitialized += delegate { Styles.DarkTitle(this); };
            Load();
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
            long total = 0;
            foreach (var e in _entries)
            {
                total += e.Bytes;
                var name = Path.GetFileName(e.Dir);
                _list.Items.Add(e.Date.ToString("yyyy-MM-dd HH:mm") + "   " + Installer.HumanSize(e.Bytes).PadLeft(9)
                    + "   " + e.Files.Count + L.T("backup.files") + "   " + name);
            }
            _summary.Text = _entries.Count == 0
                ? L.T("backup.no.backups.yet")
                : L.T("backup.total") + _entries.Count + L.T("backup.backups.using") + Installer.HumanSize(total);
            int sdirs; long sbytes;
            Installer.StageUsage(out sdirs, out sbytes);
            _stage.Text = L.T("backup.staging") + sdirs + L.T("backup.using") + Installer.HumanSize(sbytes);
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

        void DeleteSelected()
        {
            var picked = new List<Installer.BackupEntry>();
            foreach (var item in _list.SelectedItems)
            {
                int idx = _list.Items.IndexOf(item);
                if (idx >= 0 && idx < _entries.Count) picked.Add(_entries[idx]);
            }
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
            var e = Selected;
            if (e == null) return;
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
