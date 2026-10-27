
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
p = r'H:\1\_ref\lyricify-lite.dll'
d = open(p,'rb').read()
needles = ['PresentationFramework','PresentationCore','WindowsBase','MilCore','wpfgfx','Direct3D','d3d11','CompositionTarget','HwndSource','WindowChrome','AllowsTransparency','DwmExtendFrameIntoClientArea','SetWindowCompositionAttribute','ACCENT_ENABLE','DesktopAcrylic','SystemBackdrop','Windows.UI.Composition','Microsoft.UI.Composition']
for n in needles:
    u = d.count(n.encode('utf-16-le')); a = d.count(n.encode('ascii'))
    if u or a:
        print('  %-28s utf16=%-3d ascii=%-3d' % (n, u, a))
