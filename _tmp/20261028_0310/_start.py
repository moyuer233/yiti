
import ctypes, ctypes.wintypes as wt, time, sys, io
from PIL import ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32
VK_LWIN = 0x5B
KEYEVENTF_KEYUP = 0x0002
user32.keybd_event(VK_LWIN, 0, 0, 0)
time.sleep(0.15)
user32.keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, 0)
time.sleep(2.0)
img = ImageGrab.grab()
img.save(r'H:\1\WinFontSwap\_startmenu.png')
print('saved', img.size)
