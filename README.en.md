<p align="center">
  <a href="https://github.com/moyuer233/yiti"><img src="docs/icon.png" width="200" height="200" alt="Yiti"></a>
</p>

<div align="center">

# Yiti (易体)

_✨ Foolproof one-click Windows system font swapper ✨_

<a href="README.md">中文</a> | <a href="README.en.md">English</a>

</div>

<p align="center">
  <a href="https://github.com/moyuer233/yiti/blob/main/LICENSE"><img alt="license" src="https://img.shields.io/github/license/moyuer233/yiti?style=flat-square&labelColor=161b22&color=3DA639&logo=github&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti/releases"><img alt="release" src="https://img.shields.io/github/v/release/moyuer233/yiti?style=flat-square&labelColor=161b22&color=2088FF&logo=github&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti/releases"><img alt="downloads" src="https://img.shields.io/github/downloads/moyuer233/yiti/total?style=flat-square&labelColor=161b22&color=2088FF&logo=github&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti/commits/main"><img alt="commit activity" src="https://img.shields.io/github/commit-activity/m/moyuer233/yiti?style=flat-square&labelColor=161b22&color=2088FF&logo=github&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti/commits/main"><img alt="last commit" src="https://img.shields.io/github/last-commit/moyuer233/yiti?style=flat-square&labelColor=161b22&color=2088FF&logo=github&logoColor=white"/></a>
</p>
<p align="center">
  <a href="https://www.microsoft.com/windows"><img alt="platform" src="https://img.shields.io/badge/platform-Windows%207%2F8%2F10%2F11-0078D4?style=flat-square&labelColor=161b22&color=0078D4&logo=windows&logoColor=white"/></a>
  <a href="https://dotnet.microsoft.com"><img alt=".NET" src="https://img.shields.io/badge/.NET-4.8-512BD4?style=flat-square&labelColor=161b22&color=512BD4&logo=dotnet&logoColor=white"/></a>
  <a href="https://github.com/moyuer233/yiti"><img alt="deps" src="https://img.shields.io/badge/single%20exe-no%20deps-5C2D91?style=flat-square&labelColor=161b22&color=5C2D91"/></a>
</p>

![Font replacement](docs/font.png?v=2)

## Features

- **System font**: the font inside Settings, the Start menu, the taskbar, title bars and other apps
- **Shell element fonts**: small caption, menu, message box, status bar, icon
- **Automatic weight matching**: pick one font and weights are matched at 400 / 700 / 300 — e.g. Microsoft YaHei Regular / Bold / Light all switch at once; any font can be chosen manually
- **Automatic backup and rollback**: "Roll back all" restores everything in one click; the defaults are taken from `C:\Windows\WinSxS` first, so they still work when no backup exists
- **Automatic OS detection**: fonts missing on the system (Windows 7 has no Light and no Segoe UI Variable) are disabled automatically

## How it differs from noMeiryoUI

[noMeiryoUI](https://github.com/Tatsu-syo/noMeiryoUI) changes the *UI font setting* (registry `HKCU\Control Panel`), so it cannot handle: the taskbar, preinstalled apps (Settings, Paint), applications that ignore the system font, and the title bar font on Windows 11.