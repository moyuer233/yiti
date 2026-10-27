# Build WinFontSwap: GUI + console diagnostic. NOTE: keep this file ASCII-only.
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$srcDir = Join-Path $root 'src'
$outDir = Join-Path $root 'build'
New-Item -ItemType Directory -Force $outDir | Out-Null

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) { throw "csc.exe not found: $csc" }

$sources = Get-ChildItem $srcDir -Filter *.cs | ForEach-Object { $_.FullName }
$manifest = Join-Path $root 'res\app.manifest'
$flags = @(
  '/nologo', '/platform:anycpu', '/optimize+', '/codepage:65001',
  '/r:System.dll', '/r:System.Drawing.dll', '/r:System.Windows.Forms.dll', '/r:System.Core.dll'
)

& $csc @flags '/target:winexe' ('/out:' + (Join-Path $outDir 'WinFontSwap.exe')) ('/win32manifest:' + $manifest) $sources
if ($LASTEXITCODE -ne 0) { throw "GUI compile failed: $LASTEXITCODE" }

& $csc @flags '/target:exe' ('/out:' + (Join-Path $outDir 'WinFontSwap.console.exe')) $sources
if ($LASTEXITCODE -ne 0) { throw "console compile failed: $LASTEXITCODE" }

# sign outputs as moyuer233 (elevated, silent because UAC is off)
try {
  $pr = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $root 'sign.ps1') -Verb RunAs -WindowStyle Hidden -PassThru -Wait
  Write-Host ("sign exit " + $pr.ExitCode)
} catch { Write-Host "sign skipped" }

Get-ChildItem $outDir -Filter *.exe | Select-Object Name, Length | Format-Table -AutoSize
Write-Host "OK"
