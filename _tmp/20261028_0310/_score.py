
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('L')
a = np.array(shot)
# 已固定
sub = a[368:388, 588:642]
m = sub > 150
ys, xs = np.where(m)
ref = m[ys.min():ys.max()+1, xs.min():xs.max()+1]
print('ref ink', ref.shape)

txt = '已固定'
cands = [
  ('OURS YaHei(MiSans)', r'C:\Windows\Fonts\msyh.ttc', 1),
  ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
  ('DengXian', r'C:\Windows\Fonts\Deng.ttf', 0),
  ('SimSun', r'C:\Windows\Fonts\simsun.ttc', 0),
  ('SegoeUI(orig)', r'C:\ProgramData\WinFontSwap\backup\segoeui_20261028_022949\orig\segoeui.ttf', 0),
]

def score(ref, cand):
    h = min(ref.shape[0], cand.shape[0])
    w = min(ref.shape[1], cand.shape[1])
    best = -1
    for dy in range(-2, 3):
        for dx in range(-2, 3):
            r = ref
            c = np.zeros_like(ref, dtype=bool)
            y0 = max(0, dy); x0 = max(0, dx)
            cy0 = max(0, -dy); cx0 = max(0, -dx)
            hh = min(ref.shape[0]-y0, cand.shape[0]-cy0)
            ww = min(ref.shape[1]-x0, cand.shape[1]-cx0)
            if hh <= 0 or ww <= 0: continue
            import numpy as _n
            rpart = r[y0:y0+hh, x0:x0+ww]
            cpart = cand[cy0:cy0+hh, cx0:cx0+ww]
            inter = (rpart & cpart).sum()
            union = (rpart | cpart).sum()
            if union:
                s = inter/union
                if s > best: best = s
    return best

for name, path, idx in cands:
    row = []
    for size in (13, 14, 15, 16):
        try:
            f = ImageFont.truetype(path, size, index=idx)
            img = Image.new('L', (80, 30), 0)
            d = ImageDraw.Draw(img)
            d.text((4, 6), txt, font=f, fill=255)
            arr = np.array(img) > 100
            ys2, xs2 = np.where(arr)
            if len(xs2) == 0: continue
            cand = arr[ys2.min():ys2.max()+1, xs2.min():xs2.max()+1]
            s = score(ref, cand)
            row.append('%dpx:%.3f' % (size, s))
        except Exception as e:
            row.append('%dpx:ERR' % size)
    print('%-22s %s' % (name, '  '.join(row)))
