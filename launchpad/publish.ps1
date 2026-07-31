# Publish launchpad as an unpackaged, self-contained WinUI 3 app.
# WinUI 3 cannot be published as a single file — native Windows App SDK
# dependencies stay as separate files next to the exe (official limitation).
# Output layout includes a config/ folder with the example file as a template,
# so first-run writes succeed without a config/ ancestor above the exe.
#
# Usage: powershell -File publish.ps1 [-OutputDir publish]
param(
    [string]$OutputDir = "publish"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

$project = Join-Path $root "src/launchpad/launchpad.csproj"
$output = Join-Path $root $OutputDir

Write-Host "Publishing to $output"
dotnet publish $project -c Release `
    -p:RuntimeIdentifierOverride=win-x64 `
    -p:WindowsPackageType=None `
    -p:WindowsAppSDKSelfContained=true `
    -o $output
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

# dotnet publish does not copy the XAML compile outputs (xbf/pri); without them the
# published exe dies with XamlParseException at MainWindow.InitializeComponent.
# Copy them from the Release bin output.
$binOutput = Join-Path $root "src/launchpad/bin/Release/net10.0-windows10.0.19041.0/win-x64"
Copy-Item "$binOutput\*.xbf" $output -Force
Copy-Item "$binOutput\launchpad.pri" $output -Force
foreach ($sub in @("Views", "Themes")) {
    if (Test-Path (Join-Path $binOutput $sub)) {
        Copy-Item (Join-Path $binOutput $sub) $output -Recurse -Force
    }
}
Write-Host "Copied XAML compile outputs (xbf/pri) from Release bin"

$configDest = Join-Path $output "config"
New-Item -ItemType Directory -Force -Path $configDest | Out-Null
$example = Join-Path $root "config/config.example.json"
if (-not (Test-Path $example)) {
    $example = Join-Path $root "..\config\config.example.json"
}
if (Test-Path $example) {
    $target = Join-Path $configDest "config.json"
    if (-not (Test-Path $target)) {
        Copy-Item $example $target
        Write-Host "Copied config.example.json -> $target"
    }
    else {
        Write-Host "config/config.json already exists; left untouched"
    }
}

Write-Host "Done. Run: $output\launchpad.exe"
