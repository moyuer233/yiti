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

        /// <summary>按点数写入某个元素：lfHeight 用负的像素高度（= 字号 × 96/72）。</summary>
        public static void Write(string valueName, string face, double points, int weight, int charSet, bool italic)
        {
            var raw = new byte[92];
            int px = (int)Math.Round(points * 96.0 / 72.0);
            Array.Copy(BitConverter.GetBytes(-px), 0, raw, 0, 4);
            Array.Copy(BitConverter.GetBytes(weight), 0, raw, 16, 4);
            raw[20] = (byte)(italic ? 1 : 0);
            raw[21] = 0; raw[22] = 0;
            raw[23] = (byte)(charSet == 0 ? 1 : charSet);
            raw[24] = 0; raw[25] = 0; raw[26] = 0; raw[27] = 0;
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
                    foreach (var key in Keys)
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
                        n++;
                    }
                    catch { }
                }
            }
            return n;
        }
    }
}
