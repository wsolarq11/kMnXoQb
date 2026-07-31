# 实施计划：按路线图全图实施

执行顺序：A（文档）→ B（删除）→ C（依赖升级）→ D（settings 策略）→ E（CI）→ F（磁盘）→ G（实战验证）→ H（收尾）。
每 Phase 独立提交，单点可回滚。

## Phase A: 文档与配置修正

| # | 动作 | 文件 | 验证 |
|---|---|---|---|
| A1 | Quick Reference 更新为 dotnet 命令 | AGENTS.md:23-32 | Read 确认无 cargo/egui 残留 |
| A2 | 整篇重写为 WinUI 3 架构说明 | _README.md | Read 确认无 CMake/Slint 残留 |
| A3 | "58 个测试" → 60 | CLAUDE.md:39 | grep 58 无残留 |
| A4 | WindowsAppSDK 版本声明更新 | CLAUDE.md:47、.trellis/spec/winui3-csharp/index.md:15 | 与 C 阶段升级后版本一致 |
| A5 | 删 forbid-popen 钩子；mixed-line-ending fix=lf→crlf | .pre-commit-config.yaml | `pre-commit validate-config`（如有）或 YAML 检查 |
| A6 | CMake 工具链引用更新为 dotnet | .trellis/spec/guides/incremental-refactor-workflow.md:25,65 | grep CMakePresets 无残留 |

提交点 A：`chore: fix outdated docs and tooling configs (audit findings A1-A6)`

## Phase B: 文件删除

| # | 动作 | 验证 |
|---|---|---|
| B1 | git rm CMakePresets.json | git status 确认删除 |
| B2 | git rm .clang-format | 同上 |
| B3 | git rm "WT Launcher.hta" | 同上 |
| B4 | git rm .trellis/trash/terminal_command.{cpp,h} _test.cpp | 同上（trash 其他文件保留） |

提交点 B：`chore: remove retired C++/HTA era artifacts (audit findings B1-B4)`

## Phase C: 依赖升级（每步独立验证）

| # | 动作 | 验证 |
|---|---|---|
| C1 | SDK.BuildTools 26100.4654→28000.2526 | dotnet build src/launchpad |
| C2 | Test.Sdk 17.14.1→18.8.1 + runner 3.1.0→3.1.5 | dotnet test（60 全绿） |
| C3 | WindowsAppSDK 2.2.0→2.3.1 | dotnet build + dotnet test + 实战启动（并入 G3） |

验证命令：`cd launchpad && dotnet build src/launchpad/launchpad.csproj --nologo && dotnet test tests/launchpad.Core.Tests/ --nologo`
回滚点：任一失败 → `git checkout -- <csproj>` 回退该包版本，报告。

提交点 C：`chore: upgrade dependencies to latest stable (WindowsAppSDK 2.3.1, Test.Sdk 18, BuildTools 28000)`

## Phase D: settings.json 跟踪策略

| # | 动作 | 验证 |
|---|---|---|
| D1 | 读 ConfigStore.cs 确认 settings.json 缺失时的行为（自动创建 or 抛错） | Read |
| D2 | git rm --cached config/settings.json + .gitignore 加 `config/settings.json` | git status 无 settings.json；ls 文件仍在磁盘 |
| D3 | 若 D1 为抛错：文档注明需复制 example 为 settings.json（更新 README 快速开始） | Read README |

提交点 D：`chore: stop tracking runtime state settings.json (audit finding D)`

## Phase E: CI 重写

| # | 动作 | 验证 |
|---|---|---|
| E1 | ci.yml 重写：windows-latest + working-directory launchpad + dotnet build --release + dotnet test | 本机逐条执行等价命令；YAML 语法检查 |

提交点 E：`fix: rewrite CI for WinUI 3/.NET (audit finding A1)`

## Phase F: 磁盘清理（不提交，gitignore 项）

| # | 动作 | 验证 |
|---|---|---|
| F1 | 删除 build/debug/、build/release/（如有） | ls 确认 |
| F2 | 删除 wt-last-err.txt、wt-last-out.txt | 同上 |
| F3 | 删除 .cargo-cache/、.cpm-cache/（如存在） | 同上 |

## Phase G: 实战实测验真质检

| # | 动作 | 验证 |
|---|---|---|
| G1 | dotnet build --release 全绿 | 构建输出 |
| G2 | dotnet test 60 全绿 | 测试输出 |
| G3 | 实战运行：启动 exe，进程存活 ≥5s、无 crash log、settings.json 读写正常、第二实例退出 | PowerShell 进程检查 + %TEMP% 日志检查 |
| G4 | verification agent 全量复核（PASS/FAIL/PARTIAL） | agent 报告 |

## Phase H: 收尾与后续 phase

| # | 动作 |
|---|---|
| H1 | CLAUDE.md 历史部分追加本次实施记录 |
| H2 | 全部提交 + `git log` 确认 |
| H3 | task.py archive 归档本任务 |
| H4 | 产出后续 phase 提案（阶段 2 核心下沉 Rust + P/Invoke：范围、风险、决策点）写入 journal 或提案文档 |

## 审查门

- G2 前所有 Phase 完成，git status 干净（除预期未跟踪项）。
- G4 verification PASS 后进入 H；PARTIAL 则补齐再验；FAIL 则修复重跑。
