
import struct, io, sys
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
p = r'C:\ProgramData\WinFontSwap\backup\segui_20261028_021549\orig\SegUIVar.ttf'
d = open(p,'rb').read()
n = struct.unpack('>H', d[4:6])[0]
tags = {}
for i in range(n):
    rec = d[12+16*i:28+16*i]
    tags[rec[0:4].decode('latin1')] = struct.unpack('>III', rec[4:16])

noff, nln = tags['name'][1], tags['name'][2]
cnt = struct.unpack('>H', d[noff+2:noff+4])[0]
so = noff + struct.unpack('>H', d[noff+4:noff+6])[0]
names = {}
for k in range(cnt):
    pid, eid, lid, nid, l, o = struct.unpack('>HHHHHH', d[noff+6+12*k: noff+18+12*k])
    s = d[so+o:so+o+l]
    try: txt = s.decode('utf-16-be') if pid == 3 else s.decode('latin1')
    except: txt = repr(s)
    names.setdefault(nid, []).append((pid, txt))
print('name IDs:', sorted(names))
for nid in sorted(names):
    vals = set(t for _, t in names[nid])
    if nid >= 256 or nid in (1,2,4,6,16,17):
        print(' ', nid, list(vals)[:6])

foff, fln = tags['fvar'][1], tags['fvar'][2]
mv, mn, aao, res, ac, asz, ic, isz = struct.unpack('>HHHHHHHH', d[foff:foff+16])
print('fvar: axes', ac, 'axisSize', asz, 'instances', ic, 'instSize', isz, 'axesArrayOffset', aao)
axes = []
for i in range(ac):
    a = foff + aao + i*asz
    tag = d[a:a+4].decode('latin1')
    mnv, dv, mxv = struct.unpack('>iii', d[a+4:a+16])
    nid = struct.unpack('>H', d[a+18:a+20])[0]
    axes.append((tag, mnv/65536.0, dv/65536.0, mxv/65536.0, nid))
    print('  axis', tag, mnv/65536.0, dv/65536.0, mxv/65536.0, 'nameID', nid)
inst = foff + aao + ac*asz
for i in range(ic):
    b = inst + i*isz
    sid, flags = struct.unpack('>HH', d[b:b+4])
    coords = [struct.unpack('>i', d[b+4+4*j:b+8+4*j])[0]/65536.0 for j in range(ac)]
    print('  instance', i, 'nameID', sid, names.get(sid), 'coords', coords, 'flags', flags)
