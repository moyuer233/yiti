
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('L')
a = np.array(shot)
# 在开始菜单区域找亮文字行
reg = a[330:420, 590:760]
mask = reg > 150
rows = mask.sum(axis=1)
bands = []
y = 0
while y < len(rows):
    if rows[y] > 3:
        y0 = y
        while y < len(rows) and rows[y] > 0: y += 1
        y1 = y
        if 8 <= y1-y0 <= 30:
            band = mask[y0:y1, :]
            cols = band.sum(axis=0)
            xs = np.where(cols > 0)[0]
            if len(xs) > 10:
                bands.append((y0+330, y1+330, 590+xs[0], 590+xs[-1]))
    else:
        y += 1
print('bands:', bands)
if bands:
    y0, y1, x0, x1 = bands[0]
    print('using', (x0, y0, x1, y1), 'size', (x1-x0+1, y1-y0+1))
    crop = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('RGB').crop((x0-2, y0-2, x1+3, y1+3))
    crop.resize((crop.width*6, crop.height*6), Image.NEAREST).save(r'H:\1\WinFontSwap\_sm_crop2.png')
    print('opened crop saved')
