using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WinFontSwap
{
    /// <summary>预设：把「选中的字体 + 界面元素字体」存成一个小文本文件。</summary>
    internal static class Presets
    {
        public static string Dir
        {
            get { return Path.Combine(Installer.DataDir, "presets"); }
        }

        public static List<string> List()
        {
            var list = new List<string>();
            try
            {
                if (!Directory.Exists(Dir)) return list;
                foreach (var f in Directory.GetFiles(Dir, "*.txt"))
                    list.Add(Path.GetFileNameWithoutExtension(f));
                list.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch { }
            return list;
        }

        public static void Save(string name, string family, string[] faces, decimal[] sizes)
        {
            Directory.CreateDirectory(Dir);
            var sb = new StringBuilder();
            sb.AppendLine("family=" + family);
            for (int i = 0; i < faces.Length; i++)
                sb.AppendLine("win" + i + "=" + faces[i] + "|" + sizes[i]);
            File.WriteAllText(Path.Combine(Dir, name + ".txt"), sb.ToString(), Encoding.UTF8);
        }

        public static bool Load(string name, out string family, out string[] faces, out decimal[] sizes)
        {
            family = null;
            faces = new string[6];
            sizes = new decimal[6];
            var path = Path.Combine(Dir, name + ".txt");
            if (!File.Exists(path)) return false;
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string k = line.Substring(0, eq);
                string v = line.Substring(eq + 1);
                if (k == "family") { family = v; continue; }
                if (k.StartsWith("win"))
                {
                    int idx;
                    if (!int.TryParse(k.Substring(3), out idx) || idx < 0 || idx >= 6) continue;
                    int bar = v.LastIndexOf('|');
                    if (bar < 0) continue;
                    faces[idx] = v.Substring(0, bar);
                    decimal d;
                    if (decimal.TryParse(v.Substring(bar + 1), out d)) sizes[idx] = d;
                }
            }
            return true;
        }

        public static void Delete(string name)
        {
            try
            {
                var path = Path.Combine(Dir, name + ".txt");
                if (File.Exists(path)) File.Delete(path);
            }
            catch { }
        }
    }
}
