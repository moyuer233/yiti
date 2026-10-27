using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace WinFontSwap.Wpf
{
    internal static class WpfProgram
    {
        static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wpf.log");

        public static void Log(string s)
        {
            try { File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss ") + s + "\r\n"); } catch { }
        }

        [STAThread]
        static void Main(string[] args)
        {
            try { File.WriteAllText(LogPath, "=== start ===\r\n"); } catch { }
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            try { app.Resources = (ResourceDictionary)XamlReader.Parse(Styles.Xaml); }
            catch (Exception ex) { Log("styles parse failed: " + ex.Message); }
            app.DispatcherUnhandledException += delegate (object s, DispatcherUnhandledExceptionEventArgs e)
            {
                Log("UNHANDLED " + e.Exception);
                e.Handled = false;
            };
            AppDomain.CurrentDomain.UnhandledException += delegate (object s, UnhandledExceptionEventArgs e)
            {
                Log("DOMAIN " + e.ExceptionObject);
            };
            app.Run(new MainWindow());
        }
    }
}
