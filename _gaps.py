
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_set4.png').convert('L')
a = np.array(shot)
m = a[158:177, 632:777] > 130
ys, xs = np.where(m)
ink = m[ys.min():ys.max()+1, xs.min():xs.max()+1]
def gaps(mask):
    cols = mask.sum(axis=0)
    out = []
    x = 0
    while x < len(cols):
        if cols[x] == 0:
            s = x
            while x < len(cols) and cols[x] == 0: x += 1
            if s > 0 and x < len(cols) and (x - s) >= 2:
                out.append((s + x - 1) / 2.0)
        else:
            x += 1
    return out
g_ref = gaps(ink)
w_ref = ink.shape[1]
print('ref width', w_ref, 'gap centers', [round(g,1) for g in g_ref])
txt = 'Z790M Snow Dream D5'
cands = [
  ('HarmonyOS', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\HarmonyOS_Sans_SC_Regular.ttf'), 0),
  ('SegoeUI', r'C:\ProgramData\WinFontSwap\backup\segoe_harmony_20261028_030711\orig\segoeui.ttf', 0),
  ('YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
  ('MiSans', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans-Regular.ttf'), 0),
]
for name, path, idx in cands:
    if not os.path.exists(path): continue
    best = None
    for size in (12, 13, 14):
        f = ImageFont.truetype(path, size, index=idx)
        img = Image.new('L', (300, 30), 0)
        ImageDraw.Draw(img).text((3,4), txt, font=f, fill=255)
        mm = np.array(img) > 100
        yy, xx = np.where(mm)
        c = mm[yy.min():yy.max()+1, xx.min():xx.max()+1]
        g = gaps(c)
        if len(g) != len(g_ref): 
            continue
        # 归一化到相同宽度
        s = c.shape[1] / float(w_ref)
        diff = np.mean([abs(gg/s - rr) for gg, rr in zip(g, g_ref)])
        if best is None or diff < best[0]:
            best = (diff, size, c.shape[1])
    if best:
        print('  %-10s size=%d width=%d  gap平均误差=%.2fpx' % (name, best[1], best[2], best[0]))
    else:
        print('  %-10s gap 数量不匹配' % name)
