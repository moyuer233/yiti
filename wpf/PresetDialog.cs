using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace WinFontSwap.Wpf
{
    /// <summary>预设管理：把当前的选择存成命名预设，随时加载 / 重命名 / 删除。</summary>
    internal sealed class PresetDialog : Window
    {
        readonly ListBox _list = new ListBox();
        readonly TextBlock _summary = new TextBlock();
        List<PresetStore.Entry> _entries = new List<PresetStore.Entry>();
        readonly Func<Dictionary<string, string>> _collect;
        readonly Action<string, Dictionary<string, string>> _load;

        public PresetDialog(Func<Dictionary<string, string>> collect, Action<string, Dictionary<string, string>> load)
        {
            _collect = collect;
            _load = load;
            Title = L.T("preset.title");
            Width = 720;
            Height = 540;
            MinWidth = 720; MinHeight = 540; MaxWidth = 720; MaxHeight = 540;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = T.BDrawer;
            Foreground = T.Text;
            FontSize = 13;

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 概览
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });   // 按钮

            _summary.FontSize = 12;
            _summary.Foreground = T.TextDim;
            _summary.Margin = new Thickness(0, 0, 0, 10);
            Grid.SetRow(_summary, 0);
            root.Children.Add(_summary);

            _list.SelectionMode = SelectionMode.Single;
            _list.HorizontalContentAlignment = HorizontalAlignment.Stretch;
            _list.Padding = new Thickness(4);
            Grid.SetRow(_list, 1);
            root.Children.Add(_list);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var save = T.Btn2(L.T("preset.save"), true, 13);
            save.Margin = new Thickness(0, 0, 8, 0);
            save.Click += delegate { SaveCurrent(); };
            var loadBtn = T.Btn2(L.T("preset.load"), false, 13);
            loadBtn.Margin = new Thickness(0, 0, 8, 0);
            loadBtn.Click += delegate { LoadSelected(); };
            var rename = T.Btn2(L.T("preset.rename"), false, 13);
            rename.Margin = new Thickness(0, 0, 8, 0);
            rename.Click += delegate { RenameSelected(); };
            var del = T.Btn2(L.T("preset.delete"), false, 13);
            del.Margin = new Thickness(0, 0, 8, 0);
            del.Click += delegate { DeleteSelected(); };
            var open = T.Btn2(L.T("preset.open.folder"), false, 13);
            open.Margin = new Thickness(0, 0, 8, 0);
            open.Click += delegate { OpenDir(); };
            var close = T.Btn2(L.T("preset.close"), false, 13);
            close.Click += delegate { Close(); };
            bar.Children.Add(save);
            bar.Children.Add(loadBtn);
            bar.Children.Add(rename);
            bar.Children.Add(del);
            bar.Children.Add(open);
            bar.Children.Add(close);
            Grid.SetRow(bar, 2);
            root.Children.Add(bar);

            Content = root;
            SourceInitialized += delegate { Styles.DarkTitle(this); };
            Load();
        }

        PresetStore.Entry Selected
        {
            get { return _list.SelectedIndex >= 0 && _list.SelectedIndex < _entries.Count ? _entries[_list.SelectedIndex] : null; }
        }

        void Load()
        {
            _entries = PresetStore.List();
            _list.Items.Clear();
            foreach (var e in _entries)
            {
                var label = e.Name + "    " + e.Date.ToString("yyyy-MM-dd HH:mm")
                    + "    " + e.Slots + L.T("preset.slots") + " · " + e.Shell + L.T("preset.shells");
                _list.Items.Add(T.Label(label, 12, T.Text, false));
            }
            _summary.Text = _entries.Count == 0 ? L.T("preset.none") : L.T("preset.total") + _entries.Count + L.T("preset.items");
            if (_list.Items.Count > 0) _list.SelectedIndex = 0;
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
            var e = Selected;
            if (e == null) return;
            var d = new ConfirmDialog(L.T("preset.delete"), L.T("preset.delete.prompt") + "\n" + e.Name, L.T("dlg.yes"), L.T("dlg.no")) { Owner = this };
            d.ShowDialog();
            if (!d.Result) return;
            try { PresetStore.Delete(e.Name); } catch (Exception ex) { ConfirmDialog.Info(this, L.T("preset.delete"), ex.Message); }
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
