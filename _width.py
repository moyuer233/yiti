
from PIL import Image, ImageDraw, ImageFont
import numpy as np, sys, io, os
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
txt = 'Z790M Snow Dream D5'
print('截图 ink 宽度 = 142')
cands = [
  ('HarmonyOS Sans SC', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\HarmonyOS_Sans_SC_Regular.ttf'), 0),
  ('ORIG Segoe UI', r'C:\ProgramData\WinFontSwap\backup\segoe_harmony_20261028_030711\orig\segoeui.ttf', 0),
  ('ORIG YaHei', r'C:\ProgramData\WinFontSwap\backup\20261028_014731\msyh.ttc', 1),
  ('MiSans Regular', os.path.expandvars(r'%LOCALAPPDATA%\Microsoft\Windows\Fonts\MiSans-Regular.ttf'), 0),
]
for name, path, idx in cands:
    if not os.path.exists(path): print('missing', name); continue
    line = []
    for size in (12, 13, 14):
        f = ImageFont.truetype(path, size, index=idx)
        img = Image.new('L', (300, 30), 0)
        ImageDraw.Draw(img).text((3,4), txt, font=f, fill=255)
        a = np.array(img) > 100
        ys, xs = np.where(a)
        w = xs.max()-xs.min()+1
        line.append('%dpx=%d' % (size, w))
    print('  %-20s %s' % (name, '  '.join(line)))
