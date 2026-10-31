using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinFontSwap.Wpf
{
    /// <summary>字体选择器：搜索 + 每行用该字体自身渲染预览。</summary>
    internal class FontPickerDialog : Window
    {
        readonly ListBox _list = new ListBox();
        readonly TextBox _search = new TextBox();
        List<FontEntry> _pool = new List<FontEntry>();
        readonly List<FontEntry> _filtered = new List<FontEntry>();

        public FontEntry Picked { get; private set; }
        readonly bool _shellMode;

        /// <param name="shellMode">true = 给「系统元素」选字体：用 GDI 枚举，能选到可变字体的命名实例（MiSans VF Bold 等），只写名字、不生成文件。</param>
        public FontPickerDialog(bool shellMode = false)
        {
            _shellMode = shellMode;
            Title = L.T("picker.select.a.font");
            Width = 560;
            Height = 640;
            MinWidth = 560;
            MinHeight = 640;
            MaxWidth = 560;
            MaxHeight = 640;
            ResizeMode = ResizeMode.NoResize;   // 固定大小：不能拖、没有最大化
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SourceInitialized += delegate { Styles.DarkTitle(this); };
            Background = T.BDrawer;
            Foreground = T.Text;
            FontSize = 13;

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = T.Label(L.T("picker.installed.fonts"), 15, T.Text, true);
            title.Margin = new Thickness(0, 0, 0, 10);
            Grid.SetRow(title, 0);
            root.Children.Add(title);

            _search.FontSize = 13;
            _search.Margin = new Thickness(0, 0, 0, 10);
            _search.TextChanged += delegate { Filter(); };
            Grid.SetRow(_search, 1);
            root.Children.Add(_search);

            _list.FontSize = 13;
            _list.MouseDoubleClick += delegate { Accept(); };
            Grid.SetRow(_list, 2);
            root.Children.Add(_list);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0) };
            var ok = T.Btn2(L.T("picker.ok"), true, 13);
            ok.Margin = new Thickness(0, 0, 8, 0);
            ok.Click += delegate { Accept(); };
            var cancel = T.Btn2(L.T("picker.cancel"), false, 13);
            cancel.Click += delegate { DialogResult = false; };
            bar.Children.Add(ok);
            bar.Children.Add(cancel);
            Grid.SetRow(bar, 3);
            root.Children.Add(bar);

            Content = root;
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("--q:")) _search.Text = a.Substring(4);
            }
            Loaded += delegate { Load(); };
        }

        void Load()
        {
            try
            {
                if (_shellMode)
                {
                    _pool = new List<FontEntry>();
                    foreach (var it in ShellFontEnum.All)
                    {
                        _pool.Add(new FontEntry
                        {
                            Family = it.Name,
                            Subfamily = it.Italic ? "Italic" : "",
                            Weight = it.Weight,
                            PreviewFamily = it.BaseFamily,
                            Path = null
                        });
                    }
                    Filter();
                    return;
                }
                _pool = FontLibrary.All;
                WpfProgram.Log("picker pool=" + (_pool == null ? -1 : _pool.Count));
                Filter();
                WpfProgram.Log("picker items=" + _list.Items.Count);
            }
            catch (Exception ex)
            {
                WpfProgram.Log("picker load failed: " + ex.Message);
                _list.Items.Add(L.T("picker.failed.to.read.the.font") + ex.Message);
            }
        }

        void Filter()
        {
            string q = (_search.Text ?? "").Trim().ToLowerInvariant();
            _list.Items.Clear();
            _filtered.Clear();
            int n = 0;
            foreach (var f in _pool)
            {
                if (q.Length > 0 && f.Family.ToLowerInvariant().IndexOf(q) < 0) continue;
                _list.Items.Add(RowFor(f));
                _filtered.Add(f);
                n++;
                if (n > 600) break;
            }
            // 默认选中第一条：否则“确定了但没选中”会静默什么都不做，用户以为选过了
            if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        }

        /// <summary>一行预览：用该字体自身渲染「Aa + 字体名 + 字重」。</summary>
        static UIElement RowFor(FontEntry f)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            FontFamily ff = null;
            FontWeight fw = FontWeights.Normal;
            try
            {
                var famName = string.IsNullOrEmpty(f.PreviewFamily) ? f.Family : f.PreviewFamily;
                ff = new FontFamily(famName);
                if (f.Weight >= 800) fw = FontWeights.Black;
                else if (f.Weight >= 650) fw = FontWeights.Bold;
                else if (f.Weight >= 550) fw = FontWeights.SemiBold;
                else if (f.Weight >= 450) fw = FontWeights.Medium;
                else if (f.Weight >= 350) fw = FontWeights.Normal;
                else fw = FontWeights.Light;
            }
            catch { }

            var sample = new TextBlock
            {
                Text = L.T("msg.text27"),
                FontSize = 16,
                Width = 92,
                Foreground = T.Text,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (ff != null) { sample.FontFamily = ff; sample.FontWeight = fw; }

            var name = new TextBlock
            {
                Text = f.Family,
                FontSize = 14,
                Foreground = T.Text,
                VerticalAlignment = VerticalAlignment.Center
            };
            if (ff != null) { name.FontFamily = ff; name.FontWeight = fw; }

            var meta = T.Label("  w" + f.Weight + "  " + (f.Subfamily ?? ""), 11, T.TextFaint, false);
            meta.VerticalAlignment = VerticalAlignment.Center;

            sp.Children.Add(sample);
            sp.Children.Add(name);
            sp.Children.Add(meta);
            return sp;
        }

        void Accept()
        {
            int idx = _list.SelectedIndex;
            if (idx < 0 || idx >= _filtered.Count) return;
            Picked = _filtered[idx];
            DialogResult = true;
        }
    }
}
