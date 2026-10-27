
from PIL import ImageGrab
img = ImageGrab.grab(all_screens=True)
img.save(r'H:\1\WinFontSwap\_screen_now.png')
print(img.size)
