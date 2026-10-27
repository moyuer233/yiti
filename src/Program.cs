using System;
using System.IO;
using System.Windows.Forms;

namespace WinFontSwap
{
    static class Program
    {
        /// <summary>命令行把某个字体应用到三个目标（内部用，方便无人值守测试）。</summary>
        static int ApplyCli(string fontPath)
        {
            try
            {
                var entries = FontLibrary.ReadFile(fontPath);
                if (entries.Count == 0) { Console.WriteLine("读不出字体：" + fontPath); return 2; }
                var e = entries[0];
                var data = File.ReadAllBytes(fontPath);
                var list = new System.Collections.Generic.List<FontSource>();
                foreach (var target in Installer.TargetFiles)
                    list.Add(new FontSource
                    {
                        TargetFile = target,
                        SourcePath = fontPath,
                        FaceIndex = e.FaceIndex,
                        Family = e.Family,
                        Subfamily = e.Subfamily,
                        Data = data
                    });
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

        [STAThread]
        static int Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                if (args[0] == "--selftest") return SelfTest.Run();
                if (args[0] == "--writetest") return SelfTest.WriteTest();
                if (args[0] == "--acltest") return SelfTest.AclTest();
                if (args[0] == "--applytest") return SelfTest.ApplyTest();
                if (args[0] == "--rendertest") return SelfTest.RenderTest();
                if (args[0] == "--apply" && args.Length >= 2) return ApplyCli(args[1]);
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
