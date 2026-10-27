
from PIL import Image
import numpy as np, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
for size in (16,17,18,19):
    a = np.array(Image.open(r'H:\1\WinFontSwap\_orig_%d.png' % size).convert('L')).astype(int)
    b = np.array(Image.open(r'H:\1\WinFontSwap\_dw_%d.png' % size).convert('L')).astype(int)
    if a.shape != b.shape:
        print(size, 'shape differ', a.shape, b.shape); continue
    d = np.abs(a-b)
    print('%dpx  identical=%s  diffpix=%d' % (size, (d > 8).sum() == 0, (d > 8).sum()))
