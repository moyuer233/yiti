
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32
subprocess.Popen([r'H:\1\WinFontSwap\bin\WinFontSwapWpf.exe'])
time.sleep(3.5)
h = 0
def cb(w, l):
    global h
    pid = wt.DWORD(); user32.GetWindowThreadProcessId(w, ctypes.byref(pid))
    buf = ctypes.create_unicode_buffer(512)
    user32.GetWindowTextW(w, buf, 512)
    if buf.value == '字体替换' and user32.IsWindowVisible(w):
        h = w
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
print('hwnd', h)
if not h: raise SystemExit('no window')
fg = user32.GetForegroundWindow()
tidFg = user32.GetWindowThreadProcessId(fg, None)
tidMe = ctypes.windll.kernel32.GetCurrentThreadId()
user32.AttachThreadInput(tidMe, tidFg, True)
user32.SetForegroundWindow(h)
user32.AttachThreadInput(tidMe, tidFg, False)
time.sleep(1.0)
r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
x0, y0, wdt, hgt = r.left, r.top, r.right-r.left, r.bottom-r.top
print('rect', x0, y0, wdt, hgt)
def shot(name):
    ImageGrab.grab(bbox=(x0, y0, x0+wdt, y0+hgt)).save(r'H:\1\WinFontSwap\_pg_' + name + '.png')
def click(x, y):
    user32.SetCursorPos(x, y)
    time.sleep(0.2)
    user32.mouse_event(0x0002, 0, 0, 0, 0)
    time.sleep(0.05)
    user32.mouse_event(0x0004, 0, 0, 0, 0)
    time.sleep(0.9)
# 三个导航项：rail 起点 (12,44)，每项约 37px
ys = [44+18, 44+18+37, 44+18+74]
names = ['font', 'link', 'preset']
for i in range(3):
    click(x0 + 100, y0 + ys[i])
    shot(names[i])
print('done')
