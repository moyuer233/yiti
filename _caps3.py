
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import Image
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; gdi32 = ctypes.windll.gdi32
class BIH(ctypes.Structure):
    _fields_ = [('biSize', wt.DWORD), ('biWidth', wt.LONG), ('biHeight', wt.LONG), ('biPlanes', wt.WORD),
                ('biBitCount', wt.WORD), ('biCompression', wt.DWORD), ('biSizeImage', wt.DWORD),
                ('biXPelsPerMeter', wt.LONG), ('biYPelsPerMeter', wt.LONG), ('biClrUsed', wt.DWORD), ('biClrImportant', wt.DWORD)]
def grab(h):
    r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
    w, hh = r.right-r.left, r.bottom-r.top
    hs = user32.GetDC(0); hdc = gdi32.CreateCompatibleDC(hs)
    bmp = gdi32.CreateCompatibleBitmap(hs, w, hh); gdi32.SelectObject(hdc, bmp)
    user32.PrintWindow(h, hdc, 2)
    bi = BIH(); bi.biSize = ctypes.sizeof(BIH); bi.biWidth = w; bi.biHeight = -hh; bi.biPlanes = 1; bi.biBitCount = 32
    buf = ctypes.create_string_buffer(w*hh*4)
    gdi32.GetDIBits(hdc, bmp, 0, hh, buf, ctypes.byref(bi), 0)
    img = Image.frombuffer('RGBA', (w, hh), buf, 'raw', 'BGRA', 0, 1).convert('RGB')
    gdi32.DeleteObject(bmp); gdi32.DeleteDC(hdc); user32.ReleaseDC(0, hs)
    return img
def find():
    res = []
    def cb(w, l):
        buf = ctypes.create_unicode_buffer(512); user32.GetWindowTextW(w, buf, 512)
        if buf.value == '字体替换' and user32.IsWindowVisible(w): res.append(w)
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
    return res
for p in range(3):
    subprocess.Popen([r'H:\1\WinFontSwap\bin\WinFontSwapWpf.exe', '--page:%d' % p])
    time.sleep(3.2)
    ws = find()
    if not ws: print('page', p, 'no window'); continue
    h = ws[-1]
    img = grab(h)
    img.save(r'H:\1\WinFontSwap\_p3_%d.png' % p)
    print('page', p, 'saved', img.size)
    for w in ws:
        try:
            pid = wt.DWORD(); user32.GetWindowThreadProcessId(w, ctypes.byref(pid))
            subprocess.run(['taskkill','/F','/PID',str(pid.value)], capture_output=True)
        except Exception: pass
    time.sleep(0.6)
