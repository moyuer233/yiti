using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shell;

namespace WinFontSwap.Wpf
{
    /// <summary>应用风格的自绘确认框（深色卡片，替掉系统 MessageBox 的白色弹窗）。</summary>
    internal sealed class ConfirmDialog : Window
    {
        public bool Result;

        public ConfirmDialog(string title, string message, string okText, string cancelText)
        {
            Title = title;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            ResizeMode = ResizeMode.NoResize;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.Height;
            Width = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var card = new Border
            {
                Background = T.BDrawer,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(22, 18, 22, 16),
                Effect = new DropShadowEffect { BlurRadius = 26, ShadowDepth = 0, Opacity = 0.45, Color = Colors.Black }
            };
            var stack = new StackPanel();

            var tbTitle = new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = T.Text };
            stack.Children.Add(tbTitle);

            var tbMsg = new TextBlock
            {
                Text = message,
                FontSize = 13,
                Foreground = T.TextDim,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0),
                LineHeight = 20
            };
            stack.Children.Add(tbMsg);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            var cancel = T.Btn2(cancelText, false, 13);
            cancel.Margin = new Thickness(0, 0, 10, 0);
            cancel.Click += delegate { Result = false; Close(); };
            var ok = T.Btn2(okText, true, 13);
            ok.Click += delegate { Result = true; Close(); };
            bar.Children.Add(cancel);
            bar.Children.Add(ok);
            stack.Children.Add(bar);

            card.Child = stack;
            Content = card;

            // 拖动：只有标题和空白处能拖动，按钮不受影响
            tbTitle.MouseLeftButtonDown += delegate { try { DragMove(); } catch { } };
            card.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
            {
                if (e.OriginalSource is TextBlock || e.OriginalSource is Border) { try { DragMove(); } catch { } }
            };
            KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { Result = false; Close(); }
                if (e.Key == Key.Enter) { Result = true; Close(); }
            };
            Loaded += delegate { cancel.Focus(); };   // 默认焦点在取消，避免误按回车
        }
    }
}
