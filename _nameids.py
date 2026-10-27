
import struct, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
def names(path):
    d = open(path,'rb').read()
    off = 0
    if d[:4] == b'ttcf':
        n = struct.unpack('>I', d[8:12])[0]
        off = struct.unpack('>I', d[12:16])[0]
    numTables = struct.unpack('>H', d[off+4:off+6])[0]
    nameOff = None
    for i in range(numTables):
        rec = off + 12 + i*16
        tag = d[rec:rec+4]
        if tag == b'name':
            nameOff = struct.unpack('>I', d[rec+8:rec+12])[0]
    if nameOff is None: return {}
    fmt, count, strOff = struct.unpack('>HHH', d[nameOff:nameOff+6])
    out = {}
    for i in range(count):
        r = nameOff + 6 + i*12
        pid, eid, lid, nid, ln, so = struct.unpack('>HHHHHH', d[r:r+12])
        s = d[nameOff+strOff+so: nameOff+strOff+so+ln]
        try:
            if pid == 3 and eid == 1: txt = s.decode('utf-16-be')
            else: txt = s.decode('latin-1')
        except: continue
        if nid in (0,1,5,7,9,11,13,14) and nid not in out:
            out[nid] = txt
    return out
for p in [r'C:\Windows\Fonts\msyh.ttc', r'C:\Windows\Fonts\SegUIVar.ttf', r'C:\Windows\Fonts\segoeui.ttf']:
    print('==', p)
    for k, v in sorted(names(p).items()):
        print('  id%-3d %s' % (k, v[:110]))
