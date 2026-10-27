
from PIL import Image
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
img = Image.open(r'H:\1\WinFontSwap\_set3.png').convert('RGB')
bands = [(140, 152, 541, 689), (155, 172, 546, 772), (176, 189, 632, 670)]
parts = []
for i,(y0,y1,x0,x1) in enumerate(bands):
    c = img.crop((x0-2,y0-2,x1+3,y1+3))
    c = c.resize((c.width*6, c.height*6), Image.NEAREST)
    parts.append(c)
W = max(p.width for p in parts) + 20
H = sum(p.height for p in parts) + 20
out = Image.new('RGB', (W, H), (18,18,18))
y = 0
for p in parts:
    out.paste(p, (10, y)); y += p.height + 8
out.save(r'H:\1\WinFontSwap\_bands3.png')
print('ok', out.size)
