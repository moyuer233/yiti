$ErrorActionPreference = 'Continue'
$log = 'H:\1\WinFontSwap\_sign.log'
function L($m) { Add-Content -Path $log -Value ((Get-Date -Format 'HH:mm:ss') + ' ' + $m) -Encoding UTF8 }
Set-Content -Path $log -Value '=== sign start ===' -Encoding UTF8
$exe = 'H:\1\WinFontSwap\bin\WinFontSwapWpf.exe'
$subject = 'CN=moyuer233'
taskkill /F /IM WinFontSwapWpf.exe 2>&1 | Out-Null
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
  $sig = Set-AuthenticodeSignature -FilePath $exe -Certificate $cert
  L ('sign ' + $sig.Status)
  $c = Get-AuthenticodeSignature $exe
  L ('verify ' + $c.Status + ' / ' + $c.SignerCertificate.Subject)
} catch { L ('ERROR ' + $_.Exception.Message) }
L 'done'
