
import ctypes, ctypes.wintypes as wt, sys, io
from PIL import Image
import numpy as np
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; gdi32 = ctypes.windll.gdi32; kernel32 = ctypes.windll.kernel32
res = []
def cb(h, l):
    try:
        if not user32.IsWindowVisible(h): return True
        r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
        if r.right-r.left < 900 or r.bottom-r.top < 500: return True
        pid = wt.DWORD(); user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        tb = ctypes.create_unicode_buffer(128); user32.GetWindowTextW(h, tb, 128)
        if tb.value.strip() == '设置':
            res.append((int(h), r.left, r.top, r.right-r.left, r.bottom-r.top))
    except Exception:
        pass
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
print('settings window:', res)
if not res:
    raise SystemExit('no settings')
h, left, top, w, hh = res[0]
class BIH(ctypes.Structure):
    _fields_ = [('biSize', wt.DWORD), ('biWidth', wt.LONG), ('biHeight', wt.LONG), ('biPlanes', wt.WORD), ('biBitCount', wt.WORD), ('biCompression', wt.DWORD), ('biSizeImage', wt.DWORD), ('biXPelsPerMeter', wt.LONG), ('biYPelsPerMeter', wt.LONG), ('biClrUsed', wt.DWORD), ('biClrImportant', wt.DWORD)]
hdc_s = user32.GetDC(0); hdc = gdi32.CreateCompatibleDC(hdc_s)
bmp = gdi32.CreateCompatibleBitmap(hdc_s, w, hh); gdi32.SelectObject(hdc, bmp)
user32.PrintWindow(h, hdc, 2)
bi = BIH(); bi.biSize = ctypes.sizeof(BIH); bi.biWidth = w; bi.biHeight = -hh; bi.biPlanes = 1; bi.biBitCount = 32
buf = ctypes.create_string_buffer(w*hh*4)
gdi32.GetDIBits(hdc, bmp, 0, hh, buf, ctypes.byref(bi), 0)
img = Image.frombuffer('RGBA', (w, hh), buf, 'raw', 'BGRA', 0, 1).convert('RGB')
img.save(r'H:\1\WinFontSwap\_set4.png')
a = np.array(img.convert('L'))
reg = a[140:230, 300:1150]; m = reg > 130
rows = m.sum(axis=1); bands = []
y = 0
while y < len(rows):
    if rows[y] > 3:
        s = y
        while y < len(rows) and rows[y] > 0: y += 1
        e = y
        if 8 <= e-s <= 26:
            xs = np.where(m[s:e,:].sum(axis=0) > 0)[0]
            if len(xs) > 30: bands.append((140+s, 140+e, 300+xs[0], 300+xs[-1]))
    else:
        y += 1
print('bands', bands)
for i,b in enumerate(bands[:3]):
    y0,y1,x0,x1 = b
    img.crop((x0-2,y0-2,x1+2,y1+2)).resize(((x1-x0+4)*8,(y1-y0+4)*8), Image.NEAREST).save(r'H:\1\WinFontSwap\_b%d.png' % i)
print('saved')
