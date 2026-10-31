using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace WinFontSwap.Wpf
{
    /// <summary>应用风格的多选一弹窗：用于「默认值」这种要问“恢复哪一部分”的场景。</summary>
    internal sealed class ChoiceDialog : Window
    {
        public int Choice = -1;

        /// <summary>返回被点中的按钮下标；取消或关窗返回 -1。</summary>
        public static int Choose(Window owner, string title, string message, params string[] options)
        {
            var dlg = new ChoiceDialog(title, message, options);
            if (owner != null) dlg.Owner = owner;
            dlg.ShowDialog();
            return dlg.Choice;
        }

        ChoiceDialog(string title, string message, string[] options)
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
            stack.Children.Add(new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = T.Text });
            var msg = new TextBlock
            {
                Text = message,
                FontSize = 13,
                Foreground = T.TextDim,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 0),
                LineHeight = 20
            };
            stack.Children.Add(msg);

            var bar = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            var cancel = T.Btn2(L.T("dlg.no"), false, 13);
            cancel.Margin = new Thickness(0, 0, 10, 0);
            cancel.Click += delegate { Choice = -1; Styles.FadeOutThen(card, 110, delegate { Close(); }); };
            bar.Children.Add(cancel);
            if (options != null)
            {
                for (int i = 0; i < options.Length; i++)
                {
                    int idx = i;
                    var b = T.Btn2(options[i], i == options.Length - 1, 13);
                    b.Margin = new Thickness(0, 0, 10, 0);
                    b.Click += delegate { Choice = idx; Styles.FadeOutThen(card, 110, delegate { Close(); }); };
                    bar.Children.Add(b);
                }
            }
            stack.Children.Add(bar);

            card.Child = stack;
            Content = card;

            stack.MouseLeftButtonDown += delegate (object s, MouseButtonEventArgs e)
            {
                if (e.OriginalSource is TextBlock) { try { DragMove(); } catch { } }
            };
            KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.Key == Key.Escape) { Choice = -1; Styles.FadeOutThen(card, 110, delegate { Close(); }); }
            };
            Loaded += delegate { Styles.FadeIn(card, 10, 170); cancel.Focus(); };
        }
    }
}
