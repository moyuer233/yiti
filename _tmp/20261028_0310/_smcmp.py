
from PIL import Image, ImageDraw, ImageFont
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('RGB')
crop = shot.crop((608, 348, 700, 380))
crop = crop.resize((crop.width*5, crop.height*5), Image.NEAREST)
crop.save(r'H:\1\WinFontSwap\_sm_crop.png')
print('crop', crop.size)

txt = '已固定'
fonts = [
  ('OURS YaHei(=MiSans)', r'C:\Windows\Fonts\msyh.ttc', 1),
  ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
  ('DengXian', r'C:\Windows\Fonts\Deng.ttf', 0),
  ('SimSun', r'C:\Windows\Fonts\simsun.ttc', 0),
]
rowH = 70
out = Image.new('RGB', (760, rowH*(len(fonts)+1)), (28,28,28))
d = ImageDraw.Draw(out)
out.paste(crop.crop((0,0,min(crop.width,760),min(crop.height,rowH))), (0,0))
y = rowH
lab = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 12)
for name, path, idx in fonts:
    try:
        f = ImageFont.truetype(path, 15, index=idx)
        d.text((4, y+4), name, font=lab, fill=(150,150,150))
        d.text((4, y+22), txt, font=f, fill=(240,240,240))
    except Exception as e:
        d.text((4, y+4), name + ' ERR ' + str(e)[:40], font=lab, fill=(200,80,80))
    y += rowH
out = out.resize((out.width*2, out.height*2), Image.NEAREST)
out.save(r'H:\1\WinFontSwap\_sm_cmp.png')
print('ok', out.size)
