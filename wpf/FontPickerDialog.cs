using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WinFontSwap.Wpf
{
    /// <summary>字体选择器：搜索 + 列表 + 确定/取消。</summary>
    internal class FontPickerDialog : Window
    {
        readonly ListBox _list = new ListBox();
        readonly TextBox _search = new TextBox();
        List<FontEntry> _pool = new List<FontEntry>();

        public FontEntry Picked { get; private set; }

        public FontPickerDialog()
        {
            Title = "选择字体";
            Width = 520;
            Height = 620;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x22));
            Foreground = T.Text;
            FontSize = 13;

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = T.Label("系统已安装字体", 15, T.Text, true);
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
            var ok = T.Btn2("确定", true, 13);
            ok.Margin = new Thickness(0, 0, 8, 0);
            ok.Click += delegate { Accept(); };
            var cancel = T.Btn2("取消", false, 13);
            cancel.Click += delegate { DialogResult = false; };
            bar.Children.Add(ok);
            bar.Children.Add(cancel);
            Grid.SetRow(bar, 3);
            root.Children.Add(bar);

            Content = root;
            Loaded += delegate { Load(); };
        }

        void Load()
        {
            try
            {
                _pool = FontLibrary.All;
                Filter();
            }
            catch (Exception ex)
            {
                _list.Items.Add("读取字体失败：" + ex.Message);
            }
        }

        void Filter()
        {
            string q = (_search.Text ?? "").Trim().ToLowerInvariant();
            _list.Items.Clear();
            int n = 0;
            foreach (var f in _pool)
            {
                if (q.Length > 0 && f.Family.ToLowerInvariant().IndexOf(q) < 0) continue;
                _list.Items.Add(f.Display);
                n++;
                if (n > 800) break;
            }
        }

        void Accept()
        {
            int idx = _list.SelectedIndex;
            if (idx < 0) return;
            string q = (_search.Text ?? "").Trim().ToLowerInvariant();
            int seen = -1;
            foreach (var f in _pool)
            {
                if (q.Length > 0 && f.Family.ToLowerInvariant().IndexOf(q) < 0) continue;
                seen++;
                if (seen == idx) { Picked = f; DialogResult = true; return; }
            }
        }
    }
}
