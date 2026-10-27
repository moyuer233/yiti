using System;
namespace WinFontSwap
{
    internal static class Win32Probe
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            var all = Win32Fonts.ReadAll();
            for (int i = 0; i < Win32Fonts.Names.Length && i < all.Count; i++)
            {
                var x = all[i];
                Console.WriteLine("  " + Win32Fonts.Names[i].PadRight(6) + " face='" + x.Face + "'  height=" + x.HeightPx + "  weight=" + x.Weight);
            }
            Console.WriteLine("  备份存在=" + Win32Fonts.HasBackup);
        }
    }
}
