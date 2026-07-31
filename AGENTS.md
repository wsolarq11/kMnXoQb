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

- **Build**: `cd launchpad && dotnet build src/launchpad/launchpad.csproj`
- **Test**: `cd launchpad && dotnet test tests/launchpad.Core.Tests/` (60 tests)
- **Run**: `cd launchpad && dotnet run --project src/launchpad`
- **Architecture**: hexagonal — UI → UseCases → Core ← Infrastructure (see CLAUDE.md)
- **Safety**: NO shell exec — `Process.ArgumentList` argv array
- **Config**: config/config.json + config/settings.json (snake_case, legacy-compatible)
- **CI**: `.github/workflows/ci.yml` — Windows + dotnet build/test
