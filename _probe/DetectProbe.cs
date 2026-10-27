using System;
using System.IO;

namespace WinFontSwap
{
    internal static class DetectProbe
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            foreach (var f in Installer.TargetFiles)
            {
                var full = Path.Combine(Installer.FontsDir, f);
                string fam = "-", ven = "-";
                try { fam = Installer.DetectSourceFamily(f) ?? "-"; } catch (Exception ex) { fam = "ERR " + ex.Message; }
                try { ven = Installer.DetectSourceVendor(f) ?? "-"; } catch (Exception ex) { ven = "ERR " + ex.Message; }
                Console.WriteLine(f + "  family=" + fam + "  vendor=" + ven);
            }
            var st = Installer.ReadState();
            Console.WriteLine("--- state family_ keys ---");
            foreach (var kv in st) if (kv.Key.StartsWith("family_")) Console.WriteLine("  " + kv.Key + " = " + kv.Value);
            var infos = Installer.Inspect();
            Console.WriteLine("--- inspect ---");
            foreach (var i in infos) Console.WriteLine("  " + i.FileName + " applied=" + i.Applied + " family=" + i.Family + " queued=" + i.Queued);
        }
    }
}
