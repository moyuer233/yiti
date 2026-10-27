
import ctypes, ctypes.wintypes as wt, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
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
    if not user32.IsWindowVisible(h): return True
    r = wt.RECT(); user32.GetWindowRect(h, ctypes.byref(r))
    w, hh = r.right-r.left, r.bottom-r.top
    if w < 500 or hh < 300: return True
    pid = wt.DWORD(); user32.GetWindowThreadProcessId(h, ctypes.byref(pid))
    n = len(user32.GetWindowTextW(h, ctypes.create_unicode_buffer(0), 0))
    tb = ctypes.create_unicode_buffer(256)
    user32.GetWindowTextW(h, tb, 256)
    res.append((h, pname(pid.value).split('\\')[-1], tb.value[:40], w, hh, r.left, r.top))
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
for x in res:
    print(x)
