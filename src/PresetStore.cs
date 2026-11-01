using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WinFontSwap
{
    /// <summary>用户预设：把「字体替换页」和「窗口字体」的当前选择存成一份可命名的配置，随时加载。</summary>
    public static class PresetStore
    {
        public static string Root { get { return Path.Combine(Installer.DataDir, "presets"); } }

        public sealed class Entry
        {
            public string Name;
            public string Dir;
            public DateTime Date;
            public int Slots;
            public int Shell;
        }

        public static string Sanitize(string name)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0) name = "预设";
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            name = name.Replace('\\', '_').Replace('/', '_').Trim('.');
            if (name.Length == 0) name = "预设";
            if (name.Length > 60) name = name.Substring(0, 60);
            return name;
        }

        public static List<Entry> List()
        {
            var list = new List<Entry>();
            try
            {
                if (!Directory.Exists(Root)) return list;
                foreach (var d in Directory.GetDirectories(Root))
                {
                    var ini = Path.Combine(d, "preset.ini");
                    if (!File.Exists(ini)) continue;
                    var e = new Entry { Name = Path.GetFileName(d), Dir = d };
                    try { e.Date = Directory.GetCreationTime(d); } catch { e.Date = DateTime.MinValue; }
                    try
                    {
                        foreach (var line in File.ReadAllLines(ini, Encoding.UTF8))
                        {
                            int eq = line.IndexOf('=');
                            if (eq <= 0) continue;
                            var k = line.Substring(0, eq).Trim();
                            if (k.StartsWith("slot.") && k.EndsWith(".on")) e.Slots++;
                            else if (k.StartsWith("shell.") && k.EndsWith(".on")) e.Shell++;
                        }
                    }
                    catch { }
                    list.Add(e);
                }
                list.Sort(delegate (Entry a, Entry b) { return string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase); });
            }
            catch { }
            return list;
        }

        public static bool Exists(string name)
        {
            return File.Exists(Path.Combine(Root, Sanitize(name), "preset.ini"));
        }

        public static string Save(string name, IDictionary<string, string> values)
        {
            var safe = Sanitize(name);
            var dir = Path.Combine(Root, safe);
            Directory.CreateDirectory(dir);
            var sb = new StringBuilder();
            foreach (var kv in values)
            {
                if (kv.Value == null) continue;
                sb.AppendLine(kv.Key + "=" + kv.Value.Replace("\r", " ").Replace("\n", " "));
            }
            File.WriteAllText(Path.Combine(dir, "preset.ini"), sb.ToString(), Encoding.UTF8);
            return safe;
        }

        public static Dictionary<string, string> Read(string name)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var ini = Path.Combine(Root, Sanitize(name), "preset.ini");
                if (!File.Exists(ini)) return result;
                foreach (var line in File.ReadAllLines(ini, Encoding.UTF8))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    result[line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
                }
            }
            catch { }
            return result;
        }

        public static void Delete(string name)
        {
            var dir = Path.Combine(Root, Sanitize(name));
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }

        public static string Rename(string oldName, string newName)
        {
            var from = Path.Combine(Root, Sanitize(oldName));
            var to = Path.Combine(Root, Sanitize(newName));
            if (!Directory.Exists(from)) throw new InvalidOperationException("预设不存在");
            if (Directory.Exists(to) && !string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("已经有同名预设");
            if (!string.Equals(from, to, StringComparison.OrdinalIgnoreCase)) Directory.Move(from, to);
            return Path.GetFileName(to);
        }
    }
}
