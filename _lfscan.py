
import os, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
p = r'H:\1\_ref\lyricify-lite.dll'
d = open(p, 'rb').read()
print('size', len(d))
needles = [
  'DwmSetWindowAttribute','DwmExtendFrameIntoClientArea','SetWindowCompositionAttribute',
  'Mica','MicaController','DesktopAcrylic','Acrylic','AcrylicBrush','BlurBehind',
  'SystemBackdrop','DWMSBT','DWMWA_SYSTEMBACKDROP_TYPE','TransparencyKey',
  'Microsoft.UI.Composition','Win32Interop','Compositor','BackdropMaterial',
]
for n in needles:
    u = d.count(n.encode('utf-16-le'))
    a = d.count(n.encode('ascii'))
    if u or a:
        print('  %-32s utf16=%-3d ascii=%-3d' % (n, u, a))
