
from PIL import Image, ImageDraw, ImageFont
txt = open(r'H:\1\WinFontSwap\_match_text.txt', encoding='utf-8').read()
old = r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc'
cur = r'C:\Windows\Fonts\msyh.ttc'
img = Image.new('RGB', (900, 200), (250,250,250))
d = ImageDraw.Draw(img)
f1 = ImageFont.truetype(old, 16, index=1)
f2 = ImageFont.truetype(cur, 16, index=1)
d.text((6, 10), 'OLD yahei:', font=ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 11), fill=(120,120,120))
d.text((6, 30), txt, font=f1, fill=(0,0,0))
d.text((6, 106), 'NEW misans:', font=ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 11), fill=(120,120,120))
d.text((6, 126), txt, font=f2, fill=(0,0,0))
img = img.resize((900*2, 200*2), Image.NEAREST)
img.save(r'H:\1\WinFontSwap\_sm.png')
print('ok')
