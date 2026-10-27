
import struct
p = r'C:\ProgramData\WinFontSwap\stage\segoeui\segoeui.ttf'
d = open(p,'rb').read()
n = struct.unpack('>H', d[4:6])[0]
tags = {}
for i in range(n):
    rec = d[12+16*i:28+16*i]
    tags[rec[0:4].decode('latin1')] = struct.unpack('>III', rec[4:16])
print('size', len(d), 'tables', ' '.join(sorted(tags)))
off, ln = tags['name'][1], tags['name'][2]
cnt = struct.unpack('>H', d[off+2:off+4])[0]
so = off + struct.unpack('>H', d[off+4:off+6])[0]
for i in range(cnt):
    pid, eid, lid, nid, l, o = struct.unpack('>HHHHHH', d[off+6+12*i: off+18+12*i])
    if nid in (1,2,4,6):
        print(' nameID', nid, '=', d[so+o:so+o+l].decode('utf-16-be', 'replace'))
