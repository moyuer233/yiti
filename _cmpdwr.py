
from PIL import Image
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_set3.png').convert('L')
a = np.array(shot)
sub = a[155:173, 620:790]
m = sub > 130
ys, xs = np.where(m)
ink = m[ys.min():ys.max()+1, xs.min():xs.max()+1]
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
print('screenshot ink', ink.shape)
for k in ('origyahei','harmony'):
    for size in (11,12,13,14):
        p = r'H:\1\WinFontSwap\_r_%s_%d.png' % (k, size)
        if not os.path.exists(p): continue
        d = np.array(Image.open(p).convert('L')) > 100
        yy, xx = np.where(d)
        if len(xx) == 0: continue
        c = d[yy.min():yy.max()+1, xx.min():xx.max()+1]
        print('  %-10s %2dpx ink=%s IoU=%.3f' % (k, size, c.shape, iou(ink, c)))
