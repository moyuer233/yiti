$ErrorActionPreference = 'Stop'
# Gate: every tracked .ps1 must stay ASCII-only
& powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path 'H:\1\WinFontSwap' 'check-ascii.ps1')
if ($LASTEXITCODE -ne 0) { throw 'ASCII check failed' }
$root = 'H:\1\WinFontSwap'
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$gac = 'C:\Windows\Microsoft.NET\assembly'
$out = Join-Path $root 'bin\WinFontSwapWpf.exe'

Get-Process WinFontSwapWpf -ErrorAction SilentlyContinue | ForEach-Object { $_.Kill() }
Start-Sleep -Milliseconds 400

$a = @()
$a += '/nologo'
$a += '/target:winexe'
$a += ('/out:' + $out)
$a += ('/r:' + (Join-Path $gac 'GAC_MSIL\PresentationFramework\v4.0_4.0.0.0__31bf3856ad364e35\PresentationFramework.dll'))
$a += ('/r:' + (Join-Path $gac 'GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll'))
$a += ('/r:' + (Join-Path $gac 'GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll'))
$a += ('/r:' + (Join-Path $gac 'GAC_MSIL\System.Xaml\v4.0_4.0.0.0__b77a5c561934e089\System.Xaml.dll'))
$a += '/r:System.Drawing.dll'
$a += '/r:System.Core.dll'
foreach ($f in @('Sfnt.cs','Installer.cs','FontLibrary.cs','FontLink.cs','Presets.cs','Win32Fonts.cs')) {
  $a += (Join-Path $root ('src\' + $f))
}
foreach ($f in @('WpfApp.cs','MainWindow.cs','FontPickerDialog.cs','Styles.cs')) {
  $a += (Join-Path $root ('wpf\' + $f))
}

& $csc $a
if ($LASTEXITCODE -ne 0) { throw ('WPF compile failed: ' + $LASTEXITCODE) }

# sign as moyuer233 (elevated, silent because UAC is off)
$signed = 'not signed'
try {
  $pr = Start-Process powershell -ArgumentList '-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $root 'sign.ps1') -Verb RunAs -WindowStyle Hidden -PassThru -Wait
  $signed = 'sign exit ' + $pr.ExitCode
} catch { $signed = 'sign skipped' }

$fi = Get-Item $out
'WPF OK  ' + $fi.Length + ' bytes  ' + $signed
