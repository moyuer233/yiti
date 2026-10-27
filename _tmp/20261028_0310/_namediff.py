
import struct, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

def tables(path, face=0):
    d = open(path,'rb').read()
    if d[:4] == b'ttcf':
        off = struct.unpack('>I', d[12+4*face:16+4*face])[0]
    else:
        off = 0
    num = struct.unpack('>H', d[off+4:off+6])[0]
    t = {}
    for i in range(num):
        rec = d[off+12+16*i: off+28+16*i]
        tag = rec[0:4].decode('latin1')
        csum, o, l = struct.unpack('>III', rec[4:16])
        t[tag] = d[o:o+l]
    return t

ours = tables(r'C:\Windows\Fonts\msyh.ttc', 1)   # face 1 = Microsoft YaHei UI
orig = tables(r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1)
print('ours tables:', ' '.join(sorted(ours)))
print('orig tables:', ' '.join(sorted(orig)))

for tag in ('name', 'head', 'OS/2', 'post'):
    a, b = ours.get(tag), orig.get(tag)
    if a is None or b is None:
        print(tag, 'missing'); continue
    print(tag, 'len ours/orig =', len(a), len(b), 'equal =', a == b)

# 找 name 表里互相不同的 16 字节窗口
a, b = ours['name'], orig['name']
n = min(len(a), len(b))
cands = []
for i in range(0, n - 16):
    wa, wb = a[i:i+16], b[i:i+16]
    if wa != wb and wb.find(wa) < 0 and a.find(wb) < 0:
        cands.append((i, wa.hex(), wb.hex()))
print('name 差异窗口数:', len(cands))
for c in cands[:5]:
    print('  off', c[0], 'ours', c[1], 'orig', c[2])
