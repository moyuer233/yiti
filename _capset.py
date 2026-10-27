
import ctypes, ctypes.wintypes as wt, subprocess, time, sys, io
from PIL import Image
import numpy as np
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32
kernel32 = ctypes.windll.kernel32

subprocess.run('start "" ms-settings:', shell=True)
time.sleep(5)

def proc_name(pid):
    h = kernel32.OpenProcess(0x1000, False, pid)
    if not h: return ''
    buf = ctypes.create_unicode_buffer(1024); size = wt.DWORD(1024)
    ok = kernel32.QueryFullProcessImageNameW(h, 0, buf, ctypes.byref(size))
    kernel32.CloseHandle(h)
    return buf.value if ok else ''

wins = []
WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)
def cb(h, l):
    if not user32.IsWindowVisible(h): return True
    r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
    w, hh = r.right-r.left, r.bottom-r.top
    if w < 700 or hh < 400: return True
    pid = wt.DWORD(); user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
    nm = proc_name(pid.value).lower()
    if 'systemsettings' in nm:
        wins.append((h, r.left, r.top, w, hh, nm))
    return True
user32.EnumWindows(WNDENUMPROC(cb), 0)
print('settings windows:', [(x[1],x[2],x[3],x[4]) for x in wins])
if not wins:
    raise SystemExit('no settings window')
h, left, top, w, hh, nm = wins[0]

class BIH(ctypes.Structure):
    _fields_ = [('biSize', wt.DWORD), ('biWidth', wt.LONG), ('biHeight', wt.LONG), ('biPlanes', wt.WORD), ('biBitCount', wt.WORD), ('biCompression', wt.DWORD), ('biSizeImage', wt.DWORD), ('biXPelsPerMeter', wt.LONG), ('biYPelsPerMeter', wt.LONG), ('biClrUsed', wt.DWORD), ('biClrImportant', wt.DWORD)]
hdc_screen = user32.GetDC(0)
hdc = gdi32.CreateCompatibleDC(hdc_screen)
bmp = gdi32.CreateCompatibleBitmap(hdc_screen, w, hh)
gdi32.SelectObject(hdc, bmp)
user32.PrintWindow(h, hdc, 2)
bi = BIH(); bi.biSize = ctypes.sizeof(BIH); bi.biWidth = w; bi.biHeight = -hh; bi.biPlanes = 1; bi.biBitCount = 32
buf = ctypes.create_string_buffer(w*hh*4)
gdi32.GetDIBits(hdc, bmp, 0, hh, buf, ctypes.byref(bi), 0)
img = Image.frombuffer('RGBA', (w, hh), buf, 'raw', 'BGRA', 0, 1).convert('RGB')
img.save(r'H:\1\WinFontSwap\_set3.png')
print('captured', img.size)
