# Packs the portable zip: raw exe + portable marker + README.
# The marker makes the exe resolve config next to itself (upward search for a
# config/ ancestor); MSI installs have no marker and use %APPDATA%.
#
# Usage: powershell -ExecutionPolicy Bypass -File scripts/pack-portable.ps1

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$Exe = Join-Path $RepoRoot "src-tauri\target\release\launchpad-tauri.exe"
$Version = "0.1.0"
# NOT the vite dist/ (tauri build's beforeBuildCommand clears it); use release/.
$OutDir = Join-Path $RepoRoot "release"
$ZipName = "Launchpad-$Version-Windows-Portable.zip"
$StageDir = Join-Path $env:TEMP "launchpad-portable-stage"

if (-not (Test-Path $Exe)) {
    throw "Release exe not found at $Exe — run `tauri build --no-bundle` first."
}

if (Test-Path $StageDir) { Remove-Item $StageDir -Recurse -Force }
New-Item -ItemType Directory -Path $StageDir | Out-Null

Copy-Item $Exe $StageDir
# Portable marker: presence flips InstallForm to Portable.
New-Item -ItemType File -Path (Join-Path $StageDir "launchpad.portable") | Out-Null
Set-Content -Path (Join-Path $StageDir "README.txt") -Encoding UTF8 -Value @"
Launchpad $Version — Portable edition
=====================================
Unzip anywhere. Run launchpad-tauri.exe.
Config lives in a config/ folder next to the exe (or in the nearest ancestor
that already has one) — the whole folder can be moved around freely.

Requirements: WebView2 Runtime (built into Windows 11; Windows 10 install from
https://developer.microsoft.com/microsoft-edge/webview2)
"@

New-Item -ItemType Directory -Path $OutDir -Force | Out-Null
$Zip = Join-Path $OutDir $ZipName
if (Test-Path $Zip) { Remove-Item $Zip -Force }
Compress-Archive -Path (Join-Path $StageDir "*") -DestinationPath $Zip
Remove-Item $StageDir -Recurse -Force

Write-Host "Portable zip: $Zip"
