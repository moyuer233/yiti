
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
txt = '已固定'
shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('RGB')
crop = shot.crop((700, 399, 748, 420))
crop_big = crop.resize((crop.width*10, crop.height*10), Image.NEAREST)

rows = [('SCREENSHOT start menu', crop_big)]
for name, path, idx in [
    ('OURS (MiSans)', r'C:\Windows\Fonts\msyh.ttc', 1),
    ('ORIGINAL YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
]:
    img = Image.new('RGB', (48, 21), (26,26,26))
    f = ImageFont.truetype(path, 14, index=idx)
    ImageDraw.Draw(img).text((1, 3), txt, font=f, fill=(240,240,240))
    rows.append((name, img.resize((img.width*10, img.height*10), Image.NEAREST)))

W = 560
H = sum(r[1].height for r in rows) + 40
out = Image.new('RGB', (W, H), (16,16,16))
d = ImageDraw.Draw(out)
lab = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 14)
y = 0
for name, im in rows:
    d.text((6, y + 8), name, font=lab, fill=(170,170,170))
    out.paste(im, (240, y))
    y += im.height + 8
out.save(r'H:\1\WinFontSwap\_final_cmp2.png')
print('ok', out.size)

# 数值比对
mm = np.array(shot.convert('L'))[399:420, 700:748] > 150
ys, xs = np.where(mm)
ref = mm[ys.min():ys.max()+1, xs.min():xs.max()+1]
def iou(p, q):
    best = 0
    for oy in range(-2,3):
        for ox in range(-2,3):
            y0=max(0,oy); x0=max(0,ox); cy0=max(0,-oy); cx0=max(0,-ox)
            hh=min(p.shape[0]-y0, q.shape[0]-cy0); ww=min(p.shape[1]-x0, q.shape[1]-cx0)
            if hh<=0 or ww<=0: continue
            ap=p[y0:y0+hh, x0:x0+ww]; bp=q[cy0:cy0+hh, cx0:cx0+ww]
            u=(ap|bp).sum()
            if u: best=max(best,(ap&bp).sum()/u)
    return best
print('ref ink', ref.shape)
for name, path, idx in [
    ('OURS MiSans', r'C:\Windows\Fonts\msyh.ttc', 1),
    ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
    ('DengXian', r'C:\Windows\Fonts\Deng.ttf', 0),
]:
    best = (0, 0)
    for size in (12,13,14,15,16):
        img = Image.new('L', (60, 30), 0)
        f = ImageFont.truetype(path, size, index=idx)
        ImageDraw.Draw(img).text((3, 4), txt, font=f, fill=255)
        c = np.array(img) > 100
        yy, xx = np.where(c)
        if len(xx) == 0: continue
        cc = c[yy.min():yy.max()+1, xx.min():xx.max()+1]
        s = iou(ref, cc)
        if s > best[0]: best = (s, size)
    print('  %-12s best IoU=%.3f @%dpx' % (name, best[0], best[1]))
