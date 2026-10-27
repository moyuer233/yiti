
import ctypes, ctypes.wintypes as wt, time, sys, io
from PIL import Image, ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; k32 = ctypes.windll.kernel32
H = 1769618
r = wt.RECT(); user32.GetWindowRect(H, ctypes.byref(r))
x0, y0, W, Hh = r.left, r.top, r.right-r.left, r.bottom-r.top
print('LF rect', x0, y0, W, Hh)
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
    return False
def click(x, y):
    user32.SetCursorPos(int(x), int(y)); time.sleep(0.25)
    user32.mouse_event(0x0002,0,0,0,0); time.sleep(0.05)
    user32.mouse_event(0x0004,0,0,0,0); time.sleep(0.9)
names = ['general','lyrics','perapp','island','desktop','taskbar','views','hotkey','delay','advanced','support','about']
ys = [100,138,176,214,252,290,328,366,404,442,506,542]
focus()
for i, n in enumerate(names):
    click(x0 + 100, y0 + ys[i])
    ImageGrab.grab(bbox=(x0, y0, x0+W, y0+Hh)).save(r'H:\1\WinFontSwap\_lfp_' + n + '.png')
print('walked')
# 采样「通用」页颜色
img = Image.open(r'H:\1\WinFontSwap\_lfp_general.png').convert('RGB')
def px(x, y): return img.getpixel((x, y))
print('rail bg      (10,300) ', px(10,300))
print('rail sel     (60,100)  ', px(60,100))
print('content bg   (600,560) ', px(600,560))
print('card bg      (500,120) ', px(500,120))
print('card bg2     (500,350) ', px(500,350))
print('title text   (265,40)  ', px(265,40))
print('window top   (400,4)   ', px(400,4))
