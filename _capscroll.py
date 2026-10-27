
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import Image
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; gdi32 = ctypes.windll.gdi32
EXE = r'H:\1\WinFontSwap\dist\WinFontSwapWpf.exe'
class BIH(ctypes.Structure):
    _fields_=[('biSize',wt.DWORD),('biWidth',wt.LONG),('biHeight',wt.LONG),('biPlanes',wt.WORD),('biBitCount',wt.WORD),('biCompression',wt.DWORD),('biSizeImage',wt.DWORD),('biXPelsPerMeter',wt.LONG),('biYPelsPerMeter',wt.LONG),('biClrUsed',wt.DWORD),('biClrImportant',wt.DWORD)]
def grab(h):
    r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
    w, hh = r.right-r.left, r.bottom-r.top
    hs = user32.GetDC(0); hdc = gdi32.CreateCompatibleDC(hs)
    bmp = gdi32.CreateCompatibleBitmap(hs, w, hh); gdi32.SelectObject(hdc, bmp)
    user32.PrintWindow(h, hdc, 2)
    bi = BIH(); bi.biSize=ctypes.sizeof(BIH); bi.biWidth=w; bi.biHeight=-hh; bi.biPlanes=1; bi.biBitCount=32
    buf = ctypes.create_string_buffer(w*hh*4); gdi32.GetDIBits(hdc, bmp, 0, hh, buf, ctypes.byref(bi), 0)
    img = Image.frombuffer('RGBA',(w,hh),buf,'raw','BGRA',0,1).convert('RGB')
    gdi32.DeleteObject(bmp); gdi32.DeleteDC(hdc); user32.ReleaseDC(0, hs)
    return img
def win_for_pid(pid):
    res = []
    def cb(w, l):
        p = wt.DWORD(); user32.GetWindowThreadProcessId(w, ctypes.byref(p))
        if p.value == pid and user32.IsWindowVisible(w):
            b = ctypes.create_unicode_buffer(256); user32.GetWindowTextW(w, b, 256)
            if b.value == '字体替换': res.append(w)
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
    return res
for name, arg in [('top', ''), ('sys', '--scroll:450'), ('adv', '--scroll:1000')]:
    subprocess.run(['taskkill','/F','/IM','WinFontSwapWpf.exe'], capture_output=True)
    time.sleep(0.8)
    cmd = [EXE] + ([arg] if arg else [])
    proc = subprocess.Popen(cmd)
    hwnd = 0
    for _ in range(20):
        time.sleep(0.4)
        ws = win_for_pid(proc.pid)
        if ws: hwnd = ws[0]; break
    if not hwnd:
        print(name, 'no window'); proc.kill(); continue
    time.sleep(0.7)
    grab(hwnd).save(r'H:\1\WinFontSwap\_shots\_v_' + name + '.png')
    print(name, 'captured')
    proc.kill(); time.sleep(0.5)
