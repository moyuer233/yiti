<p align="center">
  <a href="https://github.com/moyuer233/yiti/blob/main/LICENSE"><img alt="License" src="https://img.shields.io/badge/license-MIT-3DA639?style=flat-square&labelColor=161b22"></a>
  <a href="https://github.com/moyuer233/yiti"><img alt="Release" src="https://img.shields.io/badge/release-v0.1.0-4D6BFE?style=flat-square&labelColor=161b22"></a>
  <a href="https://github.com/moyuer233/yiti"><img alt="Platform" src="https://img.shields.io/badge/platform-Windows%207%20%7C%208%20%7C%2010%20%7C%2011-2088FF?style=flat-square&labelColor=161b22&logo=windows&logoColor=white"></a>
  <a href="https://github.com/moyuer233/yiti"><img alt=".NET" src="https://img.shields.io/badge/.NET-4.8-512BD4?style=flat-square&labelColor=161b22&logo=dotnet&logoColor=white"></a>
</p>

<p align="center">
  <img src="docs/icon.png" width="96" height="96" alt="Yiti">
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

## How it differs from noMeiryoUI

[noMeiryoUI](https://github.com/Tatsu-syo/noMeiryoUI) changes the UI font *setting* (registry `HKCU\Control Panel`), so its own README lists four things it cannot do: the taskbar, preinstalled apps (Settings, Paint), applications that ignore the system font, and the title bar font on Windows 11.

Yiti replaces the **font files themselves**, so all of those change too. Three differences that matter:

| | Yiti | noMeiryoUI |
|---|---|---|
| Mechanism | Replaces the system font files (YaHei trio + Segoe UI) | Changes the UI font setting |
| Settings, Start menu, taskbar, title bar | ✅ all follow | ❌ explicitly unsupported |
| Weights | Pick once, regular/bold/light matched automatically | Specify each one by hand |
| Undo | Automatic backup + one-click rollback, and from the component store if the backup is gone | Export the registry yourself |

What it does not fix, we do not claim: text rendering sharpness (ClearType), apps that ship their own fonts (Office, Electron apps), and the one reboot required after the first apply.

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

## Risks (read before using)

This tool rewrites **system font files**, which means "reboot to finish, and hard to undo before that". Known risks:

- **A font with incomplete coverage can leave missing glyphs.** If the chosen font lacks CJK glyphs or the needed weights, menus and title bars may show boxes or blurry text after the reboot, and some legacy apps may render badly. The app validates that the font file parses, but it cannot judge whether the typeface looks right or covers every character.
- **Office / WPS layout and export can change.** A document that asks for "Microsoft YaHei" now gets your font, so exported images, printing and PDF conversion will differ from before (every tool in this category has the same issue).
- **The first apply requires a reboot.** Font files are swapped at boot; a forced shutdown in between can leave a partial swap that needs one more reboot to finish.
- **Antivirus may block or flag it.** Writing to `C:\Windows\Fonts` triggers security software on some machines (noMeiryoUI documents the same). A block is reported as an error, not silently ignored.
- **Windows updates may reset it.** Feature updates restore the original system fonts, so you need to apply again afterwards.
- **Undo relies on the backup or the component store.** Originals are backed up automatically and can also be taken from `C:\Windows\WinSxS`; if both were cleaned, only a restore point or installation media helps. **Create a restore point yourself before the first run.**

In short: the app backs up, rolls back and can restore originals, but it cannot promise a perfect one-click undo in every environment. Use it only if you can accept the risk of a system reinstall.

## Disclaimer

The software is provided "as is", without warranty of any kind (see [LICENSE](LICENSE)). The author is not liable for system damage, data loss or any other loss caused by using it.

## License

[MIT](LICENSE) — free to use, modify and redistribute, **as long as the author attribution and this license notice are kept**.

## Author

[moyuer233](https://github.com/moyuer233)