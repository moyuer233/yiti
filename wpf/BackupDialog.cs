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
            Title = L.T("备份管理");
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
            var clear = T.Btn2(L.T("清理暂存目录"), false, 12);
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
            var open = T.Btn2(L.T("打开目录"), false, 13);
            open.Margin = new Thickness(0, 0, 8, 0);
            open.Click += delegate { OpenDir(); };
            var del = T.Btn2(L.T("删除选中"), false, 13);
            del.Margin = new Thickness(0, 0, 8, 0);
            del.Click += delegate { DeleteSelected(); };
            var restore = T.Btn2(L.T("还原到此备份"), true, 13);
            restore.Margin = new Thickness(0, 0, 8, 0);
            restore.Click += delegate { RestoreSelected(); };
            var close = T.Btn2(L.T("关闭"), false, 13);
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
                    + "   " + e.Files.Count + L.T(" 个文件")
                    + "   " + name;
                _list.Items.Add(line);
            }
            _summary.Text = _entries.Count == 0
                ? L.T("还没有备份")
                : L.T("共 ") + _entries.Count + L.T(" 个备份，占用 ") + Installer.HumanSize(total);
            int sdirs; long sbytes;
            Installer.StageUsage(out sdirs, out sbytes);
            _stage.Text = L.T("暂存目录：") + sdirs + L.T(" 个，占用 ") + Installer.HumanSize(sbytes);
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
            var m = MessageBox.Show(this, L.T("清空暂存目录？") + "\n" + L.T("（重启队列里还有待执行项时会拒绝）"), L.T("清理暂存目录"),
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (m != MessageBoxResult.Yes) return;
            string log = "";
            int n = 0;
            try { n = Installer.ClearStage(delegate (string s) { log += s + "; "; }); }
            catch (Exception ex) { log += ex.Message; }
            MessageBox.Show(this, L.T("已清理 ") + n + L.T(" 个目录") + (string.IsNullOrEmpty(log) ? "" : "\n" + log), L.T("清理暂存目录"));
            Load();
        }

        void DeleteSelected()
        {
            var e = Selected;
            if (e == null) return;
            var m = MessageBox.Show(this, L.T("删除这个备份？") + "\n" + Path.GetFileName(e.Dir), L.T("删除选中"),
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (m != MessageBoxResult.Yes) return;
            try { Installer.DeleteBackup(e.Dir); Load(); }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, L.T("删除失败")); }
        }

        void RestoreSelected()
        {
            var e = Selected;
            if (e == null) return;
            var m = MessageBox.Show(this, L.T("还原到这个备份？重启后生效。") + "\n" + Path.GetFileName(e.Dir), L.T("还原到此备份"),
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
            if (m != MessageBoxResult.Yes) return;
            try
            {
                string log = "";
                Installer.RollbackFrom(e.Dir, delegate (string s) { log += s + "; "; });
                MessageBox.Show(this, L.T("已排入重启队列：") + log, L.T("还原到此备份"));
                Close();
            }
            catch (Exception ex) { MessageBox.Show(this, ex.Message, L.T("还原失败")); }
        }
    }
}
