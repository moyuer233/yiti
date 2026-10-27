using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using Microsoft.Win32;

namespace WinFontSwap
{
    public sealed class TargetInfo
    {
        public string FileName;
        public string Label;
        public bool Exists;
        public long Size;
        public string Family;
        public string Version;
        public string Sha;
        public bool Applied;
        public bool Queued;
    }

    public sealed class FontSource
    {
        public string TargetFile;
        public string SourcePath;
        public int FaceIndex;
        public string Family;
        public string Subfamily;
        public string Version;
        public byte[] Data;
    }

    public static class Installer
    {
        public static readonly string FontsDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");

        public static readonly string DataDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WinFontSwap");

        public static string BackupRoot { get { return Path.Combine(DataDir, "backup"); } }
        public static string StageRoot { get { return Path.Combine(DataDir, "stage"); } }
        public static string StatePath { get { return Path.Combine(DataDir, "state.ini"); } }

        public const string SessionManagerKey = @"SYSTEM\CurrentControlSet\Control\Session Manager";

        public static readonly string[] TargetFiles = { "msyh.ttc", "msyhbd.ttc", "msyhl.ttc", "SegUIVar.ttf", "segoeui.ttf" };

        /// <summary>单 face 目标（静态 Segoe UI）。</summary>
        public static bool IsSingleFace(string file) { return file == "segoeui.ttf"; }

        /// <summary>多 face 且要保留可变轴的目标（Segoe UI Variable：Variable/Text/Display/Small）。</summary>
        public static bool IsVariableTtc(string file) { return file == "SegUIVar.ttf"; }

        /// <summary>目标文件与界面行一一对应（0 常规 / 1 粗体 / 2 细体 / 3 英文数字 / 4 Segoe UI）。</summary>
        public static int SlotOf(int targetIndex)
        {
            return targetIndex;
        }

        public static string LabelOf(string file)
        {
            switch (file)
            {
                case "msyh.ttc": return "常规";
                case "msyhbd.ttc": return "粗体";
                case "msyhl.ttc": return "细体";
                case "SegUIVar.ttf": return "英文数字";
                case "segoeui.ttf": return "英文数字";
                default: return file;
            }
        }

        /// <summary>目标文件里要写进去的两个 face 的名字（微软雅黑 + 微软雅黑 UI）。</summary>
        public static NameSpec[] SpecsFor(string file)
        {
            // WinUI 请求的是 Segoe UI Variable Text/Display/Small 这些子族名，四个族名必须写进同一个文件
            if (file == "SegUIVar.ttf")
            {
                string[] names = { "Segoe UI Variable", "Segoe UI Variable Text", "Segoe UI Variable Display", "Segoe UI Variable Small" };
                var uiSpecs = new List<NameSpec>(names.Length);
                foreach (var n in names)
                {
                    uiSpecs.Add(new NameSpec
                    {
                        Family = n,
                        FamilyTypo = n,
                        Subfamily = "Regular",
                        SubfamilyTypo = "Regular",
                        FullName = n,
                        PostScript = n.Replace(" ", "")
                    });
                }
                return uiSpecs.ToArray();
            }
            if (file == "segoeui.ttf")
            {
                return new[] { new NameSpec
                {
                    Family = "Segoe UI",
                    FamilyTypo = "Segoe UI",
                    Subfamily = "Regular",
                    SubfamilyTypo = "Regular",
                    FullName = "Segoe UI",
                    PostScript = "SegoeUI"
                } };
            }
            bool ui = false;
            var list = new List<NameSpec>(2);
            for (int i = 0; i < 2; i++)
            {
                ui = i == 1;
                switch (file)
                {
                    case "msyh.ttc":
                        list.Add(new NameSpec
                        {
                            Family = ui ? "Microsoft YaHei UI" : "Microsoft YaHei",
                            FamilyTypo = ui ? "Microsoft YaHei UI" : "Microsoft YaHei",
                            Subfamily = "Regular",
                            SubfamilyTypo = "Regular",
                            FullName = ui ? "Microsoft YaHei UI" : "Microsoft YaHei",
                            PostScript = ui ? "MicrosoftYaHeiUI" : "MicrosoftYaHei"
                        });
                        break;
                    case "msyhbd.ttc":
                        list.Add(new NameSpec
                        {
                            Family = ui ? "Microsoft YaHei UI" : "Microsoft YaHei",
                            FamilyTypo = ui ? "Microsoft YaHei UI" : "Microsoft YaHei",
                            Subfamily = "Bold",
                            SubfamilyTypo = "Bold",
                            FullName = (ui ? "Microsoft YaHei UI" : "Microsoft YaHei") + " Bold",
                            PostScript = (ui ? "MicrosoftYaHeiUI" : "MicrosoftYaHei") + "-Bold"
                        });
                        break;
                    default: // msyhl.ttc
                        list.Add(new NameSpec
                        {
                            Family = ui ? "Microsoft YaHei UI Light" : "Microsoft YaHei Light",
                            FamilyTypo = ui ? "Microsoft YaHei UI" : "Microsoft YaHei",
                            Subfamily = "Regular",
                            SubfamilyTypo = "Light",
                            FullName = ui ? "Microsoft YaHei UI Light" : "Microsoft YaHei Light",
                            PostScript = ui ? "MicrosoftYaHeiUILight" : "MicrosoftYaHeiLight"
                        });
                        break;
                }
            }
            return list.ToArray();
        }

