
import struct
p = r'C:\ProgramData\WinFontSwap\backup\segui_20261028_021549\orig\SegUIVar.ttf'
d = open(p,'rb').read()
print('size', len(d), 'magic', d[:4])
if d[:4] == b'ttcf':
    n = struct.unpack('>I', d[8:12])[0]
    print('faces', n)
    offs = [struct.unpack('>I', d[12+4*i:16+4*i])[0] for i in range(n)]
    for i, off in enumerate(offs):
        num = struct.unpack('>H', d[off+4:off+6])[0]
        tags = {}
        for j in range(num):
            rec = d[off+12+16*j: off+28+16*j]
            tags[rec[0:4].decode('latin1')] = struct.unpack('>III', rec[4:16])
        # read name family
        if 'name' in tags:
            noff, nln = tags['name'][1], tags['name'][2]
            cnt = struct.unpack('>H', d[noff+2:noff+4])[0]
            so = noff + struct.unpack('>H', d[noff+4:noff+6])[0]
            fam = None
            for k in range(cnt):
                pid, eid, lid, nid, l, o = struct.unpack('>HHHHHH', d[noff+6+12*k: noff+18+12*k])
                if nid == 1 and pid == 3:
                    fam = d[so+o:so+o+l].decode('utf-16-be', 'replace')
                    break
            print('  face', i, 'family:', fam, '| tables:', ' '.join(sorted(tags)))
else:
    num = struct.unpack('>H', d[4:6])[0]
    tags = {}
    for j in range(num):
        rec = d[12+16*j: 28+16*j]
        tags[rec[0:4].decode('latin1')] = struct.unpack('>III', rec[4:16])
    print('single face, tables:', ' '.join(sorted(tags)))
