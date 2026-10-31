using System;
using System.Collections.Generic;
using System.IO;

namespace WinFontSwap
{
    internal sealed class FontEntry
    {
        public string Path;
        public int FaceIndex;
        public string Family;
        public string Subfamily;
        public string Version;
        public int Weight;

        public string FileName { get { return Path == null ? "" : System.IO.Path.GetFileName(Path); } }
        public string Display { get { return Family + "  " + (Subfamily ?? "") + "  w" + Weight; } }
    }

    /// <summary>扫描本机字体文件（只读 name / OS/2），并按家族找字重。</summary>
    internal static class FontLibrary
    {
        static List<FontEntry> _all;

        public static List<FontEntry> All
        {
            get
            {
                if (_all == null) _all = Scan();
                return _all;
            }
        }

        public static void Reset() { _all = null; }

        public static List<FontEntry> Scan()
        {
            var list = new List<FontEntry>();
            foreach (var dir in Directories())
            {
                if (!Directory.Exists(dir)) continue;
                foreach (var file in SafeFiles(dir))
                {
                    var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".ttf" && ext != ".ttc") continue;
                    foreach (var e in ReadFile(file)) list.Add(e);
                }
            }
            return list;
        }

        static IEnumerable<string> SafeFiles(string dir)
        {
            try { return Directory.GetFiles(dir); }
            catch { return new string[0]; }
        }

        static IEnumerable<string> Directories()
        {
            var win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            yield return System.IO.Path.Combine(win, "Fonts");
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return System.IO.Path.Combine(local, @"Microsoft\Windows\Fonts");
        }

