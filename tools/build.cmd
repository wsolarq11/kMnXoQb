@echo off
call "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvars64.bat" >nul
set PATH=%USERPROFILE%\.cargo\bin;%PATH%
if not exist "build\debug\CMakeCache.txt" (
    echo Configuring CMake (debug^)...
    cmake --preset debug -DRust_CARGO_TARGET=x86_64-pc-windows-msvc -DRust_RUSTUP_INSTALL_MISSING_TARGET=ON
    if errorlevel 1 exit /b 1
)
echo Building...
cmake --build build/debug
if errorlevel 1 exit /b 1
echo Done.
