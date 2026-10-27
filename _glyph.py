
import ctypes, ctypes.wintypes as wt, time, subprocess
from PIL import Image
user32 = ctypes.windll.user32; gdi32 = ctypes.windll.gdi32
class BIH(ctypes.Structure):
    _fields_=[('biSize',wt.DWORD),('biWidth',wt.LONG),('biHeight',wt.LONG),('biPlanes',wt.WORD),('biBitCount',wt.WORD),('biCompression',wt.DWORD),('biSizeImage',wt.DWORD),('biXPelsPerMeter',wt.LONG),('biYPelsPerMeter',wt.LONG),('biClrUsed',wt.DWORD),('biClrImportant',wt.DWORD)]
proc = subprocess.Popen([r'H:\1\WinFontSwap\bin\GlyphProbe.exe'])
time.sleep(2.5)
h = 0
def cb(w,l):
    global h
    n = user32.GetWindowTextLengthW(w); b = ctypes.create_unicode_buffer(n+2); user32.GetWindowTextW(w,b,n+2)
    if b.value == 'glyphprobe': h = w
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
w_, h_ = r.right-r.left, r.bottom-r.top
hs = user32.GetDC(0); hdc = gdi32.CreateCompatibleDC(hs); bmp = gdi32.CreateCompatibleBitmap(hs, w_, h_); gdi32.SelectObject(hdc, bmp)
user32.PrintWindow(h, hdc, 2)
bi = BIH(); bi.biSize=ctypes.sizeof(BIH); bi.biWidth=w_; bi.biHeight=-h_; bi.biPlanes=1; bi.biBitCount=32
buf = ctypes.create_string_buffer(w_*h_*4); gdi32.GetDIBits(hdc, bmp, 0, h_, buf, ctypes.byref(bi), 0)
Image.frombuffer('RGBA',(w_,h_),buf,'raw','BGRA',0,1).convert('RGB').save(r'H:\1\WinFontSwap\_glyphs.png')
proc.kill()
print('saved', w_, h_)
