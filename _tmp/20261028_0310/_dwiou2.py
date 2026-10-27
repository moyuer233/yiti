
from PIL import Image
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('L')
a = np.array(shot)
sub = a[368:388, 588:642]
m = sub > 150
ys, xs = np.where(m)
ref = m[ys.min():ys.max()+1, xs.min():xs.max()+1]
def iou(a,b):
    best=0
    for oy in range(-3,4):
        for ox in range(-3,4):
            y0=max(0,oy); x0=max(0,ox); cy0=max(0,-oy); cx0=max(0,-ox)
            hh=min(a.shape[0]-y0, b.shape[0]-cy0); ww=min(a.shape[1]-x0, b.shape[1]-cx0)
            if hh<=0 or ww<=0: continue
            ap=a[y0:y0+hh, x0:x0+ww]; bp=b[cy0:cy0+hh, cx0:cx0+ww]
            u=(ap|bp).sum()
            if u: best=max(best,(ap&bp).sum()/u)
    return best
print('ref', ref.shape)
for size in (16,17,18):
    p = r'H:\1\WinFontSwap\_dw_%d.png' % size
    if not os.path.exists(p): continue
    d = np.array(Image.open(p).convert('L'))
    dm = d > 100
    dy, dx = np.where(dm)
    cand = dm[dy.min():dy.max()+1, dx.min():dx.max()+1]
    print('  size %d cand=%s  IoU=%.3f' % (size, cand.shape, iou(ref, cand)))
