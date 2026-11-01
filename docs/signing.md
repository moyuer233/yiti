# 代码签名（SignPath 免费开源签名）

本地默认走**自签名**（`sign.ps1`，证书 `CN=moyuer233`，只在本机受信任）。
正式签名用 SignPath 对开源项目提供的免费签名服务，证书由 SignPath 持有，签名在 CI 里完成。

## 一、申请（需要你本人操作）

1. 打开 <https://signpath.io/free-code-signing-for-open-source-projects>，按页面填写申请；
2. 需要提供：仓库地址（https://github.com/moyuer233/yiti）、开源许可证（MIT）、构建方式说明；
3. 审核通过后，在 SignPath 控制台里建：
   - **Organization** → 记下 `organization-id`（形如 GUID，填到 GitHub 的 `SIGNPATH_ORGANIZATION_ID` 变量）；
   - **Project**：slug 填 `yiti`（与 workflow 里的 `project-slug` 一致）；
   - **Signing policy**：slug 填 `release-signing`（与 `signing-policy-slug` 一致）；
   - **Artifact configuration**：slug 填 `exe`，规则是签 `YiTi.exe` 这一个 PE 文件（与 `artifact-configuration-slug` 一致）；
4. 在 SignPath 生成 **API token**。

## 二、配置 GitHub 仓库

```
Settings → Secrets and variables → Actions
  Secrets:  SIGNPATH_API_TOKEN   = <SignPath 生成的 token>
  Variables: SIGNPATH_ORGANIZATION_ID = <SignPath 的 organization id>
```

> 推送 `.github/workflows/*.yml` 需要 workflow 权限。
> 若 `git push` 报 `refusing to allow an OAuth App to create or update workflow`，先执行：
> `gh auth refresh -h github.com -s workflow`（浏览器授权一次）。

## 三、CI 流程

`.github/workflows/build-sign.yml`：

1. `windows-latest` 上 checkout；
2. `./build-wpf.ps1 -NoSign` —— 只编译，不做自签名（构建脚本已支持该开关）；
3. 上传未签名产物 `unsigned-exe`；
4. 调 `signpath/github-action-submit-signing-request` 提交签名请求（等待完成）；
5. 上传签名后的 `signed-exe`。

未配置 `SIGNPATH_API_TOKEN` 时签名两步自动跳过，CI 只验证“能否编译”。

## 四、本地构建不受影响

- 平时开发：`build-wpf.ps1`（自签名，本机受信任，`WPF OK ... sig=Valid`）
- 需要未签名产物：`build-wpf.ps1 -NoSign`（输出 `sig=none`）
- 正式发布：等 SignPath 通过后，用 CI 产出的 `signed-exe` 作为 release 资产；届时 release 里要换掉当前的 `YiTi.exe`

## 五、签名不等于免杀软

签名能消除“未知发布者”，但本工具会修改 `C:\Windows\Fonts`，行为本身仍可能被杀软拦。若有误报，
把杀软报告与 VirusTotal 链接整理后提交误报（微软：<https://www.microsoft.com/en-us/wdsi/filesubmission>）。