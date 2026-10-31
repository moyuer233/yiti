using System;
using System.Collections.Generic;
using System.IO;

namespace WinFontSwap.Wpf
{
    /// <summary>界面语言：中文 / English。文案按语义 key 存放（见 lang\zh.toml、lang\en.toml，[分节] + "键 = 值"）。</summary>
    internal static class L
    {
        public static bool En;


        /// <summary>编译进 exe 的 lang\zh.toml / lang\en.toml 覆盖内建表（改文案要重新编译）。</summary>
        static readonly Dictionary<string, string> Override = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>读编译进 exe 的资源 lang.zh.toml / lang.en.toml。只支持用得到的 TOML 子集：[分节]、"键 = 值"、# 注释。</summary>
        public static void LoadFile()
        {
            Override.Clear();
            var name = En ? "lang.en.toml" : "lang.zh.toml";
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
                var section = "";
                foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == '#') continue;
                    if (line[0] == '[')
                    {
                        int close = line.IndexOf(']');
                        if (close > 1) section = line.Substring(1, close - 1).Trim();
                        continue;
                    }
                    int eq = IndexOutsideQuotes(line, '=');
                    if (eq <= 0) continue;
                    var k = UnquoteKey(line.Substring(0, eq).Trim());
                    var v = UnquoteValue(line.Substring(eq + 1).Trim());   // 值原样保留：不少文案是拼在数字前后的片段
                    if (k.Length == 0) continue;
                    Override[section.Length > 0 ? section + "." + k : k] = v;
                }
            }
            catch (Exception ex) { WpfProgram.Log("lang: " + ex.Message); }
        }

        static int IndexOutsideQuotes(string s, char c)
        {
            char quote = '\0';
            for (int i = 0; i < s.Length; i++)
            {
                var ch = s[i];
                if (quote != '\0')
                {
                    if (ch == '\\' && quote == '"') { i++; continue; }
                    if (ch == quote) quote = '\0';
                    continue;
                }
                if (ch == '"' || ch == '\'') { quote = ch; continue; }
                if (ch == c) return i;
            }
            return -1;
        }

        static string UnquoteKey(string s)
        {
            s = s.Trim();
            if (s.Length >= 2 && (s[0] == '"' || s[0] == '\'') && s[s.Length - 1] == s[0])
                return s.Substring(1, s.Length - 2);
            return s;
        }

        static string UnquoteValue(string s)
        {
            if (s.Length < 2) return s;
            var q = s[0];
            if (q != '"' && q != '\'') return s;
            int end = s.LastIndexOf(q);
            if (end <= 0) return s;
            var inner = s.Substring(1, end - 1);
            if (q == '\'') return inner;   // 单引号是字面量，不处理转义
            var sb = new System.Text.StringBuilder(inner.Length);
            for (int i = 0; i < inner.Length; i++)
            {
                var ch = inner[i];
                if (ch == '\\' && i + 1 < inner.Length)
                {
                    var n = inner[++i];
                    if (n == 'n') sb.Append('\n');
                    else if (n == 'r') sb.Append('\r');
                    else if (n == 't') sb.Append('\t');
                    else if (n == '"') sb.Append('"');
                    else if (n == '\\') sb.Append('\\');
                    else { sb.Append('\\'); sb.Append(n); }
                    continue;
                }
                sb.Append(ch);
            }
            return sb.ToString();
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
                // 注意：ui.lang 不存在时也必须往下走 —— 否则文案表是空的，界面会显示成原始 key
                if (File.Exists(SettingsPath))
                {
                    var s = File.ReadAllText(SettingsPath).Trim();
                    En = string.Equals(s, "en", StringComparison.OrdinalIgnoreCase);
                }
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