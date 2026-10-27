
from PIL import Image, ImageDraw, ImageFont
import os, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
txt = 'Δ-ME13'
cands = [
  ('OURS YaHei (HarmonyOS)', r'C:\Windows\Fonts\msyh.ttc', 0),
  ('OURS Segoe UI', r'C:\Windows\Fonts\segoeui.ttf', 0),
  ('OURS SegUIVar', r'C:\Windows\Fonts\SegUIVar.ttf', 0),
  ('HarmonyOS Regular', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\HarmonyOS_Sans_SC_Regular.ttf'), 0),
  ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 0),
]
for name, path, idx in cands:
    try:
        f = ImageFont.truetype(path, 14, index=idx)
        img = Image.new('L', (120, 24), 255)
        d = ImageDraw.Draw(img)
        d.text((2, 4), txt, font=f, fill=0)
        # 判断 Δ 是否有字形：渲染 Δ 单独看是否非空
        img2 = Image.new('L', (40, 24), 255)
        ImageDraw.Draw(img2).text((2, 4), 'Δ', font=f, fill=0)
        import numpy as np
        ink = (np.array(img2) < 128).sum()
        print('%-26s Δ ink=%3d  path=%s' % (name, ink, os.path.basename(path)))
    except Exception as e:
        print('%-26s ERR %s' % (name, str(e)[:60]))
