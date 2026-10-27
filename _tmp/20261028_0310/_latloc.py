
from PIL import Image
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
img = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('RGB')
a = np.array(img.convert('L'))
# 用户名字样大概在开始菜单左下角
for (x0,x1,y0,y1,label) in [(600,820,960,1010,'bottom-left area'), (600,900,540,600,'app label Steam')]:
    reg = a[y0:y1, x0:x1]
    m = reg > 140
    rows = m.sum(axis=1)
    bands = []
    y = 0
    while y < len(rows):
        if rows[y] > 2:
            s = y
            while y < len(rows) and rows[y] > 0: y += 1
            e = y
            if 6 <= e-s <= 24:
                xs = np.where(m[s:e,:].sum(axis=0) > 0)[0]
                if len(xs) > 4:
                    bands.append((s+y0, e+y0, x0+xs[0], x0+xs[-1]))
        else:
            y += 1
    print(label, '->', bands[:6])
    for b in bands[:2]:
        yy0, yy1, xx0, xx1 = b
        img.crop((xx0-1, yy0-1, xx1+2, yy1+2)).resize(((xx1-xx0+3)*8, (yy1-yy0+3)*8), Image.NEAREST).save(r'H:\1\WinFontSwap\_lat_%d.png' % (yy0))
print('saved crops')
