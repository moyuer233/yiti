# NOTE: ASCII-only. Windows PowerShell 5.1 decodes BOM-less UTF-8 as ANSI.
$ErrorActionPreference = 'Continue'
$log = 'H:\1\WinFontSwap\_sign.log'
function L($m) { Add-Content -Path $log -Value ((Get-Date -Format 'HH:mm:ss') + ' ' + $m) -Encoding UTF8 }
Set-Content -Path $log -Value '=== sign+trust+unblock ===' -Encoding UTF8
$subject = 'CN=moyuer233'
taskkill /F /IM YiTi.exe 2>&1 | Out-Null
taskkill /F /IM WinFontSwap.exe 2>&1 | Out-Null

$exes = @(
  'H:\1\WinFontSwap\dist\YiTi.exe',
  'H:\1\WinFontSwap\build\WinFontSwap.exe',
  'H:\1\WinFontSwap\build\WinFontSwap.console.exe')

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
} catch { L ('cert ERROR ' + $_.Exception.Message); $cert = $null }

foreach ($exe in $exes) {
  if (-not (Test-Path $exe)) { continue }
  try { Unblock-File -LiteralPath $exe -ErrorAction SilentlyContinue } catch { }
  try { Remove-Item -LiteralPath $exe -Stream Zone.Identifier -ErrorAction SilentlyContinue } catch { }
  if ($cert) {
    try {
      $sig = Set-AuthenticodeSignature -FilePath $exe -Certificate $cert
      L ('sign ' + (Split-Path $exe -Leaf) + ' -> ' + $sig.Status)
    } catch { L ('sign ERROR ' + $_.Exception.Message) }
  }
  try { Unblock-File -LiteralPath $exe -ErrorAction SilentlyContinue } catch { }
  $streams = (Get-Item -LiteralPath $exe -Stream * | Where-Object { $_.Stream -ne ':$DATA' } | Measure-Object).Count
  L ('zone-streams ' + (Split-Path $exe -Leaf) + ' = ' + $streams)
}

try {
  $k = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Policies\Associations'
  if (-not (Test-Path $k)) { New-Item -Path $k -Force | Out-Null }
  New-ItemProperty -Path $k -Name 'LowRiskFileTypes' -Value '.exe;.lnk;' -PropertyType String -Force | Out-Null
  L ('lowrisk = ' + (Get-ItemProperty $k -Name LowRiskFileTypes).LowRiskFileTypes)
} catch { L ('lowrisk ERROR ' + $_.Exception.Message) }

try {
  $k2 = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\AppHost'
  if (-not (Test-Path $k2)) { New-Item -Path $k2 -Force | Out-Null }
  New-ItemProperty -Path $k2 -Name 'EnableWebContentEvaluation' -Value 0 -PropertyType DWord -Force | Out-Null
  L ('apphost eval = ' + (Get-ItemProperty $k2 -Name EnableWebContentEvaluation).EnableWebContentEvaluation)
} catch { L ('apphost ERROR ' + $_.Exception.Message) }

L 'done'