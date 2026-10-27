
from PIL import Image, ImageDraw, ImageFont
import numpy as np

shot = Image.open(r'H:\1\WinFontSwap\_screen_now.png').convert('L')
# sidebar area
crop = shot.crop((20, 130, 260, 260))
crop = crop.point(lambda v: 255 if v > 120 else 0)
a = np.array(crop) > 0
rows = a.sum(axis=1)
bands = []
y = 0
while y < len(rows):
    if rows[y] > 3:
        y0 = y
        while y < len(rows) and rows[y] > 0:
            y += 1
        y1 = y
        if 8 <= y1 - y0 <= 26:
            band = a[y0:y1, :]
            cols = band.sum(axis=0)
            xs = np.where(cols > 0)[0]
            if len(xs):
                bands.append((y0 + 130, y1 + 130, xs[0] + 20, xs[-1] + 20, xs[-1]-xs[0]+1))
    else:
        y += 1
print('bands:')
for b in bands:
    print('  y%d-%d x%d-%d w=%d' % b)
crop.resize((crop.width*3, crop.height*3), Image.NEAREST).save(r'H:\1\WinFontSwap\_side.png')
