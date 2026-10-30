using System;
using System.IO;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;

namespace WinFontSwap.Wpf
{
    internal static class WpfProgram
    {
        // 默认不写日志：本机安全软件只允许这个 exe 往自身目录写，
        // 所以日志只能落在 dist 里 —— 用 --log 才写，保证 dist 只有发布产物。
        /// <summary>应用版本号：只改这里，窗口标题自动跟着变。</summary>
        public const string Version = "0.2.5";

        public static bool LogEnabled;

        static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wpf.log");
        public static string LogPathUsed { get { return LogPath; } }

        public static void Log(string s)
        {
            if (!LogEnabled) return;
            try { File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss ") + s + "\r\n"); } catch { }
        }

        [STAThread]
        static void Main(string[] args)
        {
            foreach (var a in args) { if (a == "--log") LogEnabled = true; }
            L.Load();   // 上次选过的界面语言
            if (LogEnabled) { try { File.WriteAllText(LogPath, "=== start ===\r\n"); } catch { } }

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
