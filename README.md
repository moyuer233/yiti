<p align="center">
  <a href="https://github.com/moyuer233/yiti"><img src="docs/icon.png" width="200" height="200" alt="易体"></a>
</p>

<div align="center">

# 易体 (Yiti)

_✨ 傻瓜式Windows全局字体替换工具 ✨_

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
  <a href="https://github.com/moyuer233/yiti"><img alt="deps" src="https://img.shields.io/badge/%E5%8D%95%E6%96%87%E4%BB%B6-%E6%97%A0%E4%BE%9D%E8%B5%96-5C2D91?style=flat-square&labelColor=161b22&color=5C2D91"/></a>
</p>

![字体替换](docs/font.png)

## 主要功能

- **系统字体字体**：设置、开始菜单、任务栏、标题栏等地方的字体
- **英文数字替换**：Segoe UI、Segoe UI Variable
- **界面元素字体替换**：小标题栏、菜单、消息框、状态栏、图标
- **字重自动匹配**：选一个字体就够，按 400 / 700 / 300 自动配对，微软雅黑 常规 / 粗体 / 细体，三个字重一起换；支持自定义
- **自动备份与回滚**：「全部回滚」一键还原；原版还能从 Windows 组件存储取回，备份丢了也能救
- **自动匹配系统版本**：如系统里没有的字体（Win7 没有细体和 Segoe UI Variable）会自动禁用并说明原因

## 换的是哪些文件

不是只换微软雅黑，一共 5 个：

| 文件 | 是什么 | 影响哪里 |
|---|---|---|
| `msyh.ttc` | 微软雅黑 常规 | 中文正文、设置、开始菜单、资源管理器 |
| `msyhbd.ttc` | 微软雅黑 粗体 | 所有加粗中文 |
| `msyhl.ttc` | 微软雅黑 细体 | 细体（Win8 起才有） |
| `SegUIVar.ttf` | Segoe UI Variable | 任务栏、设置顶部、Win11 新界面的英数 |
| `segoeui.ttf` | Segoe UI | 经典英文界面、老程序 |

做法是：读你的字体 → 只把字体里的名字改成 `Microsoft YaHei` / `Segoe UI` 这些 → 生成同名字体文件 → 替换系统里那 5 个 → 重启生效。**字形是你的，名字是微软的**，系统才肯用它。

另外「系统元素」那几项是写注册表 `HKCU\Control Panel\Desktop\WindowMetrics`，控制老式界面的字体和字号。

**换不了的**：自带字体的程序（Office、Electron 应用、游戏）、网页里指定字体的文字、字体渲染参数（ClearType 那类）。
### 为什么要改字体里的名字

Windows **找字体不看文件名，看字体内部的 `name` 表**（OpenType 规范里的命名表）。一个字体有 20 多个名字字段，各有编号：

| 编号 | 含义 | 系统拿它干什么 |
|---|---|---|
| 1 | 族名（Family） | 就是「微软雅黑」这四个字。GDI 的 LOGFONT、WinUI 主题里写死的字体名都指它 |
| 2 | 样式（Subfamily） | Regular / Bold / Light |
| 4 | 完整名 | 「Microsoft YaHei Bold」 |
| 6 | PostScript 名 | Adobe 系软件按它认字体 |
| 16 / 17 | 排版族名 / 排版样式 | 现代 DirectWrite 优先用这两个 |
| 3 / 18 / 21 / 22 | 唯一 ID / WWS 名 | 字体缓存与字体选单 |

易体只重写上面这 10 个字段（每条记录在 Windows 和 Mac 两种平台编码里各一份），**字形表 `glyf`、度量 `hmtx`、字符映射 `cmap`、连字 `GSUB/GPOS` 一个字节都不动**。所以换完之后：

- 界面里显示的名字还是「微软雅黑」，但**每一笔都来自你选的字体**；
- 系统按名字找字体，找到的就是这个改了名的文件，于是设置、开始菜单、任务栏都跟着变；
- 版权 / 版本 / 商标 / 设计师（0、5、7、9）**故意不改**：既保留原作者信息，也正好让程序能反查「当前装的到底是什么字体」。

