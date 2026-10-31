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
            return Row(glyph, title, desc, control, out descBlock, false);
        }

        /// <summary>disabled=true 时整行变灰且右侧控件不可点（用于「此系统不支持」的功能）。</summary>
        public static Border Row(string glyph, string title, string desc, UIElement control, out TextBlock descBlock, bool disabled)
        {
            return Row(glyph, title, desc, control, out descBlock, disabled, L.T("msg.not.supported.on.this.windows.version"));
        }

        public static Border Row(string glyph, string title, string desc, UIElement control, out TextBlock descBlock, bool disabled, string disabledTip)
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
            // 没有副标题时标题放大一档，避免整行只剩一颗小字
            bool noDesc = string.IsNullOrEmpty(desc);
            text.Children.Add(Label(title, noDesc ? 16 : 14, Text, false));
            var d = Label(desc, 12, TextDim, false);
            d.Margin = new Thickness(0, 2, 0, 0);
            if (!noDesc) text.Children.Add(d);
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
            if (disabled)
            {
                card.Opacity = 0.45;
                if (control != null) control.IsEnabled = false;   // 右侧控件不可点
                card.ToolTip = disabledTip;
                ToolTipService.SetShowOnDisabled(card, true);     // 禁用状态下也要能弹出提示
                ToolTipService.SetInitialShowDelay(card, 250);
            }
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
        ToggleButton _advCache, _advUserFonts, _advAutoWeight;
        bool _advCacheOn = true, _advUserFontsOn = true;
        bool _advAutoWeightOn = true;   // 自动按字重配对常规/粗体/细体
        bool _advLogOn;                 // 输出日志：默认关闭
        bool _fakeOld;
        FontEntry _picked;   // 用户在字体列表里实际点选的那一款（源字体行显示它，不是按字重匹配后的常规体）
        TextBlock _brandName;
        TextBlock _shellAllDesc;
        Border _mixedBanner;
        bool _switchingLang;
        TextBlock _mixedText;
        Button _mixedRestore;
        TextBox _shellAllSize;
        bool _loadingShell;   // LoadWin32 回填时不要触发“字号已设为”的状态栏提示

        /// <summary>把“全部系统元素”那一行里的字号写进 5 个元素（不写系统，等「应用」）。</summary>
        void ApplyShellSizeToAll(string text)
        {
            if (_loadingShell) return;   // 只是回填界面，不算用户操作
            double pt;
            if (!double.TryParse(text, out pt) || pt <= 0) return;
            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                if (_winSize[i] != null) _winSize[i].Text = pt.ToString("0");
                if (_winCur[i] != null)
                {
                    var face = string.IsNullOrEmpty(_winFace[i])
                        ? (_winInfo[i] != null ? _winInfo[i].Face : "")
                        : _winFace[i];
                    _winCur[i].Text = face + "   " + pt.ToString("0") + " pt";
                }
            }
            SetStatus(L.T("font.all.five.are.now") + pt.ToString("0") + " pt", false);
        }


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
            Title = L.T("font.yiti.v") + WpfProgram.Version;
            var appIcon = LoadAppIcon();
            if (appIcon != null) Icon = appIcon;
            Width = 1080;
            Height = 720;
            MinWidth = 1080;
            MinHeight = 720;
            MaxWidth = 1080;
            MaxHeight = 720;
            ResizeMode = ResizeMode.NoResize;   // 固定大小：不能拖动改变，也没有最大化
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;   // Mica 的前提：不能有 WS_CAPTION
            Background = Brushes.Transparent;

            WindowChrome.SetWindowChrome(this, new WindowChrome
            {
                CaptionHeight = 32,
                ResizeBorderThickness = new Thickness(0),
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
                // 启动时整窗淡入，避免一亮就硬切出来
                Styles.FadeIn(this, 0, 150);
                WpfProgram.Log("logpath=" + WpfProgram.LogPathUsed + "  base=" + AppDomain.CurrentDomain.BaseDirectory);
                try { DarkTitle(); } catch (Exception ex) { WpfProgram.Log("DarkTitle: " + ex.Message); }
                try { ReloadFonts(); } catch (Exception ex) { WpfProgram.Log("ReloadFonts: " + ex.Message); }
                try { RefreshTargets(); } catch (Exception ex) { WpfProgram.Log("RefreshTargets: " + ex.Message); }
                try { LoadWin32(); } catch (Exception ex) { WpfProgram.Log("LoadWin32: " + ex.Message); }
                foreach (var a in Environment.GetCommandLineArgs())
                {
                    WpfProgram.Log("arg: " + a);
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
                    else if (a.StartsWith("--shotlong:"))
                    {
                        // 整页长截图：把可滚内容按完整高度渲染（关于页这类一屏放不下的页面用）
                        var path = a.Substring(11);
                        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1200) };
                        timer.Tick += delegate
                        {
                            timer.Stop();
                            try
                            {
                                FrameworkElement target = null;
                                if (_pageScroll != null && _pageScroll.Content is FrameworkElement)
                                    target = (FrameworkElement)_pageScroll.Content;
                                else
                                    target = (FrameworkElement)Content;
                                target.UpdateLayout();
                                double w = target.ActualWidth;
                                if (_pageScroll != null && _pageScroll.ViewportWidth > 8) w = _pageScroll.ViewportWidth - 12;
                                if (w <= 0) w = Math.Max(1, ActualWidth - 260);
                                target.Measure(new Size(w, double.PositiveInfinity));
                                var fullW = Math.Max(w, target.DesiredSize.Width);
                                target.Arrange(new Rect(0, 0, fullW, target.DesiredSize.Height));
                                target.UpdateLayout();
                                var hh = (int)Math.Ceiling(target.DesiredSize.Height);
                                var ww = (int)Math.Ceiling(fullW);
                                if (hh <= 0 || ww <= 0) return;
                                const int pad = 16;
                                var rtb = new RenderTargetBitmap(ww + pad * 2, hh + pad * 2, 96, 96, PixelFormats.Pbgra32);
                                var dv = new DrawingVisual();
                                using (var dc = dv.RenderOpen())
                                {
                                    dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x24)), null, new Rect(0, 0, ww + pad * 2, hh + pad * 2));
                                    dc.DrawRectangle(new VisualBrush(target), null, new Rect(pad, pad, ww, hh));
                                }
                                rtb.Render(dv);
                                var enc = new PngBitmapEncoder();
                                enc.Frames.Add(BitmapFrame.Create(rtb));
                                using (var fs = System.IO.File.Create(path)) enc.Save(fs);
                            }
                            catch (Exception ex) { WpfProgram.Log("shotlong: " + ex.Message); }
                            Close();
                        };
                        timer.Start();
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
                    else if (a == "--logdiag")
                    {
                        // 自测：直接生成一份日志
                        try { WriteDiagnosticsLog(); } catch (Exception ex) { WpfProgram.Log("logdiag: " + ex); }
                    }
                    else if (a == "--shelltest")
                    {
                        // 自测：模拟“全部系统元素”选了 MiSans VF Medium，看 AnyShellSelected 的判定过程
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            var sb = new System.Text.StringBuilder();
                            for (int i = 0; i < Win32Fonts.Names.Length; i++)
                            {
                                _winFace[i] = "MiSans VF Medium";
                                if (_winSize[i] != null) _winSize[i].Text = "9";
                            }
                            for (int i = 0; i < Win32Fonts.Names.Length; i++)
                            {
                                var cur = _winInfo[i];
                                sb.AppendLine("row" + i + " on=" + _winOn[i] + " face=[" + _winFace[i] + "] cur=[" + (cur == null ? "null" : cur.Face) + "] size=[" + (_winSize[i] == null ? "null" : _winSize[i].Text) + "]");
                            }
                            sb.AppendLine("AnyShellSelected=" + AnyShellSelected());
                            try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "shelltest.log"), sb.ToString(), System.Text.Encoding.UTF8); } catch { }
                        }));
                    }
                    else if (a.StartsWith("--sizes:"))
                    {
                        string sz = a.Substring(8);
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            if (_shellAllSize != null) _shellAllSize.Text = sz;
                            ApplyShellSizeToAll(sz);
                        }));
                    }
                    else if (a == "--logtest")
                    {
                        // 自测：打开输出日志开关并写一条，验证开关真的会产出日志
                        _advLogOn = true;
                        MaybeLog("selftest");
                    }
                    else if (a == "--togglelang")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate { ApplyLanguage(!L.En); }));
                    }
                    else if (a == "--advgen")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate { RunApply(true); }));
                    }
                    else if (a == "--backupdemo")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            var dlg = new BackupDialog { Owner = this };
                            dlg.ShowDialog();
                        }));
                    }
                    else if (a == "--confirmdemo")
                    {
                        Dispatcher.BeginInvoke(new Action(delegate
                        {
                            var dlg = new ConfirmDialog(L.T("font.defaults"), L.T("font.switch.to.the.default.preset"), L.T("dlg.yes"), L.T("dlg.no")) { Owner = this };
                            dlg.ShowDialog();
                        }));
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

        /// <summary>Win11 原生圆角（DWMWA_WINDOW_CORNER_PREFERENCE = 33，DWMWCP_ROUND = 2）。</summary>
        void RoundCorners()
        {
            try
            {
                var h = new WindowInteropHelper(this).Handle;
                if (h == IntPtr.Zero) return;
                int pref = 2;   // DWMWCP_ROUND
                DwmSetWindowAttribute(h, 33, ref pref, sizeof(int));
            }
            catch { }
        }

        void DarkTitle()
        {
            // Win7/10 没有 DWMWA_SYSTEMBACKDROP_TYPE，别硬套；直接用纯灰底
            bool backdrop = Installer.SupportsBackdrop();
            foreach (var a in Environment.GetCommandLineArgs()) if (a == "--flat") backdrop = false;   // 自测：强制走灰底分支
            if (!backdrop)
            {
                Background = new SolidColorBrush(Color.FromRgb(0x24, 0x24, 0x28));
                WpfProgram.Log("background: flat grey (no backdrop support on this OS)");
                try
                {
                    var h0 = new WindowInteropHelper(this).Handle;
                    int dark = 1;
                    DwmSetWindowAttribute(h0, 20, ref dark, sizeof(int));
                }
                catch { }
                RoundCorners();
                return;
            }
            DarkTitleForWin11();
            RoundCorners();
        }

        void DarkTitleForWin11()
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
            var appName = T.Label(L.T("msg.yiti"), 15, T.Text, true);
            _brandName = appName;
            appName.VerticalAlignment = VerticalAlignment.Bottom;
            brand.Children.Add(appName);
            rail.Children.Add(brand);
            _nav = new ListBox
            {
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                ItemContainerStyle = NavStyle()
            };
            _nav.Items.Add(NavItem("\uE8D2", L.T("msg.fonts")));
            _nav.Items.Add(NavItem("\uE713", L.T("msg.advanced")));
            _nav.Items.Add(NavItem("\uE946", L.T("msg.about")));
            _nav.SelectionChanged += delegate { ShowPage(); };
            rail.Children.Add(_nav);
            railGrid.Children.Add(rail);
            Grid.SetColumn(railGrid, 0);
            root.Children.Add(railGrid);

            // ---- 右栏 ----
            var right = new Grid();
            right.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            right.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // 状态栏放在外壳里：任何页面的操作反馈都在同一个位置显示
            _status = T.Label("", 12, T.TextDim, false);
            _status.Margin = new Thickness(26, 0, 26, 14);
            _status.TextWrapping = TextWrapping.Wrap;
            Grid.SetRow(_status, 1);
            right.Children.Add(_status);

            // 自绘窗口按钮（WindowStyle=None 后系统不再画）
            var sys = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 6, 10, 0) };
            sys.Children.Add(WinBtn("–", delegate { WindowState = WindowState.Minimized; }));
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
            if (_switchingLang) return;   // 语言切换过程中不要重建页面
            int i = _nav.SelectedIndex;
            if (i < 0) i = 0;
            if (i == 0)
            {
                _content.Content = BuildFontPage();
                // 页面每次切换都会重建控件，必须重新灌一次真实状态，否则永远停在「读取中」
                try { UpdateSlotLabels(); } catch { }
                try { RefreshTargets(); } catch { }
                try { LoadWin32(); } catch { }
            }
            else if (i == 1) _content.Content = BuildAdvancedPage();
            else _content.Content = BuildAboutPage();
            // 切页时给内容一个短淡入，避免“啪”地一下换掉
            Styles.FadeIn(_content, 8, 160);
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
            stack.Children.Add(T.Label(L.T("msg.fonts"), 28, T.Text, true));

            // 半替换提示：只在检测到“部分目标换过、部分没换”时出现
            var bannerBar = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var bannerText = T.Label("", 12, T.Text, false);
            bannerText.VerticalAlignment = VerticalAlignment.Center;
            bannerText.Margin = new Thickness(0, 0, 12, 0);
            bannerText.TextWrapping = TextWrapping.Wrap;
            var bannerBtn = T.Btn2(L.T("font.re.apply"), false, 12);
            bannerBtn.Click += delegate { RunApply(false); };
            bannerBar.Children.Add(bannerText);
            bannerBar.Children.Add(bannerBtn);
            var restoreBtn = T.Btn2(L.T("font.restore.from.backup"), true, 12);
            restoreBtn.Margin = new Thickness(8, 0, 0, 0);
            restoreBtn.Visibility = Visibility.Collapsed;
            restoreBtn.Click += delegate
            {
                if (!Confirm(L.T("font.restore.the.missing.font.files.from.a.backup"), L.T("font.restore.from.backup"))) return;
                string msg = "";
                try
                {
                    int n2 = Installer.RestoreMissingFromBackup(delegate (string s) { msg += s + "; "; });
                    SetStatus(L.T("font.restored") + n2 + L.T("backup.files") + msg, n2 == 0);
                    ConfirmDialog.Info(this, L.T("font.restore.from.backup"), msg);
                }
                catch (Exception ex) { SetStatus(L.T("font.restore.failed") + ex.Message, true); ConfirmDialog.Info(this, L.T("font.restore.from.backup"), ex.Message); }
                RefreshTargets();
            };
            bannerBar.Children.Add(restoreBtn);
            _mixedRestore = restoreBtn;
            _mixedBanner = new Border
            {
                Background = T.B(Color.FromRgb(0x3A, 0x2E, 0x1F)),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(14, 10, 14, 10),
                Margin = new Thickness(0, 12, 0, 0),
                Visibility = Visibility.Collapsed,
                Child = bannerBar
            };
            _mixedText = bannerText;
            stack.Children.Add(_mixedBanner);
            var spacer = new Border { Height = 14 };
            stack.Children.Add(spacer);

            stack.Children.Add(T.SectionTitle(L.T("font.targets"), true));

            // 选字体
            var pickBtn = T.Btn2(L.T("font.select.font"), false, 13);
            pickBtn.Click += delegate { PickFont(); };
            TextBlock pickDesc;
            stack.Children.Add(T.Row("\uE8D2", L.T("font.source.font"), L.T("font.not.selected"), pickBtn, out pickDesc));
            _pickDesc = pickDesc;

            // 自动匹配字重：放在源字体正下方，和它的行为直接相关
            TextBlock weightDesc;
            var weightSw = T.Switch(_advAutoWeightOn);
            weightSw.Click += delegate { _advAutoWeightOn = weightSw.IsChecked == true; };
            _advAutoWeight = weightSw;
            stack.Children.Add(T.Row("\uE8DD", L.T("font.auto.weight.matching"), L.T("font.match.regular.bold.light.at.400.700.300"), weightSw, out weightDesc));

            string[] names = { L.T("font.regular"), L.T("font.bold"), L.T("font.light"), L.T("font.latin.digits"), "Segoe UI" };
            string[] glyphs = { "\uE8D2", "\uE8DD", "\uE8D3", "\uE8D2", "\uE8D2" };
            for (int i = 0; i < 5 && i < Installer.TargetFiles.Length; i++)
            {
                // Win7 等系统没有 msyhl.ttc / SegUIVar.ttf：行还在，但整行置灰不可点
                bool missing = !File.Exists(Path.Combine(Installer.FontsDir, Installer.TargetFiles[i]));
                // 自测：模拟老系统（细体/英数不存在、无用户级字体），用来核对置灰样式
                foreach (var a in Environment.GetCommandLineArgs())
                {
                    if (a == "--fake-old")
                    {
                        if (i == 2 || i == 3) missing = true;
                        _fakeOld = true;
                    }
                }
                int idx = i;
                var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
                var st = T.Label(L.T("font.now"), 11, T.TextFaint, false);
                st.VerticalAlignment = VerticalAlignment.Center;
                right.Children.Add(st);

                var prev = T.Label(L.T("font.reading"), 13, T.Text, false);
                prev.Margin = new Thickness(0, 0, 10, 0);
                prev.VerticalAlignment = VerticalAlignment.Center;
                right.Children.Add(prev);
                _fontPreview[idx] = prev;

                var meta = T.Label("", 11, T.TextFaint, false);
                meta.Margin = new Thickness(0, 0, 14, 0);
                meta.VerticalAlignment = VerticalAlignment.Center;
                right.Children.Add(meta);
                _metaText[idx] = meta;



                var b = T.Btn2(L.T("font.select"), false, 13);
                b.Margin = new Thickness(0, 0, 12, 0);
                b.Click += delegate { PickSlot(idx); };
                right.Children.Add(b);

                var sw = T.Switch(_on[i]);
                sw.Click += delegate { _on[idx] = sw.IsChecked == true; };
                right.Children.Add(sw);

                TextBlock src;
                var rowCard = T.Row(glyphs[i], names[i], missing ? L.T("font.this.windows.version.has.no.such.font") : L.T("font.not.selected"), right, out src, missing);
                stack.Children.Add(rowCard);
                _srcText[i] = src;
                if (missing) _on[i] = false;
            }

            stack.Children.Add(T.SectionHeader(L.T("font.shell.elements"), false));

            // 一键把 5 个元素设成同一款字体 / 同一个字号（和「源字体」同样的行样式）
            var shellBar = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var shellSizeLabel = T.Label(L.T("font.size"), 11, T.TextFaint, false);
            shellSizeLabel.Margin = new Thickness(0, 0, 6, 0);
            shellSizeLabel.VerticalAlignment = VerticalAlignment.Center;
            shellBar.Children.Add(shellSizeLabel);
            var shellSize = new TextBox
            {
                Text = _winSize[0] != null ? _winSize[0].Text : "9",   // 跟下面 5 行保持一致（LoadWin32 之后会再同步一次）
                FontSize = 12,
                Width = 66,
                MinHeight = 30,
                Padding = new Thickness(6, 4, 6, 4),
                Margin = new Thickness(0, 0, 10, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            shellSize.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.Key == Key.Enter) { ApplyShellSizeToAll(shellSize.Text); e.Handled = true; }
            };
            shellSize.LostFocus += delegate { ApplyShellSizeToAll(shellSize.Text); };
            shellSize.TextChanged += delegate { ApplyShellSizeToAll(shellSize.Text); };
            shellBar.Children.Add(shellSize);
            var shellPick = T.Btn2(L.T("font.select.font"), false, 13);
            shellPick.Click += delegate { PickShellFontForAll(); };
            shellBar.Children.Add(shellPick);
            TextBlock shellDesc;
            stack.Children.Add(T.Row("\uE8D2", L.T("font.all.shell.elements"), L.T("font.set.the.font.and.size.for.all.five"), shellBar, out shellDesc));
            _shellAllDesc = shellDesc;
            _shellAllSize = shellSize;

            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                int idx = i;
                var right = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

                var sizeLabel = T.Label(L.T("font.size"), 11, T.TextFaint, false);
                sizeLabel.Margin = new Thickness(0, 0, 6, 0);
                sizeLabel.VerticalAlignment = VerticalAlignment.Center;
                right.Children.Add(sizeLabel);

                var size = new TextBox
                {
                    Text = "9",
                    FontSize = 12,
                    Width = 66,
                    MinHeight = 30,
                    Padding = new Thickness(6, 4, 6, 4),
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalContentAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                };
                right.Children.Add(size);
                _winSize[i] = size;

                var b = T.Btn2(L.T("font.select"), false, 12);
                b.Margin = new Thickness(0, 0, 10, 0);
                b.Click += delegate { PickWin32Face(idx); };
                right.Children.Add(b);

                var sw = T.Switch(_winOn[idx]);
                sw.Click += delegate { _winOn[idx] = sw.IsChecked == true; };
                right.Children.Add(sw);
                _winTog[idx] = sw;



                TextBlock desc;
                stack.Children.Add(T.Row("\uE8D2", L.T(Win32Fonts.Names[i]), L.T("font.reading"), right, out desc));
                _winCur[i] = desc;
            }


            scroll.Content = stack;
            _pageScroll = scroll;
            Grid.SetRow(scroll, 0);
            root.Children.Add(scroll);

            // 底部固定操作区
            var bottom = new StackPanel { Margin = new Thickness(0, 14, 0, 0) };
            var bar = new StackPanel { Orientation = Orientation.Horizontal };
            var apply = T.Btn2(L.T("font.apply"), true, 13);
            apply.Margin = new Thickness(0, 0, 10, 0);
            apply.Click += delegate { WithBusy(apply, L.T("font.applying"), delegate { RunApply(false); }); };
            var backupEntry = T.Btn2(L.T("backup.backups"), false, 13);   // 还原/删备份都收进这里
            backupEntry.Margin = new Thickness(0, 0, 10, 0);
            backupEntry.Click += delegate { var dlg = new BackupDialog { Owner = this }; dlg.ShowDialog(); RefreshTargets(); };
            var defaults = T.Btn2(L.T("font.defaults"), false, 13);
            defaults.Click += delegate { ResetToDefaults(); };
            bar.Children.Add(apply);
            bar.Children.Add(backupEntry);
            bar.Children.Add(defaults);
            bottom.Children.Add(bar);
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
            stack.Children.Add(T.Label(L.T("msg.advanced"), 28, T.Text, true));
            stack.Children.Add(new Border { Height = 16 });


            TextBlock advDesc1;
            var cacheSw = T.Switch(_advCacheOn);
            cacheSw.Click += delegate { _advCacheOn = cacheSw.IsChecked == true; };
            _advCache = cacheSw;
            stack.Children.Add(T.Row("\uE72C", L.T("adv.clear.the.font.cache.on.apply"), "", cacheSw, out advDesc1));

            // 老系统没有「按用户安装字体」：这一项显示但置灰不可点
            bool canUserFonts = Installer.SupportsUserFonts() && !_fakeOld;
            TextBlock advDesc2;
            var userSw = T.Switch(_advUserFontsOn && canUserFonts);
            userSw.Click += delegate { _advUserFontsOn = userSw.IsChecked == true; };
            _advUserFonts = userSw;
            var userRow = T.Row("\uE74D", L.T("adv.remove.per.user.same.name.fonts"),
                canUserFonts ? "" : L.T("adv.this.windows.version.has.no.per.user.fonts"), userSw, out advDesc2, !canUserFonts);
            // 行内不再写解释，改成悬停提示
            userRow.ToolTip = new TextBlock { Text = L.T("adv.user.fonts.tip"), MaxWidth = 520, TextWrapping = TextWrapping.Wrap };
            ToolTipService.SetInitialShowDelay(userRow, 350);
            ToolTipService.SetShowDuration(userRow, 120000);
            ToolTipService.SetPlacement(userRow, System.Windows.Controls.Primitives.PlacementMode.Right);
            stack.Children.Add(userRow);

            TextBlock advDescGen;
            var genBtn = T.Btn2(L.T("adv.generate"), false, 13);
            genBtn.Click += delegate { WithBusy(genBtn, L.T("adv.generating"), delegate { RunApply(true); }); };
            stack.Children.Add(T.Row("\uE8A5", L.T("adv.generate.only"), L.T("adv.build.the.files.only.leave.the.system.alone"), genBtn, out advDescGen));



            TextBlock advDescBackup;
            var backupBtn = T.Btn2(L.T("adv.manage"), false, 13);
            backupBtn.Click += delegate
            {
                var dlg = new BackupDialog { Owner = this };
                dlg.ShowDialog();
            };
            stack.Children.Add(T.Row("\uE777", L.T("backup.backups"), L.T("adv.review.or.delete.old.backups.or.roll.back.to"), backupBtn, out advDescBackup));


            TextBlock advDescRestartExplorer;
            var restartExplorerBtn = T.Btn2(L.T("adv.restart.explorer"), false, 13);
            restartExplorerBtn.Click += delegate { RestartExplorerFromButton(); };
            stack.Children.Add(T.Row("\uE777", L.T("adv.restart.explorer"), "", restartExplorerBtn, out advDescRestartExplorer));

            TextBlock advDescLog;
            var logSw = T.Switch(_advLogOn);
            logSw.Click += delegate { _advLogOn = logSw.IsChecked == true; };
            stack.Children.Add(T.Row("\uE8A5", L.T("adv.write.a.log"), L.T("adv.writes.yy.mm.dd.ss.log.next.to.the.exe.after"), logSw, out advDescLog));

            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack, Padding = new Thickness(0, 0, 6, 12) };
            _pageScroll = scroll;
            return scroll;
        }

        /// <summary>关于页：应用名、版本、作者、用途与运行要求（版本号只在这里出现）。</summary>
        UIElement BuildAboutPage()
        {
            var stack = new StackPanel();
            stack.Children.Add(T.Label(L.T("msg.about"), 28, T.Text, true));

            // 和其它页统一：左对齐的头部（图标 + 名称 + 版本），下面是同样的卡片行
            var head = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 22, 0, 18) };
            var logo = new Border
            {
                Width = 72,
                Height = 72,
                CornerRadius = new CornerRadius(12),
                VerticalAlignment = VerticalAlignment.Center,
                Background = Brushes.Transparent
            };
            try
            {
                var bmp = LoadAppIcon();
                if (bmp != null) logo.Child = new Image { Source = bmp };
            }
            catch { }
            head.Children.Add(logo);

            var nameCol = new StackPanel { Margin = new Thickness(14, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            var nameRow = new StackPanel { Orientation = Orientation.Horizontal };
            var big = T.Label(L.T("msg.yiti"), 22, T.Text, true);
            big.VerticalAlignment = VerticalAlignment.Center;
            var ver = T.Label("V" + WpfProgram.Version, 12, T.TextDim, false);
            ver.VerticalAlignment = VerticalAlignment.Center;
            ver.Margin = new Thickness(8, 0, 0, 0);
            nameRow.Children.Add(big);
            nameRow.Children.Add(ver);
            nameCol.Children.Add(nameRow);
            head.Children.Add(nameCol);

            stack.Children.Add(head);


            TextBlock d4;
            stack.Children.Add(T.Row("\uE8A5", L.T("about.author"), "moyuer233", T.Label("", 13, T.Text, false), out d4));


            TextBlock dl;
            var langBar = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var zhBtn = T.Btn2("中文", !L.En, 12);
            zhBtn.Margin = new Thickness(0, 0, 8, 0);
            zhBtn.Click += delegate { ApplyLanguage(false); };
            var enBtn = T.Btn2("English", L.En, 12);
            enBtn.Click += delegate { ApplyLanguage(true); };
            langBar.Children.Add(zhBtn);
            langBar.Children.Add(enBtn);
            stack.Children.Add(T.Row("\uE774", L.T("about.language"), "中文 / English", langBar, out dl));

            TextBlock d6;
            var repo = T.Label("github.com/moyuer233/yiti", 13, T.Accent, false);
            repo.Cursor = Cursors.Hand;
            repo.MouseLeftButtonUp += delegate
            {
                try { System.Diagnostics.Process.Start("https://github.com/moyuer233/yiti"); } catch { }
            };
            stack.Children.Add(T.Row("\uE774", L.T("about.project.home"), L.T("about.source.code.and.issue.tracker"), repo, out d6));


            var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack, Padding = new Thickness(0, 0, 6, 12) };
            _pageScroll = scroll;
            return scroll;
        }

        // ---------------- 行为 ----------------

        void ReloadFonts()
        {
            try { FontLibrary.Scan(); } catch { }
        }

        /// <summary>把按钮切到忙碌态（文字变灰、不可点），跑完再恢复，避免长操作时界面像卡死。</summary>
        void WithBusy(Button btn, string busyText, Action work)
        {
            object old = null;
            if (btn != null) { old = btn.Content; btn.Content = busyText; btn.IsEnabled = false; }
            try { Dispatcher.Invoke(new Action(delegate { }), System.Windows.Threading.DispatcherPriority.Render); } catch { }
            try { work(); }
            finally { if (btn != null) { btn.Content = old; btn.IsEnabled = true; } }
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
            // 选的是可变字体时优先换用同家族的静态版本：
            // 可变字体的默认实例不一定是 400（MiSans VF 是 330），剥掉变体轴直接取默认实例会整体偏细
            Diagnostics.Append("pick source: " + picked.Family + " / " + (picked.Subfamily ?? "") + " w" + picked.Weight + "  " + (picked.Path ?? "(no path)"));
            var use = picked;
            if (FontLibrary.IsVariableEntry(picked))
            {
                var s400 = FontLibrary.FindStaticSibling(picked, 400);
                if (s400 != null) use = s400;
            }
            if (!_advAutoWeightOn)
            {
                // 关掉自动匹配：五档全部用你点的那一款
                _picked = picked;
                for (int i = 0; i < _slots.Length; i++) _slots[i] = use;
                UpdateSlotLabels();
                SyncWin32FromChoice();
                SetStatus(L.T("font.auto.matching.off.all.five.slots.use") + use.Family + " " + (use.Subfamily ?? ""), false);
                return;
            }
            var pool = FontLibrary.CandidatesForBase(use);
            var regular = FontLibrary.Match(pool, 400) ?? use;
            var bold = FontLibrary.Match(pool, 700) ?? regular;
            var light = FontLibrary.Match(pool, 300) ?? regular;
            _picked = picked;
            _slots[0] = regular;
            _slots[1] = bold;
            _slots[2] = light;
            _slots[3] = regular;
            _slots[4] = regular;
            UpdateSlotLabels();
            SyncWin32FromChoice();
            // 说清“你点的是哪个、被分到哪一行”，缺字重也直说（可变字体常常只有一个静态档）
            var warn = "";
            if (bold.Weight == regular.Weight) warn += L.T("font.warn.no.bold");
            if (light.Weight == regular.Weight) warn += L.T("font.warn.no.light");
            SetStatus(L.T("font.matched.by.weight.regular") + (regular.Subfamily ?? "")
                + L.T("font.bold.2") + (bold.Subfamily ?? "")
                + L.T("font.light.2") + (light.Subfamily ?? "") + warn, false);
        }

        void PickSlot(int slot)
        {
            var dlg = new FontPickerDialog();
            dlg.Owner = this;
            if (dlg.ShowDialog() != true) return;
            _slots[slot] = dlg.Picked;
            Diagnostics.Append("pick slot " + slot + " (" + Installer.TargetFiles[slot] + "): " + dlg.Picked.Family + " / " + (dlg.Picked.Subfamily ?? "") + " w" + dlg.Picked.Weight + "  " + (dlg.Picked.Path ?? "(no path)"));
            UpdateSlotLabels();
        }

        void UpdateSlotLabels()
        {
            for (int i = 0; i < 5; i++)
            {
                if (_srcText[i] == null) continue;
                var e = _slots[i];
                _srcText[i].Text = e == null ? L.T("font.not.selected") : e.Family + " " + (e.Subfamily ?? "");
            }
            if (_pickDesc != null)
            {
                // 复位成「未选择」时这里也要清掉，否则顶部源字体那行会一直留着旧字体名
                var srcEntry = _picked ?? _slots[0];
            _pickDesc.Text = srcEntry == null ? L.T("font.not.selected") : srcEntry.Family + " " + (srcEntry.Subfamily ?? "");
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
                        if (_fontPreview[i] != null) _fontPreview[i].Text = L.T("msg.file.missing");
                        parts.Add(L.T("msg.missing"));
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
                        if (string.IsNullOrEmpty(fam)) fam = L.T("msg.unknown");
                        if (_fontPreview[i] != null)
                        {
                            _fontPreview[i].Text = fam;
                            try { _fontPreview[i].FontFamily = new FontFamily(fam); } catch { }
                        }
                        // 「已替换」不再显示：状态里直接给现在用的字体族就够了；只保留「待重启」这种会影响行为的提示
                        if (info.Queued) parts.Add(L.T("msg.reboot.pending"));
                    }
                    _metaText[i].Text = string.Join("   ", parts.ToArray());
                }
                UpdateMixedBanner();
            }
            catch (Exception ex) { SetStatus(L.T("msg.failed.to.read.the.font.state") + ex.Message, true); }
        }

        /// <summary>检测“半替换”：一部分目标已经是我们的字体、另一部分还是原版/没落地。</summary>
        void UpdateMixedBanner()
        {
            if (_mixedBanner == null || _mixedText == null) return;
            Installer.ConsistencyReport rep;
            try { rep = Installer.CheckConsistency(); } catch { return; }
            bool hasMissing = rep.Missing.Count > 0;
            if (!hasMissing && !rep.IsMixed) { _mixedBanner.Visibility = Visibility.Collapsed; return; }

            if (_mixedRestore != null) _mixedRestore.Visibility = hasMissing ? Visibility.Visible : Visibility.Collapsed;

            if (hasMissing)
            {
                var names = new List<string>();
                foreach (var f in rep.Missing) names.Add(Installer.LabelOf(f));
                _mixedText.Text = L.T("msg.missing.2") + string.Join(L.T("msg.text97"), names.ToArray())
                    + (Installer.FindBackupCopy(rep.Missing[0]) != null ? L.T("msg.restorable.from.backup") : L.T("msg.not.in.any.backup"));
                _mixedBanner.Visibility = Visibility.Visible;
                return;
            }

            var bad = new List<string>();
            foreach (var f in rep.AppliedPending) bad.Add(Installer.LabelOf(f));
            foreach (var f in rep.Never) bad.Add(Installer.LabelOf(f));
            _mixedText.Text = L.T("msg.half.replaced") + string.Join(L.T("msg.text97"), bad.ToArray()) + L.T("msg.are.still.original.or.never.landed");
            _mixedBanner.Visibility = Visibility.Visible;
        }

        void LoadWin32()
        {
            try
            {
                var info = Win32Fonts.ReadAll();
                _loadingShell = true;
                for (int i = 0; i < Win32Fonts.Names.Length && i < info.Count; i++)
                {
                    _winInfo[i] = info[i];
                    // 已有用户选择时不要覆盖：这里只补“当前系统值”和空着的字段
                    if (string.IsNullOrEmpty(_winFace[i])) _winFace[i] = info[i].Face;
                    double pt = Math.Abs(info[i].HeightPx) * 72.0 / 96.0;
                    _winCur[i].Text = _winFace[i] + "   " + pt.ToString("0") + " pt";
                    if (_winSize[i] != null && string.IsNullOrEmpty(_winSize[i].Text)) _winSize[i].Text = pt.ToString("0");
                    if (i == 0 && _shellAllSize != null) _shellAllSize.Text = pt.ToString("0");   // 「全部系统元素」那一行跟着走
                }
            }
            catch { }
            finally { _loadingShell = false; }
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



        /// <summary>一键：把 5 个系统元素都设成选中的字体（字号保持不变）。</summary>
        void PickShellFontForAll()
        {
            var dlg = new FontPickerDialog(true);   // 界面元素：GDI 枚举，可选可变字体的命名实例
            dlg.Owner = this;
            if (dlg.ShowDialog() != true) return;
            var face = dlg.Picked.Family;
            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                _winFace[i] = face;
                double pt;
                double.TryParse(_winSize[i].Text, out pt);
                if (pt <= 0) pt = 9;
                if (_winCur[i] != null) _winCur[i].Text = face + "   " + pt.ToString("0") + " pt";
            }
            if (_shellAllDesc != null) _shellAllDesc.Text = face;
            SetStatus(L.T("msg.all.five.are.now") + face, false);
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
                    // 只改字号也可以写：字体留空时用系统当前字体兜底
                    var face = _winFace[i];
                    if (string.IsNullOrEmpty(face) && _winInfo[i] != null) face = _winInfo[i].Face;
                    if (string.IsNullOrEmpty(face)) continue;
                    double pt;
                    double.TryParse(_winSize[i].Text, out pt);
                    if (pt <= 0) pt = 9;
                    var src = _winInfo[i];
                    Win32Fonts.Write(Win32Fonts.Keys[i], face, pt,
                        src != null ? src.Weight : 400,
                        src != null ? src.CharSet : 1,
                        src != null && src.Italic);
                    done++;
                }
                // 关键：只写注册表要重新登录才生效；这里调 SystemParametersInfo 立即应用（和 noMeiryoUI 一致）
                var faces = new string[Win32Fonts.Keys.Length];
                var pts = new double[Win32Fonts.Keys.Length];
                for (int i = 0; i < Win32Fonts.Keys.Length; i++)
                {
                    faces[i] = _winFace[i];
                    double p;
                    double.TryParse(_winSize[i].Text, out p);
                    pts[i] = p > 0 ? p : 9;
                }
                int live = ShellMetrics.Apply(faces, pts, _winOn, _winInfo);
                SetStatus(L.T("msg.shell.elements.applied") + live + L.T("msg.item.s"), false);
            }
            catch (Exception ex) { SetStatus(L.T("msg.write.failed") + ex.Message, true); }
        }

        /// <summary>「输出日志」：把运行环境与状态写进软件目录下的 yy-MM-dd-ss.log，方便在别的机器上排查兼容性。</summary>
        /// <summary>开关打开时，每次执行操作后写一份日志。</summary>
        void MaybeLog(string action)
        {
            if (!_advLogOn) return;
            WriteDiagnosticsLog(action);
        }

        void WriteDiagnosticsLog(string action = "manual")
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("=== Yiti " + WpfProgram.Version + "  [" + action + "] ===");
                sb.AppendLine("time      : " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                try { var os = Environment.OSVersion; sb.AppendLine("os        : " + os.VersionString + "  build=" + os.Version.Build); } catch { }
                sb.AppendLine("dotnet    : " + Environment.Version);
                sb.AppendLine("exe       : " + AppDomain.CurrentDomain.BaseDirectory);
                sb.AppendLine("data      : " + Installer.DataDir);
                sb.AppendLine("admin     : " + Installer.IsAdmin);
                sb.AppendLine("backdrop  : " + Installer.SupportsBackdrop() + " (Mica needs Win11 22000+)");
                sb.AppendLine("userfonts : " + Installer.SupportsUserFonts() + " (Win10 17763+)");
                sb.AppendLine("cache svc : " + Installer.FontCacheServiceName());
                sb.AppendLine("lang      : " + (L.En ? "en" : "zh"));
                sb.AppendLine();
                sb.AppendLine("[targets]");
                foreach (var f in Installer.TargetFiles)
                {
                    var p = System.IO.Path.Combine(Installer.FontsDir, f);
                    var exists = System.IO.File.Exists(p);
                    sb.AppendLine("  " + f.PadRight(13) + (exists ? "exists  " + new System.IO.FileInfo(p).Length.ToString().PadLeft(10) + " B  " + (Installer.DetectSourceFamily(f) ?? "-") : "MISSING"));
                }
                sb.AppendLine();
                sb.AppendLine("[shell elements]");
                for (int i = 0; i < Win32Fonts.Names.Length; i++)
                {
                    var info = Win32Fonts.Read(Win32Fonts.Keys[i]);
                    sb.AppendLine("  " + Win32Fonts.Names[i].PadRight(6) + (info == null ? "(none)" : info.Face + "  " + info.HeightPx + " px"));
                }
                sb.AppendLine();
                sb.AppendLine("[backup]");
                sb.AppendLine("  win32fonts.bak : " + Win32Fonts.HasBackup);
                var latest = Installer.LatestBackup();
                sb.AppendLine("  latest font dir: " + (latest ?? "(none)"));

                var name = DateTime.Now.ToString("yy-MM-dd-ss") + ".log";
                var outPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
                if (System.IO.File.Exists(outPath)) outPath = outPath.Replace(".log", "-" + DateTime.Now.Millisecond + ".log");
                System.IO.File.WriteAllText(outPath, sb.ToString(), System.Text.Encoding.UTF8);
                SetStatus(L.T("msg.log.written") + outPath, false);
            }
            catch (Exception ex) { WpfProgram.Log("logdiag: " + ex); SetStatus(L.T("msg.failed.to.write.the.log") + ex.Message, true); }
        }

        /// <summary>把注册表里当前（刚还原的）值实时打给系统：不调 SPI 的话要重新登录才看得到变化。</summary>
        void ApplyShellFromRegistry()
        {
            var info = Win32Fonts.ReadAll();
            var faces = new string[info.Count];
            var pts = new double[info.Count];
            var on = new bool[info.Count];
            for (int i = 0; i < info.Count; i++)
            {
                var it = info[i];
                faces[i] = it == null ? "" : it.Face;
                pts[i] = it == null ? 9 : Math.Round(Math.Abs(it.HeightPx) * 72.0 / 96.0);
                on[i] = it != null && !string.IsNullOrEmpty(it.Face);
            }
            if (ShellMetrics.Apply(faces, pts, on, info.ToArray()) > 0) RefreshShell();
        }

        void RestoreWin32()
        {
            try
            {
                Win32Fonts.Restore();
                try { ApplyShellFromRegistry(); } catch { }
                SetStatus(L.T("msg.shell.elements.restored"), false);
                MaybeLog("restore-shell");
            }
            catch (Exception ex) { SetStatus(L.T("msg.restore.failed") + ex.Message, true); }
        }

        void PickWin32Face(int i)
        {
            var dlg = new FontPickerDialog(true);   // 界面元素：GDI 枚举，可选可变字体的命名实例
            dlg.Owner = this;
            if (dlg.ShowDialog() != true) return;
            _winFace[i] = dlg.Picked.Family;
            double pt;
            double.TryParse(_winSize[i].Text, out pt);
            if (pt <= 0) pt = 9;
            _winCur[i].Text = dlg.Picked.Family + "   " + pt.ToString("0") + " pt";
        }

        /// <summary>把这次要改的界面元素列成一行行文字（确认框里给用户看）。</summary>
        string ShellChangeSummary()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                if (!_winOn[i]) continue;
                var cur = _winInfo[i];
                bool faceChanged = cur == null || !string.Equals(_winFace[i], cur.Face, StringComparison.OrdinalIgnoreCase);
                double pt = 9;
                if (_winSize[i] != null) double.TryParse(_winSize[i].Text, out pt);
                if (pt <= 0) pt = 9;
                bool sizeChanged = cur == null || Math.Abs(pt - Math.Round(Math.Abs(cur.HeightPx) * 72.0 / 96.0)) > 0.4;
                if (string.IsNullOrEmpty(_winFace[i]) || (!faceChanged && !sizeChanged)) continue;
                sb.AppendLine(Win32Fonts.Names[i] + " → " + _winFace[i] + "  " + pt.ToString("0") + " pt");
            }
            return sb.ToString().TrimEnd();
        }

        /// <summary>有没有需要写入的系统元素（开关开 且 有字体名）。</summary>
        bool AnyShellSelected()
        {
            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                // 只在“用户确实动过”时才算：行开关开着，且与系统当前值不同
                if (!_winOn[i]) continue;
                var cur = _winInfo[i];
                bool faceChanged = cur == null || !string.Equals(_winFace[i], cur.Face, StringComparison.OrdinalIgnoreCase);
                bool sizeChanged = true;
                if (_winSize[i] != null && cur != null)
                {
                    double pt;
                    double.TryParse(_winSize[i].Text, out pt);
                    if (pt <= 0) pt = 9;
                    sizeChanged = Math.Abs(pt - Math.Round(Math.Abs(cur.HeightPx) * 72.0 / 96.0)) > 0.4;
                }
                if (!string.IsNullOrEmpty(_winFace[i]) && (faceChanged || sizeChanged)) return true;
            }
            return false;
        }

        List<FontSource> CollectSources()
        {
            var list = new List<FontSource>();
            for (int i = 0; i < Installer.TargetFiles.Length && i < 5; i++)
            {
                if (_slots[i] == null || !_on[i]) continue;
                var e = _slots[i];
                var data = File.ReadAllBytes(e.Path);
                // 英数（SegUIVar）优先用可变字体版本：WinUI 会按 400/600/700 请求，静态文件只能被合成
                if (i == 3 && !Sfnt.IsVariable(data, e.FaceIndex))
                {
                    var vf = FontLibrary.FindVariableSibling(e);
                    if (vf != null)
                    {
                        data = File.ReadAllBytes(vf.Path);
                        e = vf;
                    }
                }
                list.Add(new FontSource
                {
                    TargetFile = Installer.TargetFiles[i],
                    SourcePath = e.Path,
                    FaceIndex = e.FaceIndex,
                    Family = e.Family,
                    Subfamily = e.Subfamily,
                    Data = data
                });
            }
            return list;
        }

        void RunApply(bool generateOnly)
        {
            var list = CollectSources();
            var trace = new System.Text.StringBuilder();
            foreach (var s in list) trace.Append(s.TargetFile + "<-" + System.IO.Path.GetFileName(s.SourcePath) + "; ");
            Diagnostics.Append("apply: fonts=" + list.Count + " [" + trace + "] generateOnly=" + generateOnly);
            bool hasFonts = list.Count > 0;
            bool hasShell = AnyShellSelected();

            // 只改了系统元素：直接写注册表，不要求先选字体，也不问重启
            if (!hasFonts)
            {
                if (generateOnly) { var m0 = L.T("msg.pick.a.font.first.to.generate.the.font.files"); SetStatus(m0, true); ConfirmDialog.Info(this, L.T("adv.generate.only"), m0); return; }
                if (!hasShell) { SetStatus(L.T("msg.nothing.to.apply"), true); ConfirmDialog.Info(this, L.T("font.apply"), L.T("msg.nothing.to.apply")); return; }
                // 已打开的窗口不会自己换字体，重启资源管理器让外壳重新读一遍（电脑不用重启）
                string summary = ShellChangeSummary();
                if (string.IsNullOrEmpty(summary)) { SetStatus(L.T("msg.nothing.to.apply"), true); ConfirmDialog.Info(this, L.T("font.apply"), L.T("msg.nothing.to.apply")); return; }
                if (!Confirm(L.T("msg.restart.explorer.now") + "\n\n" + summary, L.T("font.apply"))) return;
                Diagnostics.Append("apply-shell: " + ShellChangeSummary().Replace("\r\n", " | "));
                ApplyWin32();          // 写注册表 + SystemParametersInfo 立即生效
                MaybeLog("apply-shell");
                bool restarted = RestartExplorer();   // 让已打开的窗口/菜单重新读字体
                string done = restarted ? L.T("msg.shell.elements.applied.2") + L.T("msg.explorer.restarted")
                                        : L.T("msg.written.but.restarting.explorer.failed");
                SetStatus(done, !restarted);
                ConfirmDialog.Info(this, L.T("font.apply"), done);
                return;
            }

            // 有字体要写：必须重启才生效 —— 直接问「是否确定重启」
            if (!generateOnly && !Confirm(L.T("msg.restart.now"), L.T("font.apply"))) return;

            // 护栏：动手前先建系统还原点（建不了不阻断，只在状态栏说明原因）
            if (!generateOnly)
            {
                SetStatus(L.T("msg.creating.a.system.restore.point"), false);
                var rpErr = Installer.CreateRestorePoint();
                if (rpErr != null) SetStatus(L.T("msg.restore.point.not.created") + rpErr, false);
                MaybeLog(rpErr == null ? "restore-point-ok" : "restore-point-failed: " + rpErr);
            }
            SetStatus(generateOnly ? L.T("msg.generating") : L.T("msg.generating.and.writing"), false);
            try
            {
                bool cache = _advCache != null && _advCache.IsChecked == true;
                bool userFonts = _advUserFonts != null && _advUserFonts.IsChecked == true;
                var res = Installer.Apply(list, delegate (string m) { }, generateOnly, cache, userFonts);
                // 和字体一起改的界面元素也要写：以前这条分支只排字体文件，界面元素被默默丢掉（默认预设时最明显）
                if (!generateOnly && AnyShellSelected())
                {
                    Diagnostics.Append("apply-shell (with fonts): " + ShellChangeSummary().Replace("\r\n", " | "));
                    ApplyWin32();
                    MaybeLog("apply-shell");
                }
                SetStatus(generateOnly ? (L.T("msg.generated.into") + res.OutputDir) : L.T("msg.written.rebooting.now"), false);
                MaybeLog(generateOnly ? "generate" : "apply-fonts");
                Diagnostics.Append("apply done: " + res.Message + " queued=" + res.Queued + " out=" + res.OutputDir);
                if (generateOnly) ConfirmDialog.Info(this, L.T("adv.generate.only"), L.T("msg.generated.into") + res.OutputDir);
                RefreshTargets();
                if (!generateOnly) RebootSoon();
            }
            catch (Exception ex)
            {
                Diagnostics.Append("apply ERROR: " + ex);
                SetStatus(L.T("msg.failed") + ex.Message, true);
                ConfirmDialog.Info(this, L.T("msg.failed"), ex.Message);
            }
        }

        /// <summary>
        /// 「默认值」= 把源字体换成微软原版（从最近一次备份的 orig\ 里取），
        /// 左侧五格显示 Microsoft YaHei / Segoe UI，接着点「应用」就写回原版。不写系统。
        /// </summary>
        /// <summary>从最近备份的 orig\ 目录取出原版字体，填进五个槽位（顺序与 TargetFiles 对应）。</summary>
        int LoadDefaultSources()
        {
            int filled = 0;
            try
            {
                var rootDir = Installer.BackupRoot;
                if (!Directory.Exists(rootDir)) return 0;
                var dirs = new List<string>(Directory.GetDirectories(rootDir));
                dirs.Sort(StringComparer.Ordinal);
                dirs.Reverse();   // 从最新往回找

                // 每个文件分别找：先看 Windows 组件存储（权威原版），再退回我们自己的备份
                for (int i = 0; i < Installer.TargetFiles.Length && i < _slots.Length; i++)
                {
                    var sys = Installer.FindOriginalInWinSxS(Installer.TargetFiles[i]);
                    if (sys != null)
                    {
                        var se = FontLibrary.ReadFile(sys);
                        if (se.Count > 0) { _slots[i] = se[0]; filled++; continue; }
                    }
                    foreach (var d in dirs)
                    {
                        var f = Path.Combine(Path.Combine(d, "orig"), Installer.TargetFiles[i]);
                        if (!File.Exists(f)) continue;
                        var entries = FontLibrary.ReadFile(f);
                        if (entries.Count == 0) continue;
                        _slots[i] = entries[0];
                        filled++;
                        break;
                    }
                }
            }
            catch (Exception ex) { SetStatus(L.T("msg.failed.to.read.the.original.fonts") + ex.Message, true); }
            return filled;
        }

        /// <summary>切换界面语言：保存选择、重建导航与页面。</summary>
        void ApplyLanguage(bool en)
        {
            if (L.En == en) return;
            L.En = en;
            L.Save();
            L.LoadFile();
            Title = L.T("msg.yiti") + " V" + WpfProgram.Version;
            if (_brandName != null) _brandName.Text = L.T("msg.yiti");
            if (_nav != null)
            {
                // 重建导航项会把 SelectedIndex 清成 -1；先记下来，重建后恢复，否则会莫名跳到「关于」页
                int keep = _nav.SelectedIndex < 0 ? 0 : _nav.SelectedIndex;
                _switchingLang = true;
                try
                {
                    _nav.Items.Clear();
                    _nav.Items.Add(NavItem("\uE8D2", L.T("msg.fonts")));
                    _nav.Items.Add(NavItem("\uE713", L.T("msg.advanced")));
                    _nav.Items.Add(NavItem("\uE8A5", L.T("msg.about")));
                    _nav.SelectedIndex = keep;
                }
                finally { _switchingLang = false; }
            }
            ShowPage();
            SetStatus(L.En ? "Language switched to English" : "已切换为中文", false);
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, string lParam, uint flags, uint timeout, out IntPtr result);

        /// <summary>系统元素只写注册表即可生效：广播 WM_SETTINGCHANGE 并重启资源管理器（不重启电脑）。</summary>
        void RefreshShell()
        {
            try
            {
                IntPtr res;
                SendMessageTimeout(new IntPtr(0xffff), 0x001A, IntPtr.Zero, "WindowMetrics", 0x0002, 1000, out res);
                SendMessageTimeout(new IntPtr(0xffff), 0x001A, IntPtr.Zero, "ImmersiveColorSet", 0x0002, 1000, out res);
            }
            catch { }
            try
            {
                var kill = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("taskkill.exe", "/f /im explorer.exe")
                { CreateNoWindow = true, UseShellExecute = false });
                if (kill != null) kill.WaitForExit(5000);
                System.Threading.Thread.Sleep(400);
                System.Diagnostics.Process.Start("explorer.exe");
                SetStatus(L.T("msg.shell.elements.applied.explorer.restarted"), false);
            }
            catch (Exception ex) { SetStatus(L.T("msg.written.but.restarting.explorer.failed") + ex.Message, true); }
        }

        /// <summary>重启资源管理器（外壳 UI 会重新读系统字体；电脑本身不用重启）。</summary>
        /// <summary>重启资源管理器：让已打开的窗口/菜单重新读一遍界面字体。返回是否成功。</summary>
        bool RestartExplorer()
        {
            try
            {
                var kill = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("taskkill.exe", "/f /im explorer.exe")
                { CreateNoWindow = true, UseShellExecute = false });
                if (kill != null) kill.WaitForExit(5000);
                System.Threading.Thread.Sleep(500);
                System.Diagnostics.Process.Start("explorer.exe");
                return true;
            }
            catch (Exception ex)
            {
                SetStatus(L.T("msg.written.but.restarting.explorer.failed") + ex.Message, true);
                return false;
            }
        }

        /// <summary>高级页那个按钮：手动重启资源管理器（和改完系统元素后应用时做的事一样）。</summary>
        void RestartExplorerFromButton()
        {
            if (!Confirm(L.T("msg.restart.explorer.now"), L.T("adv.restart.explorer"))) return;
            bool ok = RestartExplorer();
            string msg = ok ? L.T("msg.explorer.restarted") : L.T("msg.written.but.restarting.explorer.failed");
            SetStatus(msg, !ok);
            MaybeLog("restart-explorer");
            ConfirmDialog.Info(this, L.T("adv.restart.explorer"), msg);
        }

        /// <summary>确认后立即重启（不给取消窗口）。</summary>
        void RebootSoon()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("shutdown.exe", "/r /t 0 /c \"YiTi: applying fonts\"")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                System.Diagnostics.Process.Start(psi);
            }
            catch (Exception ex) { SetStatus(L.T("msg.reboot.failed") + ex.Message, true); }
        }

        bool Confirm(string question, string title)
        {
            foreach (var a in Environment.GetCommandLineArgs()) if (a == "--yes") return true;   // 自测用：跳过确认
            var dlg = new ConfirmDialog(title, question, L.T("dlg.yes"), L.T("dlg.no")) { Owner = this };
            dlg.ShowDialog();
            return dlg.Result;
        }

        void ResetToDefaults()
        {
            if (!Confirm(L.T("font.switch.to.the.default.preset"), L.T("font.defaults"))) return;
            for (int i = 0; i < _slots.Length; i++) _slots[i] = null;
            int filled = LoadDefaultSources();
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
            SyncWin32FromChoice();
            // 系统元素的“默认值”＝ Windows 自己的默认：微软雅黑 UI 9pt（不是我们备份里的历史值）
            for (int i = 0; i < Win32Fonts.Names.Length; i++)
            {
                const string defFace = "Microsoft YaHei UI";
                const int defPt = 9;
                _winFace[i] = defFace;
                if (_winSize[i] != null) _winSize[i].Text = defPt.ToString();
                if (_winCur[i] != null) _winCur[i].Text = defFace + "   " + defPt + " pt";
            }
            string msg = filled > 0 ? L.T("msg.switched.to.the.default.fonts.press.apply.to") : L.T("msg.no.usable.backup.pick.a.font.instead");
            if (filled > 0 && !string.IsNullOrEmpty(L.T("msg.defaults.pending"))) msg += L.T("msg.defaults.pending");   // 只改了界面，必须点「应用」才会写进系统
            SetStatus(msg, filled == 0);
            ConfirmDialog.Info(this, L.T("font.defaults"), msg);
        }



        void RunRollback()
        {
            try
            {
                if (Installer.LatestBackup() == null) { SetStatus(L.T("msg.there.is.no.backup.to.roll.back"), true); return; }
                Installer.Rollback(delegate (string m) { });
                SetStatus(L.T("msg.rolled.back.back.to.normal.after.a.reboot"), false);
                RefreshTargets();
            }
            catch (Exception ex) { SetStatus(L.T("msg.rollback.failed") + ex.Message, true); }
        }
    }
}
