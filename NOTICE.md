# 第三方参考

## Lyricify-Backgrounds（Apache License 2.0）

本项目的整窗渐变背景在结构上参考了
[WXRIW/Lyricify-Backgrounds](https://github.com/WXRIW/Lyricify-Backgrounds) 的
Apple Music 风格背景实现：

- 三层叠加（一层主色 scale 1.4 + 两层辅助 scale 0.7）；
- 每层独立旋转角度；
- 最后压一层暗色 scrim。

本项目为 C#/WinForms 实现，未使用其 GPU 网格（HLSL）与动画逻辑，
仅参考上述分层结构，代码为独立编写。

原项目许可：Apache License, Version 2.0
（https://github.com/WXRIW/Lyricify-Backgrounds/blob/master/LICENSE.txt）
