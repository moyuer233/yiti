<p align="center">
  <a href="https://github.com/moyuer233/yiti"><img src="docs/icon.png" width="200" height="200" alt="Yiti"></a>
</p>

<div align="center">

# Yiti (易体)

_✨ Swap the Windows system fonts for any font you like — Settings and the Start menu follow ✨_

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

![Font replacement](docs/font.png)

## Features

- **Chinese**: Microsoft YaHei Regular / Bold / Light, all three weights at once
- **Latin and digits**: Segoe UI, Segoe UI Variable
- **Legacy shell elements**: small caption, menu, message box, status bar, icon
- **Automatic weight matching**: pick one font, weights 400 / 700 / 300 are matched for you; override a single row with its **Select** button
- **Settings, Start menu, taskbar, title bar**: they all follow because the font files themselves are replaced
- **Backup and rollback built in**: one-click rollback, and originals can also be taken from the Windows component store if the backup is gone
- **Older systems degrade gracefully**: fonts that do not exist there (Light or Segoe UI Variable on Windows 7) are shown greyed out with a reason

## Which files it replaces

It is not only Microsoft YaHei — five files in total:

| File | What it is | Where it shows up |
|---|---|---|
| `msyh.ttc` | Microsoft YaHei Regular | Chinese text, Settings, Start menu, Explorer |
| `msyhbd.ttc` | Microsoft YaHei Bold | every bold Chinese label |
| `msyhl.ttc` | Microsoft YaHei Light | light weight (Windows 8 and newer) |
| `SegUIVar.ttf` | Segoe UI Variable | taskbar, Settings header, new Windows 11 UI |
| `segoeui.ttf` | Segoe UI | classic English UI, legacy apps |

How it works: read the font you picked, rewrite only its internal names to `Microsoft YaHei` / `Segoe UI`, emit files with those names, replace the five system files, reboot. **Your glyphs, Microsoft's identity** — that is what makes Windows use them.

The **shell elements** rows write `HKCU\Control Panel\Desktop\WindowMetrics`, which is the legacy Win32 UI font setting.

**What it cannot change**: apps that ship their own fonts (Office, Electron apps, games), web pages that specify a font, and text rendering parameters (ClearType and friends).
### Why the internal names have to change

Windows **does not pick fonts by file name — it reads the `name` table inside the font**. A font carries more than 20 name records, each with an ID:

| ID | Meaning | What the system does with it |
|---|---|---|
| 1 | Family | the "Microsoft YaHei" string itself; GDI LOGFONT and WinUI theme resources point at it |
| 2 | Subfamily | Regular / Bold / Light |
| 4 | Full name | "Microsoft YaHei Bold" |
| 6 | PostScript name | Adobe apps identify fonts by it |
| 16 / 17 | Typographic family / subfamily | modern DirectWrite prefers these |
| 3 / 18 / 21 / 22 | Unique ID / WWS names | font cache and font menus |

Yiti rewrites exactly those ten fields (each record exists once per platform encoding), and **leaves `glyf` outlines, `hmtx` metrics, `cmap` mappings and `GSUB/GPOS` shaping completely untouched**. So after the swap:

- the UI still says "Microsoft YaHei", but every stroke comes from your font;
- the system resolves fonts by name, finds this renamed file, and Settings, the Start menu and the taskbar follow;
- copyright / version / trademark / designer (IDs 0, 5, 7, 9) are **deliberately left intact** — original authorship stays visible, and the app can still tell which font is actually installed.

**Why `usWeightClass` in OS/2 is not rewritten**: the source files are already chosen by weight (a 700 face goes into the bold slot), so the weight metadata is correct by construction; forcing it would fight the application's own weight matching.
## How it works (in detail)

Four stages: build → replace → reboot → rollback.

### 1. Build a look-alike font

1. Read the source font (one face of a TTC), parse its table directory, keep TrueType outlines (`glyf`); CFF/OTF sources are rejected.
2. Rebuild the `name` table: IDs 1 (family), 2 (subfamily), 3/4 (full name), 6 (PostScript), 16/17 (typographic family/subfamily), 18/21/22 (WWS) become Microsoft's names; every other record (copyright 0, version 5, trademark 7, designer 9, …) is copied unchanged.
3. Package per target:

| Target file | Family names written (number of faces) |
|---|---|
| `msyh.ttc` | `Microsoft YaHei` + `Microsoft YaHei UI`, both Regular (2-face TTC) |
| `msyhbd.ttc` | same two families, Subfamily = **Bold**, PostScript names end with `-Bold` |
| `msyhl.ttc` | `Microsoft YaHei Light` + `Microsoft YaHei UI Light` (typographic family stays YaHei, typographic style Light) |
| `SegUIVar.ttf` | `Segoe UI Variable` / `… Text` / `… Display` / `… Small` in one file, **weight axis kept** |
| `segoeui.ttf` | `Segoe UI` (single face) |

4. Recompute table checksums and the `head` checkSumAdjustment, then lay out the TTC directory (face offsets rebased to file positions).
5. Two self-checks: (a) structural validation of the table directory, name records and face count; (b) **actually loading it** through GDI+ `PrivateFontCollection.AddFontFile` — if Windows cannot load it, the app aborts instead of writing it to the system.

