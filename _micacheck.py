
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; k32 = ctypes.windll.kernel32
subprocess.Popen([r'H:\1\WinFontSwap\bin\WinFontSwapWpf.exe'])
time.sleep(3.5)
h = 0
def cb(w, l):
    global h
    buf = ctypes.create_unicode_buffer(256); user32.GetWindowTextW(w, buf, 256)
    if buf.value == '字体替换' and user32.IsWindowVisible(w): h = w
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
print('hwnd', h)
def focus():
    for _ in range(4):
        fg = user32.GetForegroundWindow()
        tidFg = user32.GetWindowThreadProcessId(fg, None)
        tidMe = k32.GetCurrentThreadId()
        user32.AttachThreadInput(tidMe, tidFg, True)
        user32.ShowWindow(h, 9); user32.SetForegroundWindow(h); user32.BringWindowToTop(h)
        user32.AttachThreadInput(tidMe, tidFg, False)
        time.sleep(0.5)
        if user32.GetForegroundWindow() == h: return True
    return False
print('focused', focus())
time.sleep(0.8)
r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
img = ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom))
img.save(r'H:\1\WinFontSwap\_mica_real.png')
print('saved', img.size)
