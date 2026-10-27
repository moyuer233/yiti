
import sys, io, re, struct
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
p = r'H:\1\_ref\lyricify-lite.dll'
d = open(p, 'rb').read()
print('size', len(d))
# .baml 名字（UTF-8 与 UTF-16）
names = sorted(set(re.findall(rb'[\w/\\.-]{3,80}\.baml', d)))
print('baml count', len(names))
for n in names[:40]:
    print('  ', n.decode('utf-8', 'replace'))
print('--- resources ---')
for kw in [b'.g.resources', b'g.resources']:
    i = d.find(kw)
    print(kw, i)
# 找 WPF 相关字符串（UTF-16）
for kw in ['Mica', 'Acrylic', 'DesktopAcrylicBackdrop', 'Windows.UI.Composition', 'Compositor', 'HostBackdrop', 'SystemBackdrop', 'WCA_ACCENT', 'DWM_SYSTEMBACKDROP_TYPE', 'Aero', 'Blur']:
    u = d.count(kw.encode('utf-16-le'))
    a = d.count(kw.encode('utf-8'))
    if u or a: print('  %-26s u16=%d a=%d' % (kw, u, a))
