# launchpad-rs — Project Guide

## Overview

Cross-platform terminal launcher for AI coding agents. Rust desktop app with egui immediate-mode GUI. Manages launch items and spawns terminals with user-defined commands via zero-shell `std::process::Command`.

## Architecture

```
launchpad-rs/
  src/types.rs       Data model + JSON Schema (schemars + serde)
  src/config.rs      ConfigIO — read/write config.json + settings.json
  src/launch.rs      Zero-shell process spawn + is_dangerous detection
  src/app.rs         egui UI — immediate mode, all state + rendering
  src/main.rs        CLI dispatch (clap) + GUI entry (eframe)
  tests/             Integration tests (10 unit + 2 proptest)
```

- **Immediate mode**: UI code IS state management. No DSL, no callbacks, no generated code.
- **Single source of truth**: `LaunchItem` struct → JSON Schema → GUI form → CLI parser (all auto-derived).
- **Zero-shell exec**: `std::process::Command` with argv array, same security property as reproc.

## Quick Reference

```bash
cd launchpad-rs
cargo build --release          # 4.0 MB binary
cargo test                     # 12 tests
cargo clippy --all-targets -- -D warnings
cargo run -- list ../config/config.json          # CLI table
cargo run -- launch --dry-run ../config/config.json <id>  # preview
```

## Key Dependencies

| Dep | Purpose |
|-----|---------|
| egui / eframe | Immediate-mode GUI, cross-platform |
| serde / serde_json | JSON serialization |
| clap | CLI argument parsing |
| schemars | JSON Schema generation |
| anyhow | Error handling |
| dark-light | System theme detection |
| proptest | Property-based testing |

## Testing

```bash
cargo test                     # 12 tests: 10 unit + 2 proptest
cargo test config_round_trip   # single test
cargo test proptests::         # property tests only
```

## CI

`.github/workflows/ci.yml` — single job, 3-OS matrix (ubuntu/windows/macos):
- `cargo fmt --check`
- `cargo clippy -- -D warnings`
- `cargo test --workspace`
- `cargo build --release`

## Security Rules

- **ZERO shell execution**: `std::process::Command` with argv array, never shell string
- **NO** `popen()`, `system()`, shell string concatenation
- `is_dangerous()` validates commands before launch

## Cross-Platform

- `cfg(target_os = "windows")` / `cfg(target_os = "macos")` / Linux fallback
- Terminal detection: wt.exe > pwsh.exe > cmd.exe (Windows), osascript (macOS), gnome-terminal etc. (Linux)
- Single-instance lock via config dir `.lock` file

## Maintenance

This file must evolve with the project. Update after:
- New or removed modules
- Changed dependencies
- Architectural changes
- New CLI commands
- Changed CI jobs
