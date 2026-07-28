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
& cmd /c "`"$VcVars`" 2>nul && set INCLUDE && set LIB && set LIBPATH && set PATH" | ForEach-Object {
    if ($_ -match '^INCLUDE=(.*)') { $env:INCLUDE = $Matches[1] }
    if ($_ -match '^LIB=(.*)') { $env:LIB = $Matches[1] }
    if ($_ -match '^LIBPATH=(.*)') { $env:LIBPATH = $Matches[1] }
    if ($_ -match '^PATH=(.*)') { $msvc_path = $Matches[1] }
}
# 合并 MSVC 环境 PATH + 用户 PATH 中不含 msys2/mingw 的条目
$clean_user_path = ($env:PATH -split ';' | Where-Object { $_ -and $_ -notmatch 'msys|mingw|ucrt64' }) -join ';'
# 确保 cargo/bin 在 PATH 中（Rust 工具链路径）
$cargo_bin = "$env:USERPROFILE\.cargo\bin"
if ($clean_user_path -notlike "*$cargo_bin*") { $clean_user_path = "$cargo_bin;$clean_user_path" }
# 确保 sccache 在 PATH 中
$sccache_dir = 'C:\tools'
if ($clean_user_path -notlike "*$sccache_dir*") { $clean_user_path = "$sccache_dir;$clean_user_path" }
$env:PATH = "$msvc_path;$clean_user_path"

# Step 3: Configure using CMakePresets (使用 sccache 构建缓存)
if (-not (Test-Path (Join-Path $BuildDir 'CMakeCache.txt'))) {
    Write-Host "[build] Configuring CMake ($Config)..." -ForegroundColor Cyan
    cmake --preset $Config
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

# Step 3: Build
Write-Host "[build] Building ($Config)..." -ForegroundColor Cyan
cmake --build $BuildDir
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "[build] Done." -ForegroundColor Green
