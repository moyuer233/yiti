
from PIL import Image, ImageDraw, ImageFont
import numpy as np

txt = open(r'H:\1\WinFontSwap\_match_text.txt', encoding='utf-8').read()
old = r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc'
cur = r'C:\Windows\Fonts\msyh.ttc'

def render(path, size, idx):
    f = ImageFont.truetype(path, size, index=idx)
    img = Image.new('L', (400, size*3), 255)
    d = ImageDraw.Draw(img)
    d.text((4, 4), txt, font=f, fill=0)
    a = np.array(img) < 128
    ys, xs = np.where(a)
    if len(xs) == 0:
        return None
    return a[ys.min():ys.max()+1, xs.min():xs.max()+1]

for size in (12, 13, 14, 15, 16):
    # face 1 in original = 微软雅黑 ; face 1 in current = Microsoft YaHei UI
    o = render(old, size, 1)
    c = render(cur, size, 1)
    if o is None or c is None:
        print(size, 'render fail'); continue
    print('size %d  old %dx%d  cur %dx%d' % (size, o.shape[1], o.shape[0], c.shape[1], c.shape[0]))
    h = min(o.shape[0], c.shape[0]); w = min(o.shape[1], c.shape[1])
    oo = o[:h, :w]; cc = c[:h, :w]
    diff = int((oo != cc).sum()); tot = int(max(oo.sum(), cc.sum()))
    print('   differing px=%d  ink=%d  diff%%=%.1f' % (diff, tot, diff*100.0/max(1,tot)))
