
import os, sys, io, glob
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

roots = glob.glob(r'C:\Windows\SystemApps\Microsoft.Windows.StartMenuExperienceHost_cw5n1h2txyewy') + \
        glob.glob(r'C:\Program Files\WindowsApps\Microsoft.Windows.Settings_*')
for root in roots:
    print('==', root)
    if not os.path.isdir(root):
        print('  (不存在)'); continue
    n = 0
    for dirpath, _, files in os.walk(root):
        for f in files:
            if f.lower().endswith(('.ttf', '.ttc', '.otf')):
                p = os.path.join(dirpath, f)
                print('   FONT', p, os.path.getsize(p))
                n += 1
    print('   字体文件数:', n)
