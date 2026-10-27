
import ctypes, ctypes.wintypes as wt, time, sys, io
from PIL import ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; k32 = ctypes.windll.kernel32
H = 1769618
r = wt.RECT(); user32.GetWindowRect(H, ctypes.byref(r))
x0, y0, W, Hh = r.left, r.top, r.right-r.left, r.bottom-r.top
def focus():
    for _ in range(4):
        fg = user32.GetForegroundWindow()
        tidFg = user32.GetWindowThreadProcessId(fg, None)
        tidMe = k32.GetCurrentThreadId()
        user32.AttachThreadInput(tidMe, tidFg, True)
        user32.ShowWindow(H, 9); user32.SetForegroundWindow(H); user32.BringWindowToTop(H)
        user32.AttachThreadInput(tidMe, tidFg, False)
        time.sleep(0.3)
        if user32.GetForegroundWindow() == H: return True
def click(x, y):
    user32.SetCursorPos(int(x), int(y)); time.sleep(0.25)
    user32.mouse_event(0x0002,0,0,0,0); time.sleep(0.05)
    user32.mouse_event(0x0004,0,0,0,0); time.sleep(0.9)
def shot(n):
    ImageGrab.grab(bbox=(x0, y0, x0+W, y0+Hh)).save(r'H:\1\WinFontSwap\_lfs_' + n + '.png')
focus()
# 1) 逐应用配置
click(x0+100, y0+176); shot('perapp')
# 2) 设备延迟
click(x0+100, y0+404); shot('delay')
# 3) 高级 → 展开 Discord RPC
click(x0+100, y0+442); time.sleep(0.6)
click(x0+755, y0+440); shot('expand')
# 4) 高级 → 打开「通知」下拉
click(x0+100, y0+442); time.sleep(0.5)
click(x0+690, y0+116); shot('dropdown')
print('done')
