using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace WinFontSwap
{
    /// <summary>无界面自检：证明字体引擎能产出 Windows 认的 TTC。</summary>
    static class SelfTest
    {
        static readonly StringBuilder Log = new StringBuilder();
        static int _fail;

        public static int Run()
        {
            Line("== 字体引擎自检 ==");
            Check("用系统 msyh.ttc 重建 TTC（常规）", () =>
            {
                var src = File.ReadAllBytes(Path.Combine(Installer.FontsDir, "msyh.ttc"));
                var specs = Installer.SpecsFor("msyh.ttc");
                var data = Sfnt.BuildCollection(src, 0, specs);
                string err;
                if (!Sfnt.Verify(data, specs, out err)) throw new Exception("校验失败：" + err);
                var faces = Sfnt.FaceOffsets(data);
                if (faces.Length != 2) throw new Exception("face 数不是 2");
                var p = TempPath("selftest_regular.ttc");
                File.WriteAllBytes(p, data);
                LoadCheck(p);
                Line("  产物 " + (data.Length / 1024.0 / 1024.0).ToString("0.00") + " MB，face=" + faces.Length);
            });

            Check("用 Deng.ttf 生成 常规/粗体/细体", () =>
            {
                var src = File.ReadAllBytes(Path.Combine(Installer.FontsDir, "Deng.ttf"));
                foreach (var f in Installer.TargetFiles)
                {
                    var specs = Installer.SpecsFor(f);
                    var data = Sfnt.BuildCollection(src, 0, specs);
                    string err;
                    if (!Sfnt.Verify(data, specs, out err)) throw new Exception(f + " 校验失败：" + err);
                    var p = TempPath("selftest_" + f);
                    File.WriteAllBytes(p, data);
                    LoadCheck(p);
                    Line("  " + f + " -> " + (data.Length / 1024.0 / 1024.0).ToString("0.00") + " MB");
                }
            });

            Check("名字落到正确的家族", () =>
            {
                var src = File.ReadAllBytes(Path.Combine(Installer.FontsDir, "msyh.ttc"));
                var data = Sfnt.BuildCollection(src, 0, Installer.SpecsFor("msyhl.ttc"));
                var faces = Sfnt.FaceOffsets(data);
                string fam, sub, ver, full; int wt;
                Sfnt.TryReadNames(data, faces[0], out fam, out sub, out ver, out full, out wt);
                if (fam != "Microsoft YaHei Light") throw new Exception("细体家族名不对：" + fam);
                Sfnt.TryReadNames(data, faces[1], out fam, out sub, out ver, out full, out wt);
                if (fam != "Microsoft YaHei UI Light") throw new Exception("细体 UI 家族名不对：" + fam);
            });

            Line(_fail == 0 ? "SELFTEST OK" : "SELFTEST FAILED: " + _fail);
            try { File.WriteAllText(@"H:\1\WinFontSwap\bin\selftest.log", Log.ToString(), Encoding.UTF8); } catch { }
            Console.WriteLine(Log.ToString());
            return _fail;
        }

        public static int WriteTest()
        {
            Line("user=" + Environment.UserName);
            Line("admin=" + Installer.IsAdmin);
            foreach (var p in new[] {
                Path.Combine(Path.GetTempPath(), "wfs_writetest.txt"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "wfs_writetest.txt"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "wfs_writetest.txt"),
                @"C:\\Windows\\Temp\\wfs_writetest.txt",
                @"H:\\1\\WinFontSwap\\bin\\wfs_writetest.txt" })
            {
                try { File.WriteAllText(p, "hello"); Line("OK   " + p); }
                catch (Exception ex) { Line("FAIL " + p + " -> " + ex.GetType().Name); }
            }
            try { File.WriteAllText(@"H:\1\WinFontSwap\bin\writetest.log", Log.ToString(), Encoding.UTF8); } catch { }
            Console.WriteLine(Log.ToString());
            return 0;
        }

        public static int AclTest()
        {
            Line("== 权限接管自检 ==");
            var p = Path.Combine(Installer.FontsDir, "msyh.ttc");
            string before = Installer.SddlOf(p);
            Line("原 SDDL: " + before);
            try
            {
                Installer.EnablePrivileges();
                var saved = Installer.TakeControl(p, Line);
                Installer.GrantSystem(p);
                string mid = Installer.SddlOf(p);
                Line("接管后: " + mid);
                if (mid == before) throw new Exception("接管没有生效");
                Installer.ApplySddl(p, saved);
                string after = Installer.SddlOf(p);
                Line("还原后: " + after);
                if (after != before) throw new Exception("还原后 SDDL 与原始不一致");
                Line("[OK]   权限接管 / 还原");
            }
            catch (Exception ex) { _fail++; Line("[FAIL] 权限接管 -> " + ex.Message); }

            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WinFontSwap");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "acltest.txt"), "ok");
                Line("[OK]   ProgramData 可写：" + dir);
            }
            catch (Exception ex) { _fail++; Line("[FAIL] ProgramData 写入 -> " + ex.Message); }

            Line(_fail == 0 ? "ACLTEST OK" : "ACLTEST FAILED: " + _fail);
            try { File.WriteAllText(@"H:\1\WinFontSwap\bin\selftest.log", Log.ToString(), Encoding.UTF8); } catch { }
            Console.WriteLine(Log.ToString());
            return _fail;
        }

        public static int ApplyTest()
        {
            Line("== 应用链路自检（用系统雅黑当源，排队后立即取消，不重启） ==");
            var srcPath = Path.Combine(Installer.FontsDir, "msyh.ttc");
            var data = File.ReadAllBytes(srcPath);
            var list = new List<FontSource>();
            foreach (var f in Installer.TargetFiles)
                list.Add(new FontSource { TargetFile = f, SourcePath = srcPath, FaceIndex = 0, Data = data, Family = "Microsoft YaHei" });

            var before = new Dictionary<string, string>();
            foreach (var f in Installer.TargetFiles) before[f] = Installer.SddlOf(Path.Combine(Installer.FontsDir, f));

            try
            {
                var res = Installer.Apply(list, Line, false);
                Line("[OK]   已排队，备份：" + res.BackupDir);

                using (var k = Registry.LocalMachine.OpenSubKey(Installer.SessionManagerKey))
                {
                    var arr = k.GetValue("PendingFileRenameOperations") as string[];
                    if (arr != null)
                        for (int i = 0; i + 1 < arr.Length; i += 2)
                            Line("  队列: [" + arr[i] + "] -> [" + arr[i + 1] + "]");
                }

                foreach (var f in Installer.TargetFiles)
                {
                    var t = Path.Combine(Installer.FontsDir, f);
                    var s = Installer.SddlOf(t);
                    if (s.IndexOf("(A;;FA;;;SY)", StringComparison.Ordinal) < 0)
                        throw new Exception(f + " 没有给 SYSTEM 移动权限");
                }
                Line("[OK]   目标文件已授予 SYSTEM 移动权限");

                var infos = Installer.Inspect();
                int queued = 0;
                foreach (var i in infos) { if (i.Queued) queued++; Line("  队列状态 " + i.FileName + " = " + i.Queued); }
                if (queued != 3) throw new Exception("队列条目数量不对：" + queued);

                Installer.CancelQueue(Line);
                infos = Installer.Inspect();
                foreach (var i in infos) if (i.Queued) throw new Exception("取消后仍在队列：" + i.FileName);

                foreach (var f in Installer.TargetFiles)
                {
                    var now = Installer.SddlOf(Path.Combine(Installer.FontsDir, f));
                    if (now != before[f]) throw new Exception(f + " 权限没还原");
                }
                Line("[OK]   应用 / 取消 / 权限还原 全部通过");
            }
            catch (Exception ex)
            {
                _fail++;
                Line("[FAIL] 应用链路 -> " + ex.Message);
                try { Installer.CancelQueue(Line); Line("已尝试清理队列"); } catch { }
            }

            Line(_fail == 0 ? "APPLYTEST OK" : "APPLYTEST FAILED: " + _fail);
            try { File.WriteAllText(@"H:\1\WinFontSwap\bin\selftest.log", Log.ToString(), Encoding.UTF8); } catch { }
            Console.WriteLine(Log.ToString());
            return _fail;
        }

        public static int RenderTest()
        {
            Line("== 渲染自检：把生成的字体画出来 ==");
            try
            {
                var src = File.ReadAllBytes(Path.Combine(Installer.FontsDir, "Deng.ttf"));
                var specs = Installer.SpecsFor("msyh.ttc");
                var data = Sfnt.BuildCollection(src, 0, specs);
                var p = TempPath("render_test.ttc");
                File.WriteAllBytes(p, data);

                using (var pfc = new PrivateFontCollection())
                {
                    pfc.AddFontFile(p);
                    if (pfc.Families.Length == 0) throw new Exception("加载失败");
                    var fam = pfc.Families[0];
                    Line("  渲染使用家族：" + fam.Name);

                    using (var bmp = new System.Drawing.Bitmap(900, 260))
                    using (var g = System.Drawing.Graphics.FromImage(bmp))
                    {
                        g.Clear(System.Drawing.Color.White);
                        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                        using (var f1 = new System.Drawing.Font(fam, 26f))
                        using (var f2 = new System.Drawing.Font(fam, 14f))
                        using (var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                        {
                            g.DrawString("微软雅黑 UI · 0123456789", f1, brush, 20, 20);
                            g.DrawString("清晨的第一缕阳光，照在安静的街道上。", f2, brush, 20, 90);
                            g.DrawString("汉字覆盖测试：魑魅魍魉 龘靐齉爩 犇猋骉麤", f2, brush, 20, 130);
                            g.DrawString("Symbols: !@#$%^&*() []{} <> /\\ |", f2, brush, 20, 170);
                        }
                        bmp.Save(@"H:\1\WinFontSwap\bin\render.png", System.Drawing.Imaging.ImageFormat.Png);

                        int ink = 0;
                        for (int y = 0; y < bmp.Height; y += 2)
                            for (int x = 0; x < bmp.Width; x += 2)
                                if (bmp.GetPixel(x, y).R < 128) ink++;
                        Line("  墨点 " + ink);
                        if (ink < 500) throw new Exception("渲染出来几乎是空白");
                    }
                }
                Line("[OK]   渲染通过");
            }
            catch (Exception ex) { _fail++; Line("[FAIL] 渲染 -> " + ex.Message); }

            Line(_fail == 0 ? "RENDERTEST OK" : "RENDERTEST FAILED: " + _fail);
            try { File.WriteAllText(@"H:\1\WinFontSwap\bin\selftest.log", Log.ToString(), Encoding.UTF8); } catch { }
            Console.WriteLine(Log.ToString());
            return _fail;
        }

        static string TempPath(string name)
        {
            var dir = Path.Combine(Path.GetTempPath(), "WinFontSwapTest");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, name);
        }

        static void LoadCheck(string path)
        {
            using (var pfc = new PrivateFontCollection())
            {
                pfc.AddFontFile(path);
                if (pfc.Families.Length == 0) throw new Exception("系统加载不到这个字体");
            }
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        static void Check(string name, Action action)
        {
            try { action(); Line("[OK]   " + name); }
            catch (Exception ex) { _fail++; Line("[FAIL] " + name + " -> " + ex.Message); }
        }

        static void Line(string s) { Log.AppendLine(s); }
    }
}
