
import ctypes, ctypes.wintypes as wt, subprocess, time
from PIL import Image
import numpy as np

user32 = ctypes.windll.user32
gdi32 = ctypes.windll.gdi32

subprocess.run(['cmd','/c','start','ms-settings:'], shell=False)
time.sleep(4)

procs = subprocess.run(['powershell','-NoProfile','-Command',"(Get-Process | Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object Id,ProcessName,MainWindowTitle | Format-Table -AutoSize | Out-String -Width 200)"], capture_output=True, text=True).stdout
print(procs[:1500])
