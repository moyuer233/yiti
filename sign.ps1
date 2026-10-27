$ErrorActionPreference = 'Continue'
$log = 'H:\1\WinFontSwap\_sign.log'
function L($m) { Add-Content -Path $log -Value ((Get-Date -Format 'HH:mm:ss') + ' ' + $m) -Encoding UTF8 }
Set-Content -Path $log -Value '=== sign start ===' -Encoding UTF8
$subject = 'CN=moyuer233'
taskkill /F /IM WinFontSwapWpf.exe 2>&1 | Out-Null
taskkill /F /IM WinFontSwap.exe 2>&1 | Out-Null

try {
  $cert = Get-ChildItem Cert:\LocalMachine\My | Where-Object { $_.Subject -eq $subject } | Select-Object -First 1
  if (-not $cert) {
    $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject $subject -KeyUsage DigitalSignature -FriendlyName 'moyuer233 (WinFontSwap)' -CertStoreLocation Cert:\LocalMachine\My -NotAfter (Get-Date).AddYears(10)
    L ('created ' + $cert.Thumbprint)
  }
  $cer = Join-Path $env:TEMP 'wfs-moyuer233.cer'
  Export-Certificate -Cert $cert -FilePath $cer -Force | Out-Null
  Import-Certificate -FilePath $cer -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
  Import-Certificate -FilePath $cer -CertStoreLocation Cert:\LocalMachine\TrustedPublisher | Out-Null
  foreach ($exe in @('H:\1\WinFontSwap\bin\WinFontSwapWpf.exe','H:\1\WinFontSwap\bin\WinFontSwap.exe','H:\1\WinFontSwap\bin\WinFontSwap.console.exe')) {
    if (Test-Path $exe) {
      $sig = Set-AuthenticodeSignature -FilePath $exe -Certificate $cert
      L ('sign ' + (Split-Path $exe -Leaf) + ' -> ' + $sig.Status)
    }
  }
} catch { L ('ERROR ' + $_.Exception.Message) }

# 关掉「来自 Internet 的文件」安全警告（仅对 exe/lnk 这类本机可执行文件降低提示）
try {
  $k = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\Associations'
  if (-not (Test-Path $k)) { New-Item -Path $k -Force | Out-Null }
  New-ItemProperty -Path $k -Name 'LowRiskFileTypes' -Value '.exe;.lnk;' -PropertyType String -Force | Out-Null
  L ('lowrisk = ' + (Get-ItemProperty $k -Name LowRiskFileTypes).LowRiskFileTypes)
} catch { L ('lowrisk ERROR ' + $_.Exception.Message) }

L 'done'
