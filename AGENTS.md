<!-- TRELLIS:START -->
# Trellis Instructions

These instructions are for AI assistants working in this project.

This project is managed by Trellis. The working knowledge you need lives under `.trellis/`:

- `.trellis/workflow.md` — development phases, when to create tasks, skill routing
- `.trellis/spec/` — package- and layer-scoped coding guidelines (read before writing code in a given layer)
- `.trellis/workspace/` — per-developer journals and session traces
- `.trellis/tasks/` — active and archived tasks (PRDs, research, jsonl context)

If a Trellis command is available on your platform (e.g. `/trellis:finish-work`, `/trellis:continue`), prefer it over manual steps. Not every platform exposes every command.

If you're using Codex or another agent-capable tool, additional project-scoped helpers may live in:
- `.agents/skills/` — reusable Trellis skills
- `.codex/agents/` — optional custom subagents

Managed by Trellis. Edits outside this block are preserved; edits inside may be overwritten by a future `trellis update`.

<!-- TRELLIS:END -->

## Quick Reference

- **Build**: `cd launchpad-rs && cargo build --release` (4.0 MB binary)
- **Test**: `cd launchpad-rs && cargo test` (12 tests)
- **Lint**: `cd launchpad-rs && cargo clippy --all-targets -- -D warnings`
- **Safety**: NO shell exec — `std::process::Command` with argv array
- **GUI**: egui immediate mode — no DSL, no callbacks
- **CLI**: clap derive — `launchpad-rs {check,list,launch}`
- **Config**: JSON Schema via schemars — single source of truth
- **CI**: `.github/workflows/ci.yml` — 3-OS matrix
