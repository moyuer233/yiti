using System;
using System.IO;
using System.Windows.Forms;

namespace WinFontSwap
{
    static class Program
    {
        /// <summary>生成 Segoe UI Variable 的多 face TTC（基础 + Text/Display/Small）：--seguivars src out</summary>
        static int SegUIVarsCli(string srcPath, string outPath)
        {
            try
            {
                var data = File.ReadAllBytes(srcPath);
                var entries = FontLibrary.ReadFile(srcPath);
                int faceIndex = entries.Count > 0 ? entries[0].FaceIndex : 0;
                string[] names = { "Segoe UI Variable", "Segoe UI Variable Text", "Segoe UI Variable Display", "Segoe UI Variable Small" };
                var specs = new NameSpec[names.Length];
                for (int i = 0; i < names.Length; i++)
                {
                    specs[i] = new NameSpec
                    {
                        Family = names[i],
                        FamilyTypo = names[i],
                        Subfamily = "Regular",
                        SubfamilyTypo = "Regular",
                        FullName = names[i],
                        PostScript = names[i].Replace(" ", "")
                    };
                }
                var built = Sfnt.BuildCollection(data, faceIndex, specs, true);
                var dir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(outPath, built);
                Console.WriteLine("generated TTC " + outPath + "  " + built.Length + " bytes  faces=" + specs.Length);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("seguivars 失败：" + ex.Message);
                return 1;
            }
        }

        /// <summary>命令行把字体应用到三个目标（常规 / 粗体 / 细体，路径少于 3 个时后面沿用第一个）。</summary>
        static int ApplyCli(string[] fontPaths) { return ApplyCli(fontPaths, -1); }

        static int ApplyCli(string[] fontPaths, int onlySlot)
        {
            try
            {
                var list = new System.Collections.Generic.List<FontSource>();
                for (int i = 0; i < Installer.TargetFiles.Length; i++)
                {
                    if (onlySlot >= 0 && i != onlySlot) continue;
                    string fontPath = fontPaths[Math.Min(i, fontPaths.Length - 1)];
                    var entries = FontLibrary.ReadFile(fontPath);
                    if (entries.Count == 0) { Console.WriteLine("读不出字体：" + fontPath); return 2; }
                    var e = entries[0];
                    list.Add(new FontSource
                    {
                        TargetFile = Installer.TargetFiles[i],
                        SourcePath = fontPath,
                        FaceIndex = e.FaceIndex,
                        Family = e.Family,
                        Subfamily = e.Subfamily,
                        Data = File.ReadAllBytes(fontPath)
                    });
                }
                var res = Installer.Apply(list, delegate (string m) { Console.WriteLine(m); }, false);
                Console.WriteLine("queued=" + res.Queued);
                Console.WriteLine("backup=" + res.BackupDir);
                return res.Queued ? 0 : 3;
            }
            catch (Exception ex)
            {
                Console.WriteLine("失败：" + ex.Message);
                return 1;
            }
        }

        /// <summary>生成一个单 face 字体文件（内部用）：--gen src out family subfamily psname [keepvar]</summary>
        static int GenCli(string[] a)
        {
            try
            {
                var src = File.ReadAllBytes(a[0]);
                var entries = FontLibrary.ReadFile(a[0]);
                int faceIndex = entries.Count > 0 ? entries[0].FaceIndex : 0;
                var spec = new NameSpec
                {
                    Family = a[2],
                    FamilyTypo = a[2],
                    Subfamily = a[3],
                    SubfamilyTypo = a[3],
                    FullName = a[2] + (a[3] == "Regular" ? "" : " " + a[3]),
                    PostScript = a[4]
                };
                bool keepVar = a.Length > 5 && a[5] == "1";
                var data = Sfnt.BuildSingle(src, faceIndex, spec, keepVar);
                var dir = Path.GetDirectoryName(a[1]);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.WriteAllBytes(a[1], data);
                Console.WriteLine("generated " + a[1] + "  " + data.Length + " bytes  family=" + a[2] + " sub=" + a[3]);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("gen 失败：" + ex.Message);
                return 1;
            }
        }

        [STAThread]
        static int Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0] == "--selftest") return SelfTest.Run();
                if (args[0] == "--detect")
                {
                    foreach (var f in Installer.TargetFiles)
                    {
                        Console.WriteLine(f + "  family=" + (Installer.DetectSourceFamily(f) ?? "-") + "  vendor=" + (Installer.DetectSourceVendor(f) ?? "-"));
                    }
                    return 0;
                }
                if (args[0] == "--writetest") return SelfTest.WriteTest();
                if (args[0] == "--acltest") return SelfTest.AclTest();
                if (args[0] == "--applytest") return SelfTest.ApplyTest();
                if (args[0] == "--rendertest") return SelfTest.RenderTest();
                if (args[0] == "--generate" && args.Length >= 2)
                {
                    var list = new System.Collections.Generic.List<FontSource>();
                    var entries = FontLibrary.ReadFile(args[1]);
                    if (entries.Count == 0) { Console.WriteLine("读不出字体"); return 2; }
                    for (int i = 0; i < Installer.TargetFiles.Length; i++)
                    {
                        list.Add(new FontSource
                        {
                            TargetFile = Installer.TargetFiles[i],
                            SourcePath = args[1],
                            FaceIndex = entries[0].FaceIndex,
                            Family = entries[0].Family,
                            Subfamily = entries[0].Subfamily,
                            Data = File.ReadAllBytes(args[1])
                        });
                    }
                    var res = Installer.Apply(list, delegate (string m) { Console.WriteLine(m); }, true);
                    Console.WriteLine("out=" + res.OutputDir);
                    return 0;
                }
                if (args[0] == "--seguivars" && args.Length >= 3) return SegUIVarsCli(args[1], args[2]);
                if (args[0] == "--gen" && args.Length >= 6)
                {
                    var g = new string[args.Length - 1];
                    Array.Copy(args, 1, g, 0, args.Length - 1);
                    return GenCli(g);
                }
                if (args[0] == "--applyslot" && args.Length >= 3)
                {
                    int slot;
                    if (!int.TryParse(args[1], out slot)) { Console.WriteLine("槽位号无效"); return 2; }
                    return ApplyCli(new[] { args[2] }, slot);
                }
                if (args[0] == "--apply" && args.Length >= 2)
                {
                    var fonts = new string[args.Length - 1];
                    Array.Copy(args, 1, fonts, 0, args.Length - 1);
                    return ApplyCli(fonts);
                }
            }

            string demo = null;
            if (args != null)
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == "--demo") demo = args[i + 1];
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == "--opaque") Theme.Glass = false;
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == "--glass") Theme.Glass = true;
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == "--pick") MainForm._openPicker = true;
            if (args != null)
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == "--shot") MainForm.ShotPath = args[i + 1];
            if (args != null)
                for (int i = 0; i < args.Length - 1; i++)
                    if (args[i] == "--page") { int pg; if (int.TryParse(args[i + 1], out pg)) MainForm.StartPage = pg; }
            if (args != null)
                for (int i = 0; i < args.Length; i++)
                    if (args[i] == "--captureable") MainForm.Captureable = true;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new MainForm(demo));
                return 0;
            }
            catch (Exception ex)
            {
                TryLog(ex);
                MessageBox.Show(ex.ToString(), "字体替换 · 启动失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }

        static void TryLog(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(Installer.DataDir);
                File.AppendAllText(Path.Combine(Installer.DataDir, "error.log"),
                    "[" + DateTime.Now.ToString("s") + "] " + ex + Environment.NewLine);
            }
            catch { }
        }
    }
}
