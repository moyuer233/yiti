
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('L')
a = np.array(shot)
# 开始菜单面板大致在 x 570..1450, y 280..1010；找「已固定」这一行
reg = a[380:460, 660:860]
m = reg > 150
rows = m.sum(axis=1)
bands = []
y = 0
while y < len(rows):
    if rows[y] > 3:
        y0 = y
        while y < len(rows) and rows[y] > 0: y += 1
        y1 = y
        if 8 <= y1-y0 <= 30:
            band = m[y0:y1, :]
            xs = np.where(band.sum(axis=0) > 0)[0]
            if len(xs) > 8:
                bands.append((380+y0, 380+y1, 660+xs[0], 660+xs[-1]))
    else:
        y += 1
print('bands:', bands)
