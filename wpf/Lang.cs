using System;
using System.Collections.Generic;
using System.IO;

namespace WinFontSwap.Wpf
{
    /// <summary>界面语言：中文 / English。文案按语义 key 存放（见 lang\zh.txt、lang\en.txt 的分组注释）。</summary>
    internal static class L
    {
        public static bool En;


        /// <summary>编译进 exe 的 lang\zh.txt / lang\en.txt 覆盖内建表（改文案要重新编译）。</summary>
        static readonly Dictionary<string, string> Override = new Dictionary<string, string>(StringComparer.Ordinal);
        static string _loadedFor;

        /// <summary>读编译进 exe 的资源 lang.zh.txt / lang.en.txt；每行「原文=显示文本」，# 开头为注释。</summary>
        public static void LoadFile()
        {
            Override.Clear();
            var name = En ? "lang.en.txt" : "lang.zh.txt";
            _loadedFor = name;
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string res = null;
                foreach (var r in asm.GetManifestResourceNames())
                {
                    if (r.EndsWith(name, StringComparison.OrdinalIgnoreCase)) { res = r; break; }
                }
                if (res == null) { WpfProgram.Log("lang: resource missing (" + name + ")"); return; }
                string text;
                using (var s = asm.GetManifestResourceStream(res))
                using (var sr = new StreamReader(s, System.Text.Encoding.UTF8))
                    text = sr.ReadToEnd();
                foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var k = Unescape(line.Substring(0, eq));
                    var v = Unescape(line.Substring(eq + 1));
                    if (k.Length > 0) Override[k] = v;
                }
            }
            catch (Exception ex) { WpfProgram.Log("lang: " + ex.Message); }
        }

        static string Unescape(string s)
        {
            return s.Replace("\\n", "\n").Replace("\\r", "\r");
        }

        public static string T(string key)
        {
            if (key == null) return null;
            string v;
            if (Override.TryGetValue(key, out v)) return v;
            if (WpfProgram.LogEnabled) WpfProgram.Log("lang: missing key " + key);
            return key;   // 少翻/漏翻时直接把 key 显示出来，方便定位
        }

        static string SettingsPath
        {
            get { return Path.Combine(Installer.DataDir, "ui.lang"); }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var s = File.ReadAllText(SettingsPath).Trim();
                En = string.Equals(s, "en", StringComparison.OrdinalIgnoreCase);
            }
            catch { }
            LoadFile();
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Installer.DataDir);
                File.WriteAllText(SettingsPath, En ? "en" : "zh");
            }
            catch { }
        }
    }
}