
import struct, os

SRC = os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans VF.ttf')
OUTDIR = r'C:\ProgramData\WinFontSwap\stage\segui'
os.makedirs(OUTDIR, exist_ok=True)
OUT = os.path.join(OUTDIR, 'SegUIVar.ttf')

d = open(SRC, 'rb').read()
num = struct.unpack('>H', d[4:6])[0]
tables = {}
for i in range(num):
    rec = d[12+16*i: 28+16*i]
    tag = rec[0:4].decode('latin1')
    csum, off, ln = struct.unpack('>III', rec[4:16])
    tables[tag] = d[off:off+ln]
print('source tables:', ' '.join(sorted(tables)))

def u16(v): return struct.pack('>H', v)
def u32(v): return struct.pack('>I', v)

def name_table(entries):
    recs = []
    strings = b''
    for nid, text in entries:
        enc = text.encode('utf-16-be')
        recs.append((3, 1, 0x409, nid, len(enc), len(strings)))
        strings += enc
    hdr = u16(0) + u16(len(recs)) + u16(6 + 12*len(recs))
    recdata = b''
    for p, e, l, nid, ln, off in recs:
        recdata += u16(p) + u16(e) + u16(l) + u16(nid) + u16(ln) + u16(off)
    return hdr + recdata + strings

FAM = 'Segoe UI Variable'
SUB = 'Regular'
PS = 'SegoeUIVariable-Regular'
tables['name'] = name_table([
    (0, 'Segoe UI Variable'),
    (1, FAM),
    (2, SUB),
    (3, 'Segoe UI Variable; regular'),
    (4, FAM),
    (6, PS),
    (16, FAM),
    (17, SUB),
])

tags = sorted(tables.keys())
off = 12 + 16*len(tags)
dir_bytes = b''
body = b''
offsets = {}
for t in tags:
    data = tables[t]
    pad = (4 - (len(data) % 4)) % 4
    offsets[t] = off
    dir_bytes += t.encode('latin1') + u32(0) + u32(off) + u32(len(data))
    body += data + b'\x00'*pad
    off += len(data) + pad

def calc(data):
    if len(data) % 4: data = data + b'\x00'*(4-len(data)%4)
    s = 0
    for i in range(0, len(data), 4):
        s = (s + struct.unpack('>I', data[i:i+4])[0]) & 0xFFFFFFFF
    return s

head = bytearray(tables['head'])
head[8:12] = b'\x00\x00\x00\x00'
tables['head'] = bytes(head)
# rebuild dir with correct head checksum
dir_bytes = b''
off = 12 + 16*len(tags)
body = b''
for t in tags:
    data = tables[t]
    pad = (4 - (len(data) % 4)) % 4
    dir_bytes += t.encode('latin1') + u32(calc(data)) + u32(off) + u32(len(data))
    body += data + b'\x00'*pad
    off += len(data) + pad

sfnt = b'\x00\x01\x00\x00' + u16(len(tags)) + u16(0) + u16(0) + u16(0) + dir_bytes + body
total = calc(sfnt)
adj = (0xB1B0AFBA - total) & 0xFFFFFFFF
head_off = 12 + 16*len(tags) + sum((len(tables[t]) + ((4-len(tables[t])%4)%4)) for t in tags[:tags.index('head')])
sfnt = bytearray(sfnt)
sfnt[head_off+8:head_off+12] = u32(adj)
open(OUT, 'wb').write(bytes(sfnt))
print('written', OUT, len(sfnt), 'bytes')

# verify: reload tables
v = open(OUT,'rb').read()
n2 = struct.unpack('>H', v[4:6])[0]
tags2 = [v[12+16*i:16+16*i].decode('latin1') for i in range(n2)]
print('out tables:', ' '.join(sorted(tags2)))
from PIL import ImageFont
f = ImageFont.truetype(OUT, 24)
print('PIL loads ok, family attr:', getattr(f, 'font', None) is not None)
