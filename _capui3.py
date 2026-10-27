
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
from PIL import ImageGrab
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
subprocess.run('start "" "H:\\1\\WinFontSwap\\bin\\WinFontSwap.exe" --captureable --demo "C:\\Windows\\Fonts\\Deng.ttf"', shell=True)
time.sleep(4)
user32 = ctypes.windll.user32; kernel32 = ctypes.windll.kernel32
def pname(pid):
    h = kernel32.OpenProcess(0x1000, False, pid)
    if not h: return ''
    b = ctypes.create_unicode_buffer(1024); s = wt.DWORD(1024)
    ok = kernel32.QueryFullProcessImageNameW(h, 0, b, ctypes.byref(s))
    kernel32.CloseHandle(h)
    return b.value if ok else ''
res = []
def cb(h, l):
    try:
        if not user32.IsWindowVisible(h): return True
        r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
        if r.right-r.left < 800 or r.bottom-r.top < 500: return True
        pid = wt.DWORD(); user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
        if 'winfontswap' in pname(pid.value).lower():
            res.append(int(h))
    except Exception: pass
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
if not res: raise SystemExit('none')
h = res[0]
user32.ShowWindow(h, 9); user32.SetForegroundWindow(h)
time.sleep(1.0)
r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
ImageGrab.grab(bbox=(r.left, r.top, r.right, r.bottom)).save(r'H:\1\WinFontSwap\_ui_one3.png')
print('ok')
