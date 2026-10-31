using System;
using System.Runtime.InteropServices;

namespace WinFontSwap
{
    /// <summary>
    /// 系统元素字体的“真正生效”路径：SystemParametersInfo(SPI_SETNONCLIENTMETRICS)。
    /// 只写注册表要等重新登录才生效；noMeiryoUI 也是这么做的（见 noMeiryoUI.cpp 里的 setFont / SendMessageTimeout）。
    /// </summary>
    internal static class ShellMetrics
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct LOGFONT
        {
            public int lfHeight;
            public int lfWidth;
            public int lfEscapement;
            public int lfOrientation;
            public int lfWeight;
            public byte lfItalic;
            public byte lfUnderline;
            public byte lfStrikeOut;
            public byte lfCharSet;
            public byte lfOutPrecision;
            public byte lfClipPrecision;
            public byte lfQuality;
            public byte lfPitchAndFamily;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string lfFaceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct NONCLIENTMETRICS
        {
            public int cbSize;
            public int iBorderWidth;
            public int iScrollWidth;
            public int iScrollHeight;
            public int iCaptionWidth;
            public int iCaptionHeight;
            public LOGFONT lfCaptionFont;
            public int iSmCaptionWidth;
            public int iSmCaptionHeight;
            public LOGFONT lfSmCaptionFont;
            public int iMenuWidth;
            public int iMenuHeight;
            public LOGFONT lfMenuFont;
            public LOGFONT lfStatusFont;
            public LOGFONT lfMessageFont;
            public int iPaddedBorderWidth;
        }

        const uint SPI_GETNONCLIENTMETRICS = 0x0029;
        const uint SPI_SETNONCLIENTMETRICS = 0x002A;
        const uint SPI_GETICONTITLELOGFONT = 0x001F;
        const uint SPI_SETICONTITLELOGFONT = 0x0022;
        const uint SPIF_UPDATEINIFILE = 0x01;
        const uint SPIF_SENDCHANGE = 0x02;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SystemParametersInfoW(uint uiAction, uint uiParam, ref NONCLIENTMETRICS pvParam, uint fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool SystemParametersInfoW(uint uiAction, uint uiParam, ref LOGFONT pvParam, uint fWinIni);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        static extern IntPtr SendMessageTimeoutW(IntPtr hWnd, uint msg, IntPtr wParam, string lParam, uint flags, uint timeout, out IntPtr result);

        [DllImport("user32.dll")]
        static extern uint GetDpiForSystem();

        static int SystemDpi()
        {
            try
            {
                uint dpi = GetDpiForSystem();
                if (dpi >= 72 && dpi <= 480) return (int)dpi;
            }
            catch { }
            return 96;
        }

        static int PtToHeight(double pt)
        {
            if (pt <= 0) pt = 9;
            return -(int)Math.Round(pt * SystemDpi() / 72.0);
        }

        /// <summary>
        /// faces[i] 与 Win32Fonts.Keys 顺序一致：SmCaption / Menu / Message / Status / Icon。
        /// 返回成功写入的项数。
        /// </summary>
        public static int Apply(string[] faces, double[] points, bool[] enabled)
        {
            int done = 0;
            try
            {
                var ncm = new NONCLIENTMETRICS();
                ncm.cbSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS));
                if (!SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, (uint)ncm.cbSize, ref ncm, 0))
                {
                    // 老系统 cbSize 不含 iPaddedBorderWidth：用「结构长度 - 4」再取一次
                    int fullSize = Marshal.SizeOf(typeof(NONCLIENTMETRICS));
                    ncm = new NONCLIENTMETRICS();
                    ncm.cbSize = fullSize - 4;
                    SystemParametersInfoW(SPI_GETNONCLIENTMETRICS, (uint)ncm.cbSize, ref ncm, 0);
                    // 老系统只认「结构 - 4」这个尺寸：SET 也必须用同一个 cbSize，否则写入会被拒绝
                    ncm.cbSize = fullSize - 4;
                }

                for (int i = 0; i < Win32Fonts.Keys.Length; i++)
                {
                    if (faces == null || i >= faces.Length) continue;
                    if (enabled != null && i < enabled.Length && !enabled[i]) continue;
                    if (string.IsNullOrEmpty(faces[i])) continue;
                    int h = PtToHeight(points != null && i < points.Length ? points[i] : 9);
                    switch (i)
                    {
                        // 同步把 quality 设成 CLEARTYPE_QUALITY：注册表里若留着 0（DEFAULT），经典界面会退化成灰度抗锯齿
                        case 0: ncm.lfSmCaptionFont.lfHeight = h; ncm.lfSmCaptionFont.lfFaceName = faces[i]; ncm.lfSmCaptionFont.lfQuality = 5; done++; break;
                        case 1: ncm.lfMenuFont.lfHeight = h; ncm.lfMenuFont.lfFaceName = faces[i]; ncm.lfMenuFont.lfQuality = 5; done++; break;
                        case 2: ncm.lfMessageFont.lfHeight = h; ncm.lfMessageFont.lfFaceName = faces[i]; ncm.lfMessageFont.lfQuality = 5; done++; break;
                        case 3: ncm.lfStatusFont.lfHeight = h; ncm.lfStatusFont.lfFaceName = faces[i]; ncm.lfStatusFont.lfQuality = 5; done++; break;
                    }
                }
                bool setOk = SystemParametersInfoW(SPI_SETNONCLIENTMETRICS, (uint)ncm.cbSize, ref ncm, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE);
                if (!setOk) done = 0;   // 写失败就不报“已生效 N 项”

                // 图标字体单独一个 API
                int iconIdx = Win32Fonts.Keys.Length - 1;
                if (faces != null && iconIdx < faces.Length && !string.IsNullOrEmpty(faces[iconIdx])
                    && (enabled == null || iconIdx >= enabled.Length || enabled[iconIdx]))
                {
                    var lf = new LOGFONT();
                    lf.lfFaceName = "";
                    if (SystemParametersInfoW(SPI_GETICONTITLELOGFONT, (uint)Marshal.SizeOf(typeof(LOGFONT)), ref lf, 0))
                    {
                        lf.lfHeight = PtToHeight(points != null && iconIdx < points.Length ? points[iconIdx] : 9);
                        lf.lfFaceName = faces[iconIdx];
                        lf.lfQuality = 5;
                        if (SystemParametersInfoW(SPI_SETICONTITLELOGFONT, (uint)Marshal.SizeOf(typeof(LOGFONT)), ref lf, SPIF_UPDATEINIFILE | SPIF_SENDCHANGE))
                            done++;
                    }
                }

                IntPtr res;
                SendMessageTimeoutW(new IntPtr(0xffff), 0x001A, new IntPtr(SPI_SETNONCLIENTMETRICS), "WindowMetrics", 0x0002, 1000, out res);
            }
            catch { }
            return done;
        }
    }
}
