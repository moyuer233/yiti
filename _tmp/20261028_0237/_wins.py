
import ctypes, ctypes.wintypes as wt, subprocess, time
from PIL import Image
import numpy as np

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32
kernel32 = ctypes.windll.kernel32

def proc_name(pid):
    h = kernel32.OpenProcess(0x1000, False, pid)
    if not h:
        return ''
    buf = ctypes.create_unicode_buffer(1024)
    size = wt.DWORD(1024)
    ok = kernel32.QueryFullProcessImageNameW(h, 0, buf, ctypes.byref(size))
    kernel32.CloseHandle(h)
    return buf.value if ok else ''

windows = []
WNDENUMPROC = ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)
def cb(h, l):
    if not user32.IsWindowVisible(h):
        return True
    r = wt.RECT()
    user32.GetWindowRect(h, ctypes.byref(r))
    w, hh = r.right-r.left, r.bottom-r.top
    if w < 700 or hh < 400:
        return True
    pid = wt.DWORD()
    user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
    nm = proc_name(pid.value).lower()
    windows.append((h, pid.value, w, hh, nm, r.left, r.top))
    return True
user32.EnumWindows(WNDENUMPROC(cb), 0)
for w in windows:
    print(w[0], w[1], w[2], w[3], w[4].split('\\\\')[-1], w[5], w[6])
