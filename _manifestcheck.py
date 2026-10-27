
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
d = open(r'H:\1\WinFontSwap\dist\WinFontSwapWpf.exe','rb').read()
for kw in [b'requireAdministrator', b'req', b'asmv3', b'WinFontSwap.Wpf']:
    print('  %-22s count=%d' % (kw.decode('latin-1'), d.count(kw)))
i = d.find(b'requireAdministrator')
print('  context:', d[max(0,i-120):i+60].decode('utf-8','replace').replace('\n',' ')[:180])
