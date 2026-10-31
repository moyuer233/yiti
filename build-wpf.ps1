$ErrorActionPreference = 'Stop'
# Every tracked .ps1 must stay ASCII-only (PowerShell 5.1 decodes BOM-less UTF-8 as ANSI).
$root = $PSScriptRoot
if (-not $root) { $root = Split-Path -Parent $MyInvocation.MyCommand.Path }
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'check-ascii.ps1')
if ($LASTEXITCODE -ne 0) { throw 'ASCII check failed' }

$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$gac = 'C:\Windows\Microsoft.NET\assembly'
$outDir = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $outDir | Out-Null
$out = Join-Path $outDir 'YiTi.exe'

# Kill the running release build so csc can overwrite it (CS0016 otherwise).
Get-Process YiTi -ErrorAction SilentlyContinue | ForEach-Object { $_.Kill() }
Start-Sleep -Milliseconds 400

$a = @()
$a += '/nologo'
$a += '/target:winexe'
$a += ('/out:' + $out)
$a += ('/win32manifest:' + (Join-Path $root 'wpf\app.manifest'))
$ico = Join-Path $root 'icons\app.ico'
if (Test-Path $ico) {
  $a += ('/win32icon:' + $ico)
  $a += ('/resource:' + $ico + ',app.ico')
}
$a += ('/r:' + (Join-Path $gac 'GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll'))
$a += ('/r:' + (Join-Path $gac 'GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll'))
$a += ('/r:' + (Join-Path $gac 'GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll'))
$a += ('/r:' + (Join-Path $gac 'GAC_MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll'))
$a += '/r:System.Drawing.dll'
$a += '/r:System.Core.dll'
foreach ($f in @('AssemblyInfo.cs','Sfnt.cs','Installer.cs','FontLibrary.cs','Win32Fonts.cs','ShellMetrics.cs')) {
  $a += (Join-Path $root ('src\' + $f))
}
foreach ($f in @('WpfApp.cs','Lang.cs','MainWindow.cs','ConfirmDialog.cs','ChoiceDialog.cs','BackupDialog.cs','FontPickerDialog.cs','ShellFontEnum.cs','Diagnostics.cs','Styles.cs')) {
  $a += (Join-Path $root ('wpf\' + $f))
}

# UI strings: lang\zh.toml and lang\en.toml are embedded into the exe as resources.
$langSrc = Join-Path $root 'lang'
if (Test-Path (Join-Path $langSrc 'zh.toml')) { $a += ('/resource:' + (Join-Path $langSrc 'zh.toml') + ',lang.zh.toml') }
if (Test-Path (Join-Path $langSrc 'en.toml')) { $a += ('/resource:' + (Join-Path $langSrc 'en.toml') + ',lang.en.toml') }
& $csc $a
if ($LASTEXITCODE -ne 0) { throw ('WPF compile failed: ' + $LASTEXITCODE) }

# Sign + trust. UAC is off on the build machine, so -Verb RunAs is silent.
$signNote = 'sign skipped'
try {
  $pr = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $root 'sign.ps1') -Verb RunAs -WindowStyle Hidden -PassThru -Wait
  $signNote = 'sign exit ' + $pr.ExitCode
} catch { $signNote = 'sign skipped: ' + $_.Exception.Message }

$sig = Get-AuthenticodeSignature -LiteralPath $out
if ($sig.Status -ne 'Valid') { throw ('signature is ' + $sig.Status + ' (see _sign.log)') }

$fi = Get-Item -LiteralPath $out
'WPF OK  ' + $fi.FullName + '  ' + $fi.Length + ' bytes  ' + $signNote + '  sig=Valid'
