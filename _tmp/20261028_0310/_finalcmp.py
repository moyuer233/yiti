
from PIL import Image, ImageDraw, ImageFont
import sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
txt = '已固定'
shot = Image.open(r'H:\1\WinFontSwap\_startmenu.png').convert('RGB')
crop = shot.crop((586, 364, 646, 390))
crop = crop.resize((crop.width*8, crop.height*8), Image.NEAREST)

rows = [('SCREENSHOT (start menu)', None)]
for name, path, idx in [
    ('OURS (MiSans)', r'C:\Windows\Fonts\msyh.ttc', 1),
    ('ORIGINAL YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
]:
    img = Image.new('RGB', (60, 26), (32,32,32))
    f = ImageFont.truetype(path, 17, index=idx)
    ImageDraw.Draw(img).text((2, 3), txt, font=f, fill=(240,240,240))
    rows.append((name, img.resize((img.width*8, img.height*8), Image.NEAREST)))

W = max(r[1].width if r[1] else crop.width for r in rows) + 260
H = sum((r[1].height if r[1] else crop.height) for r in rows) + 20
out = Image.new('RGB', (W, H), (18,18,18))
d = ImageDraw.Draw(out)
lab = ImageFont.truetype(r'C:\Windows\Fonts\segoeui.ttf', 16)
y = 0
for name, im in rows:
    d.text((6, y + 10), name, font=lab, fill=(160,160,160))
    out.paste(im if im else crop, (250, y))
    y += (im.height if im else crop.height) + 8
out.save(r'H:\1\WinFontSwap\_final_cmp.png')
print('ok', out.size)
