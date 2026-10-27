using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace WinFontSwap
{
    internal static class GlyfProbe
    {
        static string TableHash(byte[] data, int faceOff, string tag)
        {
            // 简易: 在 face 的 table directory 里找 tag，返回该表内容的 SHA256
            int numTables = (data[faceOff + 4] << 8) | data[faceOff + 5];
            for (int i = 0; i < numTables; i++)
            {
                int rec = faceOff + 12 + i * 16;
                string t = System.Text.Encoding.ASCII.GetString(data, rec, 4);
                if (t != tag) continue;
                uint off = (uint)((data[rec + 8] << 24) | (data[rec + 9] << 16) | (data[rec + 10] << 8) | data[rec + 11]);
                uint len = (uint)((data[rec + 12] << 24) | (data[rec + 13] << 16) | (data[rec + 14] << 8) | data[rec + 15]);
                using (var sha = SHA256.Create())
                {
                    return BitConverter.ToString(sha.ComputeHash(data, (int)off, (int)len)).Replace("-", "");
                }
            }
            return null;
        }

        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            foreach (var f in Installer.TargetFiles)
            {
                var p = Path.Combine(Installer.FontsDir, f);
                if (!File.Exists(p)) continue;
                var d = File.ReadAllBytes(p);
                var faces = Sfnt.FaceOffsets(d);
                var h = TableHash(d, faces[0], "glyf");
                Console.WriteLine(f + "  glyf=" + (h == null ? "-" : h.Substring(0, 16)));
                // 找本机其它字体里 glyf 相同的
                int hits = 0;
                foreach (var cand in FontLibrary.All)
                {
                    if (string.Equals(Path.GetFileName(cand.Path), f, StringComparison.OrdinalIgnoreCase)) continue;
                    bool isTarget = false;
                    foreach (var t in Installer.TargetFiles) if (string.Equals(Path.GetFileName(cand.Path), t, StringComparison.OrdinalIgnoreCase)) { isTarget = true; break; }
                    if (isTarget) continue;
                    try
                    {
                        var cd = File.ReadAllBytes(cand.Path);
                        var cf = Sfnt.FaceOffsets(cd);
                        if (cand.FaceIndex >= cf.Length) continue;
                        var ch = TableHash(cd, cf[cand.FaceIndex], "glyf");
                        if (ch != null && ch == h) { Console.WriteLine("   MATCH " + cand.Family + "  (" + cand.Path + ")"); hits++; if (hits > 3) break; }
                    }
                    catch { }
                }
                if (hits == 0) Console.WriteLine("   no glyph match");
            }
        }
    }
}
