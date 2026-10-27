
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
# 主板型号行（第二 band）：x 550..775, y 159..176，去掉左侧图标：从 636 开始？
shot = Image.open(r'H:\1\WinFontSwap\_set4.png').convert('RGB')
a = np.array(shot.convert('L'))
# 在 band2 里从右侧找文字起点：图标后有空隙
band = a[159:176, 540:800] > 130
cols = band.sum(axis=0)
runs = []
x = 0
while x < len(cols):
    if cols[x] > 0:
        s = x
        while x < len(cols) and cols[x] > 0: x += 1
        runs.append((540+s, 540+x-1))
    else:
        x += 1
print('runs in band2:', runs)
# 取最右侧连续文字块（宽度最大）
runs.sort(key=lambda r: r[1]-r[0], reverse=True)
x0, x1 = runs[0]
print('picked text run', x0, x1, 'w=', x1-x0+1)
y0, y1 = 159, 176
crop = shot.crop((x0-2, y0-2, x1+2, y1+2))
crop_big = crop.resize((crop.width*8, crop.height*8), Image.NEAREST)

txt = 'Z790M Snow Dream D5'
rows = [('SCREENSHOT (settings)', crop_big)]
for name, path, idx in [
    ('HarmonyOS Sans SC', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\HarmonyOS_Sans_SC_Regular.ttf'), 0),
    ('ORIG Segoe UI', r'C:\ProgramData\WinFontSwap\backup\segoe_harmony_20261028_030711\orig\segoeui.ttf', 0),
    ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
]:
    if not os.path.exists(path): 
        print('missing', path); continue
    im = Image.new('RGB', (crop.width, crop.height), (26,30,28))
    f = ImageFont.truetype(path, 13, index=idx)
    ImageDraw.Draw(im).text((1, 1), txt, font=f, fill=(235,235,235))
    rows.append((name, im.resize((im.width*8, im.height*8), Image.NEAREST)))
W = max(r[1].width for r in rows) + 320
H = sum(r[1].height for r in rows) + 40
out = Image.new('RGB', (W, H), (16,16,16))
d = ImageDraw.Draw(out)
lab = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 15)
y = 0
for name, im in rows:
    d.text((6, y+10), name, font=lab, fill=(170,170,170))
    out.paste(im, (310, y))
    y += im.height + 8
out.save(r'H:\1\WinFontSwap\_board_cmp.png')
print('ok', out.size)
