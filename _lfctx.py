
import sys, io, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
d = open(r'H:\1\_ref\lyricify-lite.dll','rb').read()
def ctx(kw, span=120):
    out = []
    for m in re.finditer(re.escape(kw.encode('utf-16-le')), d):
        s = max(0, m.start()-span); e = min(len(d), m.end()+span)
        chunk = d[s:e].decode('utf-16-le', 'replace')
        out.append(chunk.replace('\x00',' '))
    return out
for kw in ['Acrylic', 'Compositor', 'Aero']:
    print('==== ' + kw + ' ====')
    for c in ctx(kw)[:4]:
        print('  ...' + c.strip() + '...')
# 常见的 P/Invoke 名
for name in ['SetWindowCompositionAttribute','GetWindowCompositionAttribute','DwmExtendFrameIntoClientArea','DwmSetWindowAttribute','CreateSwapChainForHwnd','IDCompositionDevice','Direct3D11','d3dcompiler','CreateDXGIFactory','RegisterForSystemBackdrop','MicaController','DesktopAcrylicController']:
    u = d.count(name.encode('utf-16-le')); a = d.count(name.encode())
    print('%-32s u16=%d ascii=%d' % (name, u, a))
