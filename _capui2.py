
import ctypes, ctypes.wintypes as wt, time, sys, io
from PIL import ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32
h = 1048628
user32.ShowWindow(h, 9)
user32.SetForegroundWindow(h)
time.sleep(1.0)
r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
print('rect', r.left, r.top, r.right-r.left, r.bottom-r.top)
img = ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom))
img.save(r'H:\1\WinFontSwap\_ui_one2.png')
print('saved', img.size)
