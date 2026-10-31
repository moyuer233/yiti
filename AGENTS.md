# 易体 / Yiti（H:\1\WinFontSwap）

Windows 系统字体一键替换工具。产物 `dist\YiTi.exe`（单文件、内嵌管理员清单），仓库 https://github.com/moyuer233/yiti（PUBLIC，main，MIT 需保留 `Copyright (c) 2026 moyuer233`）。

- 源码：`src\`（Sfnt / Installer / FontLibrary / ShellMetrics / Win32Fonts）+ `wpf\`（MainWindow / Lang / ConfirmDialog / BackupDialog / FontPickerDialog / Styles / WpfApp）。
- 构建：`build-wpf.ps1`（csc 编 WPF → 提权 `sign.ps1` 自签 → 硬断言签名 Valid），成功唯一标记 `WPF OK <path> <bytes> … sig=Valid`。所有 tracked `.ps1` 必须纯 ASCII（`check-ascii.ps1` 是闸门）。
- 文案：`lang\zh.toml` / `lang\en.toml`，TOML（`[分节]` + `"键 = \"值\""`），键是「界面位置.功能」语义名，编译进 exe；改文案只改等号右边引号里的字并重编译。
- 状态与备份：`C:\ProgramData\WinFontSwap\`（state.ini、backup\、stage\、ui.lang）。

## 发布说明（Release notes）怎么写 —— 长期规则，用户明确要求

**结构只有三段（首版可多一段「使用」）：**

```
## 新增
## 修复
## 升级影响
```

**每条写成「原来怎样 → 现在怎样」，讲用户能感知的影响，不讲实现细节。**

**以下一律不写（用户逐条否掉过）：**

- 包装标题：「这一版改了什么」「本次更新」「版本说明」这类外层小标题 —— 直接从 `## 新增` 开始。
- 版本号理由：「为什么发 0.3.0」「这次跳到 0.3 是因为…」—— 版本策略不写进 release 说明。
- 自我验证：「已验证」「测试通过」「实测正常」这类自证段落 —— 验证结果不写进 release 说明。
- 开发过程：「全仓代码审查（ocr 委派模式）发现 13 项」「这一轮」「High → Low」—— 过程不进用户文档。
- 重复的版本号首行（正文开头单独一行 `0.3.0`）—— 标题里已经有了。
- 装饰性开场句（「首个公开版本。」单独成段）。

**格式细则：**

- Release 标题统一 `易体 x.y.z`（不要 `Yiti v0.2.5`、不要带 `v`）。
- 小节名只用 `新增` / `修复` / `升级影响`（首版可加 `使用`）；不要 `交互`/`界面`/`其他`/`用途`/`说明` 这类杂项标题，按语义并入三段。
- 结尾保留一行 `详细说明与风险见 [README](https://github.com/moyuer233/yiti#readme)。`
- 改规则时**历史 release 一起改**（用户会要求「其他的呢」「其他的都改」），改完用 `gh api` 复查每个 tag 的 name 与小节。

**技术注意：**

- 中文标题/正文不要直接放进 `.ps1`（PS 5.1 按 ANSI 解码 → GitHub 上显示 `鏄撲綋`）。标题用 `gh api -X PATCH ... --input <utf8 json>`，正文用 `--notes-file <utf8 文件>`。
- 发版后核对：`gh release view --json assets` 的 `digest` 与本地 exe 的 SHA-256 一致。

## 其他既定约定

- 版本号只在用户说「发新版本」时递增；说「把 X 的发布替换一下」就原地替换同版本号（删 release + 重建 tag）。
- README.md 中文由用户自己在 GitHub 网页上改；我拉下来再据他的版本翻译 README.en.md，他的措辞一律保留。
- 截图 `docs\{font,adv,about}.png` + `_en`，必须带侧边栏（用 `--shot`，不是 `--shotlong`）。
- 每次加/改用户可见功能，同一轮内同步 README 功能列表。
