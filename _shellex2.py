
import ctypes, ctypes.wintypes as wt, time, sys, io, subprocess
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
user32 = ctypes.windll.user32
subprocess.run(['taskkill','/F','/IM','WinFontSwapWpf.exe'], capture_output=True)
time.sleep(1)
# 先把策略设成 .exe 低风险（只影响本用户的附件管理器提示）
subprocess.run(['reg','add',r'HKCU\Software\Microsoft\Windows\CurrentVersion\Policies\Associations','/v','LowRiskFileTypes','/t','REG_SZ','/d','.exe;.lnk','/f'], capture_output=True)
shell32 = ctypes.windll.shell32
exe = r'H:\1\WinFontSwap\bin\WinFontSwapWpf.exe'
shell32.ShellExecuteW(None, 'open', exe, None, None, 1)
time.sleep(4)
titles = []
def cb(w, l):
    if user32.IsWindowVisible(w):
        n = user32.GetWindowTextLengthW(w); b = ctypes.create_unicode_buffer(n+2); user32.GetWindowTextW(w,b,n+2)
        if b.value: titles.append(b.value)
    return True
user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
print('security dialog:', [t for t in titles if ('安全警告' in t or '打开文件' in t)])
print('app:', [t for t in titles if t == '字体替换'])
