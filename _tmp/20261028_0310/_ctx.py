
import os, sys, io, re
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
targets = [
  r'C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy\StartDocked.dll',
  r'C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy\resources.pri',
  r'C:\Windows\System32\Windows.UI.Xaml.dll',
]
needle = 'Segoe UI Variable'
for p in targets:
    if not os.path.exists(p):
        print('missing', p); continue
    d = open(p,'rb').read()
    print('==', os.path.basename(p), len(d))
    # utf16 hits with context
    u = needle.encode('utf-16-le')
    idx = 0
    hits = 0
    while True:
        i = d.find(u, idx)
        if i < 0: break
        hits += 1
        s = max(0, i - 160); e = min(len(d), i + 200)
        try:
            ctx = d[s:e].decode('utf-16-le', 'replace')
        except Exception:
            ctx = ''
        ctx = ''.join(ch if 32 <= ord(ch) < 0x3000 or ord(ch) > 0xE000 else '.' for ch in ctx)
        print('  hit@%d: %s' % (i, ctx[:180]))
        idx = i + 2
        if hits > 6: break
    # ascii hits
    a = needle.encode('ascii')
    print('  ascii hits:', d.count(a))
