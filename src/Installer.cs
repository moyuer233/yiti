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

        public static readonly string[] TargetFiles = { "msyh.ttc", "msyhbd.ttc", "msyhl.ttc" };

        public static string LabelOf(string file)
        {
            switch (file)
            {
                case "msyh.ttc": return "常规";
                case "msyhbd.ttc": return "粗体";
                case "msyhl.ttc": return "细体";
                default: return file;
            }
        }

        /// <summary>目标文件里要写进去的两个 face 的名字（微软雅黑 + 微软雅黑 UI）。</summary>
        public static NameSpec[] SpecsFor(string file)
        {
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
                var data = Sfnt.BuildCollection(src.Data, src.FaceIndex, specs);
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

            var state = ReadState();
            state["stamp"] = stamp;
            var applied = new List<string>();
            var pairs = new List<string[]>();

            foreach (var path in build.BuiltFiles)
            {
                var file = Path.GetFileName(path);
                var target = Path.Combine(FontsDir, file);
                if (!File.Exists(target)) { log("跳过不存在的 " + file); continue; }

                log("备份 " + file);
                var backup = Path.Combine(result.BackupDir, file);
                File.Copy(target, backup, true);

                string sddl = TakeControl(target, log);
                GrantSystem(target);
                // 新字体直接继承系统字体的 DACL（含 UWP 需要的 ALL APPLICATION PACKAGES 读权限），再给 SYSTEM 一条完全控制
                ApplySddl(path, WithSystemFull(sddl));

                pairs.Add(new[] { target, backup });
                pairs.Add(new[] { path, target });

                state["sddl_" + file] = sddl;
                state["sha_" + file] = Sha256(path);
                applied.Add(file);
            }

            if (applied.Count == 0) throw new InvalidOperationException("没有可替换的目标");

            AddQueuePairs(pairs);
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