        public static bool IsAdmin
        {
            get
            {
                using (var id = WindowsIdentity.GetCurrent())
                    return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static string Sha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var fs = File.OpenRead(path))
            {
                var hash = sha.ComputeHash(fs);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        static readonly Dictionary<string, string> _srcCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, string> _glyfCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>取某个字体 face 的 glyf 表哈希：我们只改名字不动字形，所以它能精确指认来源字体。</summary>
        static string GlyfHash(string path, int faceIndex)
        {
            var key = path + "#" + faceIndex;
            string cached;
            if (_glyfCache.TryGetValue(key, out cached)) return cached;
            string hash = null;
            try
            {
                var data = File.ReadAllBytes(path);
                var faces = Sfnt.FaceOffsets(data);
                if (faceIndex < faces.Length) hash = Sfnt.TableHash(data, faces[faceIndex], "glyf");
            }
            catch { }
            _glyfCache[key] = hash;
            return hash;
        }

        /// <summary>
        /// 反查“这个系统字体文件现在装的是哪款字体”：我们只改写 family/subfamily 那几个 nameID，
        /// 版权/版本/商标（0/5/7）仍是原字体的，拿它去和本机字体逐个比对即可指认来源。
        /// </summary>
        public static string DetectSourceFamily(string fileName)
        {
            string cached;
            if (_srcCache.TryGetValue(fileName, out cached)) return cached;
            string result = null;
            try
            {
                var target = Path.Combine(FontsDir, fileName);
                if (!File.Exists(target)) return null;
                var data = File.ReadAllBytes(target);
                var faces = Sfnt.FaceOffsets(data);
                if (faces.Length == 0) return null;
                var ids = new[] { 0, 5, 7 };
                var mine = Sfnt.NameStrings(data, faces[0], ids);
                if (string.IsNullOrEmpty(mine[0]) && string.IsNullOrEmpty(mine[1]) && string.IsNullOrEmpty(mine[2])) return null;

                var self = new List<string>();
                string matchedFamily = null;
                int matched = 0;
                foreach (var f in FontLibrary.All)
                {
                    try
                    {
                        var candName = Path.GetFileName(f.Path);
                        if (string.Equals(candName, fileName, StringComparison.OrdinalIgnoreCase)) continue;
                        // 我们自己换出来的兄弟目标文件版权字段完全一样，不能当成来源
                        bool isTarget = false;
                        foreach (var t in TargetFiles) if (string.Equals(candName, t, StringComparison.OrdinalIgnoreCase)) { isTarget = true; break; }
                        if (isTarget) continue;
                        var cand = File.ReadAllBytes(f.Path);
                        var cf = Sfnt.FaceOffsets(cand);
                        if (f.FaceIndex >= cf.Length) continue;
                        var other = Sfnt.NameStrings(cand, cf[f.FaceIndex], ids);
                        bool same = true;
                        int compared = 0;
                        for (int i = 0; i < ids.Length; i++)
                        {
                            if (string.IsNullOrEmpty(mine[i]) || string.IsNullOrEmpty(other[i])) continue;
                            compared++;
                            if (!string.Equals(mine[i], other[i], StringComparison.Ordinal)) { same = false; break; }
                        }
                        if (same && compared > 0) { matched++; matchedFamily = f.Family; if (matched > 1) break; }
                    }
                    catch { }
                }
                // 只有唯一命中才算认出来；多个候选说明是通用字段（如 Version 1.0），不敢下结论
                if (matched == 1) result = matchedFamily;
            }
            catch { }
            // 再用字形表哈希兜底（版权字段可能缺失，但字形一定在）
            if (string.IsNullOrEmpty(result))
            {
                try
                {
                    var target = Path.Combine(FontsDir, fileName);
                    if (File.Exists(target))
                    {
                        var mineHash = GlyfHash(target, 0);
                        if (!string.IsNullOrEmpty(mineHash))
                        {
                            foreach (var f in FontLibrary.All)
                            {
                                var candName = Path.GetFileName(f.Path);
                                if (string.Equals(candName, fileName, StringComparison.OrdinalIgnoreCase)) continue;
                                bool isTarget = false;
                                foreach (var t in TargetFiles) if (string.Equals(candName, t, StringComparison.OrdinalIgnoreCase)) { isTarget = true; break; }
                                if (isTarget) continue;
                                if (GlyfHash(f.Path, f.FaceIndex) == mineHash)
                                {
                                    result = f.Family;
                                    if (!string.IsNullOrEmpty(f.Subfamily) && f.Subfamily != "Regular" && f.Family.IndexOf(f.Subfamily, StringComparison.OrdinalIgnoreCase) < 0)
                                        result = f.Family + " " + f.Subfamily;
                                    break;
                                }
                            }
                        }
                    }
                }
                catch { }
            }
            _srcCache[fileName] = result;
            return result;
        }

        static readonly Dictionary<string, string> _vendorCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 从字体文件里没被改写的版权字段判断“这是谁家的字体”（Huawei→华为 等）。
        /// 精确族名无法从文件反推（我们把 family 全改成微软了），但版权方名字保留着。
        /// </summary>
        public static string DetectSourceVendor(string fileName)
        {
            string cached;
            if (_vendorCache.TryGetValue(fileName, out cached)) return cached;
            string result = null;
            try
            {
                var target = Path.Combine(FontsDir, fileName);
                if (!File.Exists(target)) return null;
                var data = File.ReadAllBytes(target);
                var faces = Sfnt.FaceOffsets(data);
                if (faces.Length == 0) return null;
                var ids = Sfnt.NameStrings(data, faces[0], new[] { 0, 9 });
                var text = ((ids[0] ?? "") + " " + (ids[1] ?? "")).ToLowerInvariant();
                if (text.Contains("huawei")) result = "华为";
                else if (text.Contains("xiaomi")) result = "小米";
                else if (text.Contains("founder")) result = "方正";
                else if (text.Contains("hanyi")) result = "汉仪";
                else if (text.Contains("monotype")) result = "Monotype";
                else if (text.Contains("google")) result = "Google";
                else if (text.Contains("adobe")) result = "Adobe";
            }
            catch { }
            _vendorCache[fileName] = result;
            return result;
        }

        public static List<TargetInfo> Inspect()
        {
            var state = ReadState();
            var res = new List<TargetInfo>();
            foreach (var f in TargetFiles)
            {
                var info = new TargetInfo { FileName = f, Label = LabelOf(f) };
                var p = Path.Combine(FontsDir, f);
                info.Exists = File.Exists(p);
                if (info.Exists)
                {
                    var fi = new FileInfo(p);
                    info.Size = fi.Length;
                    try
                    {
                        var data = File.ReadAllBytes(p);
                        var faces = Sfnt.FaceOffsets(data);
                        string fam, sub, ver, full; int wt;
                        if (Sfnt.TryReadNames(data, faces[0], out fam, out sub, out ver, out full, out wt))
                        {
                            info.Family = fam;
                            info.Version = ver;
                        }
                        info.Sha = ShaHex(data);
                    }
                    catch { }
                    string appliedSha;
                    if (state.TryGetValue("sha_" + f, out appliedSha) && appliedSha == info.Sha) info.Applied = true;
                }
                info.Queued = IsQueuedFor(f);
                res.Add(info);
            }
            return res;
        }

        static string ShaHex(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        // ---------- 状态文件 ----------

        public static Dictionary<string, string> ReadState()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!File.Exists(StatePath)) return d;
                foreach (var line in File.ReadAllLines(StatePath, Encoding.UTF8))
                {
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    d[line.Substring(0, i)] = line.Substring(i + 1);
                }
            }
            catch { }
            return d;
        }

