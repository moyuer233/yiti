using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace WinFontSwap.Wpf
{
    internal static class WpfProgram
    {
        static readonly string LogPath = PickLogPath();

        /// <summary>日志写到程序数据目录，别污染 dist 产物目录。</summary>
        static string PickLogPath()
        {
            // 依次尝试：程序数据目录 → 用户 LocalAppData → 程序目录，取第一个真正可写的
            var candidates = new string[]
            {
                Installer.DataDir,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WinFontSwap"),
                AppDomain.CurrentDomain.BaseDirectory
            };
            foreach (var dir in candidates)
            {
                try
                {
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                    var probe = Path.Combine(dir, "wpf.log.probe");
                    File.WriteAllText(probe, "x");
                    File.Delete(probe);
                    return Path.Combine(dir, "wpf.log");
                }
                catch { }
            }
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wpf.log");
        }

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
