
from PIL import Image
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('L')
a = np.array(shot)
sub = a[368:388, 588:642]
m = sub > 150
ys, xs = np.where(m)
ref = m[ys.min():ys.max()+1, xs.min():xs.max()+1]
d = np.array(Image.open(r'H:\1\WinFontSwap\_dw_render.png').convert('L'))
dm = d > 100
dy, dx = np.where(dm)
cand = dm[dy.min():dy.max()+1, dx.min():dx.max()+1]
print('ref', ref.shape, 'dw', cand.shape)
def iou(a,b):
    best=0
    for oy in range(-3,4):
        for ox in range(-3,4):
            c=np.zeros_like(a, dtype=bool)
            y0=max(0,oy); x0=max(0,ox); cy0=max(0,-oy); cx0=max(0,-ox)
            hh=min(a.shape[0]-y0, b.shape[0]-cy0); ww=min(a.shape[1]-x0, b.shape[1]-cx0)
            if hh<=0 or ww<=0: continue
            ap=a[y0:y0+hh, x0:x0+ww]; bp=b[cy0:cy0+hh, cx0:cx0+ww]
            u=(ap|bp).sum()
            if u: best=max(best,(ap&bp).sum()/u)
    return best
print('IoU(截图已固定, DWrite 渲染我们的字体) =', round(iou(ref, cand),3))
