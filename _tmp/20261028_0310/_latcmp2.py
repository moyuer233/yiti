
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_start2.png').convert('L')
a = np.array(shot)
reg = a[555:605, 690:790]
m = reg > 140
rows = m.sum(axis=1)
bands = []
y = 0
while y < len(rows):
    if rows[y] > 2:
        s = y
        while y < len(rows) and rows[y] > 0: y += 1
        e = y
        if 6 <= e-s <= 20:
            xs = np.where(m[s:e,:].sum(axis=0) > 0)[0]
            if len(xs) > 6:
                bands.append((555+s, 555+e, 690+xs[0], 690+xs[-1]))
    else:
        y += 1
print('bands', bands)
if not bands:
    raise SystemExit('no band')
y0,y1,x0,x1 = bands[0]
ink = a[y0:y1, x0:x1] > 140
print('latin ink', ink.shape)
Image.open(r'H:\1\WinFontSwap\_start2.png').convert('RGB').crop((x0-1,y0-1,x1+2,y1+2)).resize(((x1-x0+3)*10,(y1-y0+3)*10), Image.NEAREST).save(r'H:\1\WinFontSwap\_lat2.png')

txt = 'Steam'
cands = [
  ('HarmonyOS Sans SC', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\HarmonyOS_Sans_SC_Regular.ttf'), 0),
  ('ORIG Segoe UI', r'C:\ProgramData\WinFontSwap\backup\segoe_harmony_20261028_030711\orig\segoeui.ttf', 0),
  ('MiSans Regular', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans-Regular.ttf'), 0),
]
def iou(p,q):
    best=0
    for oy in range(-2,3):
        for ox in range(-2,3):
            y0=max(0,oy); x0b=max(0,ox); cy0=max(0,-oy); cx0=max(0,-ox)
            hh=min(p.shape[0]-y0, q.shape[0]-cy0); ww=min(p.shape[1]-x0b, q.shape[1]-cx0)
            if hh<=0 or ww<=0: continue
            ap=p[y0:y0+hh, x0b:x0b+ww]; bp=q[cy0:cy0+hh, cx0:cx0+ww]
            u=(ap|bp).sum()
            if u: best=max(best,(ap&bp).sum()/u)
    return best
for name, path, idx in cands:
    if not os.path.exists(path):
        print('  %-20s MISSING' % name); continue
    best=(0,0)
    for size in (10,11,12,13,14):
        img = Image.new('L', (80,30), 0)
        f = ImageFont.truetype(path, size, index=idx)
        ImageDraw.Draw(img).text((3,4), txt, font=f, fill=255)
        c = np.array(img) > 100
        yy,xx = np.where(c)
        if len(xx)==0: continue
        cc = c[yy.min():yy.max()+1, xx.min():xx.max()+1]
        s = iou(ink, cc)
        if s > best[0]: best = (s, size)
    print('  %-20s best IoU=%.3f @%dpx' % (name, best[0], best[1]))
