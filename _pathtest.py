
import ctypes, ctypes.wintypes as wt, time, shutil, os, subprocess
user32 = ctypes.windll.user32; shell32 = ctypes.windll.shell32
src = r'H:\1\WinFontSwap\dist\WinFontSwapWpf.exe'
def titles():
    out = []
    def cb(w, l):
        if user32.IsWindowVisible(w):
            n = user32.GetWindowTextLengthW(w); b = ctypes.create_unicode_buffer(n+2); user32.GetWindowTextW(w,b,n+2)
            if b.value: out.append(b.value)
        return True
    user32.EnumWindows(ctypes.WINFUNCTYPE(ctypes.c_bool, wt.HWND, wt.LPARAM)(cb), 0)
    return out
def test(path, label):
    subprocess.run(['taskkill','/F','/IM','WinFontSwapWpf.exe'], capture_output=True)
    time.sleep(0.8)
    shell32.ShellExecuteW(None, 'open', path, None, None, 1)
    res = None
    for i in range(8):
        time.sleep(0.5)
        t = titles()
        if any('安全警告' in x or '打开文件' in x for x in t): res = 'WARN'; break
        if '字体替换' in t: res = 'OK'; break
    print(label, '->', res)
    subprocess.run(['taskkill','/F','/IM','WinFontSwapWpf.exe'], capture_output=True)

test(src, 'dist(H盘)')
tmp = os.path.join(os.environ['USERPROFILE'], 'Desktop', 'wfs_test.exe')
shutil.copy2(src, tmp)
test(tmp, 'Desktop(C盘)')
os.remove(tmp)
tmp2 = r'C:\ProgramData\wfs_test.exe'
try:
    shutil.copy2(src, tmp2)
    test(tmp2, 'ProgramData')
    os.remove(tmp2)
except Exception as e:
    print('ProgramData copy failed', e)
