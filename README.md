# WinFontSwap · 系统字体一键换

把 Windows 的**微软雅黑**（msyh.ttc / msyhbd.ttc / msyhl.ttc）换成你选的字体，让**设置、开始菜单、搜索、文件资源管理器、计算器**这些 WinUI/UWP 界面一起变。

## 为什么需要它

- noMeiryoUI 之类工具只改经典 Win32（GDI）界面，**管不到 WinUI/UWP**；官方 README 里也写明了不支持设置、画图等预装应用。
- WinUI 的字体来自框架主题资源，简体中文下写死为 `Microsoft YaHei UI`，**系统没有任何「选择界面字体」的设置项**。
- 唯一可行方法：生成一个**名字仍叫 Microsoft YaHei / Microsoft YaHei UI** 的字体文件，替换掉系统里的雅黑三件套。

## 用法

1. 双击 `bin\WinFontSwap.exe`（自带管理员清单，会直接提权）。
2. 点 **选择字体…** 选一个字体文件：**粗体/细体会自动按家族匹配**（同一家族里按字重 400/700/300 找）；没匹配到的字重保留系统原样，也可以点「指定」手动给。
3. 点 **应用并重启生效** → 重启电脑。
4. 想换回来：点 **回滚** → 重启。

> 建议先用「等线 Deng.ttf」「宋体 simsun.ttc」这类一眼能看出来、又不影响阅读的字体试一次，确认整条链路通了再换真正想要的字体。

## 界面

- 单页布局，配色沿用 DevLink（浅色/深色跟随系统，`#005FB8` / `#4CC2FF` 强调色）。
- 窗口背景走**透明色键**：卡片之外的区域直接透出桌面/壁纸，卡片保持不透明以保证可读性。
- 斜体由系统合成，不需要单独匹配。

## 它做了什么

1. 读你选的字体 → 重写 name 表 → 生成含两个 face 的 TTC（`Microsoft YaHei` + `Microsoft YaHei UI`，粗体/细体的家族名也对齐系统命名）。
2. 备份原字体到 `C:\ProgramData\WinFontSwap\backup\<时间戳>\`。
3. 接管三个字体文件的权限，把「重启时替换」写进 `PendingFileRenameOperations`，**重启由系统在字体加载前完成替换**（字体文件在运行中被映射，热替换一定失败，这是唯一可靠的方式）。
4. 新字体直接沿用系统字体的 ACL（含 UWP 需要的 `ALL APPLICATION PACKAGES` 读权限）。
5. 重启后再打开本程序，会把你字体文件的权限自动还原成系统原样。

## 限制

- 只支持 TrueType 轮廓（`.ttf` / `.ttc`）。**不支持 CFF(.otf) 和可变字体(VF)**，会直接报错——这两类生成出来的 TTC 在系统里表现不可靠。
- 替换后，所有显式用「微软雅黑」的排版也会跟着变（Office / WPS 导出图片、打印）。
- Windows 功能更新可能把字体覆盖回官方版本，需要重跑一次。
- 字宽/行高与雅黑差太多的字体会让部分界面换行、截断；建议用度量接近雅黑的（如更纱黑体 Sarasa UI）。

## 救急：重启后字体不对/没了

普通情况：重新打开本程序 → **回滚** → 重启。

界面已经看不清时，用 WinRE（开机按住 Shift 点重启 → 疑难解答 → 高级选项 → 命令提示符）：

```
xcopy /y "C:\ProgramData\WinFontSwap\backup\<时间戳>\*.ttc" "C:\Windows\Fonts\"
```

## 自检命令（控制台版）

```
WinFontSwap.console.exe --selftest     字体引擎：重建/生成/名字/可加载
WinFontSwap.console.exe --rendertest   把生成的字体真正画出来，检查中文是否缺字
WinFontSwap.console.exe --acltest      权限接管与还原
WinFontSwap.console.exe --applytest    完整应用链路（排队后自动取消，不重启）
```

## 目录

```
src/        C# 源码（Sfnt 字体引擎 / Installer 安装与回滚 / MainForm 界面）
res/        应用清单（管理员、DPI）
build.ps1   用系统自带 csc 编译，无需装 SDK
bin/        产物
```

编译：`powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1`
