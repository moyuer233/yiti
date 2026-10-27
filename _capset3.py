
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_set3.png').convert('L')
a = np.array(shot)
# 设备名在 "ð-me13" 一带，扫描更宽区域
reg = a[140:230, 690:1150]
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
            if len(xs) > 15:
                bands.append((140+s, 140+e, 690+xs[0], 690+xs[-1]))
    else:
        y += 1
print('bands:', bands)
for i, b in enumerate(bands[:4]):
    y0,y1,x0,x1 = b
    print('band', i, b)
    Image.open(r'H:\1\WinFontSwap\_set3.png').convert('RGB').crop((x0-2,y0-2,x1+3,y1+3)).resize(((x1-x0+5)*7,(y1-y0+5)*7), Image.NEAREST).save(r'H:\1\WinFontSwap\_devline_%d.png' % i)
