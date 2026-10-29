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

<details>
<summary><h3>Screenshots (click to expand)</h3></summary>

![Font replacement](docs/font_en.png)

![Advanced](docs/adv_en.png)

![About](docs/about_en.png)

</details>

## Features

- **System font**: the font inside Settings, the Start menu, the taskbar, title bars and other apps
- **Shell element fonts**: small caption, menu, message box, status bar, icon
- **Automatic weight matching**: pick one font and weights are matched at 400 / 700 / 300 — e.g. Microsoft YaHei Regular / Bold / Light all switch at once; any font can be chosen manually
- **Automatic backup and rollback**: "Roll back all" restores everything in one click; the defaults are taken from `C:\Windows\WinSxS` first, so they still work when no backup exists
- **Automatic OS detection**: fonts missing on the system (Windows 7 has no Light and no Segoe UI Variable) are disabled automatically

## How it differs from noMeiryoUI

[noMeiryoUI](https://github.com/Tatsu-syo/noMeiryoUI) changes the *UI font setting* (registry `HKCU\Control Panel`), so it cannot handle: the taskbar, preinstalled apps (Settings, Paint), applications that ignore the system font, and the title bar font on Windows 11.

Yiti replaces the **system font files themselves**, so all of those change too. Two differences that matter:

| | Yiti | noMeiryoUI |
|---|---|---|
| Mechanism | Replaces the system font files (the YaHei trio + Segoe UI) | Changes the UI font setting |
| Settings, Start menu, taskbar, title bar | ✅ all follow | ❌ unsupported |
| Rollback | Automatic backup + one-click rollback, plus the component store when the backup is gone | Export the registry yourself |

Text rendering sharpness (ClearType), apps that ship their own fonts (Office, Electron apps), and the one reboot required after the first apply.

## Risks

This tool rewrites **system font files**: after applying, you must reboot, and undoing before that reboot is not practical. Known risks:

- **A font with incomplete coverage can leave missing glyphs.** If the font lacks CJK glyphs or the needed weights, menus and title bars may show boxes or blurry text after the reboot; some legacy apps may render badly.
- **Office / WPS layout and export can change.** A document asking for "Microsoft YaHei" now gets your font, so exported images, printing and PDF conversion differ from before.
- **A reboot is mandatory.** Font files are swapped at boot; a forced shutdown in between can leave the swap unfinished, needing one more reboot.
- **Antivirus may block or flag it.** It writes to `C:\Windows\Fonts`, which some security software blocks or reports; a block is reported as an error, never silently ignored.
- **Windows updates may overwrite it.** Feature updates reset the system fonts, so you have to apply again afterwards.
- **Rollback depends on the backup or the component store.** Both are created automatically, but if they were cleaned, only a restore point or installation media helps — **create a restore point before the first run**.

## Font support by Windows version

| | Windows 7 | Windows 10 1809+ | Windows 11 22000+ |
|---|---|---|---|
| YaHei Regular / Bold | ✅ | ✅ | ✅ |
| YaHei Light (`msyhl.ttc`) | ❌ not present | ✅ | ✅ |
| Segoe UI Variable (`SegUIVar.ttf`) | ❌ not present | ✅ | ✅ |
| Cleanup of per-user same-name fonts | ❌ no such mechanism | ✅ | ✅ |
| Taskbar / new Settings UI | partial (via `segoeui.ttf`) | ✅ | ✅ |
| Variable font (.ttf VF) as source | ❌ default instance only | ✅ | ✅ |

On older systems the font cache service is called `FontCache3.0.0.0`; from Windows 10 it is `FontCache`. The app picks the right one by itself.

### How far it has actually been tested

| System | Status | What was verified |
| --- | --- | --- |
| Windows 11 26200 | ✅ **Tested** | Replacing all five font files, automatic weight matching, shell elements (menu / message box / status bar / small caption / icon) applying immediately, Restore and Default preset, Chinese/English UI, backup and rollback |
| Windows 10 1809+ | ⚠️ **Should work, not tested** | Version detection is implemented (per-user fonts, `FontCache` service name, flat grey background when Mica is unavailable), but it has not been run on a real machine |
| Windows 7 / 8.1 | ⚠️ **Should work, not tested** | Missing fonts are greyed out with a reason; the `FontCache3.0.0.0` and no-Mica branches exist, but they have not been run on a real machine |

**Feedback wanted.** If you try it on Windows 7 / 8.1 / 10, please report the result either way in [Issues](https://github.com/moyuer233/yiti/issues) and I will turn the table above into verified facts.

The most useful thing you can attach is the log: open **Advanced → Write a log** and it creates `yy-mm-dd-ss.log` next to the exe (OS build, elevation, whether the five target files exist, current shell fonts, backup status). That log alone is enough.

## Files replaced

| File | Font name | Scope |
|---|---|---|
| `msyh.ttc` | Microsoft YaHei Regular | Chinese body text, Settings, Start menu, Explorer |
| `msyhbd.ttc` | Microsoft YaHei Bold | every bold Chinese label |
| `msyhl.ttc` | Microsoft YaHei Light | light weight (Windows 8 and newer) |
| `SegUIVar.ttf` | Segoe UI Variable | taskbar, Settings header, Latin text in the new Windows 11 UI |
| `segoeui.ttf` | Segoe UI | classic English UI, legacy apps |

System font replacement flow: read the font you picked → rewrite only its internal names to `Microsoft YaHei` / `Segoe UI` and the like → generate files with those names → replace the ones in the system → reboot. That is the only way Windows recognises them.

Shell element font flow: write `HKCU\Control Panel\Desktop\WindowMetrics` to control the font and size of legacy UI elements.

**What cannot be changed**: apps that ship their own fonts (Office, Electron apps, games), web pages that specify a font, and text rendering parameters (ClearType and friends).

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

Yiti rewrites exactly those ten fields (each record exists once per platform encoding), and **leaves `glyf` outlines, `hmtx` metrics, `cmap` mappings and `GSUB/GPOS` shaping untouched**. After the swap:

- the UI still says "Microsoft YaHei", but **every stroke comes from your font**;
- the system resolves fonts by name, finds this renamed file, so Settings, the Start menu and the taskbar follow;
- copyright / version / trademark / designer (IDs 0, 5, 7, 9) are **deliberately left intact**: original authorship stays visible, and the app can still tell which font is actually installed.

## How it works in detail

The whole process has three stages: build → replace → reboot.

### 1. Build: turn your font into a look-alike

1. Read the source font (for a TTC, one face by index), parse the table directory, keep only TrueType outlines (`glyf`); CFF/OTF sources are rejected.
2. Rebuild the `name` table: IDs 1 (family), 2 (subfamily), 3/4 (full name), 6 (PostScript), 16/17 (typographic family/subfamily), 18/21/22 (WWS) become Microsoft's names; every other record (copyright 0, version 5, trademark 7, designer 9, …) is copied unchanged.
3. Package per target:

| Target file | Family names written (number of faces) |
|---|---|
| `msyh.ttc` | `Microsoft YaHei` + `Microsoft YaHei UI`, both Regular (2-face TTC) |
| `msyhbd.ttc` | same two families, Subfamily = **Bold**, PostScript names end with `-Bold` |
| `msyhl.ttc` | `Microsoft YaHei Light` + `Microsoft YaHei UI Light` (typographic family stays YaHei, typographic style Light) |
| `SegUIVar.ttf` | `Segoe UI Variable` / `… Text` / `… Display` / `… Small` in one file, **weight axis kept** (variable font) |
| `segoeui.ttf` | `Segoe UI` (single face) |

4. Recompute table checksums and the `head` checkSumAdjustment, then lay out the TTC directory (face offsets rebased to file positions).
5. Two self-checks: (a) structural validation of the table directory, name records and face count; (b) **actually loading it** through GDI+ `PrivateFontCollection.AddFontFile` — if Windows cannot load it, the app aborts instead of writing it into the system.

Why multiple faces: WinUI components ask for `Microsoft YaHei UI`, classic GDI asks for `Microsoft YaHei`, and the taskbar asks for `Segoe UI Variable Text/Display/Small` — those families do not fit into a single-face file, so it has to be a multi-face TTC.

### 2. Replacing the system fonts: backup → permissions → queued renames

1. Copy the originals to `C:\ProgramData\WinFontSwap\backup\<stamp>\orig\` and record their SHA-256 in `state.ini`.
2. Take ownership, grant SYSTEM full control, and **inherit the original font's DACL** (keeping the `ALL APPLICATION PACKAGES` read ACE that UWP needs); without it, packaged apps cannot read the font.
3. Write the new files into a staging directory, then queue two renames per file in `PendingFileRenameOperations`:
   - `staged file → C:\Windows\Fonts\msyh.ttc`
   - `C:\Windows\Fonts\msyh.ttc → backup path`
   The font files are mapped by the font service, so **only the session manager can swap them at boot**.
4. Clear the font cache: stop `FontCache` (`FontCache3.0.0.0` on Windows 7), delete `FNTCACHE.DAT` and the per-user `FontCache` directory, otherwise the old glyphs survive the reboot.
5. Record the `applied` list and each new SHA for status display and rollback decisions.

#### Shell element replacement

The five values under `HKCU\Control Panel\Desktop\WindowMetrics` (`SmCaptionFont`, `MenuFont`, `MessageFont`, `StatusFont`, `IconFont`) are 92-byte `LOGFONTW` blobs: offset 0 holds the height (negative = character height) and offset `0x5C` holds a 32-wide-character face name. The app rewrites those two fields and reads them back to verify.

### 3. Rollback

Compare the SHA in `state.ini` with the current files; if they are ours, queue the originals from the backup through the same boot-time rename list and restore `WindowMetrics` from the backup. If the backup is gone, take the originals from `C:\Windows\WinSxS` by package name (`microsoftyahei` and friends).

## Why renaming a font file does not work

This is the reason the tool exists. The three naive approaches all fail:

| Approach | What happens |
|---|---|
| Install a new font (different family name) | System components never use it: the WinUI theme hardcodes `Microsoft YaHei UI` and GDI's LOGFONT hardcodes `Microsoft YaHei`; nothing looks up your family name |
| Rename the new font file to `msyh.ttc` and overwrite | The file name matches, but **the internal family name is still the original one** (e.g. HarmonyOS Sans), so Windows registers it under that name ⇒ `Microsoft YaHei` cannot be resolved and the UI falls back or shows missing glyphs; a wrong face count/order inside a TTC also breaks bold and light |
| Replace only `msyh.ttc` | Bold `msyhbd.ttc` and light `msyhl.ttc` stay YaHei, so the UI is only half swapped; the Windows 11 taskbar and Settings header use `SegUIVar.ttf`, which stays untouched; legacy menus and message boxes live in the registry and do not change either |

Four things are required, and skipping any of them leaves you with "Settings changed but the taskbar did not":

1. **Rewrite the internal names** (`name` table) so the system believes this is Microsoft YaHei;
2. **Replace all five files** — the three YaHei files plus the two Segoe UI files;
3. **Write the registry** `HKCU\Control Panel\Desktop\WindowMetrics` for legacy shell elements (menu, message box, status bar, icon);
4. **Clear the font cache and swap at boot** — the files are locked while Windows runs, and a stale cache keeps serving the old glyphs.

## Build from source

Requires the .NET Framework compiler:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-wpf.ps1
```

The output is always `dist\YiTi.exe`.

## Disclaimer

The risks of modification are stated above. The author is not liable for system damage, data loss or any other loss caused by using this software.

## License

[MIT](LICENSE) — free to use, modify and redistribute, **as long as the author attribution and this license notice are kept**.
