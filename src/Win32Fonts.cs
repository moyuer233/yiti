using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace WinFontSwap
{
    /// <summary>Win32 界面元素字体（HKCU\Control Panel\Desktop\WindowMetrics，值为 LOGFONTW 二进制）。</summary>
    internal static class Win32Fonts
    {
        public const string RegPath = @"Control Panel\Desktop\WindowMetrics";

        // 「标题栏」(CaptionFont) 不做：它被现代应用忽略，改它只会让旧程序不一致。
        public static readonly string[] Keys =
        {
            "SmCaptionFont", "MenuFont", "MessageFont", "StatusFont", "IconFont"
        };

        /// <summary>界面里没有单独一行、但写入时会跟着改的项：标题栏字体（Win11 22H2+ 生效）。</summary>
        public static readonly string[] ExtraKeys = { "CaptionFont" };

        public static readonly string[] Names =
        {
            "小标题栏", "菜单", "消息框", "状态栏", "图标"
        };

        public sealed class Info
        {
            public string Face = "";
            public int HeightPx;
            public int Weight = 400;
            public int CharSet = 1;
            public bool Italic;
            public byte[] Raw;
        }

        public static string BackupPath
        {
            get { return Path.Combine(Installer.DataDir, "win32fonts.bak"); }
        }

        public static Info Read(string valueName)
        {
            try
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RegPath, false))
                {
                    if (k == null) return null;
                    var raw = k.GetValue(valueName) as byte[];
                    if (raw == null || raw.Length < 92) return null;
                    var info = new Info();
                    info.Raw = raw;
                    info.HeightPx = BitConverter.ToInt32(raw, 0);
                    info.Weight = BitConverter.ToInt32(raw, 16);
                    info.Italic = raw[20] != 0;
                    info.CharSet = raw[23];
                    info.Face = Encoding.Unicode.GetString(raw, 28, 64).TrimEnd('\0');
                    return info;
                }
            }
            catch { return null; }
        }

        public static List<Info> ReadAll()
        {
            var list = new List<Info>();
            foreach (var key in Keys) list.Add(Read(key));
            return list;
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern uint GetDpiForSystem();

        /// <summary>当前系统 DPI（Win10+ 有 GetDpiForSystem，取不到退回 96）。</summary>
        static int SystemDpi()
        {
            try
            {
                uint dpi = GetDpiForSystem();
                if (dpi >= 72 && dpi <= 480) return (int)dpi;
            }
            catch { }
            return 96;
        }

        /// <summary>按点数写入某个元素：lfHeight 用负的像素高度（= 字号 × DPI/72，和 noMeiryoUI 的 MulDiv 一致）。</summary>
        public static void Write(string valueName, string face, double points, int weight, int charSet, bool italic)
        {
            var raw = new byte[92];
            int px = (int)Math.Round(points * SystemDpi() / 72.0);
            Array.Copy(BitConverter.GetBytes(-px), 0, raw, 0, 4);
            Array.Copy(BitConverter.GetBytes(weight), 0, raw, 16, 4);
            raw[20] = (byte)(italic ? 1 : 0);
            raw[21] = 0; raw[22] = 0;
            raw[23] = (byte)(charSet == 0 ? 1 : charSet);
            raw[24] = 0; raw[25] = 0;
            raw[26] = 5;   // CLEARTYPE_QUALITY：不写这条，经典界面会退化成灰度抗锯齿，看着更糊
            raw[27] = 0;
            var name = Encoding.Unicode.GetBytes(face);
            int n = Math.Min(name.Length, 62);
            Array.Copy(name, 0, raw, 28, n);

            using (var k = Registry.CurrentUser.CreateSubKey(RegPath))
                k.SetValue(valueName, raw, RegistryValueKind.Binary);
        }

        /// <summary>把原始值备份成 名字=base64 的文本（只备份一次，避免覆盖最初的系统值）。</summary>
        public static void BackupOnce()
        {
            try
            {
                if (File.Exists(BackupPath)) return;
                Directory.CreateDirectory(Installer.DataDir);
                var sb = new StringBuilder();
                using (var k = Registry.CurrentUser.OpenSubKey(RegPath, false))
                {
                    if (k == null) return;
                    var all = new List<string>(Keys);
                    all.AddRange(ExtraKeys);
                    foreach (var key in all)
                    {
                        var raw = k.GetValue(key) as byte[];
                        if (raw == null) continue;
                        sb.AppendLine(key + "=" + Convert.ToBase64String(raw));
                    }
                }
                File.WriteAllText(BackupPath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public static bool HasBackup { get { try { return File.Exists(BackupPath); } catch { return false; } } }


        /// <summary>还原到第一次备份时的系统默认值。</summary>
        public static int Restore()
        {
            if (!File.Exists(BackupPath)) return 0;
            int n = 0;
            var restored = new List<string>();
            using (var k = Registry.CurrentUser.CreateSubKey(RegPath))
            {
                foreach (var line in File.ReadAllLines(BackupPath, Encoding.UTF8))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string name = line.Substring(0, eq);
                    try
                    {
                        k.SetValue(name, Convert.FromBase64String(line.Substring(eq + 1)), RegistryValueKind.Binary);
                        restored.Add(name);
                        n++;
                    }
                    catch { }
                }
                // 旧备份文件里没有的追加项（例如标题栏字体）：删掉，让系统回到自己的默认，而不是留着我们改过的值
                foreach (var extra in ExtraKeys)
                {
                    if (restored.Contains(extra)) continue;
                    try { k.DeleteValue(extra, false); n++; } catch { }
                }
            }
            return n;
        }
    }
}