**为什么不改 OS/2 里的字重值（usWeightClass）**：选源文件时就按字重挑（粗体槽放 700 的源），字重标记天生就是对的；硬改反倒会和应用程序自己挑字重的逻辑打架。
## 和 noMeiryoUI 有什么不同

[noMeiryoUI](https://github.com/Tatsu-syo/noMeiryoUI) 改的是「界面字体设置」（注册表 `HKCU\Control Panel`），所以它做不到：任务栏、预装应用（设置、画图）、忽略系统字体的程序、以及 Windows 11 的标题栏字体。

易体换的是**系统字体文件本身**，这几处都可以更改。两点核心差异：

| | 易体 | noMeiryoUI |
|---|---|---|
| 原理 | 替换系统字体文件（雅黑三件套 + Segoe UI） | 改界面字体设置 |
| 设置、开始菜单、任务栏、标题栏 | ✅ 一起变 | ❌ 不支持 |
| 回滚 | 自动备份 + 一键回滚，备份丢了还能从系统组件存储取原版 | 需要自己导出注册表 |

字体渲染的清晰度（ClearType）、自带字体的应用（Office、Electron 类）、以及首次必须重启才生效。

## 食用

1. 点「选择字体…」挑一个字体
2. 点「应用」
3. 重启一次电脑

## 还原

原版字体优先从 Windows 组件存储（`C:\Windows\WinSxS`）取，其次用第一次应用时自动做的备份。

## 风险

这个工具改的是**系统字体文件**，属于「改完必须重启、重启前不好回头」的操作。已知风险如下：

- **装错字体可能导致界面缺字**：字体汉字不全或缺对应字重时，重启后菜单、标题栏会出现方框或糊字；严重时部分老程序显示异常。
- **影响 Office / WPS 的排版与导出**：文档里的「微软雅黑」此时已是你的字体，导出图片、打印、转 PDF 会和原来不一样。
- **应用必须重启**：字体在重启时才真正替换，中间强制关机可能更换不成功，需要再重启一次。
- **可能被杀软拦或误报**：要修改 `C:\Windows\Fonts`文件夹，部分安全软件会拦截甚至报毒；被拦时明确报错，不静默失败。
- **系统更新可能覆盖**：Windows 大版本更新会重置系统字体，更新后要重新应用一次。
- **回滚依赖备份或组件存储**：会自动备份、也能从 WinSxS 取原版，但两者都被清理过就只剩还原点或安装介质 —— **动手前建议自己再建一个还原点**。

## 支持的系统

Windows 7 / 8 / 10 / 11。需要管理员权限（要写 `C:\Windows\Fonts`），程序自带提权声明，双击即可。

各版本能换的东西不一样，程序会自动探测：系统里没有的字体显示成灰色不可点，悬停说明「不支持当前系统」。

| | Win7 | Win10 1809+ | Win11 22000+ |
|---|---|---|---|
| 雅黑 常规 / 粗体 | ✅ | ✅ | ✅ |
| 雅黑 细体（`msyhl.ttc`） | ❌ 系统没有 | ✅ | ✅ |
| 英数可变字体（`SegUIVar.ttf`） | ❌ 系统没有 | ✅ | ✅ |
| 清理用户级同名字体 | ❌ 无此机制 | ✅ | ✅ |
| 任务栏 / 设置等新界面 | 部分（靠 `segoeui.ttf`） | ✅ | ✅ |
| 可变字体（.ttf 的 VF 版）当源字体 | ❌ 只画默认字重 | ✅ | ✅ |
| 窗口背景 | 纯灰底 | 纯灰底 | 云母（Mica） |

老系统上「字体缓存服务」叫 `FontCache3.0.0.0`，Win10 之后是 `FontCache`，程序会按系统自己选。

## 从源码构建

需要`.NET Framework`编译器：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build-wpf.ps1
```

产物固定在 `dist\YiTi.exe`。

## 免责

本软件已明确说明修改风险，因使用本软件导致的系统异常、数据丢失或其他损失，程序作者不承担相关责任。

## 许可

[MIT](LICENSE) —— 可以自由使用、修改、分发，**但必须保留作者署名与许可声明**。