
import ctypes, ctypes.wintypes as wt
from PIL import Image, ImageDraw, ImageFont, ImageChops
import numpy as np

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32

# find settings window
import subprocess
hwnd = None
EnumWindows = user32.EnumWindows
WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)
pid_target = None
for p in subprocess.run(['powershell','-NoProfile','-Command',"(Get-Process SystemSettings | Where-Object {$_.MainWindowHandle -ne 0} | Select-Object -First 1).Id"], capture_output=True, text=True).stdout.split():
    if p.strip().isdigit():
        pid_target = int(p.strip())
print('settings pid', pid_target)

rects = []
def cb(h, l):
    pid = wt.DWORD()
    user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
    if pid.value == pid_target and user32.IsWindowVisible(h):
        r = wt.RECT()
        user32.GetWindowRect(h, ctypes.byref(r))
        if r.right - r.left > 400:
            rects.append((r.left, r.top, r.right, r.bottom))
    return True
EnumWindows(WNDENUMPROC(cb), 0)
print('windows', rects)
if not rects:
    raise SystemExit('no window')
l, t, r, b = max(rects, key=lambda x: (x[2]-x[0])*(x[3]-x[1]))
w, h = r-l, b-t
print('rect', l, t, w, h)

# capture via PrintWindow
hwnd = None
def cb2(hh, l2):
    global hwnd
    pid = wt.DWORD()
    user32.GetWindowThreadProcessId(hh, ctypes.byref(pid))
    if pid.value == pid_target and user32.IsWindowVisible(hh):
        rr = wt.RECT()
        user32.GetWindowRect(hh, ctypes.byref(rr))
        if (rr.right-rr.left) == w:
            hwnd = hh
    return True
EnumWindows(WNDENUMPROC(cb2), 0)

hdc_screen = user32.GetDC(0)
hdc_mem = gdi32.CreateCompatibleDC(hdc_screen)
hbmp = gdi32.CreateCompatibleBitmap(hdc_screen, w, h)
gdi32.SelectObject(hdc_mem, hbmp)
user32.PrintWindow(hwnd, hdc_mem, 2)

class BITMAPINFOHEADER(ctypes.Structure):
    _fields_ = [('biSize', wt.DWORD), ('biWidth', wt.LONG), ('biHeight', wt.LONG), ('biPlanes', wt.WORD), ('biBitCount', wt.WORD), ('biCompression', wt.DWORD), ('biSizeImage', wt.DWORD), ('biXPelsPerMeter', wt.LONG), ('biYPelsPerMeter', wt.LONG), ('biClrUsed', wt.DWORD), ('biClrImportant', wt.DWORD)]
bi = BITMAPINFOHEADER()
bi.biSize = ctypes.sizeof(BITMAPINFOHEADER); bi.biWidth = w; bi.biHeight = -h; bi.biPlanes = 1; bi.biBitCount = 32; bi.biCompression = 0
buf = ctypes.create_string_buffer(w*h*4)
gdi32.GetDIBits(hdc_mem, hbmp, 0, h, buf, ctypes.byref(bi), 0)
img = Image.frombuffer('RGBA', (w, h), buf, 'raw', 'BGRA', 0, 1).convert('RGB')
img.save(r'H:\1\WinFontSwap\_settings_now3.png')
print('captured', img.size)

# find small text band in main content area
arr = np.array(img.convert('L')).astype(np.int16)
region = arr[:, 400:1000]
bright = region > 130
rows = bright.sum(axis=1)
cands = []
y = 0
while y < len(rows):
    if rows[y] > 5:
        y0 = y
        while y < len(rows) and rows[y] > 0:
            y += 1
        y1 = y
        if 8 <= y1-y0 <= 26:
            band = bright[y0:y1, :]
            cols = band.sum(axis=0)
            xs = np.where(cols > 0)[0]
            if len(xs):
                width = xs[-1]-xs[0]+1
                if 120 <= width <= 260:
                    cands.append((y0, y1, 400+xs[0], 400+xs[-1], width))
    else:
        y += 1
print('candidates:')
for c in cands:
    print('  y%d-%d x%d-%d w=%d' % c)