        public static List<FontEntry> ReadFile(string path)
        {
            var result = new List<FontEntry>();
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    var head = new byte[12];
                    if (fs.Read(head, 0, 12) < 12) return result;
                    string magic = "" + (char)head[0] + (char)head[1] + (char)head[2] + (char)head[3];
                    var offsets = new List<long>();
                    if (magic == "ttcf")
                    {
                        int n = (head[8] << 24) | (head[9] << 16) | (head[10] << 8) | head[11];
                        if (n <= 0 || n > 64) return result;
                        var buf = new byte[4 * n];
                        fs.Position = 12;
                        int read = 0;
                        while (read < buf.Length) { int k = fs.Read(buf, read, buf.Length - read); if (k <= 0) break; read += k; }
                        for (int i = 0; i < n && 4 * i + 3 < read; i++)
                            offsets.Add(((long)buf[4 * i] << 24) | ((long)buf[4 * i + 1] << 16) | ((long)buf[4 * i + 2] << 8) | buf[4 * i + 3]);
                    }
                    else offsets.Add(0);

                    for (int i = 0; i < offsets.Count; i++)
                    {
                        string fam, sub, ver;
                        int weight;
                        if (!Sfnt.TryReadNamesAt(fs, offsets[i], out fam, out sub, out ver, out weight)) continue;
                        result.Add(new FontEntry
                        {
                            Path = path,
                            FaceIndex = i,
                            Family = fam,
                            Subfamily = sub,
                            Version = ver,
                            Weight = weight
                        });
                    }
                }
            }
            catch { }
            return result;
        }

        /// <summary>同一家族 + 同目录里其它字重。</summary>
        public static List<FontEntry> CandidatesFor(FontEntry picked)
        {
            var list = new List<FontEntry>();
            if (picked == null) return list;
            foreach (var e in All)
                if (string.Equals(e.Family, picked.Family, StringComparison.OrdinalIgnoreCase))
                    AddUnique(list, e);

            var dir = System.IO.Path.GetDirectoryName(picked.Path);
            if (dir != null)
                foreach (var e in ReadFile(picked.Path))
                    AddUnique(list, e);

            if (dir != null)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                        if (ext != ".ttf" && ext != ".ttc") continue;
                        foreach (var e in ReadFile(file))
                            if (string.Equals(e.Family, picked.Family, StringComparison.OrdinalIgnoreCase))
                                AddUnique(list, e);
                    }
                }
                catch { }
            }
            return list;
        }

        static readonly string[] WeightWords = { "ultra light", "extra light", "semi light", "demi light", "ultralight", "extralight", "semilight", "demilight", "extra bold", "semi bold", "ultra bold", "extrabold", "semibold", "ultrabold", "thin", "light", "regular", "normal", "book", "medium", "bold", "black", "heavy", "italic", "oblique" };

        /// <summary>去掉尾部字重词，得到“家族基名”：HarmonyOS Sans SC Light → harmonyos sans sc。</summary>
        public static string FamilyBase(string family)
        {
            if (string.IsNullOrEmpty(family)) return "";
            var s = family.ToLowerInvariant().Trim();
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var w in WeightWords)
                {
                    if (s.EndsWith(" " + w))
                    {
                        s = s.Substring(0, s.Length - w.Length - 1).Trim();
                        changed = true;
                        break;
                    }
                }
            }
            return s;
        }

        /// <summary>按“家族基名”找同家族的各字重（跨文件、跨 Light/Bold 这类独立家族名）。</summary>
        public static List<FontEntry> CandidatesForBase(FontEntry picked)
        {
            var list = new List<FontEntry>();
            if (picked == null) return list;
            var baseName = FamilyBase(picked.Family);
            if (baseName.Length == 0) return CandidatesFor(picked);

            foreach (var e in All)
                if (FamilyBase(e.Family) == baseName) AddUnique(list, e);

            var dir = System.IO.Path.GetDirectoryName(picked.Path);
            if (dir != null)
            {
                try
                {
                    foreach (var file in Directory.GetFiles(dir))
                    {
                        var ext = System.IO.Path.GetExtension(file).ToLowerInvariant();
                        if (ext != ".ttf" && ext != ".ttc" && ext != ".otf") continue;
                        foreach (var e in ReadFile(file))
                            if (FamilyBase(e.Family) == baseName) AddUnique(list, e);
                    }
                }
                catch { }
            }
            return list;
        }

        static void AddUnique(List<FontEntry> list, FontEntry e)
        {
            foreach (var x in list)
                if (x.Path == e.Path && x.FaceIndex == e.FaceIndex) return;
            list.Add(e);
        }

        /// <summary>按目标字重挑最接近的一个；差太远（>200）视为没有。</summary>
        public static FontEntry Match(List<FontEntry> pool, int target)
        {
            FontEntry best = null;
            int bestDiff = int.MaxValue;
            foreach (var e in pool)
            {
                int diff = Math.Abs(e.Weight - target);
                if (diff < bestDiff) { best = e; bestDiff = diff; }
            }
            return bestDiff <= 200 ? best : null;
        }

        /// <summary>这个条目所在文件是不是可变字体（只看文件名像 VF 的，避免把全库字体都读一遍）。</summary>
        public static bool IsVariableEntry(FontEntry e)
        {
            if (e == null) return false;
            try { return Sfnt.IsVariable(File.ReadAllBytes(e.Path), e.FaceIndex); }
            catch { return false; }
        }

        /// <summary>
        /// 找同一个家族的可变字体版本：英数那个槽位（SegUIVar）在 WinUI 里会被按 400/600/700 多档请求，
        /// 只有可变字体才能对上真实字重，静态文件会被系统合成。
        /// </summary>
        public static FontEntry FindVariableSibling(FontEntry picked)
        {
            if (picked == null) return null;
            if (IsVariableEntry(picked)) return picked;
            // 必须同一个家族（去掉 VF / Variable 这类后缀后比较），否则会把别的字体当成“同族可变版”
            var baseName = NormalizeBase(picked.Family);
            foreach (var e in All)
            {
                var n = (e.FileName ?? "").ToLowerInvariant();
                var f = (e.Family ?? "").ToLowerInvariant();
                if (n.IndexOf("vf", StringComparison.Ordinal) < 0 && f.IndexOf("vf", StringComparison.Ordinal) < 0
                    && n.IndexOf("variable", StringComparison.Ordinal) < 0 && f.IndexOf("variable", StringComparison.Ordinal) < 0) continue;
                if (NormalizeBase(e.Family) != baseName) continue;
                if (IsVariableEntry(e)) return e;
            }
            return null;
        }

        static string NormalizeBase(string family)
        {
            var b = FamilyBase(family);
            b = b.Replace(" variable", "").Replace("variable", "");
            b = b.Replace(" vf", "").Replace("vf", "");
            return b.Trim();
        }

        public static string Label(int weight)
        {
            if (weight < 350) return "细体";
            if (weight < 500) return "常规";
            if (weight < 650) return "中黑";
            return "粗体";
        }
    }
}
