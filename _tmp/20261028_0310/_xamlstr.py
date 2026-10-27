
import glob, os, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
roots = [
  r'C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy',
  r'C:\Windows\SystemApps\MicrosoftWindows.Client.CBS_cw5n1h2txyewy',
]
needles = ['Segoe UI Variable Text', 'Segoe UI Variable Display', 'Segoe UI Variable Small', 'Segoe UI Variable']
for root in roots:
    if not os.path.isdir(root):
        print('missing', root); continue
    print('== root', os.path.basename(root))
    for dirpath, _, files in os.walk(root):
        for f in files:
            if not f.lower().endswith(('.dll','.xbf','.pri','.exe','.mui')):
                continue
            p = os.path.join(dirpath, f)
            try:
                d = open(p,'rb').read()
            except Exception:
                continue
            for n in needles:
                c = d.count(n.encode('utf-16-le'))
                if c:
                    print('   %-32s %s x%d' % (n, f, c))
