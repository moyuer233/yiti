
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
p = r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc'
d = open(p,'rb').read()
for n in ['Monotype','Founder','ZHONGYI','Microsoft YaHei','Microsoft Corporation','Ascender','Segoe UI','6.30','Version']:
    u = d.count(n.encode('utf-16-le'))
    if u: print('%-22s utf16=%d' % (n, u))
# 列出 name 表里所有 UTF-16 字符串（前 40 条）
import struct
off = 0
if d[:4] == b'ttcf':
    off = struct.unpack('>I', d[16:20])[0]   # face 1
num = struct.unpack('>H', d[off+4:off+6])[0]
for i in range(num):
    rec = d[off+12+16*i: off+28+16*i]
    if rec[0:4] == b'name':
        csum, no, nl = struct.unpack('>III', rec[4:16])
        cnt = struct.unpack('>H', d[no+2:no+4])[0]
        so = no + struct.unpack('>H', d[no+4:no+6])[0]
        seen = set()
        for k in range(cnt):
            pid, eid, lid, nid, l, o = struct.unpack('>HHHHHH', d[no+6+12*k: no+18+12*k])
            s = d[so+o:so+o+l]
            try: txt = s.decode('utf-16-be')
            except: txt = ''
            if txt and txt not in seen and 2 <= len(txt) < 60:
                seen.add(txt)
                print('  ', nid, repr(txt[:60]))
