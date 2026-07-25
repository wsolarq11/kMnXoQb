# WT Launcher build helper
# Usage: pwsh tools/build.ps1 [debug|release]
# Ensures MSVC environment is initialized before building.

param(
    [ValidateSet('debug', 'release')]
    [string]$Config = 'debug'
)

$RootDir = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$BuildDir = Join-Path $RootDir 'build' $Config
$VcVars = 'C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvars64.bat'

# Step 1: Ensure MSVC environment
if (-not (Test-Path $VcVars)) {
    Write-Error "VC vars not found at: $VcVars"
    Write-Error 'Install Visual Studio Build Tools or update the path in tools/build.ps1'
    exit 1
}

Write-Host "[build] Initializing MSVC x64 environment..." -ForegroundColor Cyan
$env:INCLUDE = ''
$env:LIB = ''
$env:LIBPATH = ''
& cmd /c "`"$VcVars`" 2>nul && set INCLUDE && set LIB && set LIBPATH" | ForEach-Object {
    if ($_ -match '^INCLUDE=(.*)') { $env:INCLUDE = $Matches[1] }
    if ($_ -match '^LIB=(.*)') { $env:LIB = $Matches[1] }
    if ($_ -match '^LIBPATH=(.*)') { $env:LIBPATH = $Matches[1] }
}

# Step 2: Configure (only if CMake cache missing)
if (-not (Test-Path (Join-Path $BuildDir 'CMakeCache.txt'))) {
    Write-Host "[build] Configuring CMake ($Config)..." -ForegroundColor Cyan
    cmake -S $RootDir -B $BuildDir -G Ninja `
        -DCMAKE_BUILD_TYPE=$Config `
        -DCMAKE_TOOLCHAIN_FILE='C:/tools/vcpkg/scripts/buildsystems/vcpkg.cmake' `
        -DCMAKE_PREFIX_PATH='C:/tools/slint' `
        -DCMAKE_CXX_STANDARD=23 `
        -DCMAKE_CXX_STANDARD_REQUIRED=ON
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# Step 3: Build
Write-Host "[build] Building ($Config)..." -ForegroundColor Cyan
cmake --build $BuildDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[build] Done." -ForegroundColor Green
