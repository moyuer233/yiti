using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GlyphProbe
{
    internal static class P
    {
        [STAThread]
        static void Main()
        {
            var app = new Application();
            var w = new Window
            {
                Width = 360,
                Height = 700,
                WindowStyle = WindowStyle.None,
                Left = 100,
                Top = 100,
                Background = Brushes.Black,
                Title = "glyphprobe"
            };
            var sp = new StackPanel { Margin = new Thickness(12) };
            string[] codes = { "E8D2", "E8D3", "E8DD", "E790", "E74D", "E8F4", "E777", "E8A5", "E8B7", "E713", "E8AB", "E8C8", "E8EF", "E8F1", "E9D9", "E7C3", "E72C", "E8E5", "EA86", "E8B9" };
            foreach (var c in codes)
            {
                var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 4, 0, 4) };
                var ico = new TextBlock
                {
                    Text = ((char)Convert.ToInt32(c, 16)).ToString(),
                    FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                    FontSize = 22,
                    Width = 40,
                    Foreground = Brushes.White
                };
                var lab = new TextBlock { Text = c, FontSize = 13, Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center };
                row.Children.Add(ico);
                row.Children.Add(lab);
                sp.Children.Add(row);
            }
            w.Content = sp;
            w.Show();
            app.Run();
        }
    }
}
