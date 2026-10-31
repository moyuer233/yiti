using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinFontSwap.Wpf
{
    /// <summary>备份管理：列出 C:\ProgramData\WinFontSwap\backup 下的历史备份，可删除 / 还原到某一次。</summary>
    internal sealed class BackupDialog : Window
    {
        readonly ListBox _list = new ListBox();
        readonly TextBlock _summary = new TextBlock();
        readonly TextBlock _stage = new TextBlock();
        List<Installer.BackupEntry> _entries = new List<Installer.BackupEntry>();

        public BackupDialog()
        {
            Title = L.T("backup.backups");
            Width = 720;
            Height = 560;
            MinWidth = 720; MinHeight = 560; MaxWidth = 720; MaxHeight = 560;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = T.BDrawer;
            Foreground = T.Text;
            FontSize = 13;

            var root = new Grid { Margin = new Thickness(16) };
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
            _stage.Text = "";
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
            head.Margin = new Thickness(0, 0, 0, 10);
            Grid.SetRow(head, 0);
            root.Children.Add(head);

            _list.HorizontalContentAlignment = HorizontalAlignment.Left;
            _list.Padding = new Thickness(4, 4, 4, 4);
            Grid.SetRow(_list, 1);
            root.Children.Add(_list);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var open = T.Btn2(L.T("backup.open.folder"), false, 13);
            open.Margin = new Thickness(0, 0, 8, 0);
            open.Click += delegate { OpenDir(); };
            var del = T.Btn2(L.T("backup.delete"), false, 13);
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
            Grid.SetRow(bar, 2);
            root.Children.Add(bar);

            Content = root;
            SourceInitialized += delegate { Styles.DarkTitle(this); };
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
                var line = e.Date.ToString("yyyy-MM-dd HH:mm") + "   " + Installer.HumanSize(e.Bytes).PadLeft(9)
                    + "   " + e.Files.Count + L.T("backup.files")
                    + "   " + name;
                _list.Items.Add(line);
            }
            _summary.Text = _entries.Count == 0
                ? L.T("backup.no.backups.yet")
                : L.T("backup.total") + _entries.Count + L.T("backup.backups.using") + Installer.HumanSize(total);
            int sdirs; long sbytes;
            Installer.StageUsage(out sdirs, out sbytes);
            _stage.Text = L.T("backup.staging") + sdirs + L.T("backup.using") + Installer.HumanSize(sbytes);
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
            var m = MessageBox.Show(this, L.T("backup.clear.the.staging.directory") + "\n" + L.T("backup.refused.while.the.reboot.queue.still.holds.o"), L.T("backup.clear.staging"),
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (m != MessageBoxResult.Yes) return;
            string log = "";
            int n = 0;
            try { n = Installer.ClearStage(delegate (string s) { log += s + "; "; }); }
            catch (Exception ex) { log += ex.Message; }
            MessageBox.Show(this, L.T("backup.cleared") + n + L.T("backup.folders") + (string.IsNullOrEmpty(log) ? "" : "\n" + log), L.T("backup.clear.staging"));
            Load();
        }

        void DeleteSelected()
        {
            var e = Selected;
            if (e == null) return;
            var m = MessageBox.Show(this, L.T("backup.delete.this.backup") + "\n" + Path.GetFileName(e.Dir), L.T("backup.delete"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (m != MessageBoxResult.Yes) return;
            try { Installer.DeleteBackup(e.Dir); Load(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, L.T("backup.delete.failed")); }
        }

        void RestoreSelected()
        {
            var e = Selected;
            if (e == null) return;
            var m = MessageBox.Show(this, L.T("backup.restore.this.backup.it.takes.effect.after.a.") + "\n" + Path.GetFileName(e.Dir), L.T("backup.restore.this.backup"),
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (m != MessageBoxResult.Yes) return;
            try
            {
                string log = "";
                Installer.RollbackFrom(e.Dir, delegate (string s) { log += s + "; "; });
                MessageBox.Show(this, L.T("backup.queued.for.the.next.reboot") + log, L.T("backup.restore.this.backup"));
                Close();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, L.T("backup.restore.failed")); }
        }
    }
}
