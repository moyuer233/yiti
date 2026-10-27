<p align="center">
  <a href="https://github.com/moyuer233/yiti/blob/main/LICENSE"><img alt="License" src="https://img.shields.io/badge/license-MIT-3DA639?style=flat-square&labelColor=161b22"></a>
  <a href="https://github.com/moyuer233/yiti"><img alt="Release" src="https://img.shields.io/badge/release-v0.1.0-4D6BFE?style=flat-square&labelColor=161b22"></a>
  <a href="https://github.com/moyuer233/yiti"><img alt="Platform" src="https://img.shields.io/badge/platform-Windows%207%20%7C%208%20%7C%2010%20%7C%2011-2088FF?style=flat-square&labelColor=161b22&logo=windows&logoColor=white"></a>
  <a href="https://github.com/moyuer233/yiti"><img alt=".NET" src="https://img.shields.io/badge/.NET-4.8-512BD4?style=flat-square&labelColor=161b22&logo=dotnet&logoColor=white"></a>
</p>

# Yiti (易体)

Swap the Windows system fonts for any font you like. Pick a font, hit Apply, reboot — Settings, the Start menu and Explorer all follow.

![Font replacement](docs/font.png)

## What it replaces

- **Chinese**: Microsoft YaHei Regular / Bold / Light
- **Latin and digits**: Segoe UI, Segoe UI Variable
- **Legacy shell elements**: small caption, menu, message box, status bar, icon

Picking one font is enough: weights are matched automatically (regular 400, bold 700, light 300). Use the per-row **Select** button when you want to override a single slot.

## Quick start

1. Click **Select font…** and choose a font
2. Click **Apply**
3. Reboot once (font files are swapped at boot time)

## Undo

| Entry | What it does |
|---|---|
| **Roll back all** | Restores the original font files and the shell element settings |
| **Defaults → Apply** | Switches the source fonts back to the Microsoft originals and writes them |

Originals come from the Windows component store (`C:\Windows\WinSxS`) first, then from the automatic backup made on the first apply — **undo keeps working even if the backup is gone**.

## Supported systems

Windows 7 / 8 / 10 / 11. Fonts missing on older systems (for example Light or Segoe UI Variable on Windows 7) are shown greyed out with a reason instead of a button that does nothing.

## Notes

- Administrator rights are required (it writes to `C:\Windows\Fonts`); the executable is manifested, so just double-click it
- A reboot is required after the first apply
- Only the system UI changes — apps that ship their own fonts are untouched

## Build from source

No Visual Studio needed, only the .NET Framework compiler shipped with Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-wpf.ps1
```

The output is always `dist\WinFontSwapWpf.exe`.

## License

[MIT](LICENSE) — free to use, modify and redistribute, **as long as the author attribution and this license notice are kept**.

## Author

[moyuer233](https://github.com/moyuer233)
