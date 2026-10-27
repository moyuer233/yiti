
from PIL import Image
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('L')
a = np.array(shot)
sub = a[366:390, 586:644]
m = sub > 150
ys, xs = np.where(m)
ref = m[ys.min():ys.max()+1, xs.min():xs.max()+1]
def iou(p, q):
    best = 0; bestoff=(0,0)
    for oy in range(-3,4):
        for ox in range(-3,4):
            y0=max(0,oy); x0=max(0,ox); cy0=max(0,-oy); cx0=max(0,-ox)
            hh=min(p.shape[0]-y0, q.shape[0]-cy0); ww=min(p.shape[1]-x0, q.shape[1]-cx0)
            if hh<=0 or ww<=0: continue
            ap=p[y0:y0+hh, x0:x0+ww]; bp=q[cy0:cy0+hh, cx0:cx0+ww]
            u=(ap|bp).sum()
            if u:
                s=(ap&bp).sum()/u
                if s>best: best, bestoff = s, (oy,ox)
    return best
print('ref ink', ref.shape)
def load(p):
    d = np.array(Image.open(p).convert('L'))
    mm = d > 100
    y, x = np.where(mm)
    return mm[y.min():y.max()+1, x.min():x.max()+1]
for label, prefix in (('ORIG YaHei', '_orig_'), ('OURS YaHei/MiSans', '_dw_')):
    for size in (16,17,18,19):
        p = r'H:\1\WinFontSwap\%s%d.png' % (prefix, size)
        if not os.path.exists(p): continue
        c = load(p)
        print('  %-18s %dpx ink=%s IoU=%.3f' % (label, size, c.shape, iou(ref, c)))
