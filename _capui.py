
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import Image
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
subprocess.run('start "" "H:\\1\\WinFontSwap\\bin\\WinFontSwap.exe" --captureable --demo "C:\\Windows\\Fonts\\Deng.ttf"', shell=True)
time.sleep(4)
user32 = ctypes.windll.user32; gdi32 = ctypes.windll.gdi32; kernel32 = ctypes.windll.kernel32
def pname(pid):
    h = kernel32.OpenProcess(0x1000, False, pid)
    if not h: return ''
    b = ctypes.create_unicode_buffer(1024); s = wt.DWORD(1024)
    ok = kernel32.QueryFullProcessImageNameW(h, 0, b, ctypes.byref(s))
    kernel32.CloseHandle(h)
    return b.value if ok else ''
res = []
def cb(h, l):
    try:
        if not user32.IsWindowVisible(h): return True
        r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
        w, hh = r.right-r.left, r.bottom-r.top
        if w < 800 or hh < 500: return True
        pid = wt.DWORD(); user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        if 'winfontswap' in pname(pid.value).lower():
            tb = ctypes.create_unicode_buffer(128); user32.GetWindowTextW(h, tb, 128)
            res.append((int(h), r.left, r.top, w, hh, tb.value))
    except Exception: pass
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
print('app windows:', res)
if not res: raise SystemExit('none')
h, left, top, w, hh, title = res[0]
class BIH(ctypes.Structure):
    _fields_ = [('biSize', wt.DWORD), ('biWidth', wt.LONG), ('biHeight', wt.LONG), ('biPlanes', wt.WORD), ('biBitCount', wt.WORD), ('biCompression', wt.DWORD), ('biSizeImage', wt.DWORD), ('biXPelsPerMeter', wt.LONG), ('biYPelsPerMeter', wt.LONG), ('biClrUsed', wt.DWORD), ('biClrImportant', wt.DWORD)]
hdcs = user32.GetDC(0); hdc = gdi32.CreateCompatibleDC(hdcs)
bmp = gdi32.CreateCompatibleBitmap(hdcs, w, hh); gdi32.SelectObject(hdc, bmp)
user32.PrintWindow(h, hdc, 2)
bi = BIH(); bi.biSize = ctypes.sizeof(BIH); bi.biWidth = w; bi.biHeight = -hh; bi.biPlanes = 1; bi.biBitCount = 32
buf = ctypes.create_string_buffer(w*hh*4)
gdi32.GetDIBits(hdc, bmp, 0, hh, buf, ctypes.byref(bi), 0)
Image.frombuffer('RGBA', (w, hh), buf, 'raw', 'BGRA', 0, 1).convert('RGB').save(r'H:\1\WinFontSwap\_ui_one.png')
print('saved', w, hh)
