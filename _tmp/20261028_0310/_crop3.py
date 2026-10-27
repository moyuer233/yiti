
from PIL import Image
img = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('RGB')
c = img.crop((600, 480, 1500, 720))
c = c.resize((c.width, c.height))
c.save(r'H:\1\WinFontSwap\_smlower.png')
print(c.size)
