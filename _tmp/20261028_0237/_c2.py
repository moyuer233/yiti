
from PIL import Image
im = Image.open(r'H:\1\WinFontSwap\_ui5.png')
crop = im.crop((215, 245, 760, 330))
crop = crop.resize((crop.width*2, crop.height*2), Image.NEAREST)
crop.save(r'H:\1\WinFontSwap\_slots2.png')
print(crop.size)
