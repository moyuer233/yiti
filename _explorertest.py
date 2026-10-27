
import ctypes, ctypes.wintypes as wt, time, subprocess
user32 = ctypes.windll.user32
exe = r'H:\1\WinFontSwap\dist\WinFontSwapWpf.exe'
def titles():
    out = []
    def cb(w, l):
        if user32.IsWindowVisible(w):
            n = user32.GetWindowTextLengthW(w); b = ctypes.create_unicode_buffer(n+2); user32.GetWindowTextW(w,b,n+2)
            if b.value: out.append(b.value)
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
    return out
subprocess.run(['taskkill','/F','/IM','WinFontSwapWpf.exe'], capture_output=True)
time.sleep(1)
# 用 explorer 走真实的“资源管理器启动”路径
subprocess.run(['explorer.exe', exe])
res = None
for i in range(10):
    time.sleep(0.6)
    t = titles()
    warn = [x for x in t if ('安全警告' in x or '打开文件' in x)]
    app = [x for x in t if x == '字体替换']
    if warn or app:
        print(i, 'warn=', warn, 'app=', app)
        break
else:
    print('nothing')
subprocess.run(['taskkill','/F','/IM','WinFontSwapWpf.exe'], capture_output=True)
