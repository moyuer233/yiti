using System;
using System.Windows;
using System.Windows.Markup;

namespace WinFontSwap.Wpf
{
    /// <summary>Window.Resources 用的隐式深色样式（LF 风格：无边框输入、深色列表与滚动条）。</summary>
    internal static class Styles
    {
        public const string Xaml = @"
<ResourceDictionary xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'
                    xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
  <SolidColorBrush x:Key='InputBg' Color='#26262B'/>
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
          <Border Background='{TemplateBinding Background}' CornerRadius='4'>
            <ScrollViewer x:Name='PART_ContentHost' Margin='{TemplateBinding Padding}'/>
          </Border>
        </ControlTemplate>
      </Setter.Value>
    </Setter>
  </Style>
</ResourceDictionary>";
    }
}
