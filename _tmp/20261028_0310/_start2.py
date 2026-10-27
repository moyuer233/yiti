
import ctypes, time, sys, io
from PIL import ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32
VK_LWIN = 0x5B
user32.keybd_event(VK_LWIN, 0, 0, 0); time.sleep(0.15); user32.keybd_event(VK_LWIN, 0, 2, 0)
time.sleep(2.0)
ImageGrab.grab().save(r'H:\1\WinFontSwap\_start2.png')
print('ok')
