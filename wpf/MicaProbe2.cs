using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace MicaProbe2
{
    internal static class Program
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MARGINS { public int l, r, t, b; }
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int s);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int a, out int v, int s);
        [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr h, ref MARGINS m);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int idx);

        static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "micaprobe.log");

        [STAThread]
        static void Main()
        {
            var app = new Application();
            app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            File.WriteAllText(LogPath, "=== mica probe " + Environment.OSVersion + " ===\r\n");

            // variant: 0 = 默认 WindowStyle + WindowChrome(GFF 0)
            //          1 = None + WindowChrome(GFF 0)
            //          2 = None 无 WindowChrome
            //          3 = SingleBorderWindow 无 WindowChrome + ExtendFrame
            for (int v = 0; v < 4; v++)
            {
                var w = new Window();
                w.Width = 520;
                w.Height = 260;
                w.WindowStartupLocation = WindowStartupLocation.Manual;
                w.Left = 80 + v * 30;
                w.Top = 80 + v * 30;
                w.Title = "probe" + v;
                w.Background = Brushes.Transparent;
                w.Content = new System.Windows.Controls.TextBlock
                {
                    Text = "variant " + v,
                    Foreground = Brushes.White,
                    FontSize = 18,
                    Margin = new Thickness(20)
                };

                if (v == 0 || v == 1)
                {
                    WindowChrome.SetWindowChrome(w, new WindowChrome
                    {
                        CaptionHeight = 32,
                        ResizeBorderThickness = new Thickness(6),
                        GlassFrameThickness = new Thickness(0),
                        CornerRadius = new CornerRadius(0),
                        UseAeroCaptionButtons = true
                    });
                }
                if (v == 1 || v == 2) w.WindowStyle = WindowStyle.None;

                int vv = v;
                w.Loaded += delegate { Apply(w, vv); };
                w.Show();
                Pump(700);
                Report(w, v);
                w.Close();
                Pump(200);
            }
            File.AppendAllText(LogPath, "done\r\n");
            app.Shutdown();
        }

        static void Pump(int ms)
        {
            var end = DateTime.Now.AddMilliseconds(ms);
            while (DateTime.Now < end)
            {
                System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background, new Action(delegate { }));
                System.Threading.Thread.Sleep(20);
            }
        }

        static void Apply(Window w, int v)
        {
            try
            {
                var hwnd = new WindowInteropHelper(w).Handle;
                var src = PresentationSource.FromVisual(w) as HwndSource;
                if (src != null && src.CompositionTarget != null)
                    src.CompositionTarget.BackgroundColor = Colors.Transparent;

                int dark = 1;
                DwmSetWindowAttribute(hwnd, 20, ref dark, 4);

                var m = new MARGINS { l = -1, r = -1, t = -1, b = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref m);

                int type = 2;
                int hr = DwmSetWindowAttribute(hwnd, 38, ref type, 4);
                File.AppendAllText(LogPath, "v" + v + " set_hr=0x" + hr.ToString("x8") + "\r\n");
            }
            catch (Exception ex) { File.AppendAllText(LogPath, "v" + v + " err " + ex.Message + "\r\n"); }
        }

        static void Report(Window w, int v)
        {
            try
            {
                var hwnd = new WindowInteropHelper(w).Handle;
                int back;
                int hr = DwmGetWindowAttribute(hwnd, 38, out back, 4);
                int style = GetWindowLong(hwnd, -16);
                File.AppendAllText(LogPath,
                    "v" + v + " read_hr=0x" + hr.ToString("x8") + " type=" + back +
                    " style=0x" + style.ToString("x8") +
                    " caption=" + ((style & 0x00C00000) == 0x00C00000) + "\r\n");
            }
            catch { }
        }
    }
}
