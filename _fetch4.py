
import urllib.request, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
os.environ['HTTPS_PROXY'] = 'http://127.0.0.1:7897'
os.environ['HTTP_PROXY'] = 'http://127.0.0.1:7897'
u = 'https://raw.githubusercontent.com/lepoco/wpfui/main/src/Wpf.Ui/Interop/UnsafeNativeMethods.cs'
req = urllib.request.Request(u, headers={'User-Agent': 'dsh-agent'})
t = urllib.request.urlopen(req, timeout=40).read().decode('utf-8', 'replace')
i = t.find('RemoveWindowCaption')
print(t[max(0, i-400): i+1200] if i >= 0 else 'not found')
