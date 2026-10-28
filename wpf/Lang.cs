using System;
using System.Collections.Generic;
using System.IO;

namespace WinFontSwap.Wpf
{
    /// <summary>界面语言：中文 / English。以中文原文为键，未收录的一律回退中文。</summary>
    internal static class L
    {
        public static bool En;

        static readonly Dictionary<string, string> Map = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "未选择", "Not selected" },
            { "回滚失败：", "Rollback failed: " },
            { "易体", "Yiti" },
            { "易体 V", "Yiti V" },
            { "字体替换", "Fonts" },
            { "高级", "Advanced" },
            { "关于", "About" },
            { "读取中", "Reading…" },
            { "选择", "Select" },
            { "全部回滚", "Roll back all" },
            { "先选一个字体。", "Pick a font first." },
            { "没有可回滚的备份。", "There is no backup to roll back." },
            { "不支持当前系统", "Not supported on this Windows version" },
            { "替换目标", "Targets" },
            { "选择字体…", "Select font…" },
            { "源字体", "Source font" },
            { "常规", "Regular" },
            { "粗体", "Bold" },
            { "细体", "Light" },
            { "英文数字", "Latin & digits" },
            { "当前状态：", "Now: " },
            { "此版本 Windows 没有这个字体", "This Windows version has no such font" },
            { "系统元素", "Shell elements" },
            { "字号", "Size" },
            { "应用", "Apply" },
            { "默认值", "Defaults" },
            { "写入时清字体缓存", "Clear the font cache on apply" },
            { "重启后由系统重建", "Rebuilt by Windows after a reboot" },
            { "写入时清用户级同名字体", "Remove per-user same-name fonts" },
            { "会挡住替换", "They shadow the system font" },
            { "此版本 Windows 没有用户级字体", "This Windows version has no per-user fonts" },
            { "生成", "Generate" },
            { "只生成不写入", "Generate only" },
            { "只生成字体文件，不动系统", "Build the files only; leave the system alone" },
            { "还原", "Restore" },
            { "只还原字体文件", "Restore font files only" },
            { "从最近一次备份还原字体文件", "Restore the font files from the latest backup" },
            { "恢复", "Restore" },
            { "恢复失败：", "Restore failed: " },
            { "只还原用户级注册", "Restore per-user registry only" },
            { "导入最近一次的注册表备份", "Import the latest registry backup" },
            { "把 Windows 系统字体换成你喜欢的字体。", "Swap the Windows system fonts for any font you like." },
            { "版本", "Version" },
            { "当前版本号", "Current version" },
            { "能换什么", "What it replaces" },
            { "雅黑常规/粗体/细体、Segoe UI，以及标题栏等老式界面元素", "YaHei regular/bold/light, Segoe UI, and legacy shell elements" },
            { "支持系统", "Supported systems" },
            { "Windows 7 / 8 / 10 / 11（老系统没有的字体不会显示）", "Windows 7 / 8 / 10 / 11 (fonts missing on older systems are not shown)" },
            { "作者", "Author" },
            { "还原方式", "How to undo" },
            { "随时可用「全部回滚」或「默认值」取回系统原版字体", "\"Roll back all\" or \"Defaults\" brings back the Microsoft originals at any time" },
            { "项目主页", "Project home" },
            { "源码与问题反馈", "Source code and issue tracker" },
            { "替换前会自动备份原版字体，第一次应用后需要重启一次才会全局生效。", "Originals are backed up automatically; the first apply takes effect system-wide after a reboot." },
            { "文件缺失", "File missing" },
            { "缺文件", "missing" },
            { "未识别", "Unknown" },
            { "待重启", "reboot pending" },
            { "读系统字体状态失败：", "Failed to read the font state: " },
            { "系统元素已写入 ", "Shell elements written: " },
            { "写入失败：", "Write failed: " },
            { "系统元素已还原", "Shell elements restored" },
            { "还原失败：", "Restore failed: " },
            { "正在生成…", "Generating…" },
            { "正在生成并写入…", "Generating and writing…" },
            { "已生成到 ", "Generated into " },
            { "已写入，重启后生效。", "Written; takes effect after a reboot." },
            { "失败：", "Failed: " },
            { "读原版字体失败：", "Failed to read the original fonts: " },
            { "已切到默认字体，点「应用」写回", "Switched to the default fonts — press Apply to write them back" },
            { "没有可用备份，只能选字体", "No usable backup; pick a font instead" },
            { "把字体文件、用户级字体注册都还原到替换前？\r\n重启后生效。", "Restore both the font files and the per-user font registration?\nTakes effect after a reboot." },
            { "没有字体备份; ", "No font backup; " },
            { "已全部回滚：", "Rolled back everything: " },
            { " 重启后恢复。", " comes back after a reboot." },
            { "已还原字体文件，重启后恢复。", "Font files restored; back to normal after a reboot." },
            { "已回滚，重启后恢复。", "Rolled back; back to normal after a reboot." },
            { "选择字体", "Select a font" },
            { "系统已安装字体", "Installed fonts" },
            { "确定", "OK" },
            { "取消", "Cancel" },
            { "读取字体失败：", "Failed to read the font: " },
            { "界面语言", "Language" },
            { "小标题栏", "Small caption" },
            { "菜单", "Menu" },
            { "消息框", "Message box" },
            { "状态栏", "Status bar" },
            { "图标", "Icon" },
            { "已切换为中文", "Switched to Chinese" },
            { "重启后生效。", "Takes effect after a reboot." },
        };

        /// <summary>外置文案文件（exe 同目录 lang\zh.txt / lang\en.txt）覆盖内建表，方便改文案不用重新编译。</summary>
        static readonly Dictionary<string, string> Override = new Dictionary<string, string>(StringComparer.Ordinal);
        static string _loadedFor;

        public static string LangDir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lang"); }
        }

        /// <summary>读编译进 exe 的资源 lang.zh.txt / lang.en.txt；每行「原文=显示文本」，# 开头为注释。</summary>
        public static void LoadFile()
        {
            Override.Clear();
            var name = En ? "lang.en.txt" : "lang.zh.txt";
            _loadedFor = name;
            try
            {
                var asm = System.Reflection.Assembly.GetExecutingAssembly();
                string res = null;
                foreach (var r in asm.GetManifestResourceNames())
                {
                    if (r.EndsWith(name, StringComparison.OrdinalIgnoreCase)) { res = r; break; }
                }
                if (res == null) { WpfProgram.Log("lang: resource missing (" + name + ")"); return; }
                string text;
                using (var s = asm.GetManifestResourceStream(res))
                using (var sr = new StreamReader(s, System.Text.Encoding.UTF8))
                    text = sr.ReadToEnd();
                foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    var k = Unescape(line.Substring(0, eq));
                    var v = Unescape(line.Substring(eq + 1));
                    if (k.Length > 0) Override[k] = v;
                }
            }
            catch (Exception ex) { WpfProgram.Log("lang: " + ex.Message); }
        }

        static string Unescape(string s)
        {
            return s.Replace("\\n", "\n").Replace("\\r", "\r");
        }

        public static string T(string zh)
        {
            if (zh == null) return zh;
            string v;
            if (Override.TryGetValue(zh, out v)) return v;
            if (!En) return zh;
            string en;
            return Map.TryGetValue(zh, out en) ? en : zh;
        }

        static string SettingsPath
        {
            get { return Path.Combine(Installer.DataDir, "ui.lang"); }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return;
                var s = File.ReadAllText(SettingsPath).Trim();
                En = string.Equals(s, "en", StringComparison.OrdinalIgnoreCase);
            }
            catch { }
            LoadFile();
        }

        public static void Save()
        {
            try
            {
                Directory.CreateDirectory(Installer.DataDir);
                File.WriteAllText(SettingsPath, En ? "en" : "zh");
            }
            catch { }
        }
    }
}