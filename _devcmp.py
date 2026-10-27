
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
img = Image.open(r'H:\1\WinFontSwap\_set3.png').convert('RGB')
a = np.array(img.convert('L'))
reg = a[140:230, 300:1150]
m = reg > 130
rows = m.sum(axis=1)
bands = []
y = 0
while y < len(rows):
    if rows[y] > 3:
        s = y
        while y < len(rows) and rows[y] > 0: y += 1
        e = y
        if 8 <= e-s <= 26:
            xs = np.where(m[s:e,:].sum(axis=0) > 0)[0]
            if len(xs) > 30:
                bands.append((140+s, 140+e, 300+xs[0], 300+xs[-1]))
    else:
        y += 1
print('bands:', bands)
if bands:
    y0,y1,x0,x1 = bands[0]
    print('picked', bands[0], 'w=', x1-x0+1)
    img.crop((x0-2,y0-2,x1+3,y1+3)).resize(((x1-x0+5)*5,(y1-y0+5)*5), Image.NEAREST).save(r'H:\1\WinFontSwap\_dev0.png')
    ink = a[y0:y1, x0:x1] > 130
    txt = 'Z790M Snow Dream D5'
    cands = [
      ('HarmonyOS Regular', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\HarmonyOS_Sans_SC_Regular.ttf'), 0),
      ('ORIG Segoe UI', r'C:\ProgramData\WinFontSwap\backup\segoe_harmony_20261028_030711\orig\segoeui.ttf', 0),
      ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
      ('MiSans Regular', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans-Regular.ttf'), 0),
    ]
    def iou(p,q):
        best=0
        for oy in range(-3,4):
            for ox in range(-3,4):
                yy=max(0,oy); xx=max(0,ox); cy=max(0,-oy); cx=max(0,-ox)
                hh=min(p.shape[0]-yy, q.shape[0]-cy); ww=min(p.shape[1]-xx, q.shape[1]-cx)
                if hh<=0 or ww<=0: continue
                ap=p[yy:yy+hh, xx:xx+ww]; bp=q[cy:cy+hh, cx:cx+ww]
                u=(ap|bp).sum()
                if u: best=max(best,(ap&bp).sum()/u)
        return best
    print('ink', ink.shape)
    for name, path, idx in cands:
        if not os.path.exists(path): print('  MISSING', name); continue
        best=(0,0)
        for size in (11,12,13,14,15):
            im2 = Image.new('L', (200,30), 0)
            f = ImageFont.truetype(path, size, index=idx)
            ImageDraw.Draw(im2).text((3,4), txt, font=f, fill=255)
            c = np.array(im2) > 100
            yy,xx2 = np.where(c)
            if len(xx2)==0: continue
            cc = c[yy.min():yy.max()+1, xx2.min():xx2.max()+1]
            s = iou(ink, cc)
            if s>best[0]: best=(s,size)
        print('  %-20s IoU=%.3f @%dpx' % (name, best[0], best[1]))
