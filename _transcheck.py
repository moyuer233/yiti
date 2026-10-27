
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import ImageGrab, Image
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; k32 = ctypes.windll.kernel32
BOX = (420, 156, 1500, 876)
def find():
    res = []
    def cb(w, l):
        buf = ctypes.create_unicode_buffer(256); user32.GetWindowTextW(w, buf, 256)
        if buf.value == '字体替换' and user32.IsWindowVisible(w): res.append(w)
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
    return res
# 1) 先拍一张「没有我们窗口」的底
subprocess.run(['taskkill','/F','/IM','WinFontSwapWpf.exe'], capture_output=True)
time.sleep(1.2)
ImageGrab.grab(bbox=BOX).save(r'H:\1\WinFontSwap\_base.png')
# 2) 起窗口，拍同一区域
proc = subprocess.Popen([r'H:\1\WinFontSwap\bin\WinFontSwapWpf.exe'])
time.sleep(3.5)
ws = find()
h = ws[-1]
for _ in range(4):
    fg = user32.GetForegroundWindow()
    tidFg = user32.GetWindowThreadProcessId(fg, None)
    tidMe = k32.GetCurrentThreadId()
    user32.AttachThreadInput(tidMe, tidFg, True)
    user32.ShowWindow(h, 9); user32.SetForegroundWindow(h); user32.BringWindowToTop(h)
    user32.AttachThreadInput(tidMe, tidFg, False)
    time.sleep(0.3)
    if user32.GetForegroundWindow() == h: break
time.sleep(0.8)
ImageGrab.grab(bbox=BOX).save(r'H:\1\WinFontSwap\_over.png')
proc.kill()
a = Image.open(r'H:\1\WinFontSwap\_base.png').convert('RGB')
b = Image.open(r'H:\1\WinFontSwap\_over.png').convert('RGB')
pts = [(900, 690), (700, 60), (300, 690), (950, 130)]
for p in pts:
    print('at', p, 'base', a.getpixel(p), 'with-window', b.getpixel(p))