Multiple faces are mandatory: WinUI asks for `Microsoft YaHei UI`, classic GDI asks for `Microsoft YaHei`, and the taskbar asks for `Segoe UI Variable Text/Display/Small` — those families do not fit into a single-face file.

### 2. Replace: backup → permissions → pending rename

1. Copy the originals to `C:\ProgramData\WinFontSwap\backup\<stamp>\orig\` and record their SHA-256 in `state.ini`.
2. Take ownership, grant SYSTEM full control, and **inherit the original DACL** (keeping the `ALL APPLICATION PACKAGES` read ACE that UWP needs) — without it packaged apps cannot read the font.
3. Write the new files to a staging directory, then queue two renames per file in `PendingFileRenameOperations`:
   - `staged file → C:\Windows\Fonts\msyh.ttc`
   - `C:\Windows\Fonts\msyh.ttc → backup path`
   The font files are mapped by the font service, so **only the session manager can swap them at boot**.
4. Clear the font cache: stop `FontCache` (`FontCache3.0.0.0` on Windows 7), delete `FNTCACHE.DAT` and the per-user `FontCache` directory, otherwise the old glyphs survive the reboot.
5. Record the applied list and new hashes for status display and rollback decisions.

### 3. Shell elements (optional)

Five values under `HKCU\Control Panel\Desktop\WindowMetrics` (`SmCaptionFont`, `MenuFont`, `MessageFont`, `StatusFont`, `IconFont`) are 92-byte `LOGFONTW` blobs: offset 0 holds the height (negative = character height) and offset `0x5C` holds a 32-wide-character face name. The app rewrites those fields and reads them back to verify.

### 4. Rollback

Compare the SHA in `state.ini` against the current files; if they are ours, queue the originals from the backup through the same boot-time rename list and restore `WindowMetrics` from the backup. If the backup is gone, take the originals from `C:\Windows\WinSxS` by package name (`microsoftyahei` and friends).
## Why renaming a font file does not work

This is the reason the tool exists. The three naive approaches all fail:

| Approach | What happens |
|---|---|
| Install a new font (different family name) | System components never use it: the WinUI theme hardcodes `Microsoft YaHei UI` and GDI's LOGFONT hardcodes `Microsoft YaHei`; nothing looks up your family name |
| Rename the new font file to `msyh.ttc` and overwrite | The file name matches, but **the internal family name is still the original one** (e.g. HarmonyOS Sans), so Windows registers it under that name ⇒ `Microsoft YaHei` cannot be resolved and the UI falls back or shows missing glyphs; wrong face count/order inside a TTC also breaks bold and light |
| Replace only `msyh.ttc` | Bold `msyhbd.ttc` and light `msyhl.ttc` stay YaHei, so the UI is only half swapped; the Windows 11 taskbar and Settings header use `SegUIVar.ttf`, which stays untouched; legacy menus and message boxes live in the registry and do not change either |

Four things are required, and skipping any of them leaves you with "Settings changed but the taskbar did not":

1. **Rewrite the internal names** (`name` table) so the system believes this is Microsoft YaHei;
2. **Replace all five files** — the three YaHei files plus the two Segoe UI files;
3. **Write the registry** `HKCU\Control Panel\Desktop\WindowMetrics` for legacy shell elements (menu, message box, status bar, icon);
4. **Clear the font cache and swap at boot** — the files are locked while Windows runs, and a stale cache keeps serving the old glyphs.
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

What can be replaced differs per version, and the app detects it: fonts missing on the OS are shown greyed out with a tooltip instead of a button that does nothing.

| | Windows 7 | Windows 10 1809+ | Windows 11 22000+ |
|---|---|---|---|
| YaHei Regular / Bold | ✅ | ✅ | ✅ |
| YaHei Light (`msyhl.ttc`) | ❌ not present | ✅ | ✅ |
| Segoe UI Variable (`SegUIVar.ttf`) | ❌ not present | ✅ | ✅ |
| Cleanup of per-user same-name fonts | ❌ no such mechanism | ✅ | ✅ |
| Taskbar / new Settings UI | partial (via `segoeui.ttf`) | ✅ | ✅ |
| Variable font (.ttf VF) as source | ❌ default instance only | ✅ | ✅ |
| Window background | flat grey | flat grey | Mica |

On older systems the font cache service is `FontCache3.0.0.0`; from Windows 10 it is `FontCache`. The app picks the right one.

## Build from source

No Visual Studio needed, only the .NET Framework compiler shipped with Windows:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-wpf.ps1
```

The output is always `dist\YiTi.exe`.

## Disclaimer

The software is provided "as is", without warranty of any kind (see [LICENSE](LICENSE)). The author is not liable for system damage, data loss or any other loss caused by using it.

## License

[MIT](LICENSE) — free to use, modify and redistribute, **as long as the author attribution and this license notice are kept**.

## Author

[moyuer233](https://github.com/moyuer233)