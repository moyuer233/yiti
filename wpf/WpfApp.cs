using System;
using System.Windows;

namespace WinFontSwap.Wpf
{
    internal static class WpfProgram
    {
        [STAThread]
        static void Main(string[] args)
        {
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            app.Run(new MainWindow());
        }
    }
}
