
import ctypes, ctypes.wintypes as wt, time, sys, io
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
shell32 = ctypes.windll.shell32; user32 = ctypes.windll.user32
exe = r'H:\1\WinFontSwap\bin\WinFontSwapWpf.exe'
# 模拟资源管理器双击（ShellExecute）
shell32.ShellExecuteW(None, 'open', exe, None, None, 1)
time.sleep(4)
titles = []
def cb(w, l):
    if user32.IsWindowVisible(w):
        n = user32.GetWindowTextLengthW(w); b = ctypes.create_unicode_buffer(n+2); user32.GetWindowTextW(w,b,n+2)
        if b.value: titles.append(b.value)
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
print('windows:', titles)
bad = [t for t in titles if ('安全警告' in t) or ('打开文件' in t) or ('未知发布者' in t)]
print('security dialog:', bad)
print('app window:', [t for t in titles if t == '字体替换'])
