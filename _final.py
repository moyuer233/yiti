
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import Image, ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; k32 = ctypes.windll.kernel32
def find():
    res = []
    def cb(w, l):
        buf = ctypes.create_unicode_buffer(256); user32.GetWindowTextW(w, buf, 256)
        if buf.value == '字体替换' and user32.IsWindowVisible(w): res.append(w)
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
    return res
for p in range(3):
    proc = subprocess.Popen([r'H:\1\WinFontSwap\bin\WinFontSwapWpf.exe', '--page:%d' % p])
    time.sleep(3.2)
    ws = find()
    if not ws: print('no window'); continue
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
    time.sleep(0.7)
    r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
    ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom)).save(r'H:\1\WinFontSwap\_final_%d.png' % p)
    print('page', p, 'ok', r.right-r.left, r.bottom-r.top)
    proc.kill(); time.sleep(0.6)
