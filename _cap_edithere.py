
import ctypes, ctypes.wintypes as wt, time, subprocess, os
from PIL import Image
user32 = ctypes.windll.user32; gdi32 = ctypes.windll.gdi32
class BIH(ctypes.Structure):
    _fields_=[('biSize',wt.DWORD),('biWidth',wt.LONG),('biHeight',wt.LONG),('biPlanes',wt.WORD),('biBitCount',wt.WORD),('biCompression',wt.DWORD),('biSizeImage',wt.DWORD),('biXPelsPerMeter',wt.LONG),('biYPelsPerMeter',wt.LONG),('biClrUsed',wt.DWORD),('biClrImportant',wt.DWORD)]
out = subprocess.run(['tasklist','/FI','IMAGENAME eq EditHere.exe','/FO','CSV','/NH'], capture_output=True, text=True, encoding='gbk', errors='replace').stdout
pids = set()
for line in out.splitlines():
    parts = line.strip().strip('"').split('","')
    if len(parts) > 1:
        try: pids.add(int(parts[1]))
        except: pass
res = []
def cb(w, l):
    p = wt.DWORD(); user32.GetWindowThreadProcessId(w, ctypes.byref(p))
    if p.value in pids and user32.IsWindowVisible(w):
        b = ctypes.create_unicode_buffer(256); user32.GetWindowTextW(w, b, 256)
        r = wt.RECT(); user32.GetWindowRect(w, ctypes.byref(r))
        if r.right-r.left > 300: res.append((w, b.value, r))
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
print('edithere windows:', [(t, (r.right-r.left, r.bottom-r.top)) for _, t, r in res])
if res:
    h, t, r = res[0]
    w, hh = r.right-r.left, r.bottom-r.top
    hs = user32.GetDC(0); hdc = gdi32.CreateCompatibleDC(hs)
    bmp = gdi32.CreateCompatibleBitmap(hs, w, hh); gdi32.SelectObject(hdc, bmp)
    user32.PrintWindow(h, hdc, 2)
    bi = BIH(); bi.biSize=ctypes.sizeof(BIH); bi.biWidth=w; bi.biHeight=-hh; bi.biPlanes=1; bi.biBitCount=32
    buf = ctypes.create_string_buffer(w*hh*4); gdi32.GetDIBits(hdc, bmp, 0, hh, buf, ctypes.byref(bi), 0)
    Image.frombuffer('RGBA',(w,hh),buf,'raw','BGRA',0,1).convert('RGB').save(r'H:\1\WinFontSwap\_shots\edithere_now.png')
    print('captured edithere window')
