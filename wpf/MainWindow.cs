using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;
using System.Windows.Threading;

namespace WinFontSwap.Wpf
{
    /// <summary>视觉常量：完全对照 Lyricify Fusion 的设置窗口。</summary>
    internal static class T
    {
        public static Brush B(Color c) { var b = new SolidColorBrush(c); b.Freeze(); return b; }

        public static readonly Brush Bg = B(Color.FromRgb(0x20, 0x20, 0x24));        // 窗口底
        public static readonly Brush Card = B(Color.FromRgb(0x2D, 0x2D, 0x32));      // 卡片
        public static readonly Brush CardHover = B(Color.FromRgb(0x33, 0x33, 0x39));
        public static readonly Brush RailPill = B(Color.FromArgb(0x1F, 255, 255, 255)); // 选中胶囊
        public static readonly Brush RailHover = B(Color.FromArgb(0x0D, 255, 255, 255));
        public static readonly Brush Text = B(Color.FromRgb(0xFF, 0xFF, 0xFF));
        public static readonly Brush TextDim = B(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF));
        public static readonly Brush TextFaint = B(Color.FromArgb(0x80, 0xFF, 0xFF, 0xFF));
        public static readonly Brush Accent = B(Color.FromRgb(0x4C, 0xC2, 0xFF));   // Win11 强调色
        public static readonly Brush Btn = B(Color.FromArgb(0x14, 255, 255, 255));
        public static readonly Brush BtnHover = B(Color.FromArgb(0x24, 255, 255, 255));
        public static readonly Brush TrackOff = B(Color.FromArgb(0x3D, 255, 255, 255));
        public static readonly Brush BDrawer = B(Color.FromRgb(0x24, 0x24, 0x29));   // 弹窗/下拉底色

        public static TextBlock Label(string text, double size, Brush brush, bool bold)
        {
            var tb = new TextBlock { Text = text, FontSize = size, Foreground = brush };
            if (bold) tb.FontWeight = FontWeights.SemiBold;
            tb.TextTrimming = TextTrimming.CharacterEllipsis;
            tb.VerticalAlignment = VerticalAlignment.Center;
            return tb;
        }

        public static Button Btn2(string text, bool primary, double size)
        {
            var b = new Button { Content = text, FontSize = size, Foreground = primary ? B(Color.FromRgb(0x10, 0x10, 0x12)) : Text, Cursor = Cursors.Hand };
            b.Padding = new Thickness(14, 6, 14, 6);
            b.MinHeight = 32;
            b.BorderThickness = new Thickness(0);
            var normal = primary ? Accent : Btn;
            b.Background = normal;
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            f.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            cp.SetValue(ContentPresenter.MarginProperty, new TemplateBindingExtension(Control.PaddingProperty));
            f.AppendChild(cp);
            var tpl = new ControlTemplate(typeof(Button));
            tpl.VisualTree = f;
            b.Template = tpl;
            var hover = primary ? B(Color.FromRgb(0x6C, 0xD0, 0xFF)) : BtnHover;
            b.MouseEnter += delegate { b.Background = hover; };
            b.MouseLeave += delegate { b.Background = normal; };
            return b;
        }

