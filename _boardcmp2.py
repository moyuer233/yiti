
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_set4.png').convert('RGB')
a = np.array(shot.convert('L'))
y0, y1 = 158, 177
x0, x1 = 632, 777
m = a[y0:y1, x0:x1] > 130
ys, xs = np.where(m)
ink = m[ys.min():ys.max()+1, xs.min():xs.max()+1]
print('text ink', ink.shape)
bx0, by0 = x0+xs.min(), y0+ys.min()
bx1, by1 = x0+xs.max(), y0+ys.max()
crop = shot.crop((bx0-2, by0-2, bx1+2, by1+2))
crop_big = crop.resize((crop.width*8, crop.height*8), Image.NEAREST)
txt = 'Z790M Snow Dream D5'
rows = [('SCREENSHOT settings', crop_big)]
for name, path, idx in [
    ('HarmonyOS Sans SC', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\HarmonyOS_Sans_SC_Regular.ttf'), 0),
    ('ORIG Segoe UI', r'C:\ProgramData\WinFontSwap\backup\segoe_harmony_20261028_030711\orig\segoeui.ttf', 0),
    ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
]:
    if not os.path.exists(path): continue
    im = Image.new('RGB', (crop.width, crop.height), (26,30,28))
    f = ImageFont.truetype(path, 13, index=idx)
    ImageDraw.Draw(im).text((1, 1), txt, font=f, fill=(235,235,235))
    rows.append((name, im.resize((im.width*8, im.height*8), Image.NEAREST)))
W = max(r[1].width for r in rows) + 260
H = sum(r[1].height for r in rows) + 30
out = Image.new('RGB', (W, H), (16,16,16))
d = ImageDraw.Draw(out)
lab = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 14)
y = 0
for name, im in rows:
    d.text((6, y+8), name, font=lab, fill=(170,170,170))
    out.paste(im, (250, y))
    y += im.height + 6
out.save(r'H:\1\WinFontSwap\_board_cmp2.png')
print('ok', out.size)
