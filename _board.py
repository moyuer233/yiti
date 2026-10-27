
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shot = Image.open(r'H:\1\WinFontSwap\_set3.png').convert('L')
a = np.array(shot)
# 主板型号那行（含图标的 band 里，右侧是文字）
sub = a[155:173, 620:790]
m = sub > 130
ys, xs = np.where(m)
print('raw ink', m[ys.min():ys.max()+1, xs.min():xs.max()+1].shape)
ink = m[ys.min():ys.max()+1, xs.min():xs.max()+1]
txt = 'Z790M Snow Dream D5'
cands = [
  ('HarmonyOS Regular', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\HarmonyOS_Sans_SC_Regular.ttf'), 0),
  ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
  ('ORIG Segoe UI', r'C:\ProgramData\WinFontSwap\backup\segoe_harmony_20261028_030711\orig\segoeui.ttf', 0),
  ('MiSans Regular', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans-Regular.ttf'), 0),
]
def iou(p,q):
    best=0
    for oy in range(-2,3):
        for ox in range(-2,3):
            yy=max(0,oy); xx=max(0,ox); cy=max(0,-oy); cx=max(0,-ox)
            hh=min(p.shape[0]-yy, q.shape[0]-cy); ww=min(p.shape[1]-xx, q.shape[1]-cx)
            if hh<=0 or ww<=0: continue
            ap=p[yy:yy+hh, xx:xx+ww]; bp=q[cy:cy+hh, cx:cx+ww]
            u=(ap|bp).sum()
            if u: best=max(best,(ap&bp).sum()/u)
    return best
for name, path, idx in cands:
    if not os.path.exists(path): print('MISSING', name); continue
    best=(0,0,None)
    for size in (10,11,12,13,14,15):
        im2 = Image.new('L', (260, 30), 0)
        f = ImageFont.truetype(path, size, index=idx)
        ImageDraw.Draw(im2).text((3,4), txt, font=f, fill=255)
        c = np.array(im2) > 100
        yy, xx2 = np.where(c)
        if len(xx2) == 0: continue
        cc = c[yy.min():yy.max()+1, xx2.min():xx2.max()+1]
        s = iou(ink, cc)
        if s > best[0]: best = (s, size, cc.shape)
    print('  %-20s IoU=%.3f @%dpx ink=%s' % (name, best[0], best[1], best[2]))
print('screenshot ink =', ink.shape)