        static void WriteState(Dictionary<string, string> d)
        {
            Directory.CreateDirectory(DataDir);
            var lines = new List<string>();
            foreach (var kv in d) lines.Add(kv.Key + "=" + kv.Value);
            File.WriteAllLines(StatePath, lines.ToArray(), Encoding.UTF8);
        }

        static void SetState(string key, string value)
        {
            var d = ReadState();
            if (value == null) d.Remove(key); else d[key] = value;
            WriteState(d);
        }

        // ---------- 队列 ----------

        public static bool IsQueuedFor(string file)
        {
            try
            {
                using (var k = Registry.LocalMachine.OpenSubKey(SessionManagerKey))
                {
                    var arr = k == null ? null : k.GetValue("PendingFileRenameOperations") as string[];
                    if (arr == null) return false;
                    foreach (var s in arr)
                        if (s != null && s.IndexOf("\\Fonts\\" + file, StringComparison.OrdinalIgnoreCase) >= 0) return true;
                }
            }
            catch { }
            return false;
        }

        static List<string[]> ReadQueueOrDefault()
        {
            var pairs = new List<string[]>();
            using (var k = Registry.LocalMachine.OpenSubKey(SessionManagerKey))
            {
                if (k == null) return pairs;
                var arr = k.GetValue("PendingFileRenameOperations") as string[];
                if (arr == null) return pairs;
                for (int i = 0; i + 1 < arr.Length; i += 2) pairs.Add(new[] { arr[i], arr[i + 1] });
            }
            return pairs;
        }

