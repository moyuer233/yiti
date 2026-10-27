
import ctypes, ctypes.wintypes as wt, sys, io
from PIL import Image
import numpy as np
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; gdi32 = ctypes.windll.gdi32
h = 1574674
class BIH(ctypes.Structure):
    _fields_ = [('biSize', wt.DWORD), ('biWidth', wt.LONG), ('biHeight', wt.LONG), ('biPlanes', wt.WORD), ('biBitCount', wt.WORD), ('biCompression', wt.DWORD), ('biSizeImage', wt.DWORD), ('biXPelsPerMeter', wt.LONG), ('biYPelsPerMeter', wt.LONG), ('biClrUsed', wt.DWORD), ('biClrImportant', wt.DWORD)]
r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
w, hh = r.right-r.left, r.bottom-r.top
hdc_s = user32.GetDC(0)
hdc = gdi32.CreateCompatibleDC(hdc_s)
bmp = gdi32.CreateCompatibleBitmap(hdc_s, w, hh)
gdi32.SelectObject(hdc, bmp)
user32.PrintWindow(h, hdc, 2)
bi = BIH(); bi.biSize = ctypes.sizeof(BIH); bi.biWidth = w; bi.biHeight = -hh; bi.biPlanes = 1; bi.biBitCount = 32
buf = ctypes.create_string_buffer(w*hh*4)
gdi32.GetDIBits(hdc, bmp, 0, hh, buf, ctypes.byref(bi), 0)
img = Image.frombuffer('RGBA', (w, hh), buf, 'raw', 'BGRA', 0, 1).convert('RGB')
img.save(r'H:\1\WinFontSwap\_set3.png')
a = np.array(img.convert('L'))
# 找设备名/主板名那两行拉丁文字（左侧卡片里）
reg = a[150:260, 700:1200]
m = reg > 130
rows = m.sum(axis=1)
bands = []
y = 0
while y < len(rows):
    if rows[y] > 3:
        s = y
        while y < len(rows) and rows[y] > 0: y += 1
        e = y
        if 8 <= e-s <= 26:
            xs = np.where(m[s:e,:].sum(axis=0) > 0)[0]
            if len(xs) > 20:
                bands.append((150+s, 150+e, 700+xs[0], 700+xs[-1]))
    else:
        y += 1
print('bands:', bands)
for (y0,y1,x0,x1) in bands[:3]:
    img.crop((x0-2,y0-2,x1+3,y1+3)).resize(((x1-x0+5)*6,(y1-y0+5)*6), Image.NEAREST).save(r'H:\1\WinFontSwap\_line_%d.png' % y0)
print('saved')
