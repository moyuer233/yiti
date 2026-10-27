# NOTE: this file MUST stay ASCII-only (Windows PowerShell 5.1 decodes BOM-less UTF-8 as ANSI).
# Standard: every TRACKED .ps1 in this repo is ASCII-only. Scratch scripts go to _tmp\ and are not tracked.
$root = $PSScriptRoot
if (-not $root) { $root = 'H:\1\WinFontSwap' }
$files = @()
if (Test-Path (Join-Path $root '.git')) {
  Push-Location $root
  $tracked = & git ls-files '*.ps1' 2>$null
  Pop-Location
  foreach ($t in $tracked) { $files += (Join-Path $root $t) }
} else {
  $files = Get-ChildItem $root -Filter *.ps1 -File | ForEach-Object { $_.FullName }
}
$bad = @()
foreach ($f in $files) {
  if (-not (Test-Path $f)) { continue }
  $bytes = [System.IO.File]::ReadAllBytes($f)
  $high = 0
  foreach ($b in $bytes) { if ($b -gt 127) { $high++ } }
  if ($high -gt 0) { $bad += ($f + '  non-ascii-bytes=' + $high) }
}
if ($bad.Count -gt 0) {
  'FAIL: non-ASCII tracked .ps1 found'
  $bad | ForEach-Object { '  ' + $_ }
  exit 1
}
'OK: all ' + $files.Count + ' tracked .ps1 are ASCII-only'
exit 0
