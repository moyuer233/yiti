
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import Image, ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32
k32 = ctypes.windll.kernel32
def find_lf():
    res = []
    def cb(w, l):
        buf = ctypes.create_unicode_buffer(256); user32.GetWindowTextW(w, buf, 256)
        if buf.value == 'L' and user32.IsWindowVisible(w):
            r = wt.RECT(); user32.GetWindowRect(w, ctypes.byref(r))
            if r.right-r.left > 400 and r.bottom-r.top > 400: res.append((w, r))
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
    return res
def focus(h):
    fg = user32.GetForegroundWindow()
    tidFg = user32.GetWindowThreadProcessId(fg, None)
    tidMe = k32.GetCurrentThreadId()
    user32.AttachThreadInput(tidMe, tidFg, True)
    user32.ShowWindow(h, 9); user32.SetForegroundWindow(h)
    user32.AttachThreadInput(tidMe, tidFg, False)
    time.sleep(0.8)
def click(x, y):
    user32.SetCursorPos(int(x), int(y)); time.sleep(0.2)
    user32.mouse_event(0x0002,0,0,0,0); time.sleep(0.05)
    user32.mouse_event(0x0004,0,0,0,0); time.sleep(1.0)
res = find_lf()
print('lf windows', [(h, r.right-r.left, r.bottom-r.top) for h, r in res])
if not res: raise SystemExit('no lf')
h, r = res[0]
x0, y0 = r.left, r.top
w, hh = r.right-r.left, r.bottom-r.top
names = ['general','lyrics','perapp','island','desktop','taskbar','views','hotkey','delay','advanced','support','about']
ys = [100,138,176,214,252,290,328,366,404,442,506,542]
for i in range(len(names)):
    focus(h)
    click(x0 + 100, y0 + ys[i])
    ImageGrab.grab(bbox=(x0, y0, x0+w, y0+hh)).save(r'H:\1\WinFontSwap\_lf_' + names[i] + '.png')
    print('shot', names[i])
