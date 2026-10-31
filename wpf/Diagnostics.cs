using System;
using System.IO;
using System.Text;

namespace WinFontSwap.Wpf
{
    /// <summary>
    /// 轻量操作日志：始终写到 C:\ProgramData\WinFontSwap\actions.log（截断到最近 200 KB）。
    /// 出问题时不用重新开 --log，直接看这个文件就能复现用户点了什么。
    /// </summary>
    internal static class Diagnostics
    {
        static readonly object Lock = new object();
        static string PathUsed
        {
            get { return System.IO.Path.Combine(Installer.DataDir, "actions.log"); }
        }

        public static void Append(string line)
        {
            try
            {
                lock (Lock)
                {
                    Directory.CreateDirectory(Installer.DataDir);
                    var p = PathUsed;
                    try
                    {
                        var fi = new FileInfo(p);
                        if (fi.Exists && fi.Length > 200 * 1024) File.Delete(p);
                    }
                    catch { }
                    File.AppendAllText(p, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + line + "\r\n", Encoding.UTF8);
                }
            }
            catch { }
        }
    }
}
