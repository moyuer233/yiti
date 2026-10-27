
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
pat = 'MicrosoftYaHei'.encode('utf-16-le')
for label, p in [('OURS', r'C:\Windows\Fonts\msyh.ttc'), ('ORIG', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc')]:
    d = open(p,'rb').read()
    print('==', label)
    i = 0
    n = 0
    while n < 6:
        i = d.find(pat, i)
        if i < 0: break
        s = max(0, i-24); e = min(len(d), i+len(pat)+16)
        print('   @%d ctx=%s' % (i, d[s:e].hex()))
        i += 2; n += 1
