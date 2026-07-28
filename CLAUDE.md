# WT Launcher — Project Guide

## Overview

Cross-platform terminal launcher for AI coding agents. C++23 desktop app with Slint UI framework. Manages launch items and spawns terminals with user-defined commands.

## Architecture

```
src/core/       core_lib (STATIC)     — Config, Launcher, LaunchPlanBuilder, is_dangerous
src/platform/   platform_lib (STATIC)  — PathResolver, TerminalLauncher, SingleInstance, ThemeDetector
src/app.cpp     wt-launcher (EXEC)    — Slint UI integration
tests/core/     core_tests            — doctest unit tests
tests/platform/ platform_tests        — doctest + Trompeloeil mock tests
```

- Dependency direction: `core_lib` → `platform_lib` → `wt-launcher`
- All platform abstractions: virtual base class + `static auto create() -> std::unique_ptr<T>` factory

## Toolchain

- **Compiler**: GCC 16.1.0 (MinGW via MSYS2 UCRT64)
- **Build**: CMake 3.29+ + Ninja / MinGW Makefiles
- **Packages**: CPM.cmake (all deps from source, no vcpkg)
- **Rust**: Required for Slint compilation

## Key Deps (CPM.cmake)

| Dep | Version | Purpose |
|-----|---------|---------|
| reproc | 14.2.5 | Cross-platform process spawning (argv, no shell) |
| glaze | 4.4.3 | JSON serialization |
| fmt | 11.1.4 | Formatting |
| spdlog | 1.15.3 | Logging |
| trompeloeil | v47 | Mock framework (header-only) |
| doctest | 2.4.11 | Test framework (header-only) |
| Slint | release/1 | UI framework (FetchContent, Rust) |

## Build & Test

```powershell
cmake --preset debug
cmake --build build/debug --target wt-launcher          # ~7s incremental
cmake --build build/debug --target core_tests            # unit tests
cmake --build build/debug --target platform_tests        # mock tests
ctest --test-dir build/debug --output-on-failure          # 23 tests
```

## Security Rules (CRITICAL)

- **ZERO shell execution**: All subprocesses via `reproc::process::start({argv...})`
- **NO** `::popen()`, `::system()`, shell string concatenation
- Blocked by `.clang-tidy` + pre-commit hook `forbid-popen`
- `core::is_dangerous()` validates commands before launch

## Cross-Platform Patterns

- Platform differences in `.cpp` files only via `#ifdef _WIN32` / `__APPLE__` / `__linux__`
- Headers expose abstract interfaces, never `#ifdef`
- All three platform layers (PathResolver, SingleInstance, ThemeDetector) use same virtual base + factory pattern

## C++23 Features Used

- `std::expected<T, Error>` — error handling
- `std::jthread` + `std::stop_token` — RAII threads, cooperative cancellation
- `std::filesystem` — path operations
- `std::optional` — nullable fields

## CI

GitHub Actions workflow at `.github/workflows/ci.yml`:
- Matrix: ubuntu-latest + windows-latest
- Caches: Cargo registry + CPM source cache
- Tests: core_tests + platform_tests
- Static analysis: clang-tidy (Linux)
