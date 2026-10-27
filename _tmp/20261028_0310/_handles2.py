
import ctypes, ctypes.wintypes as wt, os, sys, io, subprocess
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
ntdll = ctypes.windll.ntdll
kernel32 = ctypes.windll.kernel32
ntdll.NtQuerySystemInformation.restype = ctypes.c_long
ntdll.NtQuerySystemInformation.argtypes = [ctypes.c_ulong, ctypes.c_void_p, ctypes.c_ulong, ctypes.POINTER(ctypes.c_ulong)]

def pids(name):
    out = subprocess.run(['tasklist','/FI','IMAGENAME eq ' + name,'/FO','CSV','/NH'], capture_output=True)
    res = []
    for line in out.stdout.decode('utf-8','replace').splitlines():
        parts = [p.strip('"') for p in line.split('","')]
        if len(parts) > 1 and parts[0].lower() == name.lower():
            try: res.append(int(parts[1]))
            except: pass
    return res

CLS = 64
size_needed = ctypes.c_ulong(0)
sz = 1 << 22
buf = None
for attempt in range(8):
    buf = ctypes.create_string_buffer(sz)
    st = ntdll.NtQuerySystemInformation(CLS, buf, sz, ctypes.byref(size_needed))
    code = st & 0xFFFFFFFF
    if code == 0:
        break
    if code == 0xC0000004:
        sz = max(size_needed.value + 65536, sz * 2)
        continue
    print('query failed', hex(code)); sys.exit(1)
else:
    print('buffer too small'); sys.exit(1)

count = ctypes.c_size_t.from_address(ctypes.addressof(buf)).value
base = ctypes.addressof(buf) + 16
entry = 40
by_pid = {}
for i in range(count):
    addr = base + i * entry
    pid = ctypes.c_size_t.from_address(addr + 8).value
    hv = ctypes.c_void_p.from_address(addr + 16).value
    by_pid.setdefault(pid, []).append(hv)

PROCESS_DUP_HANDLE = 0x0040
GFPN = kernel32.GetFinalPathNameByHandleW
GFPN.restype = wt.DWORD
GFPN.argtypes = [wt.HANDLE, wt.LPWSTR, wt.DWORD, wt.DWORD]

targets = []
for exe in ('StartMenuExperienceHost.exe','SystemSettings.exe'):
    for p in pids(exe):
        targets.append((exe + ':' + str(p), p))
print('targets:', [t[0] for t in targets])

for label, pid in targets:
    hproc = kernel32.OpenProcess(PROCESS_DUP_HANDLE, False, pid)
    if not hproc:
        print('==', label, 'OpenProcess failed', ctypes.GetLastError()); continue
    found = set()
    for h in by_pid.get(pid, []):
        dup = wt.HANDLE()
        if not kernel32.DuplicateHandle(wt.HANDLE(hproc), wt.HANDLE(h), kernel32.GetCurrentProcess(), ctypes.byref(dup), 0, False, 0x2):
            continue
        b = ctypes.create_unicode_buffer(1024)
        r = GFPN(dup, b, 1024, 0)
        if r > 0:
            low = b.value.lower()
            if low.endswith(('.ttf','.ttc','.otf')):
                found.add(b.value)
        kernel32.CloseHandle(dup)
    kernel32.CloseHandle(hproc)
    print('==', label, '字体句柄', len(found))
    for f in sorted(found):
        print('   ', f)
