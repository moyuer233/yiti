using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinFontSwap.Wpf
{
    /// <summary>
    /// 用 GDI 枚举“界面元素可用”的字体名。GDI 会把可变字体的命名实例（MiSans VF Bold / Demibold …）
    /// 当成独立名字列出来 —— 写进 LOGFONT 就能直接用，不需要生成字体文件。
    /// </summary>
    internal static class ShellFontEnum
    {
        public sealed class Item
        {
            public string Name;
            public int Weight;
            public bool Italic;
            public string BaseFamily;   // 去掉尾部字重词后的家族名，用于预览
            public byte CharSet;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct LOGFONT
        {
            public int lfHeight, lfWidth, lfEscapement, lfOrientation, lfWeight;
            public byte lfItalic, lfUnderline, lfStrikeOut, lfCharSet, lfOutPrecision, lfClipPrecision, lfQuality, lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string lfFaceName;
        }

        delegate int EnumProc(IntPtr lpelfe, IntPtr lpntme, int fontType, IntPtr lParam);

        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        static extern int EnumFontFamiliesExW(IntPtr hdc, ref LOGFONT lf, EnumProc cb, IntPtr lParam, int flags);

        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);

        static readonly string[] StyleWords =
        {
            "extra light", "extralight", "ultra light", "ultralight", "semi light", "semilight",
            "demi light", "demilight", "extra bold", "extrabold", "semi bold", "semibold",
            "ultra bold", "ultrabold", "demibold", "demi bold", "thin", "light", "regular",
            "normal", "book", "medium", "bold", "black", "heavy", "italic", "oblique"
        };

        static string BaseOf(string name)
        {
            var s = (name ?? "").Trim();
            var low = s.ToLowerInvariant();
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var w in StyleWords)
                {
                    if (low.EndsWith(" " + w))
                    {
                        s = s.Substring(0, s.Length - w.Length - 1).Trim();
                        low = s.ToLowerInvariant();
                        changed = true;
                        break;
                    }
                }
            }
            return s;
        }

        static List<Item> _cache;

        public static List<Item> All
        {
            get
            {
                if (_cache != null) return _cache;
                var list = new List<Item>();
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var hdc = GetDC(IntPtr.Zero);
                try
                {
                    EnumProc cb = delegate (IntPtr lpelfe, IntPtr lpntme, int fontType, IntPtr lParam)
                    {
                        var lf = (LOGFONT)Marshal.PtrToStructure(lpelfe, typeof(LOGFONT));
                        if (lf.lfFaceName == null || lf.lfFaceName.Length == 0) return 1;
                        if (lf.lfFaceName.StartsWith("@", StringComparison.Ordinal)) return 1;   // 竖排变体
                        var key = lf.lfFaceName + "|" + lf.lfWeight + "|" + lf.lfItalic;
                        if (seen.Contains(key)) return 1;
                        seen.Add(key);
                        list.Add(new Item
                        {
                            Name = lf.lfFaceName,
                            Weight = lf.lfWeight,
                            Italic = lf.lfItalic != 0,
                            CharSet = lf.lfCharSet,
                            BaseFamily = BaseOf(lf.lfFaceName)
                        });
                        return 1;
                    };
                    // 覆盖简体中文 / 西欧 / 默认三套字符集，避免漏掉只挂某个 charset 的字体
                    foreach (byte cs in new byte[] { 1, 2, 0 })
                    {
                        var lf = new LOGFONT { lfCharSet = cs, lfFaceName = "" };
                        EnumFontFamiliesExW(hdc, ref lf, cb, IntPtr.Zero, 0);
                    }
                }
                catch { }
                finally { ReleaseDC(IntPtr.Zero, hdc); }
                list.Sort(delegate (Item a, Item b)
                {
                    int c = string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase);
                    return c != 0 ? c : a.Weight.CompareTo(b.Weight);
                });
                _cache = list;
                return list;
            }
        }
    }
}
