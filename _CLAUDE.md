# WT Launcher — Project Guide

## Overview

Cross-platform terminal launcher for AI coding agents. C++23 desktop app with Slint UI framework. Manages launch items and spawns terminals with user-defined commands.

## Architecture

```
src/core/       core_lib (STATIC, PURE)   — zero side effects, zero global state
  LaunchPlanBuilder, quote_arg, is_dangerous, SelectedStore
  deduplicate_id, filter_items, validate_rules, split_by_whitespace
  ConfigIO, Launcher, LaunchItem, LaunchPlan, Error
  FilesystemIface (abstract filesystem contract)

src/platform/   platform_lib (STATIC)     — all OS I/O implementations
  TerminalLauncher, PathResolver, SingleInstance,
  ThemeDetector, DialogProvider, ConPTYLauncher

src/shell/      shell_lib (STATIC)        — shared business layer + wiring
  launcher_app.h/.cpp (LauncherApp), app.h/.cpp (GUI shell),
  logger.h, real_filesystem.h

src/            wt-launcher (EXEC)        — CLI + GUI entry points
  main.cpp (argv dispatch), cli_check.cpp, cli_launch.cpp

tests/core/     core_tests    (99)        — doctest + rapidcheck unit tests
tests/platform/ platform_tests (4)       — doctest + Trompeloeil mock tests
tests/shell/    shell_tests   (15)       — LauncherApp integration tests
```

- Dependency direction: `core_lib` (pure) ← `platform_lib` (I/O) ← `wt-launcher` (wiring)
- **Pure Core**: `core_lib` has NO spdlog, NO `std::filesystem` I/O, NO global mutable state. All I/O goes through injected `FilesystemIface`.
- All platform abstractions: virtual base class + `static auto create() -> std::unique_ptr<T>` factory
- All dependencies injected into `App` via constructor (no singletons, no direct factory calls inside App)

## LaunchPlan — Zero-Shell Security Contract

The single most important architectural concept. LaunchPlan is the data contract between core and platform layers that eliminates shell injection by design:

1. `LaunchPlanBuilder::build(LaunchItem)` — constructs pure-data LaunchPlan (command / working_dir / terminal_override / is_dangerous)
2. `TerminalLauncher::populate(plan)` — platform layer fills executable + args (e.g., wt.exe, pwsh.exe, gnome-terminal)
3. `TerminalLauncher::launch(plan)` — posix_spawn / CreateProcessW direct exec, no shell at any point

core layer has zero platform knowledge; platform layer has zero shell strings; argv travels from source to exec without ever passing through an interpreter.

## Error Handling

All fallible functions return `std::expected<T, core::Error>`. `core::Error` provides static factories:

```cpp
Error::ConfigNotFound(path)
Error::ConfigParseError(detail)
Error::ConfigWriteError(detail)
Error::DirectoryNotFound(path)
Error::CommandEmpty()
Error::TerminalNotFound(name)
Error::LaunchFailed(detail)
Error::InvalidItem(detail)
Error::Internal(detail)
```

## Toolchain

- **Compiler**: GCC 16.1.0 (MinGW via MSYS2 UCRT64)
- **Build**: CMake 3.29+ + MinGW Makefiles (local) / Ninja (CI)
- **Packages**: CPM.cmake (all deps from source, no vcpkg)
- **Rust**: Required for Slint compilation
- **Build cache**: sccache configured in debug preset (`cargo install sccache`)

## Key Deps (CPM.cmake)

| Dep | Version | Purpose |
|-----|---------|---------|
| reproc | 14.2.5 | Cross-platform process spawning (argv, no shell) |
| glaze | 4.4.3 | JSON serialization |
| fmt | 11.1.4 | Formatting |
| spdlog | 1.15.3 | Logging |
| trompeloeil | v47 | Mock framework (header-only) |
| doctest | 2.4.11 | Test framework (header-only) |
| rapidcheck | master | Property-based testing (QuickCheck style) |
| Slint | release/1 | UI framework (FetchContent, Rust) |

