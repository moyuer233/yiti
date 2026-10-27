
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

ours = r'C:\Windows\Fonts\msyh.ttc'
orig = r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc'

def render(path, txt, size, idx=1):
    f = ImageFont.truetype(path, size, index=idx)
    img = Image.new('L', (60, 40), 0)
    ImageDraw.Draw(img).text((6, 8), txt, font=f, fill=255)
    a = np.array(img) > 100
    ys, xs = np.where(a)
    if len(xs) == 0: return None
    return a[ys.min():ys.max()+1, xs.min():xs.max()+1]

def iou(a, b):
    h = min(a.shape[0], b.shape[0]); w = min(a.shape[1], b.shape[1])
    best = 0
    for dy in (-1,0,1):
        for dx in (-1,0,1):
            y0=max(0,dy); x0=max(0,dx); cy0=max(0,-dy); cx0=max(0,-dx)
            hh=min(a.shape[0]-y0, b.shape[0]-cy0); ww=min(a.shape[1]-x0, b.shape[1]-cx0)
            if hh<=0 or ww<=0: continue
            ap=a[y0:y0+hh, x0:x0+ww]; bp=b[cy0:cy0+hh, cx0:cx0+ww]
            u=(ap|bp).sum()
            if u: best=max(best,(ap&bp).sum()/u)
    return best

chars = '已固定全部搜索应用设置文档推荐'
sizes = (13,14,15,16)
res = []
for ch in chars:
    best = 0; bestinfo=''
    for s in sizes:
        a = render(ours, ch, s); b = render(orig, ch, s)
        if a is None or b is None: continue
        sc = iou(a,b)
        if sc > best: best, bestinfo = sc, '%dpx' % s
    res.append((best, ch, bestinfo))
res.sort()
print('=== 最不相似的字符（越小越容易分辨）===')
for sc, ch, info in res[:8]:
    print('  %s  同尺寸 IoU=%.3f (%s)' % (ch, sc, info))
print('=== 最相似的 ===')
for sc, ch, info in res[-3:]:
    print('  %s  IoU=%.3f (%s)' % (ch, sc, info))
