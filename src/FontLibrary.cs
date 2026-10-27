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
                    if (ext != ".ttf" && ext != ".ttc" && ext != ".ttf" ) continue;
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

        public static string Label(int weight)
        {
            if (weight < 350) return "细体";
            if (weight < 500) return "常规";
            if (weight < 650) return "中黑";
            return "粗体";
        }
    }
}