        static string Prefixed(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.IndexOf(@"\??\", StringComparison.Ordinal) >= 0) return path;   // 已有本机路径前缀（含 Windows 的 *1\??\ 形式）
            return @"\??\" + path;
        }

        static void WriteQueue(List<string[]> pairs)
        {
            using (var k = Registry.LocalMachine.OpenSubKey(SessionManagerKey, true))
            {
                if (k == null) throw new InvalidOperationException("无法打开 Session Manager 注册表键");
                var list = new List<string>();
                foreach (var p in pairs)
                {
                    list.Add(Prefixed(p[0]));
                    list.Add(string.IsNullOrEmpty(p[1]) ? "" : Prefixed(p[1]));
                }
                if (list.Count == 0) k.DeleteValue("PendingFileRenameOperations", false);
                else k.SetValue("PendingFileRenameOperations", list.ToArray(), RegistryValueKind.MultiString);
            }
        }

        static void RemoveQueueEntries(params string[] files)
        {
            var pairs = ReadQueueOrDefault();
            pairs.RemoveAll(p => IsOurs(p[0]) || IsOurs(p[1]));
            WriteQueue(pairs);
        }

        /// <summary>这条队列记录是不是本工具写的（源/目标沾到 WinFontSwap 或目标字体名）。</summary>
        static bool IsOurs(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            if (s.IndexOf("WinFontSwap", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            foreach (var f in TargetFiles)
                if (s.EndsWith("\\" + f, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        static void AddQueuePairs(List<string[]> pairs)
        {
            var all = ReadQueueOrDefault();
            all.AddRange(pairs);
            WriteQueue(all);
        }

        // ---------- 权限 ----------

        [StructLayout(LayoutKind.Sequential)]
        struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID Luid; public uint Attributes; }

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool OpenProcessToken(IntPtr h, uint access, out IntPtr token);
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern bool LookupPrivilegeValue(string host, string name, out LUID luid);
        [DllImport("advapi32.dll", SetLastError = true)]
        static extern bool AdjustTokenPrivileges(IntPtr token, bool disableAll, ref TOKEN_PRIVILEGES state,
                                                 uint len, IntPtr prev, IntPtr ret);
        [DllImport("kernel32.dll")]
        static extern IntPtr GetCurrentProcess();
        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr h);

        public static void EnablePrivileges()
        {
            EnablePrivilege("SeTakeOwnershipPrivilege");
            EnablePrivilege("SeRestorePrivilege");
            EnablePrivilege("SeBackupPrivilege");
            EnablePrivilege("SeSecurityPrivilege");
        }

        static void EnablePrivilege(string name)
        {
            IntPtr token;
            if (!OpenProcessToken(GetCurrentProcess(), 0x28 /*ADJUST|QUERY*/, out token)) return;
            try
            {
                LUID luid;
                if (LookupPrivilegeValue(null, name, out luid))
                {
                    var tp = new TOKEN_PRIVILEGES { PrivilegeCount = 1, Luid = luid, Attributes = 0x2 };
                    AdjustTokenPrivileges(token, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
                }
            }
            finally { CloseHandle(token); }
        }

        internal static string SddlOf(string path)
        {
            var sec = File.GetAccessControl(path, AccessControlSections.Access | AccessControlSections.Owner);
            return sec.GetSecurityDescriptorSddlForm(AccessControlSections.Access | AccessControlSections.Owner);
        }

        /// <summary>
        /// 还原 ACL。DACL 用 .NET 写；所有者交给 icacls（.NET 设外部账户所有者需要额外特权，容易被拒）。
        /// </summary>
        internal static void ApplySddl(string path, string sddl)
        {
            int di = sddl.IndexOf("D:", StringComparison.Ordinal);
            string dacl = di > 0 ? sddl.Substring(di) : sddl;
            var sec = File.GetAccessControl(path, AccessControlSections.Access);
            sec.SetSecurityDescriptorSddlForm(dacl, AccessControlSections.Access);
            File.SetAccessControl(path, sec);

            string ownerSid = null;
            if (sddl.StartsWith("O:", StringComparison.Ordinal))
            {
                int end = sddl.IndexOf("G:", 2, StringComparison.Ordinal);
                if (end < 0) end = sddl.IndexOf("D:", 2, StringComparison.Ordinal);
                if (end > 2) ownerSid = sddl.Substring(2, end - 2);
            }
            if (!string.IsNullOrEmpty(ownerSid))
            {
                var psi = new ProcessStartInfo("icacls", "\"" + path + "\" /setowner \"*" + ownerSid + "\"");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (var p = Process.Start(psi))
                {
                    p.StandardOutput.ReadToEnd();
                    p.StandardError.ReadToEnd();
                    p.WaitForExit();
                }
            }
        }

        /// <summary>接管文件：把所有者改成 Administrators 并给完全控制，返回原始 SDDL。</summary>
        internal static string TakeControl(string path, Action<string> log)
        {
            string sddl = SddlOf(path);
            var sec = File.GetAccessControl(path, AccessControlSections.Access | AccessControlSections.Owner);
            var admins = new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null);
            sec.SetOwner(admins);
            sec.AddAccessRule(new FileSystemAccessRule(admins, FileSystemRights.FullControl, AccessControlType.Allow));
            File.SetAccessControl(path, sec);
            return sddl;
        }

        /// <summary>取 SDDL 里的 DACL 部分（D: 之后）。</summary>
        static string DaclOf(string sddl)
        {
            int di = sddl.IndexOf("D:", StringComparison.Ordinal);
            return di >= 0 ? sddl.Substring(di) : sddl;
        }

        /// <summary>原样沿用系统字体的 DACL，再补一条 SYSTEM 完全控制（SMSS 重启时要能移动它）。</summary>
        static string WithSystemFull(string sddl)
        {
            return DaclOf(sddl) + "(A;;FA;;;SY)";
        }

        /// <summary>接管后再还原 ACL：目标可能已经换成新字体（只剩 RX），直接 SetAccessControl 会没权限。</summary>
        internal static void RestoreAcl(string path, string sddl)
        {
            try { TakeControl(path, null); } catch { }
            ApplySddl(path, sddl);
        }

        /// <summary>让 SYSTEM 能移动该文件（重启队列由 SMSS 以 SYSTEM 身份执行）。</summary>
        internal static void GrantSystem(string path)
        {
            var sec = File.GetAccessControl(path, AccessControlSections.Access);
            var sys = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            sec.AddAccessRule(new FileSystemAccessRule(sys, FileSystemRights.FullControl, AccessControlType.Allow));
            File.SetAccessControl(path, sec);
        }

        // ---------- 生成 ----------

        public sealed class BuildResult
        {
            public List<string> BuiltFiles = new List<string>();
            public string OutputDir;
        }

        /// <summary>
        /// 删掉 HKCU 里与本工具目标同名的字体注册。
        /// DWrite（WinUI / Edge / 设置 / 开始菜单）优先使用用户级字体，不删这些条目，替换 C:\Windows\Fonts 完全无效。
        /// </summary>
        static int ClearUserFontShadows(string backupDir, Action<string> log)
        {
            const string sub = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts";
            using (var key = Registry.CurrentUser.OpenSubKey(sub, true))
            {
                if (key == null) return 0;
                var hit = new List<string>();
                foreach (var n in key.GetValueNames())
                {
                    var lower = n.ToLowerInvariant();
                    if (lower.Contains("yahei") || lower.Contains("雅黑") || lower.StartsWith("segoe ui"))
                        hit.Add(n);
                }
                if (hit.Count == 0) return 0;

                try
                {
                    var reg = Path.Combine(backupDir, "HKCU-Fonts.reg");
                    var psi = new ProcessStartInfo("reg.exe",
                        "export \"HKCU\\SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\Fonts\" \"" + reg + "\" /y");
                    psi.CreateNoWindow = true;
                    psi.UseShellExecute = false;
                    var p = Process.Start(psi);
                    if (p != null) p.WaitForExit(10000);
                }
                catch { }

                int done = 0;
                foreach (var n in hit)
                {
                    try { key.DeleteValue(n, false); done++; }
                    catch { }
                }
                log("已移除 " + done + " 条用户级字体注册（DWrite 优先读用户级字体，不删它们界面不会跟随）");
                return done;
            }
        }

        /// <summary>把最近一次导出过的 HKCU 字体注册备份导回去（恢复用户级字体）。</summary>
        public static string RestoreUserFonts()
        {
            if (!Directory.Exists(BackupRoot)) return "没有备份";
            string newest = null;
            DateTime newestTime = DateTime.MinValue;
            foreach (var f in Directory.GetFiles(BackupRoot, "HKCU-Fonts*.reg", SearchOption.AllDirectories))
            {
                var t = File.GetLastWriteTime(f);
                if (t > newestTime) { newestTime = t; newest = f; }
            }
            if (newest == null) return "没有备份";
            var psi = new ProcessStartInfo("reg.exe", "import \"" + newest + "\"") { CreateNoWindow = true, UseShellExecute = false };
            var p = Process.Start(psi);
            if (p != null) p.WaitForExit(15000);
            return "已恢复：" + Path.GetFileName(newest);
        }

        /// <summary>清字体缓存，让 DWrite/WinUI 重启后按新文件重建（失败只记日志，不阻断）。</summary>
        static void ClearFontCaches(Action<string> log)
        {
            try
            {
                var psi = new ProcessStartInfo("sc.exe", "stop FontCache") { CreateNoWindow = true, UseShellExecute = false };
                var p = Process.Start(psi);
                if (p != null) p.WaitForExit(8000);
                System.Threading.Thread.Sleep(800);

                var fnt = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "System32", "FNTCACHE.DAT");
                try { if (File.Exists(fnt)) File.Delete(fnt); } catch { }

                var userCache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "FontCache");
                try { if (Directory.Exists(userCache)) Directory.Delete(userCache, true); } catch { }

                var psi2 = new ProcessStartInfo("sc.exe", "start FontCache") { CreateNoWindow = true, UseShellExecute = false };
                var p2 = Process.Start(psi2);
                if (p2 != null) p2.WaitForExit(8000);
                log("已清字体缓存（重启后由系统重建）");
            }
            catch (Exception ex)
            {
                log("清字体缓存失败（不影响替换）：" + ex.Message);
            }
        }

        static string NewStamp()
        {
            return DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public static BuildResult BuildFonts(IList<FontSource> sources, string outDir, Action<string> log)
        {
            Directory.CreateDirectory(outDir);
            var result = new BuildResult { OutputDir = outDir };
            foreach (var src in sources)
            {
                var specs = SpecsFor(src.TargetFile);
                log("生成 " + src.TargetFile + "（源：" + Path.GetFileName(src.SourcePath) + "）");
                byte[] data;
                if (IsVariableTtc(src.TargetFile))
                {
                    if (Sfnt.IsVariable(src.Data, src.FaceIndex))
                        log("  可变字体：保留字重轴，并写入 " + specs.Length + " 个族名（Variable/Text/Display/Small）");
                    else
                        log("  注意：源不是可变字体，英文数字的字重会变单一");
                    data = Sfnt.BuildCollection(src.Data, src.FaceIndex, specs, true);
                }
                else if (IsSingleFace(src.TargetFile))
                {
                    if (Sfnt.IsVariable(src.Data, src.FaceIndex))
                        log("  可变字体：取默认字重做成静态字体");
                    data = Sfnt.BuildSingle(src.Data, src.FaceIndex, specs[0], false);
                }
                else
                {
                    if (Sfnt.IsVariable(src.Data, src.FaceIndex))
                        log("  注意：源是可变字体，会取它的默认字重（通常 400，相当于常规体）");
                    data = Sfnt.BuildCollection(src.Data, src.FaceIndex, specs);
                }
                string err;
                if (!Sfnt.Verify(data, specs, out err))
                    throw new InvalidDataException(src.TargetFile + " 校验失败：" + err);
                var outPath = Path.Combine(outDir, src.TargetFile);
                File.WriteAllBytes(outPath, data);
                VerifyLoadable(outPath, log);
                result.BuiltFiles.Add(outPath);
                log("  " + src.TargetFile + " 已生成 " + (data.Length / 1024.0 / 1024.0).ToString("0.0") + " MB");
            }
            return result;
        }

        static void VerifyLoadable(string path, Action<string> log)
        {
            try
            {
                using (var pfc = new PrivateFontCollection())
                {
                    pfc.AddFontFile(path);
                    if (pfc.Families.Length == 0) throw new InvalidDataException("系统无法加载生成的字体");
                    var names = new List<string>();
                    foreach (var fam in pfc.Families) names.Add(fam.Name);
                    log("  系统可加载：" + string.Join(" / ", names.ToArray()));
                }
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("生成的字体系统加载失败：" + ex.Message);
            }
        }

        // ---------- 应用 ----------

        public sealed class ApplyResult
        {
            public bool Queued;
            public string BackupDir;
            public string OutputDir;
            public string Message;
        }

        public static ApplyResult Apply(IList<FontSource> sources, Action<string> log, bool generateOnly)
        {
            return Apply(sources, log, generateOnly, true, true);
        }

        public static ApplyResult Apply(IList<FontSource> sources, Action<string> log, bool generateOnly, bool clearCaches, bool clearUserFonts)
        {
            if (!IsAdmin) throw new InvalidOperationException("需要管理员权限");
            if (sources == null || sources.Count == 0) throw new InvalidOperationException("没有选择字体");

            var stamp = NewStamp();
            var stageDir = Path.Combine(StageRoot, stamp);
            var build = BuildFonts(sources, stageDir, log);

            var result = new ApplyResult { BackupDir = Path.Combine(BackupRoot, stamp), OutputDir = stageDir };

            if (generateOnly)
            {
                result.Message = "已生成到 " + stageDir;
                return result;
            }

            EnablePrivileges();
            Directory.CreateDirectory(result.BackupDir);
            if (clearUserFonts)
            {
                try { ClearUserFontShadows(result.BackupDir, log); }
                catch (Exception ex) { log("清理用户级字体注册失败：" + ex.Message); }
            }

            var state = ReadState();
            state["stamp"] = stamp;
            var applied = new List<string>();
            var pairs = new List<string[]>();
            // 记录每个目标写进去的是哪个字体族，界面才能显示“已替换为 X”
            var srcFamily = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in sources)
            {
                if (s != null && !string.IsNullOrEmpty(s.TargetFile)) srcFamily[s.TargetFile] = s.Family == null ? "" : s.Family;
            }
            // 安全副本放 orig\ 子目录；backup\<文件> 这个路径要留给「重启时把原文件改名搬进来」，
            // 那个目标路径必须不存在（MOVEFILE_DELAY_UNTIL_REBOOT 不能覆盖已存在的目标）
            var safetyDir = Path.Combine(result.BackupDir, "orig");
            Directory.CreateDirectory(safetyDir);

            foreach (var path in build.BuiltFiles)
            {
                var file = Path.GetFileName(path);
                var target = Path.Combine(FontsDir, file);
                if (!File.Exists(target)) { log("跳过不存在的 " + file); continue; }

                log("备份 " + file);
                var backup = Path.Combine(result.BackupDir, file);
                File.Copy(target, Path.Combine(safetyDir, file), true);
                if (File.Exists(backup)) File.Delete(backup);   // 目标必须不存在，否则重启时改名会失败

                string sddl = TakeControl(target, log);
                GrantSystem(target);
                // 新字体直接继承系统字体的 DACL（含 UWP 需要的 ALL APPLICATION PACKAGES 读权限），再给 SYSTEM 一条完全控制
                ApplySddl(path, WithSystemFull(sddl));

                pairs.Add(new[] { target, backup });
                pairs.Add(new[] { path, target });

                state["sddl_" + file] = sddl;
                state["sha_" + file] = Sha256(path);
                string fam;
                if (srcFamily.TryGetValue(file, out fam)) state["family_" + file] = fam;
                applied.Add(file);
            }

            if (applied.Count == 0) throw new InvalidOperationException("没有可替换的目标");

            AddQueuePairs(pairs);
            if (clearCaches) ClearFontCaches(log);
            state["applied"] = string.Join(";", applied.ToArray());
            state["aclPending"] = "1";
            state["date"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            WriteState(state);

            log("已写入重启替换队列：" + string.Join("、", applied.ToArray()));
            result.Queued = true;
            result.Message = "重启后生效";
            return result;
        }

        public static void CancelQueue(Action<string> log)
        {
            if (!IsAdmin) throw new InvalidOperationException("需要管理员权限");
            EnablePrivileges();
            RemoveQueueEntries(TargetFiles);
            var state = ReadState();
            if (state.ContainsKey("aclPending"))
            {
                foreach (var f in TargetFiles)
                {
                    string sddl;
                    if (!state.TryGetValue("sddl_" + f, out sddl)) continue;
                    var p = Path.Combine(FontsDir, f);
                    try { RestoreAcl(p, sddl); log("已还原 " + f + " 权限"); }
                    catch (Exception ex) { log("还原 " + f + " 权限失败：" + ex.Message); }
                }
                state.Remove("aclPending");
                WriteState(state);
            }
            log("已取消排队");
        }

        /// <summary>重启替换完成后，把字体文件权限还原成系统原样。</summary>
        public static void FixPendingAcl(Action<string> log)
        {
            var state = ReadState();
            if (!state.ContainsKey("aclPending")) return;
            if (!IsAdmin) return;
            EnablePrivileges();
            bool allDone = true;
            foreach (var f in TargetFiles)
            {
                string sha, sddl;
                if (!state.TryGetValue("sha_" + f, out sha) || !state.TryGetValue("sddl_" + f, out sddl)) continue;
                var p = Path.Combine(FontsDir, f);
                try
                {
                    if (Sha256(p) == sha)
                    {
                        RestoreAcl(p, sddl);
                        log("已恢复 " + f + " 的文件权限");
                    }
                    else allDone = false;
                }
                catch (Exception ex) { allDone = false; log("恢复 " + f + " 权限失败：" + ex.Message); }
            }
            if (allDone)
            {
                state.Remove("aclPending");
                WriteState(state);
            }
        }

        /// <summary>
        /// 找 Windows 组件存储里的原版字体：C:\Windows\WinSxS\amd64_microsoft-windows-f..*<名字>_<版本>\<文件>。
        /// 这是最权威的原版来源（比我们自己的备份可靠——早期备份可能备份到的是已替换的文件）。
        /// </summary>
        public static string FindOriginalInWinSxS(string fileName)
        {
            try
            {
                var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "WinSxS");
                if (!Directory.Exists(root)) return null;
                // WinSxS 的目录名用的是包的“长名”（microsoftyahei / segoe_ui_variable），跟文件名不一样，得映射
                var patterns = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
                patterns["msyh.ttc"] = new[] { "microsoftyahei" };
                patterns["msyhbd.ttc"] = new[] { "microsoftyaheibold" };
                patterns["msyhl.ttc"] = new[] { "microsoftyaheilight" };
                patterns["seguiVar.ttf"] = new[] { "segoe_ui_variable" };
                patterns["segoeui.ttf"] = new[] { "segoeui" };
                string[] keys;
                if (!patterns.TryGetValue(fileName, out keys)) keys = new[] { Path.GetFileNameWithoutExtension(fileName) };

                string best = null;
                long bestVer = -1;
                foreach (var dir in Directory.GetDirectories(root))
                {
                    var dirName = Path.GetFileName(dir).ToLowerInvariant();
                    bool hit = false;
                    foreach (var k in keys) if (dirName.Contains(k.ToLowerInvariant())) { hit = true; break; }
                    if (!hit) continue;
                    if (fileName.Equals("msyh.ttc", StringComparison.OrdinalIgnoreCase) &&
                        (dirName.Contains("bold") || dirName.Contains("light"))) continue;
                    var cand = Path.Combine(dir, fileName);
                    if (!File.Exists(cand)) continue;
                    long ver = 0;
                    var m = System.Text.RegularExpressions.Regex.Match(Path.GetFileName(dir), @"_(\d+\.\d+\.\d+\.\d+)_");
                    if (m.Success)
                    {
                        var parts = m.Groups[1].Value.Split('.');
                        for (int i = 0; i < parts.Length; i++) ver = ver * 1000 + long.Parse(parts[i]);
                    }
                    if (ver > bestVer) { bestVer = ver; best = cand; }
                }
                return best;
            }
            catch { return null; }
        }

        public static string LatestBackup()
        {
            if (!Directory.Exists(BackupRoot)) return null;
            var dirs = Directory.GetDirectories(BackupRoot);
            Array.Sort(dirs, StringComparer.Ordinal);
            for (int i = dirs.Length - 1; i >= 0; i--)
                if (File.Exists(Path.Combine(dirs[i], "state.ini"))) return dirs[i];
            return dirs.Length > 0 ? dirs[dirs.Length - 1] : null;
        }

        public sealed class BackupInfo
        {
            public string Dir;
            public DateTime Date;
            public List<string> Files = new List<string>();
            public long Bytes;
        }

        public static List<BackupInfo> Backups()
        {
            var list = new List<BackupInfo>();
            if (!Directory.Exists(BackupRoot)) return list;
            foreach (var dir in Directory.GetDirectories(BackupRoot))
            {
                var info = new BackupInfo { Dir = dir };
                foreach (var f in TargetFiles)
                {
                    var p = Path.Combine(dir, f);
                    if (File.Exists(p)) { info.Files.Add(f); info.Bytes += new FileInfo(p).Length; }
                }
                if (info.Files.Count == 0) continue;
                DateTime dt;
                if (DateTime.TryParseExact(Path.GetFileName(dir), "yyyyMMdd_HHmmss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out dt)) info.Date = dt;
                else info.Date = Directory.GetLastWriteTime(dir);
                list.Add(info);
            }
            list.Sort(delegate (BackupInfo a, BackupInfo b) { return b.Date.CompareTo(a.Date); });
            return list;
        }

        public static void Rollback(Action<string> log)
        {
            var backup = LatestBackup();
            if (backup == null) throw new InvalidOperationException("找不到备份");
            RollbackFrom(backup, log);
        }

        public static void RollbackFrom(string backup, Action<string> log)
        {
            if (!IsAdmin) throw new InvalidOperationException("需要管理员权限");
            EnablePrivileges();
            if (!Directory.Exists(backup)) throw new InvalidOperationException("备份目录不存在");

            var files = new List<string>();
            foreach (var f in TargetFiles)
                if (File.Exists(Path.Combine(backup, f))) files.Add(f);
            if (files.Count == 0) throw new InvalidOperationException("备份目录是空的");

            var stage = Path.Combine(StageRoot, "rollback_" + NewStamp());
            Directory.CreateDirectory(stage);

            var state = ReadState();
            var pairs = new List<string[]>();
            foreach (var f in files)
            {
                var target = Path.Combine(FontsDir, f);
                var bak = Path.Combine(backup, f);
                var sink = Path.Combine(stage, f);

                if (File.Exists(target))
                {
                    var sddl = TakeControl(target, log);
                    GrantSystem(target);
                    pairs.Add(new[] { target, sink });
                    state["sddl_" + f] = sddl;
                    GrantSystem(bak);
                    pairs.Add(new[] { bak, target });
                }
                else
                {
                    // 目标已经没了（上次替换半途失败），没有被占用，直接拷回去最快
                    log(f + " 已不存在，直接放回备份");
                    File.Copy(bak, target, true);
                    try { RestoreAcl(target, SddlOf(bak)); } catch { }
                }

                state["sha_" + f] = Sha256(bak);
            state.Remove("family_" + f);
            }

            AddQueuePairs(pairs);
            state["applied"] = string.Join(";", files.ToArray());
            state["aclPending"] = "1";
            state["date"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            WriteState(state);
            log("回滚已排入重启队列：" + string.Join("、", files.ToArray()));
        }

        public static string HumanSize(long bytes)
        {
            if (bytes > 1024L * 1024L) return (bytes / 1024.0 / 1024.0).ToString("0.0") + " MB";
            if (bytes > 1024L) return (bytes / 1024.0).ToString("0.0") + " KB";
            return bytes + " B";
        }

        public static string ReadFontVersion(string file)
        {
            var p = Path.Combine(FontsDir, file);
            if (!File.Exists(p)) return null;
            try
            {
                var data = File.ReadAllBytes(p);
                var faces = Sfnt.FaceOffsets(data);
                string fam, sub, ver, full; int wt;
                if (Sfnt.TryReadNames(data, faces[0], out fam, out sub, out ver, out full, out wt)) return ver;
            }
            catch { }
            return null;
        }
    }
}
