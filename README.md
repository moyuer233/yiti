<p align="center">
  <a href="https://github.com/moyuer233/yiti"><img src="docs/icon.png" width="200" height="200" alt="易体"></a>
</p>

<div align="center">

# 易体 (Yiti)

_✨ 傻瓜式Windows全局字体替换工具 ✨_

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
  <a href="https://github.com/moyuer233/yiti"><img alt="deps" src="https://img.shields.io/badge/%E5%8D%95%E6%96%87%E4%BB%B6-%E6%97%A0%E4%BE%9D%E8%B5%96-5C2D91?style=flat-square&labelColor=161b22&color=5C2D91"/></a>
</p>

![字体替换](docs/font.png?v=2)

## 主要功能

- **系统字体替换**：设置、开始菜单、任务栏、标题栏等应用内的字体
- **界面元素字体替换**：小标题栏、菜单、消息框、状态栏、图标
- **字重自动匹配**：选中一个字体，字重按 400 / 700 / 300 自动匹配，如：微软雅黑 常规 / 粗体 / 细体，三个字重同时更换；支持自定义选择字体
- **自动备份与回滚**：「全部回滚」一键还原；默认值优先从`C:\Windows\WinSxS`获取，以便备份不存在时恢复默认值
- **自动匹配系统版本**：系统内缺失的字体（如Win7 没有细体和 Segoe UI Variable）会自动禁用

## 和 noMeiryoUI 有什么不同

[noMeiryoUI](https://github.com/Tatsu-syo/noMeiryoUI) 改的是「界面字体设置」（注册表 `HKCU\Control Panel`），所以它做不到：任务栏、预装应用（设置、画图）、忽略系统字体的程序、以及 Windows 11 的标题栏字体。