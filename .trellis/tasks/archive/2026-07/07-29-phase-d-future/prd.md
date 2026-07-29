# Phase D: Future Enhancements

## Goal

Extend the launcher beyond desktop: Web (WASM), TUI (terminal UI), and multi-client IPC mode.

## Requirements

### Web Version (WASM via Trunk)
- [ ] `trunk build` produces a static web app from the same Rust codebase
- [ ] egui renders in browser via WebGL/Wgpu backend
- [ ] Config file loaded via file upload or localStorage
- [ ] Launch items displayed and filterable in browser

### TUI Mode (ratatui)
- [ ] `launchpad-rs tui <config>` launches a terminal-based UI
- [ ] Full keyboard navigation (ratatui built-in)
- [ ] Same config format as GUI (code reuse via shared `types` + `config` modules)
- [ ] Launch via same `launch` module

### IPC Mode
- [ ] Daemon process manages item state and launches
- [ ] CLI client communicates with daemon via Unix domain socket / named pipe
- [ ] GUI client connects to same daemon
- [ ] Multiple clients share single config state

## Acceptance Criteria

1. `trunk serve` opens launcher UI in browser
2. `launchpad-rs tui config.json` shows terminal UI with same items
3. `launchpad-rs daemon` starts background process; `launchpad-rs client list` shows items

## Notes

- Modules to share across all UIs: `types`, `config`, `launch`, `dangerous` detection
- Each UI mode is a separate binary target in Cargo.toml (or feature-gated)
- TUI and IPC modes are additive — they don't require changing existing GUI code
