using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;

namespace WinFontSwap
{
    /// <summary>中文回退链（FontLink\SystemLink，MULTI_SZ："文件,字体名"）。</summary>
    internal static class FontLink
    {
        const string RegPath = @"Software\Microsoft\Windows NT\CurrentVersion\FontLink\SystemLink";

        public static List<string> Keys()
        {
            var set = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RegPath, false))
                    if (k != null) foreach (var n in k.GetValueNames()) set.Add(n);
            }
            catch { }
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(RegPath, false))
                    if (k != null) foreach (var n in k.GetValueNames()) set.Add(n);
            }
            catch { }
            var list = new List<string>(set);
            return list;
        }

        public static string[] Read(string key)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RegPath, false))
                {
                    if (k != null)
                    {
                        var v = k.GetValue(key) as string[];
                        if (v != null) return v;
                    }
                }
                using (var k = Registry.LocalMachine.OpenSubKey(RegPath, false))
                {
                    if (k != null)
                    {
                        var v = k.GetValue(key) as string[];
                        if (v != null) return v;
                    }
                }
            }
            catch { }
            return new string[0];
        }

        /// <summary>把一条「文件,字体名」插到回退链最前面（已存在则不重复）。</summary>
        public static void Prepend(string key, string entry)
        {
            var cur = Read(key);
            var list = new List<string>();
            list.Add(entry);
            foreach (var v in cur)
            {
                if (string.IsNullOrEmpty(v)) continue;
                if (string.Equals(v.Trim(), entry, StringComparison.OrdinalIgnoreCase)) continue;
                list.Add(v);
            }
            Write(key, list.ToArray());
        }

        public static void Write(string key, string[] values)
        {
            using (var k = Registry.CurrentUser.CreateSubKey(RegPath))
            {
                if (values == null || values.Length == 0) k.DeleteValue(key, false);
                else k.SetValue(key, values, RegistryValueKind.MultiString);
            }
        }

        public static bool WriteAll(string key, string text)
        {
            var lines = text.Replace("\r\n", "\n").Split('\n');
            var list = new List<string>();
            foreach (var l in lines)
            {
                var s = l.Trim();
                if (s.Length > 0) list.Add(s);
            }
            Write(key, list.ToArray());
            return true;
        }
    }
}
