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

        /// <summary>
        /// 和「默认预设」那种小弹窗同款：无系统标题栏、深色圆角卡片 + 外阴影、标题在卡片里、整窗可拖动、Esc 关闭。
        /// 用于预设管理 / 备份管理 / 字体选择这类独立窗口。
        /// </summary>
        public static UIElement PopupFrame(Window w, FrameworkElement body, string title)
        {
            try
            {
                w.WindowStyle = WindowStyle.None;
                w.AllowsTransparency = true;
                w.Background = Brushes.Transparent;
                w.ResizeMode = ResizeMode.NoResize;
            }
            catch { }

            var card = new Border
            {
                Background = T.BDrawer,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(20, 16, 20, 16),
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 26,
                    ShadowDepth = 0,
                    Opacity = 0.45,
                    Color = Colors.Black
                }
            };
            var content = new Grid();
            content.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            content.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            var head = new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold, Foreground = T.Text };
            Grid.SetRow(head, 0);
            content.Children.Add(head);
            var inner = new Grid { Margin = new Thickness(0, 12, 0, 0) };
            inner.Children.Add(body);
            Grid.SetRow(inner, 1);   // 占满剩余高度，列表才能正常拉伸
            content.Children.Add(inner);
            card.Child = content;

            var layer = new Grid { Margin = new Thickness(14) };
            layer.Children.Add(card);

            card.MouseLeftButtonDown += delegate (object s, System.Windows.Input.MouseButtonEventArgs e)
            {
                if (e.OriginalSource is TextBlock || e.OriginalSource is Border) { try { w.DragMove(); } catch { } }
            };
            w.KeyDown += delegate (object s, System.Windows.Input.KeyEventArgs e)
            {
                if (e.Key == System.Windows.Input.Key.Escape) { try { w.Close(); } catch { } }
            };
            w.Loaded += delegate { Styles.FadeIn(card, 8, 150); };
            return layer;
        }

        static readonly IEasingFunction Ease = new CubicEase { EasingMode = EasingMode.EaseOut };

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
                var fade = new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Ease };
                el.BeginAnimation(UIElement.OpacityProperty, fade);
                if (tt != null)
                {
                    var move = new DoubleAnimation(offsetY, 0, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Ease };
                    tt.BeginAnimation(TranslateTransform.YProperty, move);
                }
            }
            catch { el.Opacity = 1; }
        }

        /// <summary>淡出后执行回调（弹窗关闭用）。</summary>
        public static void FadeOutThen(UIElement el, int ms, Action done)
        {
            if (el == null) { if (done != null) done(); return; }
            try
            {
                var fade = new DoubleAnimation(el.Opacity, 0, new Duration(TimeSpan.FromMilliseconds(ms))) { EasingFunction = Ease };
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
