using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinFontSwap
{
    /// <summary>一套配色（沿用 DevLink 的浅色/深色 token）。</summary>
    internal sealed class Palette
    {
        public string Name;
        public Color Bg, Surface, SurfaceAlt, Line, Text, TextDim, Accent, AccentText, Ok, Warn, Err, Hover;
    }

    internal static class Theme
    {
        public static readonly Palette Light = new Palette
        {
            Name = "浅色",
            Bg = Color.FromArgb(0xF3, 0xF3, 0xF3),
            Surface = Color.FromArgb(0xFF, 0xFF, 0xFF),
            SurfaceAlt = Color.FromArgb(0xFA, 0xFA, 0xFA),
            Line = Color.FromArgb(0xE1, 0xE1, 0xE1),
            Text = Color.FromArgb(0x1A, 0x1A, 0x1A),
            TextDim = Color.FromArgb(0x5C, 0x5C, 0x5C),
            Accent = Color.FromArgb(0x00, 0x5F, 0xB8),
            AccentText = Color.White,
            Ok = Color.FromArgb(0x0F, 0x7B, 0x0F),
            Warn = Color.FromArgb(0x9D, 0x5D, 0x00),
            Err = Color.FromArgb(0xC4, 0x2B, 0x1C),
            Hover = Color.FromArgb(0xEC, 0xEC, 0xEC),
        };

        public static readonly Palette Dark = new Palette
        {
            Name = "深色",
            Bg = Color.FromArgb(0x20, 0x20, 0x20),
            Surface = Color.FromArgb(0x2B, 0x2B, 0x2B),
            SurfaceAlt = Color.FromArgb(0x32, 0x32, 0x32),
            Line = Color.FromArgb(0x3D, 0x3D, 0x3D),
            Text = Color.FromArgb(0xF5, 0xF5, 0xF5),
            TextDim = Color.FromArgb(0xB0, 0xB0, 0xB0),
            Accent = Color.FromArgb(0x4C, 0xC2, 0xFF),
            AccentText = Color.FromArgb(0x00, 0x2B, 0x3D),
            Ok = Color.FromArgb(0x6C, 0xCB, 0x5A),
            Warn = Color.FromArgb(0xE8, 0xC1, 0x4A),
            Err = Color.FromArgb(0xFF, 0x6B, 0x6B),
            Hover = Color.FromArgb(0x38, 0x38, 0x38),
        };

        public static Palette Current = Light;
        public static string Pref = "system";

        /// <summary>窗口背景走 Mica/亚克力：背景用纯黑绘制，DWM 会把扩展框区域当玻璃处理。</summary>
        public static bool Glass = true;

        /// <summary>“透明”的背景色：玻璃模式下必须是纯黑，否则 DWM 当成不透明。</summary>
        /// <summary>色键：这块颜色会被抠成完全透明。用近黑而不是纯品红，抗锯齿边缘更自然。</summary>
        public static readonly Color KeyColor = Color.FromArgb(1, 1, 1);

        public static Color BackPaint
        {
            get { return Glass ? KeyColor : Current.Bg; }
        }

        public static bool SystemPrefersDark()
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var v = k == null ? null : k.GetValue("AppsUseLightTheme");
                    if (v is int) return (int)v == 0;
                }
            }
            catch { }
            return false;
        }

        public static void Apply(string pref, Control root)
        {
            Pref = pref;
            Current = pref == "dark" ? Dark : (pref == "light" ? Light : (SystemPrefersDark() ? Dark : Light));
            if (root != null)
            {
                if (!Glass) root.BackColor = Current.Bg;
                Restyle(root);
                root.Invalidate(true);
            }
        }

        static void Restyle(Control c)
        {
            var p = Current;
            if (c is CardPanel) { ((CardPanel)c).Invalidate(); }
            else if (c is RoundedButton) { c.Invalidate(); }
            else if (c is ToggleSwitch) { c.Invalidate(); }
            else if (c is NavButton) { c.Invalidate(); }
            else if (c is CaptionButton) { c.Invalidate(); }
            else if (c is PreviewBox) { c.Invalidate(); }
            else if (c is Label)
            {
                var lbl = (Label)c;
                lbl.ForeColor = (lbl.Tag as string) == "dim" ? p.TextDim : p.Text;
                lbl.BackColor = Color.Transparent;   // 交给父级画，避免 Label 自己铺一块底色
            }
            else if (c is TextBoxBase)
            {
                c.BackColor = p.SurfaceAlt;
                c.ForeColor = p.TextDim;
            }
            else if (c is Panel)
            {
                var tag = c.Tag as string;
                if (tag == "glass") c.BackColor = Color.Transparent;
                else c.BackColor = tag == "surface" ? p.Surface : (tag == "alt" ? p.SurfaceAlt : p.Bg);
            }

            foreach (Control child in c.Controls) Restyle(child);
        }
    }
}
