
import ctypes, ctypes.wintypes as wt, subprocess, time, sys
from PIL import Image, ImageGrab, ImageFont
import numpy as np

subprocess.run('start "" ms-settings:', shell=True)
time.sleep(5)
shot = ImageGrab.grab(all_screens=True)
shot.save(r'H:\1\WinFontSwap\_set_live.png')
g = shot.convert('L')
arr = np.array(g)

# locate the small line: search large area for a text band ~10-16 px tall, 150-210 wide
best = None
for y in range(500, min(1040, arr.shape[0]-16)):
    row = arr[y, 300:1100]
    if (row > 120).sum() < 5:
        continue
    y0 = y
    y1 = y
    while y1 + 1 < arr.shape[0] and (arr[y1+1, 300:1100] > 120).sum() > 0 and y1 - y0 < 20:
        y1 += 1
    if not (9 <= y1 - y0 + 1 <= 17):
        continue
    band = arr[y0:y1+1, 300:1100] > 120
    cols = band.sum(axis=0)
    xs = np.where(cols > 0)[0]
    if len(xs) == 0:
        continue
    w = xs[-1] - xs[0] + 1
    if 150 <= w <= 210:
        cand = (y0, y1, 300+xs[0], 300+xs[-1], w)
        print('candidate', cand)
        best = cand
print('chosen', best)
if best is None:
    sys.exit(1)
y0, y1, x0, x1, w = best
crop = g.crop((x0, y0, x1+1, y1+1))
crop.resize((crop.width*5, crop.height*5), Image.NEAREST).save(r'H:\1\WinFontSwap\_line_live.png')
print('crop', crop.size)
