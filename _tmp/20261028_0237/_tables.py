
import struct, sys, os

def faces(path):
    d = open(path,'rb').read()
    if d[:4] == b'ttcf':
        n = struct.unpack('>I', d[8:12])[0]
        offs = [struct.unpack('>I', d[12+4*i:16+4*i])[0] for i in range(n)]
    else:
        offs = [0]
    out = []
    for off in offs:
        sfnt = d[off:off+4]
        num = struct.unpack('>H', d[off+4:off+6])[0]
        tables = {}
        for i in range(num):
            rec = d[off+12+16*i: off+28+16*i]
            tag = rec[0:4].decode('latin1')
            csum, toff, tlen = struct.unpack('>III', rec[4:16])
            tables[tag] = (toff, tlen)
        out.append((sfnt, tables))
    return out

def show(label, path):
    print('==', label, os.path.basename(path), os.path.getsize(path), 'bytes')
    for i,(sfnt, t) in enumerate(faces(path)):
        print('  face %d sfnt=%r tables=%d: %s' % (i, sfnt, len(t), ' '.join(sorted(t.keys()))))

files = [
 ('original YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc'),
 ('current YaHei', r'C:\Windows\Fonts\msyh.ttc'),
 ('MiSans source', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans-Regular.ttf')),
 ('MiSans VF', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans VF.ttf')),
 ('SimSun source', r'C:\Windows\Fonts\simsun.ttc'),
]
data = {}
for label, p in files:
    if not os.path.exists(p):
        print('== %s MISSING %s' % (label, p)); continue
    show(label, p)
    data[label] = faces(p)

def tags(label, face=0):
    return set(data[label][face][1].keys()) if label in data else set()

print()
print('original-only tables:', sorted(tags('original YaHei') - tags('current YaHei')))
print('current-only tables :', sorted(tags('current YaHei') - tags('original YaHei')))
print('MiSans src-only     :', sorted(tags('MiSans source') - tags('current YaHei')))
print('current-not-in-src  :', sorted(tags('current YaHei') - tags('MiSans source')))
if 'MiSans VF' in data:
    print('VF tables           :', sorted(tags('MiSans VF')))
