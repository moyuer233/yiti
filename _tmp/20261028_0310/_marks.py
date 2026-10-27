
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
files = {
  'ours msyh.ttc': r'C:\Windows\Fonts\msyh.ttc',
  'orig msyh.ttc': r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc',
  'ours SegUIVar': r'C:\Windows\Fonts\SegUIVar.ttf',
  'orig SegUIVar': r'C:\ProgramData\WinFontSwap\backup\segui_20261028_021549\orig\SegUIVar.ttf',
  'ours segoeui': r'C:\Windows\Fonts\segoeui.ttf',
}
needles = ['MiSans', 'Xiaomi', 'SegoeUIVariable-Regular', 'MiSans VF']
for label, p in files.items():
    try:
        d = open(p,'rb').read()
    except Exception as e:
        print(label, 'ERR', e); continue
    out = []
    for n in needles:
        u = d.count(n.encode('utf-16-le'))
        a = d.count(n.encode('ascii'))
        if u or a:
            out.append('%s(utf16=%d,ascii=%d)' % (n, u, a))
    print('%-16s %s' % (label, ' '.join(out) if out else '(无标记)'))
