# 编译 WinFontSwap：GUI 版 + 控制台诊断版
# 用法：powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$srcDir = Join-Path $root 'src'
$binDir = Join-Path $root 'bin'
New-Item -ItemType Directory -Force $binDir | Out-Null

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "找不到 csc.exe: $csc" }

$sources = Get-ChildItem $srcDir -Filter *.cs | ForEach-Object { $_.FullName }
$manifest = Join-Path $root 'res\app.manifest'
$flags = @(
  '/nologo', '/platform:anycpu', '/optimize+', '/codepage:65001',
  '/r:System.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Core.dll'
)

& $csc @flags '/target:winexe' ('/out:' + (Join-Path $binDir 'WinFontSwap.exe')) ('/win32manifest:' + $manifest) $sources
if ($LASTEXITCODE -ne 0) { throw "GUI 编译失败：$LASTEXITCODE" }

& $csc @flags '/target:exe' ('/out:' + (Join-Path $binDir 'WinFontSwap.console.exe')) $sources
if ($LASTEXITCODE -ne 0) { throw "控制台版编译失败：$LASTEXITCODE" }

Get-ChildItem $binDir -Filter *.exe | Select-Object Name, Length | Format-Table -AutoSize
Write-Host "OK"