        public static ToggleButton Switch(bool on)
        {
            var tg = new ToggleButton { IsChecked = on, Width = 40, Height = 20, Cursor = Cursors.Hand };
            var track = new FrameworkElementFactory(typeof(Border));
            track.Name = "track";
            track.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));
            track.SetValue(Border.BackgroundProperty, TrackOff);
            var knob = new FrameworkElementFactory(typeof(System.Windows.Shapes.Ellipse));
            knob.Name = "knob";
            knob.SetValue(FrameworkElement.WidthProperty, 12.0);
            knob.SetValue(FrameworkElement.HeightProperty, 12.0);
            knob.SetValue(System.Windows.Shapes.Shape.FillProperty, B(Color.FromRgb(0xF0, 0xF0, 0xF0)));
            knob.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            knob.SetValue(FrameworkElement.MarginProperty, new Thickness(4));
            track.AppendChild(knob);
            var tpl = new ControlTemplate(typeof(ToggleButton));
            tpl.VisualTree = track;
            tg.Template = tpl;
            var refresh = new Action(delegate
            {
                var tr = (Border)tg.Template.FindName("track", tg);
                var kn = (System.Windows.Shapes.Ellipse)tg.Template.FindName("knob", tg);
                if (tr == null || kn == null) return;
                bool on2 = tg.IsChecked == true;
                tr.Background = on2 ? Accent : TrackOff;
                kn.HorizontalAlignment = on2 ? HorizontalAlignment.Right : HorizontalAlignment.Left;
                kn.Fill = on2 ? B(Color.FromRgb(0x10, 0x10, 0x12)) : B(Color.FromRgb(0xF0, 0xF0, 0xF0));
            });
            tg.Click += delegate { refresh(); };
            tg.Loaded += delegate { refresh(); };
            return tg;
        }

        /// <summary>Segoe Fluent Icons 字形（Win11 自带字体）。</summary>
        public static TextBlock Icon(string glyph, double size, Brush brush)
        {
            var tb = new TextBlock
            {
                Text = glyph,
                FontFamily = new FontFamily("Segoe Fluent Icons, Segoe MDL2 Assets"),
                FontSize = size,
                Foreground = brush,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            return tb;
        }

        /// <summary>LF 式卡片行：图标 + 标题/说明 + 右侧控件。</summary>
        public static Border Row(string glyph, string title, string desc, UIElement control, out TextBlock descBlock)
        {
            var card = new Border
            {
                Background = Card,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 12, 16, 12),
                Margin = new Thickness(0, 0, 0, 12)
            };
            var g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var ic = Icon(glyph, 16, TextDim);
            ic.Margin = new Thickness(0, 0, 12, 0);
            ic.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(ic, 0);
            g.Children.Add(ic);

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(Label(title, 14, Text, false));
            var d = Label(desc, 12, TextDim, false);
            d.Margin = new Thickness(0, 2, 0, 0);
            text.Children.Add(d);
            Grid.SetColumn(text, 1);
            g.Children.Add(text);

            if (control != null)
            {
                var cfe = control as FrameworkElement;
                if (cfe != null) cfe.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(control, 2);
                g.Children.Add(control);
            }
            card.Child = g;
            descBlock = d;
            return card;
        }

        /// <summary>区段标题（LF 的「歌词检索与保存」那种）。</summary>
        /// <summary>分节标题 + 右侧小动作（与下方行卡片同一套栅格）。</summary>
        public static UIElement SectionHeader(string text, bool first, params UIElement[] actions)
        {
            var g = new Grid();
            g.Margin = new Thickness(2, first ? 0 : 14, 0, 10);
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var tb = Label(text, 13, TextFaint, false);
            tb.VerticalAlignment = VerticalAlignment.Center;
            g.Children.Add(tb);
            if (actions != null && actions.Length > 0)
            {
                var sp = new StackPanel { Orientation = Orientation.Horizontal };
                for (int i = 0; i < actions.Length; i++)
                {
                    var el = actions[i] as FrameworkElement;
                    if (el != null && i < actions.Length - 1) el.Margin = new Thickness(0, 0, 8, 0);
                    sp.Children.Add(actions[i]);
                }
                Grid.SetColumn(sp, 1);
                g.Children.Add(sp);
            }
            return g;
        }

        public static TextBlock SectionTitle(string text, bool first)
        {
            var tb = Label(text, 13, TextFaint, false);
            tb.Margin = new Thickness(2, first ? 0 : 14, 0, 10);
            return tb;
        }

        /// <summary>程序化改 IsChecked 后重刷开关外观（Track 颜色 + 圆点位置）。</summary>
        public static void RefreshToggleVisual(ToggleButton tg)
        {
            if (tg == null) return;
            var tr = tg.Template == null ? null : (Border)tg.Template.FindName("track", tg);
            var kn = tg.Template == null ? null : (System.Windows.Shapes.Ellipse)tg.Template.FindName("knob", tg);
            if (tr == null || kn == null) return;
            bool on = tg.IsChecked == true;
            tr.Background = on ? Accent : TrackOff;
            kn.HorizontalAlignment = on ? HorizontalAlignment.Right : HorizontalAlignment.Left;
            kn.Fill = on ? B(Color.FromRgb(0x10, 0x10, 0x12)) : B(Color.FromRgb(0xF0, 0xF0, 0xF0));
        }

        public static Border CardBox()
        {
            return new Border
            {
                Background = Card,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(16, 14, 16, 14),
                Margin = new Thickness(0, 0, 0, 10)
            };
        }

        /// <summary>LF 卡片里的两行文字：标题 14 + 说明 12（55% 白）。</summary>
        public static StackPanel TwoLine(string title, string desc)
        {
            var sp = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            sp.Children.Add(Label(title, 14, Text, false));
            if (!string.IsNullOrEmpty(desc)) sp.Children.Add(Label(desc, 12, TextDim, false));
            return sp;
        }
    }

    internal class MainWindow : Window
    {
        [StructLayout(LayoutKind.Sequential)]
        struct MARGINS { public int l, r, t, b; }
        [DllImport("dwmapi.dll")] static extern int DwmSetWindowAttribute(IntPtr h, int a, ref int v, int s);
        [DllImport("dwmapi.dll")] static extern int DwmExtendFrameIntoClientArea(IntPtr h, ref MARGINS m);
        [DllImport("dwmapi.dll")] static extern int DwmGetWindowAttribute(IntPtr h, int a, out int v, int s);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr h, int idx);

        const double RailW = 218;

        ContentControl _content;
        ScrollViewer _pageScroll;
        ListBox _nav;
        TextBlock _status;
        Image _bg;
        System.Windows.Shapes.Rectangle _tint;
        System.Windows.Threading.DispatcherTimer _bgTimer;
        DateTime _lastCapture = DateTime.MinValue;

        FontEntry[] _slots = new FontEntry[5];
        bool[] _on = new bool[5] { true, true, true, true, true };
        TextBlock[] _srcText = new TextBlock[5];
        TextBlock[] _metaText = new TextBlock[5];
        TextBlock[] _fontPreview = new TextBlock[5];
        readonly Dictionary<string, string> _appliedFamily = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        ToggleButton[] _tog = new ToggleButton[5];
        TextBlock[] _winCur = new TextBlock[Win32Fonts.Names.Length];
        TextBox[] _winSize = new TextBox[Win32Fonts.Names.Length];
        Win32Fonts.Info[] _winInfo = new Win32Fonts.Info[Win32Fonts.Names.Length];
        string[] _winFace = new string[Win32Fonts.Names.Length];
        bool[] _winOn = new bool[Win32Fonts.Names.Length];
        ToggleButton[] _winTog = new ToggleButton[Win32Fonts.Names.Length];
        ToggleButton _advCache, _advUserFonts;
        bool _advCacheOn = true, _advUserFontsOn = true;


        /// <summary>从 exe 内嵌资源读应用图标（csc /resource: 的 LogicalName 是 app.ico，用清单资源名更可靠）。</summary>
        internal static BitmapSource LoadAppIcon()
        {
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string name = null;
                foreach (var n in asm.GetManifestResourceNames())
                {
                    if (n.EndsWith("app.ico", StringComparison.OrdinalIgnoreCase)) { name = n; break; }
                }
                if (name == null)
                {
                    WpfProgram.Log("icon: resources=[" + string.Join(",", asm.GetManifestResourceNames()) + "]");
                    return null;
                }
                using (var s = asm.GetManifestResourceStream(name))
                {
                    if (s == null) return null;
                    var dec = new IconBitmapDecoder(s, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    BitmapFrame best = null;
                    foreach (var f in dec.Frames) if (best == null || f.PixelWidth > best.PixelWidth) best = f;
                    return best;
                }
            }
            catch (Exception ex) { WpfProgram.Log("icon: " + ex.Message); return null; }
        }

        public MainWindow()
        {
            for (int i = 0; i < _winOn.Length; i++) _winOn[i] = true;
            Title = "易体 V" + WpfProgram.Version;
            var appIcon = LoadAppIcon();
            if (appIcon != null) Icon = appIcon;
            Width = 1080;
            Height = 720;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;   // Mica 的前提：不能有 WS_CAPTION
            Background = Brushes.Transparent;

            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 32,
                ResizeBorderThickness = new Thickness(6),
                GlassFrameThickness = new Thickness(0),
                CornerRadius = new CornerRadius(0),
                UseAeroCaptionButtons = true
            });

            BuildUi();
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a.StartsWith("--page:"))
                {
                    int p;
                    if (int.TryParse(a.Substring(7), out p) && p >= 0 && p < 3) _nav.SelectedIndex = p;
                }
            }
            SourceInitialized += delegate { DarkTitle(); };
            foreach (var a in Environment.GetCommandLineArgs())
            {
                if (a == "--picker") Loaded += delegate { Dispatcher.BeginInvoke(new Action(delegate { PickFont(); })); };
            }
            Loaded += delegate
            {
                WpfProgram.Log("logpath=" + WpfProgram.LogPathUsed + "  base=" + AppDomain.CurrentDomain.BaseDirectory);
                try { DarkTitle(); } catch (Exception ex) { WpfProgram.Log("DarkTitle: " + ex.Message); }
                try { ReloadFonts(); } catch (Exception ex) { WpfProgram.Log("ReloadFonts: " + ex.Message); }
                try { RefreshTargets(); } catch (Exception ex) { WpfProgram.Log("RefreshTargets: " + ex.Message); }
                try { LoadWin32(); } catch (Exception ex) { WpfProgram.Log("LoadWin32: " + ex.Message); }
                foreach (var a in Environment.GetCommandLineArgs())
                {
                    // 自测：切一圈页再回来，验证状态不会停在「读取中」
                    if (a == "--cycle" && _nav != null)
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            _nav.SelectedIndex = 1;
                            _nav.SelectedIndex = 2;
                            _nav.SelectedIndex = 0;
                        }));
                    }
                    else if (a.StartsWith("--shot:"))
                    {
                        // 自绘截图：程序自己把界面渲染成 PNG（不需要外部截图，也不受提权窗口隔离影响）
                        var path = a.Substring(7);
                        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                        timer.Tick += delegate
                        {
                            timer.Stop();
                            try
                            {
                                var fe = (FrameworkElement)Content;
                                fe.UpdateLayout();
                                var w = (int)Math.Ceiling(fe.ActualWidth);
                                var hh = (int)Math.Ceiling(fe.ActualHeight);
                                if (w <= 0 || hh <= 0) return;
                                var rtb = new RenderTargetBitmap(w, hh, 96, 96, PixelFormats.Pbgra32);
                                var dv = new DrawingVisual();
                                using (var dc = dv.RenderOpen())
                                {
                                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x24)), null, new Rect(0, 0, w, hh));
                                    dc.DrawRectangle(new VisualBrush(fe), null, new Rect(0, 0, w, hh));
                                }
                                rtb.Render(dv);
                                var enc = new PngBitmapEncoder();
                                enc.Frames.Add(BitmapFrame.Create(rtb));
                                using (var fs = System.IO.File.Create(path)) enc.Save(fs);
                            }
                            catch (Exception ex) { WpfProgram.Log("shot: " + ex.Message); }
                            Close();
                        };
                        timer.Start();
                    }
                    else if (a.StartsWith("--pick:"))
                    {
                        var want = a.Substring(7).ToLowerInvariant();
                        foreach (var f in FontLibrary.All)
                        {
                            if (f.Family.ToLowerInvariant().Contains(want))
                            {
                                ApplyPicked(f);
                                break;
                            }
                        }
                    }
                    else if (a == "--reset")
                    {
                        ResetToDefaults();
                    }
                    else if (a.StartsWith("--scroll:") && _pageScroll != null)
                    {
                        double off;
                        if (double.TryParse(a.Substring(9), out off)) _pageScroll.ScrollToVerticalOffset(off);
                    }
                }
            };
        }

        bool _micaOk;

        void DarkTitle()
        {
            try
            {
                var hwnd = new WindowInteropHelper(this).Handle;

                int dark = 1;
                DwmSetWindowAttribute(hwnd, 20, ref dark, 4);          // 深色标题栏

                var m = new MARGINS { l = -1, r = -1, t = -1, b = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref m);              // 客户区变成玻璃区

                int backdrop = 3;                                       // DWMSBT_TRANSIENTWINDOW (Acrylic)：模糊背后的实时画面（Mica=2 只取壁纸）
                int hr = DwmSetWindowAttribute(hwnd, 38, ref backdrop, 4);
                if (hr != 0)
                {
                    backdrop = 3;                                       // DWMSBT_TRANSIENTWINDOW (Acrylic)
                    hr = DwmSetWindowAttribute(hwnd, 38, ref backdrop, 4);
                }
                _micaOk = hr == 0;

                // 关键：WPF 合成目标默认是不透明的黑，改成透明 Mica 才能透出来
                var src = PresentationSource.FromVisual(this) as HwndSource;
                if (src != null && src.CompositionTarget != null)
                    src.CompositionTarget.BackgroundColor = Colors.Transparent;

                // 诊断：把 HRESULT 与窗口样式落盘，判断 Mica 到底被系统接受没有
                try
                {
                    var log = new System.Text.StringBuilder();
                    log.AppendLine("hwnd=" + hwnd);
                    log.AppendLine("backdrop_hr=0x" + hr.ToString("x8"));
                    log.AppendLine("mica_ok=" + (hr == 0));
                    int style = GetWindowLong(hwnd, -16);
                    log.AppendLine("style=0x" + style.ToString("x8") + " WS_CAPTION=" + ((style & 0x00C00000) == 0x00C00000));
                    int ex = GetWindowLong(hwnd, -20);
                    log.AppendLine("exstyle=0x" + ex.ToString("x8") + " layered=" + ((ex & 0x80000) != 0));
                    int back;
                    int got = DwmGetWindowAttribute(hwnd, 38, out back, 4);
                    log.AppendLine("readback_hr=0x" + got.ToString("x8") + " type=" + back);
                    log.AppendLine("os=" + Environment.OSVersion);
                    // 诊断信息写进主日志，不再往 dist 里落文件
                    WpfProgram.Log("backdrop: " + log.ToString().Replace("\r\n", " | "));
                }
                catch { }

            }
            catch { }
        }

        void BuildUi()
        {
            var root = new Grid();
            root.Background = Brushes.Transparent;   // 必须透明，否则盖住 Mica

            // 背景交给 DWM 的 Mica（真 GPU 合成、零延迟），这里只压一层深色保证文字对比度
            _tint = new System.Windows.Shapes.Rectangle();
            _tint.Fill = new SolidColorBrush(Color.FromArgb(0x59, 0x10, 0x10, 0x14));
            root.Children.Add(_tint);

            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(RailW) });
            root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // ---- 左栏（LF：深色、胶囊选中） ----
            var railGrid = new Grid { Background = new SolidColorBrush(Color.FromArgb(0x40, 0x10, 0x10, 0x14)) };
            var rail = new StackPanel { Margin = new Thickness(12, 14, 12, 12) };
            // 应用名 + 版本（窗口没有系统标题栏，名字与版本显示在这里）
            var brand = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 0, 22) };
            try
            {
                var logo = new Image
                {
                    Source = LoadAppIcon(),
                    Width = 20,
                    Height = 20,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center,
                    SnapsToDevicePixels = true
                };
                RenderOptions.SetBitmapScalingMode(logo, BitmapScalingMode.HighQuality);
                brand.Children.Add(logo);
            }
            catch (Exception ex) { WpfProgram.Log("logo: " + ex.Message); }
            var appName = T.Label("易体", 15, T.Text, true);
            appName.VerticalAlignment = VerticalAlignment.Bottom;
            var appVer = T.Label("V" + WpfProgram.Version, 11, T.TextFaint, false);
            appVer.VerticalAlignment = VerticalAlignment.Bottom;
            appVer.Margin = new Thickness(6, 0, 0, 1);
            brand.Children.Add(appName);
            brand.Children.Add(appVer);
            rail.Children.Add(brand);
            _nav = new ListBox
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                ItemContainerStyle = NavStyle()
            };
            _nav.Items.Add(NavItem("\uE8D2", "字体替换"));
            _nav.Items.Add(NavItem("\uE713", "高级"));
            _nav.Items.Add(NavItem("\uE8A5", "预设"));
            _nav.SelectionChanged += delegate { ShowPage(); };
            rail.Children.Add(_nav);
            railGrid.Children.Add(rail);
            Grid.SetColumn(railGrid, 0);
            root.Children.Add(railGrid);

            // ---- 右栏 ----
            var right = new Grid();
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // 自绘窗口按钮（WindowStyle=None 后系统不再画）
            var sys = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 6, 10, 0) };
            sys.Children.Add(WinBtn("–", delegate { WindowState = WindowState.Minimized; }));
            sys.Children.Add(WinBtn("□", delegate { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }));
            sys.Children.Add(WinBtn("×", delegate { Close(); }));
            Grid.SetColumn(sys, 1);
            root.Children.Add(sys);

            _content = new ContentControl();
            _content.Margin = new Thickness(26, 40, 26, 18);
            Grid.SetRow(_content, 0);
            right.Children.Add(_content);

            Grid.SetColumn(right, 1);
            root.Children.Add(right);

            Content = root;
            _nav.SelectedIndex = 0;

        }


        static Button WinBtn(string glyph, Action act)
        {
            var b = new Button
            {
                Content = glyph,
                Width = 40,
                Height = 28,
                FontSize = 13,
                Foreground = T.TextDim,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            WindowChrome.SetIsHitTestVisibleInChrome(b, true);
            var f = new FrameworkElementFactory(typeof(Border));
            f.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
            f.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Control.BackgroundProperty));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            cp.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp);
            var tpl = new ControlTemplate(typeof(Button));
            tpl.VisualTree = f;
            b.Template = tpl;
            b.MouseEnter += delegate { b.Background = T.BtnHover; b.Foreground = T.Text; };
            b.MouseLeave += delegate { b.Background = Brushes.Transparent; b.Foreground = T.TextDim; };
            b.Click += delegate { act(); };
            return b;
        }

        static UIElement NavItem(string glyph, string text)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var ic = T.Icon(glyph, 15, T.TextDim);
            ic.Margin = new Thickness(0, 0, 12, 0);
            ic.VerticalAlignment = VerticalAlignment.Center;
            sp.Children.Add(ic);
            sp.Children.Add(T.Label(text, 14, T.TextDim, false));
            return sp;
        }

        static Style NavStyle()
        {
            var st = new Style(typeof(ListBoxItem));
            st.Setters.Add(new Setter(Control.MarginProperty, new Thickness(0, 1, 0, 1)));
            st.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(12, 9, 12, 9)));
            st.Setters.Add(new Setter(Control.ForegroundProperty, T.TextDim));
            st.Setters.Add(new Setter(Control.FontSizeProperty, 14.0));
            st.Setters.Add(new Setter(Control.CursorProperty, Cursors.Hand));
            st.Setters.Add(new Setter(ListBoxItem.TemplateProperty, NavTpl()));
            return st;
        }

        static ControlTemplate NavTpl()
        {
            var outer = new FrameworkElementFactory(typeof(Grid));
            var bar = new FrameworkElementFactory(typeof(Border));
            bar.Name = "bar";
            bar.SetValue(Border.WidthProperty, 3.0);
            bar.SetValue(Border.CornerRadiusProperty, new CornerRadius(2));
            bar.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            bar.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Left);
            bar.SetValue(FrameworkElement.MarginProperty, new Thickness(0, 3, 0, 3));
            bar.SetValue(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Stretch);
            var fill = new FrameworkElementFactory(typeof(Border));
            fill.Name = "fill";
            fill.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            fill.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            fill.SetValue(Border.PaddingProperty, new TemplateBindingExtension(Control.PaddingProperty));
            fill.SetValue(FrameworkElement.MarginProperty, new Thickness(3, 0, 0, 0));
            var cp = new FrameworkElementFactory(typeof(ContentPresenter));
            fill.AppendChild(cp);
            outer.AppendChild(fill);
            outer.AppendChild(bar);   // 蓝条画在底色之上
            var tpl = new ControlTemplate(typeof(ListBoxItem));
            tpl.VisualTree = outer;

            var sel = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
            sel.Setters.Add(new Setter(Border.BackgroundProperty, T.Accent, "bar"));
            sel.Setters.Add(new Setter(Border.BackgroundProperty, T.RailPill, "fill"));
            sel.Setters.Add(new Setter(Control.ForegroundProperty, T.Text));
            tpl.Triggers.Add(sel);

            var hover = new Trigger { Property = ListBoxItem.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, T.RailHover, "fill"));
            tpl.Triggers.Add(hover);
            return tpl;
        }

        void ShowPage()
        {
            int i = _nav.SelectedIndex;
            if (i == 0)
            {
                _content.Content = BuildFontPage();
                // 页面每次切换都会重建控件，必须重新灌一次真实状态，否则永远停在「读取中」
                try { UpdateSlotLabels(); } catch { }
                try { RefreshTargets(); } catch { }
                try { LoadWin32(); } catch { }
            }
            else if (i == 1) _content.Content = BuildAdvancedPage();
            else _content.Content = BuildPresetPage();
        }

        // ---------------- 页面 ----------------

        UIElement BuildFontPage()
        {
            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(0, 0, 6, 12)
            };
            var stack = new StackPanel();
            stack.Children.Add(T.Label("字体替换", 28, T.Text, true));
            var spacer = new Border { Height = 14 };
            stack.Children.Add(spacer);

            stack.Children.Add(T.SectionTitle("替换目标", true));

            // 选字体
            var pickBtn = T.Btn2("选择字体…", false, 13);
            pickBtn.Click += delegate { PickFont(); };
            TextBlock pickDesc;
            stack.Children.Add(T.Row("\uE8D2", "源字体", "未选择", pickBtn, out pickDesc));
            _pickDesc = pickDesc;

            string[] names = { "常规", "粗体", "细体", "英文数字", "Segoe UI" };
            string[] glyphs = { "\uE8D2", "\uE8DD", "\uE8D3", "\uE8D2", "\uE8D2" };
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var st = T.Label("当前状态：", 11, T.TextFaint, false);
                st.VerticalAlignment = VerticalAlignment.Center;
                right.Children.Add(st);

                var prev = T.Label("读取中", 13, T.Text, false);
                prev.Margin = new Thickness(0, 0, 10, 0);
                prev.VerticalAlignment = VerticalAlignment.Center;
                right.Children.Add(prev);
                _fontPreview[idx] = prev;

                var meta = T.Label("", 11, T.TextFaint, false);
                meta.Margin = new Thickness(0, 0, 14, 0);
                meta.VerticalAlignment = VerticalAlignment.Center;
                right.Children.Add(meta);
                _metaText[idx] = meta;



                var b = T.Btn2("选择", false, 13);
                b.Margin = new Thickness(0, 0, 12, 0);
                b.Click += delegate { PickSlot(idx); };
                right.Children.Add(b);

                var sw = T.Switch(_on[i]);
                sw.Click += delegate { _on[idx] = sw.IsChecked == true; };
                right.Children.Add(sw);

                TextBlock src;
                stack.Children.Add(T.Row(glyphs[i], names[i], "未选择", right, out src));
                _srcText[i] = src;
            }

            stack.Children.Add(T.SectionHeader("系统元素", false));


            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                int idx = i;
                var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                var sizeLabel = T.Label("字号", 11, T.TextFaint, false);
                sizeLabel.Margin = new Thickness(0, 0, 6, 0);
                sizeLabel.VerticalAlignment = VerticalAlignment.Center;
                right.Children.Add(sizeLabel);

                var size = new TextBox
                {
                    Text = "9",
                    FontSize = 12,
                    Width = 66,
                    Height = 30,
                    Padding = new Thickness(4, 4, 4, 4),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center
                };
                right.Children.Add(size);
                _winSize[i] = size;

                var b = T.Btn2("选择", false, 12);
                b.Margin = new Thickness(0, 0, 10, 0);
                b.Click += delegate { PickWin32Face(idx); };
                right.Children.Add(b);

                var sw = T.Switch(_winOn[idx]);
                sw.Click += delegate { _winOn[idx] = sw.IsChecked == true; };
                right.Children.Add(sw);
                _winTog[idx] = sw;



                TextBlock desc;
                stack.Children.Add(T.Row("\uE8D2", Win32Fonts.Names[i], "读取中", right, out desc));
                _winCur[i] = desc;
            }


            scroll.Content = stack;
            _pageScroll = scroll;
            Grid.SetRow(scroll, 0);
            root.Children.Add(scroll);

            // 底部固定操作区
            var bottom = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
            var bar = new StackPanel { Orientation = Orientation.Horizontal };
            var apply = T.Btn2("应用", true, 13);
            apply.Margin = new Thickness(0, 0, 10, 0);
            apply.Click += delegate { RunApply(false); };
            var rollback = T.Btn2("全部回滚", false, 13);
            rollback.Margin = new Thickness(0, 0, 10, 0);
            rollback.Click += delegate { RunRollbackAll(); };
            var defaults = T.Btn2("默认值", false, 13);
            defaults.Click += delegate { ResetToDefaults(); };
            bar.Children.Add(apply);
            bar.Children.Add(rollback);
            bar.Children.Add(defaults);
            bottom.Children.Add(bar);
            _status = T.Label("", 12, T.TextDim, false);
            _status.Margin = new Thickness(0, 8, 0, 0);
            bottom.Children.Add(_status);
            Grid.SetRow(bottom, 1);
            root.Children.Add(bottom);

            return root;
        }

        TextBlock _pickDesc;

        UIElement AdvRow(string title, string desc, out ToggleButton toggle, bool on)
        {
            var row = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(T.TwoLine(title, desc));
            var sw = T.Switch(on);
            Grid.SetColumn(sw, 1);
            row.Children.Add(sw);
            toggle = sw;
            return row;
        }

        /// <summary>高级页：独立成页（用户要求），放调试与单独还原入口。</summary>
        UIElement BuildAdvancedPage()
        {
            var stack = new StackPanel();
            stack.Children.Add(T.Label("高级", 28, T.Text, true));
            stack.Children.Add(new Border { Height = 16 });

            TextBlock advDesc1;
            var cacheSw = T.Switch(_advCacheOn);
            cacheSw.Click += delegate { _advCacheOn = cacheSw.IsChecked == true; };
            _advCache = cacheSw;
            stack.Children.Add(T.Row("\uE72C", "写入时清字体缓存", "重启后由系统重建", cacheSw, out advDesc1));

            TextBlock advDesc2;
            var userSw = T.Switch(_advUserFontsOn);
            userSw.Click += delegate { _advUserFontsOn = userSw.IsChecked == true; };
            _advUserFonts = userSw;
            stack.Children.Add(T.Row("\uE74D", "写入时清用户级同名字体", "会挡住替换", userSw, out advDesc2));

            TextBlock advDescGen;
            var genBtn = T.Btn2("生成", false, 13);
            genBtn.Click += delegate { RunApply(true); };
            stack.Children.Add(T.Row("\uE8A5", "只生成不写入", "只生成字体文件，不动系统", genBtn, out advDescGen));

            TextBlock advDescFont;
            var fontBtn = T.Btn2("还原", false, 13);
            fontBtn.Click += delegate { RunRollbackFontsOnly(); };
            stack.Children.Add(T.Row("\uE777", "只还原字体文件", "从最近一次备份还原字体文件", fontBtn, out advDescFont));

            TextBlock advDesc3;
            var rb = T.Btn2("恢复", false, 13);
            rb.Click += delegate
            {
                try { SetStatus(Installer.RestoreUserFonts(), false); }
                catch (Exception ex) { SetStatus("恢复失败：" + ex.Message, true); }
            };
            stack.Children.Add(T.Row("\uE777", "只还原用户级注册", "导入最近一次的注册表备份", rb, out advDesc3));

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack, Padding = new Thickness(0, 0, 6, 12) };
            _pageScroll = scroll;
            return scroll;
        }

        UIElement BuildPresetPage()
        {
            var stack = new StackPanel();
            stack.Children.Add(T.Label("预设", 28, T.Text, true));
            var sub2 = T.Label("把当前这套设置存下来，下次一键载入。", 12, T.TextDim, false);
            sub2.Margin = new Thickness(0, 6, 0, 0);
            stack.Children.Add(sub2);
            stack.Children.Add(new Border { Height = 16 });

            var card = T.CardBox();
            var inner = new StackPanel();
            var list = new ListBox { Height = 220, FontSize = 13, Background = T.B(Color.FromRgb(0x26, 0x26, 0x2B)), BorderThickness = new Thickness(0), Foreground = T.Text, Margin = new Thickness(0, 0, 0, 10) };
            inner.Children.Add(list);

            var nameBox = new TextBox
            {
                FontSize = 13,
                Margin = new Thickness(0, 0, 0, 10),
                Background = T.B(Color.FromRgb(0x26, 0x26, 0x2B)),
                Foreground = T.Text,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(8, 6, 8, 6)
            };
            nameBox.Height = 32;
            nameBox.Width = 240;
            TextBlock nameDesc;
            stack.Children.Add(T.Row("\uE8A5", "预设名字", "保存时用这个名字", nameBox, out nameDesc));

            var bar = new StackPanel { Orientation = Orientation.Horizontal };
            var saveBtn = T.Btn2("另存为", true, 13);
            saveBtn.Margin = new Thickness(0, 0, 8, 0);
            var delBtn = T.Btn2("删除", false, 13);
            bar.Children.Add(saveBtn);
            bar.Children.Add(delBtn);
            inner.Children.Add(bar);

            var status = T.Label("", 12, T.TextDim, false);
            status.Margin = new Thickness(0, 8, 0, 0);
            inner.Children.Add(status);

            saveBtn.Click += delegate
            {
                string nm = (nameBox.Text ?? "").Trim();
                if (nm.Length == 0) { status.Text = "先起个名字"; return; }
                var faces = new string[5];
                var sizes = new decimal[5];
                for (int i = 0; i < 5; i++) faces[i] = _slots[i] == null ? "" : _slots[i].Family;
                try
                {
                    Presets.Save(nm, "", faces, sizes);
                    list.Items.Clear();
                    foreach (var n in Presets.List()) list.Items.Add(n);
                    status.Text = "已保存预设 " + nm;
                }
                catch (Exception ex) { status.Text = "保存失败：" + ex.Message; }
            };
            var loadPreset = new Action<string>(delegate (string item)
            {
                string fam; string[] faces; decimal[] sizes;
                if (!Presets.Load(item, out fam, out faces, out sizes)) { status.Text = "读取失败"; return; }
                var pool = FontLibrary.All;
                for (int i = 0; i < 5 && i < faces.Length; i++)
                {
                    if (string.IsNullOrEmpty(faces[i])) continue;
                    foreach (var f in pool)
                    {
                        if (f.Family == faces[i]) { _slots[i] = f; break; }
                    }
                }
                UpdateSlotLabels();
                status.Text = "已载入预设 " + item + "，点「应用」才生效";
            });
            list.SelectionChanged += delegate
            {
                var item = list.SelectedItem as string;
                if (item != null) loadPreset(item);
            };
            delBtn.Click += delegate
            {
                var item = list.SelectedItem as string;
                if (item == null) { status.Text = "先选一个预设"; return; }
                try
                {
                    Presets.Delete(item);
                    list.Items.Clear();
                    foreach (var n in Presets.List()) list.Items.Add(n);
                    status.Text = "已删除 " + item;
                }
                catch (Exception ex) { status.Text = "删除失败：" + ex.Message; }
            };

            card.Child = inner;
            stack.Children.Add(card);
            return stack;
        }

        // ---------------- 行为 ----------------

        void ReloadFonts()
        {
            try { FontLibrary.Scan(); } catch { }
        }

        void SetStatus(string text, bool error)
        {
            if (_status == null) return;
            _status.Text = text;
            _status.Foreground = error ? T.B(Color.FromRgb(0xFF, 0x8A, 0x80)) : T.TextDim;
        }

        void PickFont()
        {
            var dlg = new FontPickerDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() != true) return;

            ApplyPicked(dlg.Picked);
        }

        /// <summary>选定一个字体后：按字重自动挑同家族文件（常规 400 / 粗体 700 / 细体 300），英文数字与 Segoe UI 跟常规。 </summary>
        void ApplyPicked(FontEntry picked)
        {
            if (picked == null) return;
            var pool = FontLibrary.CandidatesForBase(picked);
            var regular = FontLibrary.Match(pool, 400) ?? picked;
            var bold = FontLibrary.Match(pool, 700) ?? regular;
            var light = FontLibrary.Match(pool, 300) ?? regular;
            _slots[0] = regular;
            _slots[1] = bold;
            _slots[2] = light;
            _slots[3] = regular;
            _slots[4] = regular;
            UpdateSlotLabels();
            SyncWin32FromChoice();
        }

        void PickSlot(int slot)
        {
            var dlg = new FontPickerDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() != true) return;
            _slots[slot] = dlg.Picked;
            UpdateSlotLabels();
        }

        void UpdateSlotLabels()
        {
            for (int i = 0; i < 5; i++)
            {
                if (_srcText[i] == null) continue;
                var e = _slots[i];
                _srcText[i].Text = e == null ? "未选择" : e.Family + " " + (e.Subfamily ?? "");
            }
            if (_pickDesc != null)
            {
                // 复位成「未选择」时这里也要清掉，否则顶部源字体那行会一直留着旧字体名
                _pickDesc.Text = _slots[0] == null ? "未选择" : _slots[0].Family + " " + (_slots[0].Subfamily ?? "");
            }
        }

        void RefreshTargets()
        {
            try
            {
                var infos = Installer.Inspect();
                var state = Installer.ReadState();
                _appliedFamily.Clear();
                foreach (var kv in state)
                {
                    if (kv.Key.StartsWith("family_")) _appliedFamily[kv.Key.Substring(7)] = kv.Value;
                }
                for (int i = 0; i < infos.Count && i < _metaText.Length; i++)
                {
                    if (_metaText[i] == null) continue;
                    var info = infos[i];
                    var parts = new List<string>();
                    if (!info.Exists)
                    {
                        if (_fontPreview[i] != null) _fontPreview[i].Text = "文件缺失";
                        parts.Add("缺文件");
                    }
                    else
                    {
                        // 按反馈：直接显示“现在装进去的是哪个字体”，不再显示槽位原始族名；不显示文件大小
                        var fam = info.Family;
                        {
                            // 不看 state 的 applied 标记：只要文件里能读出别家厂商/字体，就以它为准（旧版本装的也要显示对）
                            string replaced;
                            if (_appliedFamily.TryGetValue(info.FileName, out replaced) && !string.IsNullOrEmpty(replaced)) fam = replaced;
                            else
                            {
                                var det = Installer.DetectSourceFamily(info.FileName);
                                if (!string.IsNullOrEmpty(det)) fam = det;

                            }
                        }
                        if (string.IsNullOrEmpty(fam)) fam = "未识别";
                        if (_fontPreview[i] != null)
                        {
                            _fontPreview[i].Text = fam;
                            try { _fontPreview[i].FontFamily = new FontFamily(fam); } catch { }
                        }
                        // 「已替换」不再显示：状态里直接给现在用的字体族就够了；只保留「待重启」这种会影响行为的提示
                        if (info.Queued) parts.Add("待重启");
                    }
                    _metaText[i].Text = string.Join("   ", parts.ToArray());
                }
            }
            catch (Exception ex) { SetStatus("读系统字体状态失败：" + ex.Message, true); }
        }

        void LoadWin32()
        {
            try
            {
                var info = Win32Fonts.ReadAll();
                for (int i = 0; i < Win32Fonts.Names.Length && i < info.Count; i++)
                {
                    _winInfo[i] = info[i];
                    _winFace[i] = info[i].Face;
                    double pt = Math.Abs(info[i].HeightPx) * 72.0 / 96.0;
                    _winCur[i].Text = info[i].Face + "   " + pt.ToString("0") + " pt";
                    _winSize[i].Text = pt.ToString("0");
                }
            }
            catch { }
        }


        /// <summary>把当前选中的字体同步到系统元素那几行（只改界面，不写注册表）。</summary>
        void SyncWin32FromChoice()
        {
            var src = _slots[0];
            if (src == null) return;
            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                if (!_winOn[i]) continue;
                _winFace[i] = src.Family;
                if (_winCur[i] != null && _winSize[i] != null)
                    _winCur[i].Text = src.Family + "   " + _winSize[i].Text + " pt";
            }
        }

        void FollowCurrentFont()
        {
            var src = _slots[0];
            if (src == null) { SetStatus("先选一个字体。", true); return; }
            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                if (!_winOn[i]) continue;
                _winFace[i] = src.Family;
                _winCur[i].Text = src.Family + "   " + _winSize[i].Text + " pt";
            }
            ApplyWin32();
        }

        void ApplyWin32()
        {
            try
            {
                Win32Fonts.BackupOnce();
                int done = 0;
                for (int i = 0; i < Win32Fonts.Names.Length; i++)
                {
                    if (!_winOn[i]) continue;
                    if (string.IsNullOrEmpty(_winFace[i])) continue;
                    double pt;
                    double.TryParse(_winSize[i].Text, out pt);
                    if (pt <= 0) pt = 9;
                    var src = _winInfo[i];
                    Win32Fonts.Write(Win32Fonts.Keys[i], _winFace[i], pt,
                        src != null ? src.Weight : 400,
                        src != null ? src.CharSet : 1,
                        src != null && src.Italic);
                    done++;
                }
                SetStatus("系统元素已写入 " + done + " 项，注销后生效。", false);
            }
            catch (Exception ex) { SetStatus("写入失败：" + ex.Message, true); }
        }

        void RestoreWin32()
        {
            try { Win32Fonts.Restore(); SetStatus("系统元素已还原", false); }
            catch (Exception ex) { SetStatus("还原失败：" + ex.Message, true); }
        }

        void PickWin32Face(int i)
        {
            var dlg = new FontPickerDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() != true) return;
            _winFace[i] = dlg.Picked.Family;
            double pt;
            double.TryParse(_winSize[i].Text, out pt);
            if (pt <= 0) pt = 9;
            _winCur[i].Text = dlg.Picked.Family + "   " + pt.ToString("0") + " pt";
        }

        List<FontSource> CollectSources()
        {
            var list = new List<FontSource>();
            for (int i = 0; i < Installer.TargetFiles.Length && i < 5; i++)
            {
                if (_slots[i] == null || !_on[i]) continue;
                var e = _slots[i];
                list.Add(new FontSource
                {
                    TargetFile = Installer.TargetFiles[i],
                    SourcePath = e.Path,
                    FaceIndex = e.FaceIndex,
                    Family = e.Family,
                    Subfamily = e.Subfamily,
                    Data = File.ReadAllBytes(e.Path)
                });
            }
            return list;
        }

        void RunApply(bool generateOnly)
        {
            var list = CollectSources();
            if (list.Count == 0) { SetStatus("先选一个字体。", true); return; }
            SetStatus(generateOnly ? "正在生成…" : "正在生成并写入…", false);
            try
            {
                bool cache = _advCache != null && _advCache.IsChecked == true;
                bool userFonts = _advUserFonts != null && _advUserFonts.IsChecked == true;
                var res = Installer.Apply(list, delegate (string m) { }, generateOnly, cache, userFonts);
                if (!generateOnly) { try { FollowCurrentFont(); } catch { } }
                SetStatus(generateOnly ? ("已生成到 " + res.OutputDir) : "已写入，重启后生效。", false);
                RefreshTargets();
            }
            catch (Exception ex) { SetStatus("失败：" + ex.Message, true); }
        }

        /// <summary>把界面复位到刚打开的样子：未选择字体、开关全开、系统元素读回系统当前值。不写系统。</summary>
        void ResetToDefaults()
        {
            for (int i = 0; i < _slots.Length; i++) _slots[i] = null;
            for (int i = 0; i < _on.Length; i++)
            {
                _on[i] = true;
                if (_tog[i] != null) { _tog[i].IsChecked = true; T.RefreshToggleVisual(_tog[i]); }
            }
            for (int i = 0; i < _winOn.Length; i++)
            {
                _winOn[i] = true;
                if (_winTog[i] != null) { _winTog[i].IsChecked = true; T.RefreshToggleVisual(_winTog[i]); }
            }
            UpdateSlotLabels();
            try { LoadWin32(); } catch { }
            SetStatus("已恢复默认值", false);
        }

        void RunRollbackAll()
        {
            try
            {
                if (MessageBox.Show(this, "把字体文件、用户级字体注册都还原到替换前？\r\n重启后生效。", "全部回滚",
                        MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes) return;
                string log1 = "";
                if (Installer.LatestBackup() != null) Installer.Rollback(delegate (string m) { log1 += m + "; "; });
                else log1 = "没有字体备份; ";
                try { Win32Fonts.Restore(); } catch { }
                string log2 = Installer.RestoreUserFonts();
                SetStatus("已全部回滚：" + log1 + log2 + " 重启后恢复。", false);
                RefreshTargets();
            }
            catch (Exception ex) { SetStatus("回滚失败：" + ex.Message, true); }
        }

        void RunRollbackFontsOnly()
        {
            try
            {
                if (Installer.LatestBackup() == null) { SetStatus("没有可回滚的备份。", true); return; }
                Installer.Rollback(delegate (string m) { });
                SetStatus("已还原字体文件，重启后恢复。", false);
                RefreshTargets();
            }
            catch (Exception ex) { SetStatus("回滚失败：" + ex.Message, true); }
        }

        void RunRollback()
        {
            try
            {
                if (Installer.LatestBackup() == null) { SetStatus("没有可回滚的备份。", true); return; }
                Installer.Rollback(delegate (string m) { });
                SetStatus("已回滚，重启后恢复。", false);
                RefreshTargets();
            }
            catch (Exception ex) { SetStatus("回滚失败：" + ex.Message, true); }
        }
    }
}
