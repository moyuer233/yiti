
import ctypes, ctypes.wintypes as wt, os, sys, io, subprocess
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

ntdll = ctypes.windll.ntdll
kernel32 = ctypes.windll.kernel32

class UNICODE_STRING(ctypes.Structure):
    _fields_ = [('Length', wt.USHORT), ('MaximumLength', wt.USHORT), ('Buffer', ctypes.c_void_p)]

def pids(name):
    out = subprocess.run(['tasklist','/FI','IMAGENAME eq ' + name,'/FO','CSV','/NH'], capture_output=True)
    res = []
    for line in out.stdout.decode('utf-8','replace').splitlines():
        parts = [p.strip('"') for p in line.split('","')]
        if len(parts) > 1 and parts[0].lower() == name.lower():
            res.append(int(parts[1]))
    return res

SystemExtendedHandleInformation = 64
buf = ctypes.create_string_buffer(1 << 20)
size = ctypes.c_ulong(0)
while True:
    st = ntdll.NtQuerySystemInformation(SystemExtendedHandleInformation, buf, len(buf), ctypes.byref(size))
    if st == 0:
        break
    if st == 0xC0000004:
        buf = ctypes.create_string_buffer(len(buf) * 2)
        if len(buf) > (1 << 28):
            print('too many handles'); sys.exit(1)
        continue
    print('NtQuerySystemInformation failed', hex(st & 0xFFFFFFFF)); sys.exit(1)

count = ctypes.c_size_t.from_buffer(buf, 0).value
base = ctypes.addressof(buf) + 16
entry_size = 40
entries = {}
for i in range(count):
    addr = base + i * entry_size
    pid = ctypes.c_size_t.from_address(addr + 8).value
    handle = ctypes.c_void_p.from_address(addr + 16).value
    if pid not in entries:
        entries[pid] = []
    entries[pid].append(handle)

PROCESS_DUP_HANDLE = 0x0040
targets = {}
for exe in ('StartMenuExperienceHost.exe', 'SystemSettings.exe', 'explorer.exe'):
    for p in pids(exe):
        targets[exe + ':' + str(p)] = p
print('targets:', targets)

GetFinalPathNameByHandleW = kernel32.GetFinalPathNameByHandleW
GetFinalPathNameByHandleW.restype = wt.DWORD
GetFinalPathNameByHandleW.argtypes = [wt.HANDLE, wt.LPWSTR, wt.DWORD, wt.DWORD]

for label, pid in targets.items():
    hproc = kernel32.OpenProcess(PROCESS_DUP_HANDLE, False, pid)
    if not hproc:
        print('open failed', label); continue
    found = set()
    for h in entries.get(pid, []):
        dup = wt.HANDLE()
        ok = kernel32.DuplicateHandle(wt.HANDLE(hproc), wt.HANDLE(h), kernel32.GetCurrentProcess(), ctypes.byref(dup), 0, False, 0x2)
        if not ok:
            continue
        buf2 = ctypes.create_unicode_buffer(1024)
        r = GetFinalPathNameByHandleW(dup, buf2, 1024, 0)
        if r > 0:
            path = buf2.value
            low = path.lower()
            if low.endswith(('.ttf', '.ttc', '.otf')) or 'fonts' in low:
                found.add(path)
        kernel32.CloseHandle(dup)
    kernel32.CloseHandle(hproc)
    print('==', label)
    for f in sorted(found):
        print('   ', f)
