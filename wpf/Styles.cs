using System;
using System.Runtime.InteropServices;
using System.Windows;
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
    <Setter Property='Padding' Value='8,6'/>
    <Setter Property='CaretBrush' Value='#FFFFFF'/>
    <Setter Property='SelectionBrush' Value='#4CC2FF'/>
    <Setter Property='Template'>
      <Setter.Value>
        <ControlTemplate TargetType='TextBox'>
          <Border x:Name='ring' Background='{TemplateBinding Background}' CornerRadius='4' BorderThickness='1' BorderBrush='#22FFFFFF'>
            <ScrollViewer x:Name='PART_ContentHost' Margin='{TemplateBinding Padding}'/>
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
