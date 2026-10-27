
import struct
p = r'C:\Windows\Fonts\SegUIVar.ttf'
d = open(p,'rb').read()
print('size', len(d), 'magic', d[:4])
n = struct.unpack('>H', d[4:6])[0]
print('tables', n)
tags = {}
for i in range(n):
    rec = d[12+16*i:28+16*i]
    tag = rec[0:4].decode('latin1')
    cs, off, ln = struct.unpack('>III', rec[4:16])
    tags[tag] = (off, ln)
print(' '.join(sorted(tags)))
off, ln = tags['name']
print('name table len', ln)
cnt = struct.unpack('>H', d[off+2:off+4])[0]
strOff = off + struct.unpack('>H', d[off+4:off+6])[0]
for i in range(cnt):
    r = d[off+6+12*i: off+18+12*i]
    pid, eid, lid, nid, l, o = struct.unpack('>HHHHHH', r)
    s = d[strOff+o:strOff+o+l]
    try:
        txt = s.decode('utf-16-be')
    except Exception:
        txt = repr(s)
    if nid in (1,2,4,6,16,17):
        print('  nameID', nid, 'plat', pid, '=', txt)
