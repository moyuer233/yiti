<p align="center">
  <a href="https://github.com/moyuer233/yiti"><img src="docs/icon.png" width="200" height="200" alt="Yiti"></a>
</p>

<div align="center">

# Yiti (易体)

_✨ Swap the Windows system fonts for any font you like — Settings and the Start menu follow ✨_

</div>

<p align="center">
  <a href="https://github.com/moyuer233/yiti/blob/main/LICENSE"><img alt="license" src="https://img.shields.io/github/license/moyuer233/yiti?style=flat-square&labelColor=161b22&color=3DA639&logo=github&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti/releases"><img alt="release" src="https://img.shields.io/badge/release-v0.1.0-4D6BFE?style=flat-square&labelColor=161b22&color=4D6BFE&logo=github&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti/releases"><img alt="downloads" src="https://img.shields.io/github/downloads/moyuer233/yiti/total?style=flat-square&labelColor=161b22&color=2088FF&logo=github&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti/commits/main"><img alt="commit activity" src="https://img.shields.io/github/commit-activity/m/moyuer233/yiti?style=flat-square&labelColor=161b22&color=2088FF&logo=github&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti/commits/main"><img alt="last commit" src="https://img.shields.io/github/last-commit/moyuer233/yiti?style=flat-square&labelColor=161b22&color=2088FF&logo=github&logoColor=white"/></a>
</p>
<p align="center">
  <a href="https://www.microsoft.com/windows"><img alt="platform" src="https://img.shields.io/badge/platform-Windows%207%2F8%2F10%2F11-0078D4?style=flat-square&labelColor=161b22&color=0078D4&logo=windows&logoColor=white"/></a>
  <a href="https://dotnet.microsoft.com"><img alt=".NET" src="https://img.shields.io/badge/.NET-4.8-512BD4?style=flat-square&labelColor=161b22&color=512BD4&logo=dotnet&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti"><img alt="deps" src="https://img.shields.io/badge/single%20exe-no%20deps-5C2D91?style=flat-square&labelColor=161b22&color=5C2D91"/></a>
</p>

![Font replacement](docs/font.png)

## Features

- **Chinese**: Microsoft YaHei Regular / Bold / Light, all three weights at once
- **Latin and digits**: Segoe UI, Segoe UI Variable
- **Legacy shell elements**: small caption, menu, message box, status bar, icon
- **Automatic weight matching**: pick one font, weights 400 / 700 / 300 are matched for you; override a single row with its **Select** button
- **Settings, Start menu, taskbar, title bar**: they all follow because the font files themselves are replaced
- **Backup and rollback built in**: one-click rollback, and originals can also be taken from the Windows component store if the backup is gone
- **Older systems degrade gracefully**: fonts that do not exist there (Light or Segoe UI Variable on Windows 7) are shown greyed out with a reason

## How it differs from noMeiryoUI

[noMeiryoUI](https://github.com/Tatsu-syo/noMeiryoUI) changes the UI font *setting* (registry `HKCU\Control Panel`), so its own README lists four things it cannot do: the taskbar, preinstalled apps (Settings, Paint), applications that ignore the system font, and the title bar font on Windows 11.

Yiti replaces the **font files themselves**, so all of those change too. Three differences that matter:

| | Yiti | noMeiryoUI |
|---|---|---|
| Mechanism | Replaces the system font files (YaHei trio + Segoe UI) | Changes the UI font setting |
| Settings, Start menu, taskbar, title bar | ✅ all follow | ❌ explicitly unsupported |
| Weights | Pick once, regular/bold/light matched automatically | Specify each one by hand |
| Undo | Automatic backup + one-click rollback, plus the component store if the backup is gone | Export the registry yourself |

What it does not fix, we do not claim: text rendering sharpness (ClearType), apps that ship their own fonts (Office, Electron apps), and the one reboot required after the first apply.

## Quick start

1. Click **Select font…** and choose a font
2. Click **Apply**
3. Reboot once (font files are swapped at boot)

## Undo

| Entry | What it does |
|---|---|
| **Roll back all** | Restores the original font files and the shell element settings |
| **Defaults → Apply** | Switches the source fonts back to the Microsoft originals and writes them |

Originals come from the Windows component store (`C:\Windows\WinSxS`) first, then from the automatic backup made on the first apply.

## Risks (read before using)

This tool rewrites **system font files**, which means "reboot to finish, and hard to undo before that". Known risks:

- **A font with incomplete coverage can leave missing glyphs.** If the chosen font lacks CJK glyphs or the needed weights, menus and title bars may show boxes or blurry text after the reboot, and some legacy apps may render badly. The app validates that the font file parses, but it cannot judge whether the typeface looks right or covers every character.
- **Office / WPS layout and export can change.** A document that asks for "Microsoft YaHei" now gets your font, so exported images, printing and PDF conversion will differ (every tool in this category has the same issue).
- **The first apply requires a reboot.** Font files are swapped at boot; a forced shutdown in between can leave a partial swap that needs one more reboot.
- **Antivirus may block or flag it.** Writing to `C:\Windows\Fonts` triggers security software on some machines (noMeiryoUI documents the same). A block is reported as an error, not silently ignored.
- **Windows updates may reset it.** Feature updates restore the original system fonts, so you need to apply again afterwards.
- **Undo relies on the backup or the component store.** If both were cleaned, only a restore point or installation media helps. **Create a restore point yourself before the first run.**

## Supported systems

Windows 7 / 8 / 10 / 11. Administrator rights are required (it writes to `C:\Windows\Fonts`); the executable is manifested, so just double-click it.

## Build from source

No Visual Studio needed, only the .NET Framework compiler shipped with Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-wpf.ps1
```

The output is always `dist\WinFontSwapWpf.exe`.

## Disclaimer

The software is provided "as is", without warranty of any kind (see [LICENSE](LICENSE)). The author is not liable for system damage, data loss or any other loss caused by using it.

## License

[MIT](LICENSE) — free to use, modify and redistribute, **as long as the author attribution and this license notice are kept**.

## Author

[moyuer233](https://github.com/moyuer233)
