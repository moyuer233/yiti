using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WinFontSwap.Wpf
{
    /// <summary>应用风格的单行输入框：用来给预设起名字。</summary>
    internal sealed class PromptDialog : Window
    {
        public string Value = null;
        readonly TextBox _box;

        public static string Ask(Window owner, string title, string label, string initial)
        {
            var dlg = new PromptDialog(title, label, initial);
            if (owner != null) dlg.Owner = owner;
            dlg.ShowDialog();
            return dlg.Value;
        }

        PromptDialog(string title, string label, string initial)
        {
            Title = title;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.Height;
            Width = 428;   // 400 的卡片 + 两侧阴影留白
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var card = new Border
            {
                Background = T.BDrawer,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(22, 18, 22, 16),
                Effect = new DropShadowEffect { BlurRadius = 26, ShadowDepth = 0, Opacity = 0.45, Color = Colors.Black }
            };
            var stack = new StackPanel();
            stack.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = T.Text });
            stack.Children.Add(new TextBlock { Text = label, FontSize = 13, Foreground = T.TextDim, Margin = new Thickness(0, 10, 0, 0) });
            _box = new TextBox { Text = initial ?? "", FontSize = 13, MinHeight = 30, Margin = new Thickness(0, 8, 0, 0), Padding = new Thickness(8, 5, 8, 5) };
            stack.Children.Add(_box);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
            var cancel = T.Btn2(L.T("dlg.no"), false, 13);
            cancel.Margin = new Thickness(0, 0, 10, 0);
            cancel.Click += delegate { Value = null; Styles.FadeOutThen(card, 110, delegate { Close(); }); };
            bar.Children.Add(cancel);
            var ok = T.Btn2(L.T("dlg.ok"), true, 13);
            ok.Click += delegate { Accept(); };
            bar.Children.Add(ok);
            stack.Children.Add(bar);

            card.Child = stack;
            Content = new Grid { Margin = new Thickness(14), Children = { card } };   // 留白给外阴影，否则阴影会被窗口边缘裁掉

            _box.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { Accept(); e.Handled = true; }
            };
            KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { Value = null; Styles.FadeOutThen(card, 110, delegate { Close(); }); }
            };
            Loaded += delegate
            {
                Styles.FadeIn(card, 10, 170);
                _box.Focus();
                _box.SelectAll();
            };
        }

        void Accept()
        {
            var text = (_box.Text ?? "").Trim();
            if (text.Length == 0) { _box.Focus(); return; }
            Value = text;
            Styles.FadeOutThen(((Border)Content), 110, delegate { Close(); });
        }
    }
}
