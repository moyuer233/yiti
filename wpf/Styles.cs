using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace WinFontSwap.Wpf
{
    /// <summary>Window.Resources 用的隐式深色样式（LF 风格：无边框输入、深色列表与滚动条）。</summary>
    internal static class Styles
    {
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        /// <summary>给带系统标题栏的窗口（字体选择 / 备份管理）套深色标题栏 + 圆角。</summary>
        public static void DarkTitle(Window w)
        {
            try
            {
                var h = new WindowInteropHelper(w).Handle;
                if (h == IntPtr.Zero) return;
                int dark = 1;
                DwmSetWindowAttribute(h, 20, ref dark, sizeof(int));   // DWMWA_USE_IMMERSIVE_DARK_MODE
                int pref = 2;
                DwmSetWindowAttribute(h, 33, ref pref, sizeof(int));   // DWMWA_WINDOW_CORNER_PREFERENCE = ROUND
            }
            catch { }
        }

        /// <summary>和主页面同一套自绘窗口按钮（– 最小化 / × 关闭）：透明底、悬停变亮、命中区 40×28。</summary>
        public static System.Windows.Controls.Button WindowButton(string glyph, Action act)
        {
            var b = new System.Windows.Controls.Button
            {
                Content = glyph,
                Width = 40,
                Height = 28,
                FontSize = 13,
                Foreground = T.TextDim,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(b, true);
            var f = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            f.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(4));
            f.SetValue(System.Windows.Controls.Border.BackgroundProperty, new System.Windows.TemplateBindingExtension(System.Windows.Controls.Control.BackgroundProperty));
            var cp = new System.Windows.FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
            cp.SetValue(System.Windows.Controls.ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            cp.SetValue(System.Windows.Controls.ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            f.AppendChild(cp);
            var tpl = new System.Windows.Controls.ControlTemplate(typeof(System.Windows.Controls.Button));
            tpl.VisualTree = f;
            b.Template = tpl;
            b.MouseEnter += delegate { b.Background = T.BtnHover; b.Foreground = T.Text; };
            b.MouseLeave += delegate { b.Background = Brushes.Transparent; b.Foreground = T.TextDim; };
            b.Click += delegate { act(); };
            return b;
        }

        /// <summary>把对话框做成和主页面一样的外观：无系统标题栏，左上角标题、右上角自绘关闭按钮，窗口自己画深色背景。</summary>
        public static UIElement DialogFrame(Window w, FrameworkElement body, string title)
        {
            try
            {
                w.WindowStyle = WindowStyle.None;
                w.AllowsTransparency = true;   // 真透明：卡片圆角外才是透明，不会露出黑角
                w.Background = Brushes.Transparent;
                System.Windows.Shell.WindowChrome.SetWindowChrome(w, new System.Windows.Shell.WindowChrome
                {
                    CaptionHeight = 40,
                    ResizeBorderThickness = new Thickness(0),
                    GlassFrameThickness = new Thickness(0),
                    CornerRadius = new CornerRadius(0),
                    UseAeroCaptionButtons = false    // 系统不画按钮，和主页面一样自己画
                });
            }
            catch { }

            var card = new System.Windows.Controls.Border { Background = T.BDrawer, CornerRadius = new CornerRadius(10), Child = body };
            var layer = new Grid();
            layer.Children.Add(card);

            var cap = new Grid { Height = 40, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(18, 4, 6, 0) };
            cap.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            cap.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
            var label = T.Label(title, 13, T.Text, false);
            label.VerticalAlignment = VerticalAlignment.Center;
            cap.Children.Add(label);
            var close = WindowButton("×", delegate { try { w.Close(); } catch { } });
            close.VerticalAlignment = VerticalAlignment.Center;
            System.Windows.Controls.Grid.SetColumn(close, 1);
            cap.Children.Add(close);
            layer.Children.Add(cap);
            w.Loaded += delegate { PopupIn(card); };   // 管理窗口也走 Win11 弹出层的入场
            return layer;
        }

        static readonly IEasingFunction Ease = new CubicEase { EasingMode = EasingMode.EaseOut };

        /// <summary>cubic-bezier 缓动：WPF 没有现成的，这里按标准算法求值（x 解出 t 再取 y）。</summary>
        sealed class BezierEase : IEasingFunction
        {
            readonly double _x1, _y1, _x2, _y2;
            public BezierEase(double x1, double y1, double x2, double y2) { _x1 = x1; _y1 = y1; _x2 = x2; _y2 = y2; }
            static double A(double p1, double p2) { return 1.0 - 3.0 * p2 + 3.0 * p1; }
            static double B(double p1, double p2) { return 3.0 * p2 - 6.0 * p1; }
            static double C(double p1) { return 3.0 * p1; }
            static double Calc(double t, double p1, double p2)
            {
                return ((A(p1, p2) * t + B(p1, p2)) * t + C(p1)) * t;
            }
            public double Ease(double x)
            {
                if (x <= 0) return 0;
                if (x >= 1) return 1;
                double lo = 0, hi = 1;
                for (int i = 0; i < 24; i++)
                {
                    double mid = (lo + hi) / 2;
                    if (Calc(mid, _x1, _x2) < x) lo = mid; else hi = mid;
                }
                return Calc((lo + hi) / 2, _y1, _y2);
            }
        }

        /// <summary>Windows 11 的减速曲线（实测设置应用：250ms、约 30px 位移）。</summary>
        public static readonly IEasingFunction Standard = new BezierEase(0.1, 0.9, 0.2, 1);

        /// <summary>淡入：从 opacity 0 到 1，可带一点竖直位移（offsetY>0 时向上浮起）。</summary>
        public static void FadeIn(UIElement el, double offsetY, int ms)
        {
            if (el == null) return;
            try
            {
                el.Opacity = 0;
                TranslateTransform tt = null;
                if (Math.Abs(offsetY) > 0.01)
                {
                    tt = new TranslateTransform(0, offsetY);
                    el.RenderTransform = tt;
                }
                var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Standard };
                el.BeginAnimation(UIElement.OpacityProperty, fade);
                if (tt != null)
                {
                    var move = new DoubleAnimation(offsetY, 0, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Standard };
                    tt.BeginAnimation(TranslateTransform.YProperty, move);
                }
            }
            catch { el.Opacity = 1; }
        }

        /// <summary>弹窗入场：淡入 + 从 0.96 放大到 1（对齐 Win11 弹出层的 200ms）。</summary>
        public static void PopupIn(FrameworkElement el, int ms = 200)
        {
            if (el == null) return;
            try
            {
                el.Opacity = 0;
                var st = new ScaleTransform(0.96, 0.96);
                el.RenderTransform = st;
                el.RenderTransformOrigin = new Point(0.5, 0.5);
                var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Standard };
                el.BeginAnimation(UIElement.OpacityProperty, fade);
                var sx = new DoubleAnimation(0.96, 1, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Standard };
                var sy = new DoubleAnimation(0.96, 1, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Standard };
                st.BeginAnimation(ScaleTransform.ScaleXProperty, sx);
                st.BeginAnimation(ScaleTransform.ScaleYProperty, sy);
            }
            catch { el.Opacity = 1; }
        }

        /// <summary>淡出后执行回调（弹窗关闭用）。</summary>
        public static void FadeOutThen(UIElement el, int ms, Action done)
        {
            if (el == null) { if (done != null) done(); return; }
            try
            {
                var fade = new DoubleAnimation(el.Opacity, 0, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Standard };
                fade.Completed += delegate { try { if (done != null) done(); } catch { } };
                el.BeginAnimation(UIElement.OpacityProperty, fade);
            }
            catch { if (done != null) done(); }
        }

        public const string Xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <SolidColorBrush x:Key='InputBg' Color='#26262B'/>
  <!-- 深色提示气泡：默认是白底黑字，在暗色界面里很扎眼 -->
  <Style TargetType='ToolTip'>
    <Setter Property='Background' Value='#303038'/>
    <Setter Property='Foreground' Value='#F0F0F2'/>
    <Setter Property='BorderBrush' Value='#3E3E46'/>
    <Setter Property='BorderThickness' Value='1'/>
    <Setter Property='Padding' Value='8,5'/>
    <Setter Property='FontSize' Value='12'/>
    <Setter Property='HasDropShadow' Value='True'/>
    <!-- 提示一律贴在鼠标旁边 -->
    <Setter Property='Placement' Value='Mouse'/>
    <Setter Property='HorizontalOffset' Value='12'/>
    <Setter Property='VerticalOffset' Value='16'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ToolTip'>
          <Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}'
                  BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='6' Padding='{TemplateBinding Padding}'>
            <ContentPresenter/>
          </Border>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
  <SolidColorBrush x:Key='ItemHover' Color='#1FFFFFFF'/>
  <SolidColorBrush x:Key='ItemSel' Color='#33FFFFFF'/>

  <Style TargetType='ScrollBar'>
    <Setter Property='Width' Value='4'/>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ScrollBar'>
          <Grid Background='Transparent'>
            <Track x:Name='PART_Track' IsDirectionReversed='true'>
              <Track.Thumb>
                <Thumb>
                  <Thumb.Template>
                    <ControlTemplate TargetType='Thumb'>
                      <Border CornerRadius='4' Background='#40FFFFFF' Margin='1'/>
                    </ControlTemplate>
                  </Thumb.Template>
                </Thumb>
              </Track.Thumb>
            </Track>
          </Grid>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='ListBox'>
    <Setter Property='Background' Value='Transparent'/>
    <Setter Property='BorderThickness' Value='0'/>
    <Setter Property='Foreground' Value='#FFFFFF'/>
    <Setter Property='ScrollViewer.HorizontalScrollBarVisibility' Value='Disabled'/>
    <Setter Property='Padding' Value='0'/>
  </Style>

  <Style TargetType='ListBoxItem'>
    <Setter Property='Padding' Value='8,6'/>
    <Setter Property='Foreground' Value='#DDFFFFFF'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='ListBoxItem'>
          <Border x:Name='bd' CornerRadius='4' Background='Transparent' Padding='{TemplateBinding Padding}'>
            <ContentPresenter/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsMouseOver' Value='True'>
              <Setter TargetName='bd' Property='Background' Value='#1FFFFFFF'/>
            </Trigger>
            <Trigger Property='IsSelected' Value='True'>
              <Setter TargetName='bd' Property='Background' Value='#33FFFFFF'/>
              <Setter Property='Foreground' Value='#FFFFFF'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>

  <Style TargetType='TextBox'>
    <Setter Property='Background' Value='#26262B'/>
    <Setter Property='Foreground' Value='#FFFFFF'/>
    <Setter Property='BorderThickness' Value='0'/>
    <Setter Property='Padding' Value='8,3'/>
    <!-- 文字垂直居中：默认模板没有把 VerticalContentAlignment 传给 ScrollViewer，短输入框就会上下被裁 -->
    <Setter Property='VerticalContentAlignment' Value='Center'/>
    <Setter Property='CaretBrush' Value='#FFFFFF'/>
    <Setter Property='SelectionBrush' Value='#4CC2FF'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='TextBox'>
          <Border x:Name='ring' Background='{TemplateBinding Background}' CornerRadius='4' BorderThickness='1' BorderBrush='#22FFFFFF' Padding='{TemplateBinding Padding}'>
            <ScrollViewer x:Name='PART_ContentHost' Focusable='False'
                        VerticalAlignment='{TemplateBinding VerticalContentAlignment}'
                        HorizontalScrollBarVisibility='Hidden'
                        VerticalScrollBarVisibility='Hidden'/>
          </Border>
          <ControlTemplate.Triggers>
            <Trigger Property='IsKeyboardFocused' Value='True'>
              <Setter TargetName='ring' Property='BorderBrush' Value='#4CC2FF'/>
              <Setter TargetName='ring' Property='Background' Value='#2E2E34'/>
            </Trigger>
          </ControlTemplate.Triggers>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
</ResourceDictionary>";
    }
}
