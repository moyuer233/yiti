
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('L')
a = np.array(shot)
reg = a[560:600, 685:770]
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
                bands.append((560+s, 560+e, 685+xs[0], 685+xs[-1]))
    else:
        y += 1
print('bands', bands)
if not bands:
    sys.exit()
y0, y1, x0, x1 = bands[0]
ink = (a[y0:y1, x0:x1] > 140)
print('latin ink', ink.shape)
Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('RGB').crop((x0-1,y0-1,x1+2,y1+2)).resize(((x1-x0+3)*10,(y1-y0+3)*10), Image.NEAREST).save(r'H:\1\WinFontSwap\_lat_crop.png')

txt = 'Steam'
cands = [
    ('OURS segoeui (MiSans)', r'C:\Windows\Fonts\segoeui.ttf', 0),
    ('ORIG segoeui', r'C:\ProgramData\WinFontSwap\backup\segoeui_20261028_022949\orig\segoeui.ttf', 0),
    ('ORIG SegUIVar', r'C:\ProgramData\WinFontSwap\backup\segui_20261028_021549\orig\SegUIVar.ttf', 0),
    ('MiSans Regular', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans-Regular.ttf'), 0),
]
def iou(p, q):
    best = 0; bs = 0
    for oy in range(-2,3):
        for ox in range(-2,3):
            yy0=max(0,oy); xx0=max(0,ox); cy0=max(0,-oy); cx0=max(0,-ox)
            hh=min(p.shape[0]-yy0, q.shape[0]-cy0); ww=min(p.shape[1]-xx0, q.shape[1]-cx0)
            if hh<=0 or ww<=0: continue
            ap=p[yy0:yy0+hh, xx0:xx0+ww]; bp=q[cy0:cy0+hh, cx0:cx0+ww]
            u=(ap|bp).sum()
            if u:
                s=(ap&bp).sum()/u
                if s>best: best=s
    return best
for name, path, idx in cands:
    if not os.path.exists(path):
        print('  %-24s MISSING' % name); continue
    best = (0,0,None)
    for size in (10,11,12,13,14):
        img = Image.new('L', (80, 30), 0)
        f = ImageFont.truetype(path, size, index=idx)
        ImageDraw.Draw(img).text((3, 4), txt, font=f, fill=255)
        c = np.array(img) > 100
        yy, xx = np.where(c)
        if len(xx) == 0: continue
        cc = c[yy.min():yy.max()+1, xx.min():xx.max()+1]
        s = iou(ink, cc)
        if s > best[0]: best = (s, size, cc.shape)
    print('  %-24s best IoU=%.3f @%dpx ink=%s' % (name, best[0], best[1], best[2]))