## Build & Test

```powershell
# Configure & build
cmake --preset debug
cmake --build build/debug --target wt-launcher

# Build test targets
cmake --build build/debug --target core_tests
cmake --build build/debug --target platform_tests
cmake --build build/debug --target shell_tests

# Run all tests (118 total)
ctest --test-dir build/debug --output-on-failure

# Run test executables directly
./build/debug/tests/core/core_tests
./build/debug/tests/core/core_tests -tc="quote_arg*"
./build/debug/tests/shell/shell_tests
```

## CLI Entry Points

```
wt-launcher                            → Slint GUI
wt-launcher --check <config.json>      → validate all items (JSON to stdout)
wt-launcher launch <config.json> <id>  → launch item by id
```

## LauncherApp — Shared Business Layer

`shell::LauncherApp` is the single source of truth for all business operations. Both GUI (`App`) and CLI entry points delegate to it.

**Factory injection pattern for testing:**

```cpp
// Production:
auto app = LauncherApp(config, launcher, theme);
// → launch() uses TerminalLauncher::create() → WinTerminalLauncher → wt.exe

// Test:
auto app = LauncherApp(config, launcher, theme,
    []() { return create_conpty_launcher(); });
// → launch() uses ConPTYLauncher → pseudoconsole (no window, full control)
```

## ConPTY Launcher (Test)

`ConPTYLauncher` (`platform/terminal_launcher_conpty.cpp`) uses Windows Pseudoconsole API to spawn processes without a visible window. `ClosePseudoConsole` cleanly terminates all attached processes. Used for automated testing of the full `LauncherApp::launch()` path.

