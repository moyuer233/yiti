using System;
using System.IO;
using System.Windows.Forms;

namespace WinFontSwap
{
    static class Program
    {
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
