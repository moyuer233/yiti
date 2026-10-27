
import ctypes, ctypes.wintypes as wt, time, sys, io
from PIL import Image, ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32; k32 = ctypes.windll.kernel32

WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)
def enum_all():
    out = []
    def cb(h, l):
        vis = user32.IsWindowVisible(h)
        n = user32.GetWindowTextLengthW(h)
        buf = ctypes.create_unicode_buffer(n + 2)
        user32.GetWindowTextW(h, buf, n + 2)
        r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
        pid = wt.DWORD(); user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        out.append((h, buf.value, (r.left, r.top, r.right-r.left, r.bottom-r.top), vis, pid.value))
        return True
    user32.EnumWindows(WNDENUMPROC(cb), 0)
    return out
wins = enum_all()
import subprocess
out = subprocess.run(['tasklist','/FI','IMAGENAME eq Lyricify Lite.exe','/FO','CSV','/NH'], capture_output=True, text=True, encoding='gbk', errors='replace').stdout
print(out.strip()[:300])
pids = set()
for line in out.splitlines():
    parts = line.strip().strip('"').split('","')
    if len(parts) > 1:
        try: pids.add(int(parts[1]))
        except: pass
print('pids', pids)
for (h, t, rect, vis, pid) in wins:
    if pid in pids and vis and rect[2] > 300:
        print('  win', h, repr(t), rect)
