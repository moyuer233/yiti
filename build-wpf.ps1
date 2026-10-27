$ErrorActionPreference = 'Stop'
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
if ($LASTEXITCODE -ne 0) { throw ('WPF 编译失败：' + $LASTEXITCODE) }
$fi = Get-Item $out
'WPF OK  ' + $fi.Length + ' bytes  ' + $fi.LastWriteTime
