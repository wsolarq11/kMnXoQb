# Phase C: Quality Hardening

## Goal

Ensure production readiness: CI passes on all platforms, zero lint warnings, dependency audit, benchmarks.

## Requirements

- [ ] CI pipeline passes on all 3 OS (Windows/macOS/Linux) — `cargo test --workspace`
- [ ] CI lint passes — `cargo fmt --check` + `cargo clippy -- -D warnings`
- [ ] Release binary size under 5 MB (currently 4.0 MB ✓)
- [ ] `cargo bench` benchmarks for launch path and config I/O
- [ ] `cargo audit` for dependency security
- [ ] All 12 tests pass on all platforms in CI
- [ ] Cross-compilation check: `cargo check --target x86_64-unknown-linux-gnu`
- [ ] Cross-compilation check: `cargo check --target x86_64-apple-darwin`
- [ ] Update existing project's `CLAUDE.md` to document the Rust architecture

## Acceptance Criteria

1. GitHub Actions CI matrix (3 OS) all green
2. Clippy zero warnings on all targets (achieved locally ✓)
3. Binary size ≤ 5 MB release (4.0 MB ✓)
4. No known security vulnerabilities in dependencies
5. CLAUDE.md updated with Rust architecture section

## Notes

- CI cannot run locally; must be verified after pushing to GitHub
- Cross-compilation checks need appropriate targets installed via rustup
- `cargo audit` requires `cargo install cargo-audit`
