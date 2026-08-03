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

## 项目约定（双栈提交与状态滞后检测）

1. **提交前缀约定**：所有提交信息第一行必须以 `[tauri]` 或 `[winui]` 开头标明所属栈（双栈并行动作），由 pre-commit 的 commit-msg hook（`.pre-commit-config.yaml` 中 `commit-msg-stack-prefix`）机器强制，无前缀的提交被拒绝。
2. **SessionStart 滞后检测**：会话开始时对比 CLAUDE.md「验证状态矩阵」的更新日期与 `git log -1 --format=%ad --date=short` 的最近提交日期；若矩阵滞后于最近提交，主动更新矩阵（含滞后工作项状态与证据）后再开始任务，并提示用户。
