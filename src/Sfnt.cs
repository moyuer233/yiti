using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WinFontSwap
{
    /// <summary>一整套要写进同一份字体文件里的命名。</summary>
    public sealed class NameSpec
    {
        public string Family;         // nameID 1
        public string FamilyTypo;     // nameID 16
        public string Subfamily;      // nameID 2
        public string SubfamilyTypo;  // nameID 17
        public string FullName;       // nameID 4
        public string PostScript;     // nameID 6
    }

    public sealed class SfntTable
    {
        public string Tag;
        public byte[] Data;
        public SfntTable(string tag, byte[] data) { Tag = tag; Data = data; }
    }

    /// <summary>
    /// 最小可用的 sfnt/TTC 读写器：读表 → 改 name 表 → 重新打包成 TTC。
    /// Windows 只认 name 表里的族名，所以这一层就够了，不引入任何字体库。
    /// </summary>
    public static class Sfnt
    {
        static int U16(byte[] b, int o) { return (b[o] << 8) | b[o + 1]; }
        static uint U32(byte[] b, int o) { return ((uint)b[o] << 24) | ((uint)b[o + 1] << 16) | ((uint)b[o + 2] << 8) | b[o + 3]; }
        static void W16(byte[] b, int o, int v) { b[o] = (byte)(v >> 8); b[o + 1] = (byte)v; }
        static void W32(byte[] b, int o, uint v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }
        static int Align4(int v) { return (v + 3) & ~3; }

        public static int[] FaceOffsets(byte[] file)
        {
            if (file.Length < 12) throw new InvalidDataException("文件太小，不是字体");
            string magic = Encoding.ASCII.GetString(file, 0, 4);
            if (magic != "ttcf") return new[] { 0 };
            int n = (int)U32(file, 8);
            if (n <= 0 || n > 512) throw new InvalidDataException("TTC 头异常");
            var res = new int[n];
            for (int i = 0; i < n; i++) res[i] = (int)U32(file, 12 + i * 4);
            return res;
        }

        public static uint FaceVersion(byte[] file, int faceOff) { return U32(file, faceOff); }

        public static List<SfntTable> ReadTables(byte[] file, int faceOff)
        {
            if (faceOff < 0 || faceOff + 12 > file.Length) throw new InvalidDataException("字体头越界");
            int n = U16(file, faceOff + 4);
            if (n <= 0 || n > 512) throw new InvalidDataException("表数量异常");
            var list = new List<SfntTable>(n);
            for (int i = 0; i < n; i++)
            {
                int rec = faceOff + 12 + i * 16;
                if (rec + 16 > file.Length) throw new InvalidDataException("表目录越界");
                string tag = Encoding.ASCII.GetString(file, rec, 4);
                uint off = U32(file, rec + 8);
                uint len = U32(file, rec + 12);
                if (off + len > file.Length) throw new InvalidDataException("表数据越界：" + tag);
                var data = new byte[len];
                Buffer.BlockCopy(file, (int)off, data, 0, (int)len);
                list.Add(new SfntTable(tag, data));
            }
            return list;
        }

        public static bool TryReadNames(byte[] file, int faceOff, out string family, out string subfamily,
                                        out string version, out string fullName, out int weightClass)
        {
            family = subfamily = version = fullName = null;
            weightClass = 400;
            try
            {
                var tables = ReadTables(file, faceOff);
                var name = tables.Find(t => t.Tag == "name");
                if (name == null) return false;
                var recs = ParseNames(name.Data);
                family = First(recs, 1);
                subfamily = First(recs, 2);
                version = First(recs, 5);
                fullName = First(recs, 4);
                var os2 = tables.Find(t => t.Tag == "OS/2");
                if (os2 != null && os2.Data.Length >= 6) weightClass = U16(os2.Data, 4);
                return family != null;
            }
            catch { return false; }
        }

        /// <summary>按 nameID 取字符串（用于指纹比对：版权/版本/商标这些我们不改写的字段）。</summary>
        public static string[] NameStrings(byte[] file, int faceOff, int[] ids)
        {
            var res = new string[ids.Length];
            try
            {
                var tables = ReadTables(file, faceOff);
                var name = tables.Find(t => t.Tag == "name");
                if (name == null) return res;
                var recs = ParseNames(name.Data);
                for (int i = 0; i < ids.Length; i++) res[i] = First(recs, ids[i]);
            }
            catch { }
            return res;
        }

        static void ReadExact(Stream s, byte[] buf, int count)
        {
            int off = 0;
            while (off < count)
            {
                int n = s.Read(buf, off, count - off);
                if (n <= 0) throw new EndOfStreamException();
                off += n;
            }
        }

        /// <summary>只读 name / OS2 两张表（扫全机字体用，避免整份文件读进内存）。</summary>
        public static bool TryReadNamesAt(Stream s, long faceOffset, out string family, out string subfamily,
                                          out string version, out int weight)
        {
            family = subfamily = version = null;
            weight = 400;
            try
            {
                s.Position = faceOffset;
                var head = new byte[12];
                ReadExact(s, head, 12);
                int n = U16(head, 4);
                if (n <= 0 || n > 512) return false;
                var dir = new byte[16 * n];
                ReadExact(s, dir, dir.Length);

                uint nameOff = 0, nameLen = 0, os2Off = 0, os2Len = 0;
                bool hasName = false, hasOs2 = false;
                for (int i = 0; i < n; i++)
                {
                    int rec = i * 16;
                    string tag = Encoding.ASCII.GetString(dir, rec, 4);
                    uint off = U32(dir, rec + 8);
                    uint len = U32(dir, rec + 12);
                    if (tag == "name") { nameOff = off; nameLen = len; hasName = true; }
                    else if (tag == "OS/2") { os2Off = off; os2Len = len; hasOs2 = true; }
                }
                if (!hasName || nameLen == 0 || nameLen > 200000) return false;

                var nb = new byte[nameLen];
                s.Position = nameOff;
                ReadExact(s, nb, (int)nameLen);
                var recs = ParseNames(nb);
                family = First(recs, 1);
                subfamily = First(recs, 2);
                version = First(recs, 5);

                if (hasOs2 && os2Len >= 6)
                {
                    var ob = new byte[6];
                    s.Position = os2Off;
                    ReadExact(s, ob, 6);
                    weight = U16(ob, 4);
                }
                return family != null;
            }
            catch { return false; }
        }

        static string First(List<NameRecord> recs, int id)
        {
            NameRecord best = null;
            foreach (var r in recs)
            {
                if (r.NameId != id) continue;
                if (r.PlatformId == 3 && r.LanguageId == 0x409) return r.Value;
                if (best == null) best = r;
            }
            return best == null ? null : best.Value;
        }

        sealed class NameRecord
        {
            public int PlatformId, EncodingId, LanguageId, NameId;
            public string Value;
            public byte[] Raw;
            public bool IsWindows;
        }

        static List<NameRecord> ParseNames(byte[] t)
        {
            if (t.Length < 6) throw new InvalidDataException("name 表损坏");
            int count = U16(t, 2);
            int storage = U16(t, 4);
            var list = new List<NameRecord>(count);
            for (int i = 0; i < count; i++)
            {
                int r = 6 + i * 12;
                if (r + 12 > t.Length) throw new InvalidDataException("name 记录越界");
                int pid = U16(t, r), eid = U16(t, r + 2), lid = U16(t, r + 4), nid = U16(t, r + 6);
                int len = U16(t, r + 8), off = U16(t, r + 10);
                if (storage + off + len > t.Length) throw new InvalidDataException("name 字符串越界");
                var raw = new byte[len];
                Buffer.BlockCopy(t, storage + off, raw, 0, len);
                var rec = new NameRecord
                {
                    PlatformId = pid, EncodingId = eid, LanguageId = lid, NameId = nid, Raw = raw,
                    IsWindows = pid == 0 || pid == 3
                };
                rec.Value = rec.IsWindows ? DecodeUtf16Be(raw) : DecodeAscii(raw);
                list.Add(rec);
            }
            return list;
        }

        static string DecodeUtf16Be(byte[] raw)
        {
            var sb = new StringBuilder(raw.Length / 2);
            for (int i = 0; i + 1 < raw.Length; i += 2) sb.Append((char)((raw[i] << 8) | raw[i + 1]));
            return sb.ToString();
        }

        static string DecodeAscii(byte[] raw)
        {
            var chars = new char[raw.Length];
            for (int i = 0; i < raw.Length; i++) chars[i] = (char)raw[i];
            return new string(chars);
        }

        static string ReplacementFor(int nameId, NameSpec s)
        {
            switch (nameId)
            {
                case 1: return s.Family;
                case 2: return s.Subfamily;
                case 3: return s.FullName;
                case 4: return s.FullName;
                case 6: return s.PostScript;
                case 16: return s.FamilyTypo ?? s.Family;
                case 17: return s.SubfamilyTypo ?? s.Subfamily;
                case 18: return s.FullName;
                case 21: return s.FamilyTypo ?? s.Family;
                case 22: return s.SubfamilyTypo ?? s.Subfamily;
                default: return null;
            }
        }

        public static byte[] RebuildName(byte[] table, NameSpec spec)
        {
            var recs = ParseNames(table);
            var newRaw = new byte[recs.Count][];
            for (int i = 0; i < recs.Count; i++)
            {
                var r = recs[i];
                var s = ReplacementFor(r.NameId, spec);
                if (s == null) { newRaw[i] = r.Raw; continue; }
                newRaw[i] = r.IsWindows ? EncodeUtf16Be(s) : EncodeAscii(s);
            }

            int count = recs.Count;
            int headerLen = 6 + count * 12;
            int total = headerLen;
            var offsets = new int[count];
            for (int i = 0; i < count; i++)
            {
                offsets[i] = total - headerLen;
                total += newRaw[i].Length;
                if (total - headerLen > 0xFFFF) throw new InvalidDataException("name 表超过 64KB，无法写入");
            }

            var outBuf = new byte[total];
            W16(outBuf, 0, 0);
            W16(outBuf, 2, count);
            W16(outBuf, 4, headerLen);

            int strPos = headerLen;
            for (int i = 0; i < count; i++)
            {
                var r = recs[i];
                int rec = 6 + i * 12;
                W16(outBuf, rec, r.PlatformId);
                W16(outBuf, rec + 2, r.EncodingId);
                W16(outBuf, rec + 4, r.LanguageId);
                W16(outBuf, rec + 6, r.NameId);
                W16(outBuf, rec + 8, newRaw[i].Length);
                W16(outBuf, rec + 10, offsets[i]);
                Buffer.BlockCopy(newRaw[i], 0, outBuf, strPos, newRaw[i].Length);
                strPos += newRaw[i].Length;
            }
            return outBuf;
        }

        static byte[] EncodeUtf16Be(string s)
        {
            var b = new byte[s.Length * 2];
            for (int i = 0; i < s.Length; i++) { b[i * 2] = (byte)(s[i] >> 8); b[i * 2 + 1] = (byte)s[i]; }
            return b;
        }

        static byte[] EncodeAscii(string s)
        {
            var b = new byte[s.Length];
            for (int i = 0; i < s.Length; i++) b[i] = s[i] < 0x80 ? (byte)s[i] : (byte)'?';
            return b;
        }

        static uint TableChecksum(byte[] d)
        {
            uint sum = 0;
            int i = 0;
            for (; i + 4 <= d.Length; i += 4)
                sum += ((uint)d[i] << 24) | ((uint)d[i + 1] << 16) | ((uint)d[i + 2] << 8) | d[i + 3];
            if (i < d.Length)
            {
                uint v = 0; int shift = 24;
                for (; i < d.Length; i++) { v |= (uint)d[i] << shift; shift -= 8; }
                sum += v;
            }
            return sum;
        }

        static uint FileChecksum(byte[] b)
        {
            uint sum = 0;
            for (int i = 0; i + 4 <= b.Length; i += 4)
                sum += ((uint)b[i] << 24) | ((uint)b[i + 1] << 16) | ((uint)b[i + 2] << 8) | b[i + 3];
            return sum;
        }

        public static byte[] BuildFace(uint version, List<SfntTable> tables)
        {
            tables.Sort((a, b) => string.CompareOrdinal(a.Tag, b.Tag));
            int n = tables.Count;
            int headerLen = 12 + 16 * n;
            int total = headerLen;
            var offs = new int[n];
            for (int i = 0; i < n; i++) { offs[i] = total; total += Align4(tables[i].Data.Length); }

            var buf = new byte[total];
            W32(buf, 0, version);
            W16(buf, 4, n);
            int pow = 1, log = 0;
            while (pow * 2 <= n) { pow *= 2; log++; }
            W16(buf, 6, pow * 16);
            W16(buf, 8, log);
            W16(buf, 10, n * 16 - pow * 16);

            int headIdx = tables.FindIndex(t => t.Tag == "head");
            if (headIdx >= 0 && tables[headIdx].Data.Length >= 12)
                for (int k = 8; k < 12; k++) tables[headIdx].Data[k] = 0;

            for (int i = 0; i < n; i++)
            {
                int rec = 12 + i * 16;
                var tag = Encoding.ASCII.GetBytes(tables[i].Tag);
                Buffer.BlockCopy(tag, 0, buf, rec, 4);
                W32(buf, rec + 4, TableChecksum(tables[i].Data));
                W32(buf, rec + 8, (uint)offs[i]);
                W32(buf, rec + 12, (uint)tables[i].Data.Length);
                Buffer.BlockCopy(tables[i].Data, 0, buf, offs[i], tables[i].Data.Length);
            }

            if (headIdx >= 0)
            {
                uint adj = 0xB1B0AFBA - FileChecksum(buf);
                W32(buf, offs[headIdx] + 8, adj);
            }
            return buf;
        }

        struct TableLoc { public int Offset; public uint Checksum; public uint Length; }

        static byte[] NameTableOf(List<SfntTable> tables, NameSpec spec)
        {
            var name = tables.Find(t => t.Tag == "name");
            if (name == null) throw new InvalidDataException("字体缺少 name 表");
            return RebuildName(name.Data, spec);
        }

        /// <summary>是不是可变字体（带 fvar 表）。</summary>
        public static bool IsVariable(byte[] source, int faceIndex)
        {
            try
            {
                var faces = FaceOffsets(source);
                if (faceIndex < 0 || faceIndex >= faces.Length) return false;
                var tables = ReadTables(source, faces[faceIndex]);
                return tables.Find(t => t.Tag == "fvar") != null;
            }
            catch { return false; }
        }

        public static byte[] BuildCollection(byte[] source, int faceIndex, NameSpec[] specs)
        {
            return BuildCollection(source, faceIndex, specs, false);
        }

        /// <summary>多 face TTC；keepVariation=true 时保留 fvar/gvar（各 face 共享同一份表）。</summary>
        public static byte[] BuildCollection(byte[] source, int faceIndex, NameSpec[] specs, bool keepVariation)
        {
            var faces = FaceOffsets(source);
            if (faceIndex < 0 || faceIndex >= faces.Length) throw new InvalidDataException("face 序号越界");
            int faceOff = faces[faceIndex];
            uint version = FaceVersion(source, faceOff);
            var baseTables = ReadTables(source, faceOff);

            if (baseTables.Find(t => t.Tag == "CFF ") != null)
                throw new InvalidDataException("暂不支持 CFF(.otf) 轮廓，请选择 TrueType(.ttf/.ttc) 字体");
            if (baseTables.Find(t => t.Tag == "glyf") == null)
                throw new InvalidDataException("字体缺少 glyf 表，不是 TrueType 轮廓");
            // 可变字体：默认去掉变体轴相关表（退化成默认实例）；keepVariation 时保留
            if (!keepVariation)
            {
                string[] variation = { "fvar", "gvar", "HVAR", "VVAR", "MVAR", "avar", "cvar", "STAT" };
                foreach (var tag in variation)
                {
                    var t = baseTables.Find(x => x.Tag == tag);
                    if (t != null) baseTables.Remove(t);
                }
            }

            // 第一个 face 带完整表；其余 face 只带自己的 name 表，别的表指向第一个 face（TTC 允许共享）
            var firstTables = new List<SfntTable>(baseTables.Count);
            foreach (var t in baseTables) firstTables.Add(new SfntTable(t.Tag, (byte[])t.Data.Clone()));
            var first = firstTables.Find(t => t.Tag == "name");
            if (first == null) throw new InvalidDataException("字体缺少 name 表");
            first.Data = RebuildName(first.Data, specs[0]);
            var firstFace = BuildFace(version, firstTables);

            int headerLen = 12 + 4 * specs.Length;
            int firstStart = Align4(headerLen);
            int total = Align4(firstStart + firstFace.Length);

            var extraNames = new List<byte[]>(Math.Max(0, specs.Length - 1));
            var extraStarts = new List<int>(Math.Max(0, specs.Length - 1));
            for (int i = 1; i < specs.Length; i++)
            {
                var nameData = NameTableOf(baseTables, specs[i]);
                extraNames.Add(nameData);
                extraStarts.Add(total);
                total += Align4(12 + 16 * baseTables.Count + Align4(nameData.Length));
            }

            var outBuf = new byte[total];
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("ttcf"), 0, outBuf, 0, 4);
            W32(outBuf, 4, 0x00010000);
            W32(outBuf, 8, (uint)specs.Length);
            W32(outBuf, 12, (uint)firstStart);
            for (int i = 1; i < specs.Length; i++) W32(outBuf, 12 + i * 4, (uint)extraStarts[i - 1]);

            Buffer.BlockCopy(firstFace, 0, outBuf, firstStart, firstFace.Length);
            var map = RebaseFace(outBuf, firstStart, firstFace.Length);

            for (int i = 1; i < specs.Length; i++)
                WriteSharedFace(outBuf, extraStarts[i - 1], version, baseTables, extraNames[i - 1], map);

            return outBuf;
        }

        /// <summary>生成单 face 的普通 sfnt（.ttf）；keepVariation=true 时保留 fvar/gvar 等可变轴表。</summary>
        public static byte[] BuildSingle(byte[] source, int faceIndex, NameSpec spec, bool keepVariation)
        {
            var faces = FaceOffsets(source);
            if (faceIndex < 0 || faceIndex >= faces.Length) throw new InvalidDataException("face 序号越界");
            int faceOff = faces[faceIndex];
            uint version = FaceVersion(source, faceOff);
            var tables = ReadTables(source, faceOff);

            if (tables.Find(t => t.Tag == "CFF ") != null)
                throw new InvalidDataException("暂不支持 CFF(.otf) 轮廓，请选择 TrueType(.ttf/.ttc) 字体");
            if (tables.Find(t => t.Tag == "glyf") == null)
                throw new InvalidDataException("字体缺少 glyf 表，不是 TrueType 轮廓");
            if (!keepVariation)
            {
                string[] variation = { "fvar", "gvar", "HVAR", "VVAR", "MVAR", "avar", "cvar", "STAT" };
                foreach (var tag in variation)
                {
                    var t2 = tables.Find(x => x.Tag == tag);
                    if (t2 != null) tables.Remove(t2);
                }
            }

            int nameIdx = tables.FindIndex(t => t.Tag == "name");
            if (nameIdx < 0) throw new InvalidDataException("字体缺少 name 表");
            tables[nameIdx] = new SfntTable("name", RebuildName(tables[nameIdx].Data, spec));
            tables.Sort(delegate (SfntTable a, SfntTable b) { return string.CompareOrdinal(a.Tag, b.Tag); });

            int n = tables.Count;
            int total = 12 + 16 * n;
            var offs = new int[n];
            for (int i = 0; i < n; i++)
            {
                offs[i] = total;
                total += Align4(tables[i].Data.Length);
            }

            var buf = new byte[total];
            W32(buf, 0, version);
            W16(buf, 4, n);
            int pow = 1, log = 0;
            while (pow * 2 <= n) { pow *= 2; log++; }
            W16(buf, 6, pow * 16);
            W16(buf, 8, log);
            W16(buf, 10, n * 16 - pow * 16);

            int headPos = -1;
            for (int i = 0; i < n; i++)
            {
                int rec = 12 + i * 16;
                Buffer.BlockCopy(Encoding.ASCII.GetBytes(tables[i].Tag), 0, buf, rec, 4);
                W32(buf, rec + 4, TableChecksum(tables[i].Data));
                W32(buf, rec + 8, (uint)offs[i]);
                W32(buf, rec + 12, (uint)tables[i].Data.Length);
                Buffer.BlockCopy(tables[i].Data, 0, buf, offs[i], tables[i].Data.Length);
                if (tables[i].Tag == "head") headPos = offs[i] + 8;
            }
            if (headPos > 0)
            {
                W32(buf, headPos, 0);
                uint sum = 0;
                for (int i = 0; i + 4 <= total; i += 4)
                    sum += ((uint)buf[i] << 24) | ((uint)buf[i + 1] << 16) | ((uint)buf[i + 2] << 8) | buf[i + 3];
                W32(buf, headPos, 0xB1B0AFBA - sum);
            }
            return buf;
        }

        /// <summary>把第一个 face 的表偏移从「相对 face」改成「相对文件」，顺便修 head 校验和。</summary>
        static Dictionary<string, TableLoc> RebaseFace(byte[] buf, int faceStart, int faceLen)
        {
            int n = (buf[faceStart + 4] << 8) | buf[faceStart + 5];
            var map = new Dictionary<string, TableLoc>(StringComparer.Ordinal);
            int headAbs = -1;
            for (int i = 0; i < n; i++)
            {
                int rec = faceStart + 12 + i * 16;
                uint oldOff = ((uint)buf[rec + 8] << 24) | ((uint)buf[rec + 9] << 16) | ((uint)buf[rec + 10] << 8) | buf[rec + 11];
                uint newOff = oldOff + (uint)faceStart;
                W32(buf, rec + 8, newOff);
                uint cs = ((uint)buf[rec + 4] << 24) | ((uint)buf[rec + 5] << 16) | ((uint)buf[rec + 6] << 8) | buf[rec + 7];
                uint len = ((uint)buf[rec + 12] << 24) | ((uint)buf[rec + 13] << 16) | ((uint)buf[rec + 14] << 8) | buf[rec + 15];
                string tag = Encoding.ASCII.GetString(buf, rec, 4);
                map[tag] = new TableLoc { Offset = (int)newOff, Checksum = cs, Length = len };
                if (tag == "head") headAbs = (int)newOff;
            }
            if (headAbs >= 0)
            {
                W32(buf, headAbs + 8, 0);
                uint sum = 0;
                for (int i = faceStart; i + 4 <= faceStart + faceLen; i += 4)
                    sum += ((uint)buf[i] << 24) | ((uint)buf[i + 1] << 16) | ((uint)buf[i + 2] << 8) | buf[i + 3];
                W32(buf, headAbs + 8, 0xB1B0AFBA - sum);
            }
            return map;
        }

        /// <summary>只写一个 face 的头 + 记录 + 自己的 name 表，其余表复用第一个 face 的数据。</summary>
        static void WriteSharedFace(byte[] buf, int faceStart, uint version, List<SfntTable> baseTables,
                                    byte[] nameData, Dictionary<string, TableLoc> map)
        {
            var tags = new List<string>(baseTables.Count);
            foreach (var t in baseTables) tags.Add(t.Tag);
            tags.Sort(StringComparer.Ordinal);

            int n = tags.Count;
            int recLen = 12 + 16 * n;
            int namePos = faceStart + recLen;

            W32(buf, faceStart, version);
            W16(buf, faceStart + 4, n);
            int pow = 1, log = 0;
            while (pow * 2 <= n) { pow *= 2; log++; }
            W16(buf, faceStart + 6, pow * 16);
            W16(buf, faceStart + 8, log);
            W16(buf, faceStart + 10, n * 16 - pow * 16);

            for (int i = 0; i < n; i++)
            {
                string tag = tags[i];
                int rec = faceStart + 12 + i * 16;
                Buffer.BlockCopy(Encoding.ASCII.GetBytes(tag), 0, buf, rec, 4);
                if (tag == "name")
                {
                    W32(buf, rec + 4, TableChecksum(nameData));
                    W32(buf, rec + 8, (uint)namePos);
                    W32(buf, rec + 12, (uint)nameData.Length);
                    Buffer.BlockCopy(nameData, 0, buf, namePos, nameData.Length);
                }
                else
                {
                    var loc = map[tag];
                    W32(buf, rec + 4, loc.Checksum);
                    W32(buf, rec + 8, (uint)loc.Offset);
                    W32(buf, rec + 12, loc.Length);
                }
            }
        }


        public static bool Verify(byte[] ttc, NameSpec[] expected, out string error)
        {
            error = null;
            try
            {
                var faces = FaceOffsets(ttc);
                if (faces.Length != expected.Length) { error = "face 数量不对：" + faces.Length; return false; }
                for (int i = 0; i < faces.Length; i++)
                {
                    string fam, sub, ver, full; int wt;
                    if (!TryReadNames(ttc, faces[i], out fam, out sub, out ver, out full, out wt))
                    { error = "第 " + (i + 1) + " 个 face 读不出名字"; return false; }
                    if (fam != expected[i].Family) { error = "名字不符：" + fam + " != " + expected[i].Family; return false; }
                    var tables = ReadTables(ttc, faces[i]);
                    if (tables.Find(t => t.Tag == "cmap") == null) { error = "缺少 cmap"; return false; }
                    if (tables.Find(t => t.Tag == "glyf") == null) { error = "缺少 glyf"; return false; }
                }
                return true;
            }
            catch (Exception ex) { error = ex.Message; return false; }
        }
    }
}
