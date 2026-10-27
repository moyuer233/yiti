
import glob, os, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
targets = [
  r'C:\Windows\System32\Microsoft.UI.Xaml.dll',
  r'C:\Windows\System32\Windows.UI.Xaml.dll',
  r'C:\Windows\System32\Windows.UI.Xaml.Phone.dll',
]
needles = ['Segoe UI Variable Text', 'Segoe UI Variable Display', 'Segoe UI Variable Small', 'Segoe UI Variable', 'Microsoft YaHei UI']
for t in targets:
    if not os.path.exists(t):
        print('missing', t); continue
    d = open(t,'rb').read()
    print('==', os.path.basename(t), len(d), 'bytes')
    for n in needles:
        u = n.encode('utf-16-le')
        a = d.count(u)
        b = d.count(n.encode('ascii'))
        if a or b:
            print('   %-30s utf16=%d ascii=%d' % (n, a, b))