**Known limitation**: WindowsTerminal (wt.exe) tabs cannot be programmatically closed from outside. This is a Microsoft design constraint (GitHub #15747, closed as not planned). ConPTY + factory injection is the recommended test approach.

## Logging

Use the macros defined in `src/shell/logger.h` — never call spdlog directly:

```cpp
APP_LOG_TRACE(...)   // stripped in release builds
APP_LOG_DEBUG(...)
APP_LOG_INFO(...)
APP_LOG_WARN(...)
APP_LOG_ERROR(...)
```

Log file: `<config_dir>/launchpad.log` (rotating, 5 MB × 3 files). Console + file sinks both active.

**Note**: `core_lib` does NOT use logging. All core functions communicate errors through `std::expected<T, Error>` return values. Logging is a shell-layer concern.

## Pre-commit Hooks

```powershell
pip install pre-commit && pre-commit install

# Run on all files manually
pre-commit run --all-files
```

- `forbid-popen` hook (pygrep): blocks `::popen(`, `::system(`, `::pclose(` in C++ sources
- Whitespace / YAML / merge-conflict checks from pre-commit-hooks

## Security Rules (CRITICAL)

- **ZERO shell execution**: All subprocesses via `reproc::process::start({argv...})`
- **NO** `::popen()`, `::system()`, shell string concatenation
- Blocked by `.clang-tidy` + pre-commit hook `forbid-popen`
- `core::is_dangerous()` validates commands before launch
- See LaunchPlan section above for the architectural enforcement

## Naming Conventions

Enforced by `.clang-tidy` (`readability-identifier-naming`):

| Element | Style |
|---------|-------|
| Classes | `CamelCase` |
| Functions | `lower_case` |
| Namespaces | `lower_case` |
| Variables | `lower_case` |
| Member variables | `camelBack` |
| Global constants | `UPPER_CASE` |

## Cross-Platform Patterns

- Platform differences in `.cpp` files only via `#ifdef _WIN32` / `__APPLE__` / `__linux__`
- Headers expose abstract interfaces, never `#ifdef`
- All three platform layers (PathResolver, SingleInstance, ThemeDetector) use same virtual base + factory pattern

## Slint UI

`.slint` files under `src/ui/` are compiled to C++ via `slint_target_sources()` in CMake — no manual C++ UI code needed. Icons use Lucide SVG. Theme variables in `ui/theme.slint`.

## C++23 Features Used

- `std::expected<T, Error>` — error handling
- `std::jthread` + `std::stop_token` — RAII threads, cooperative cancellation
- `std::filesystem` — path operations
- `std::optional` — nullable fields

## Tool Scripts

| Script | Purpose |
|--------|---------|
| `tools/build.ps1` | MSVC build helper (initializes MSVC environment, then cmake --build) |
| `tools/verify.ps1` | Config validation gate: JSON parse, field completeness, id uniqueness, dangerous command detection, quote_arg rule assertions, optional directory existence check (`-CheckDirs`) |

## CI

GitHub Actions workflow at `.github/workflows/ci.yml`:
- Matrix: ubuntu-latest + windows-latest
- Caches: Cargo registry + CPM source cache
- Tests: core_tests + platform_tests + shell_tests
- Static analysis: clang-tidy (Linux)
- CodeQL: security-and-quality queries (Linux, parallel job)

## Maintenance

**This file must evolve with the project.** After implementing changes that affect any of the following, proactively review and update this file:

- New or removed source modules / libraries
- Changed dependency versions or new dependencies
- Modified build commands, presets, or generators
- New or changed tool scripts
- Architectural changes (new layers, changed data flow, new contracts)
- New error codes or error handling patterns
- Changed naming conventions or style rules
- New or removed CI jobs

## Rust / egui Architecture (2026-07)

A Rust + egui POC (`launchpad-rs/`) demonstrates an alternative architecture that eliminates 20 UX pain points identified in the C++ + Slint codebase.

### Design Principles

1. **Immediate mode GUI** (egui): UI code IS state management. No DSL, no callback binding, no generated code.
2. **Single source of truth** (schemars + serde): `LaunchItem` struct → JSON Schema → GUI form → CLI parser — all auto-derived.
3. **Zero-shell exec** (std::process::Command): argv goes directly to CreateProcessW/posix_spawn. Same security property as reproc.
4. **Single language** (Rust): No C++/Slint/CMake context switching. One `cargo build` for everything.

### Quick Reference

```bash
cd launchpad-rs
cargo build --release          # 4.0 MB binary
cargo test                     # 12 tests (10 unit + 2 proptest)
cargo run -- list ../config/config.json   # CLI table
cargo run -- launch --dry-run ../config/config.json <id>  # preview
```

### Module Map

| File | Purpose | Lines |
|------|---------|-------|
| `src/types.rs` | LaunchItem, AppSettings, WindowState — JSON Schema source | 115 |
| `src/config.rs` | serde ConfigIO (read/write config.json + settings.json) | 72 |
| `src/launch.rs` | Zero-shell process spawn + is_dangerous detection | 123 |
| `src/app.rs` | egui UI — all 20 pain points eliminated in this file | 739 |
| `src/main.rs` | clap CLI dispatch + eframe GUI entry | 287 |
| `tests/integration_test.rs` | 10 unit + 2 proptest property tests | 212 |

### Key Differences from C++ Architecture

- **No UI DSL**: egui immediate mode. Adding a field = add to struct + add one UI line.
- **No callback cascades**: State and UI coexist in the same function.
- **No CMake**: `cargo build` handles everything.
- **No Glaze manual reflection**: `#[derive(Serialize, Deserialize, JsonSchema)]`.
- **No hand-written CLI parser**: `#[derive(clap::Parser)]` with auto-generated `--help`.

### CI

`launchpad-rs/.github/workflows/ci.yml`:
- Matrix: ubuntu-latest + windows-latest + macos-latest
- Lint: clippy + rustfmt
- Tests: `cargo test --workspace`
- Release build: `cargo build --release`
