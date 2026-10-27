
from PIL import Image, ImageDraw, ImageFont
txt = open(r'H:\1\WinFontSwap\_match_text.txt', encoding='utf-8').read()
old = r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc'
cur = r'C:\Windows\Fonts\msyh.ttc'
his = Image.open(r'C:\Users\19195\.dsh\attachments\v1\objects\42\423c65c6fe12cd28231c2326f665d69facc4f1e9a91f4bad42c79b740ffcbc69').convert('RGB')

def strip(fontpath, idx, size, bg, fg):
    f = ImageFont.truetype(fontpath, size, index=idx)
    img = Image.new('RGB', (240, 26), bg)
    d = ImageDraw.Draw(img)
    d.text((3, 5), txt, font=f, fill=fg)
    return img

s_old = strip(old, 1, 14, (45,50,45), (235,235,235))
s_new = strip(cur, 1, 14, (45,50,45), (235,235,235))
rows = [his, s_old, s_new]
labels = ['YOUR SCREENSHOT', 'OLD YaHei (face1)', 'CURRENT YaHei (face1)']
W = 1080
out = Image.new('RGB', (W, 3*140), (20,20,20))
y = 0
for im, lab in zip(rows, labels):
    zoom = im.resize((im.width*4, im.height*4), Image.NEAREST)
    out.paste(zoom.crop((0,0,min(zoom.width,W),min(zoom.height,120))), (0, y+16))
    d2 = ImageDraw.Draw(out)
    d2.text((4, y+2), lab, fill=(150,150,150))
    y += 140
out.save(r'H:\1\WinFontSwap\_three.png')
print('ok', out.size)
