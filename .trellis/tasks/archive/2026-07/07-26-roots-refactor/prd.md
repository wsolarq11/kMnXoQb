# 根治性架构重构：剪掉注入/阻塞/泄漏/配置错位/跨平台分裂源头

## Goal

把 WT Launcher 的"字符串拼接 + shell 执行"反模式从源头上剪掉，改为"LaunchPlan argv 结构 + 直接 exec"。使命令注入、UI 阻塞、句柄泄漏、配置字段错位、跨平台分裂五类问题从结构上消失，而非修补。

## Requirements

- 引入 `core::LaunchPlan`（executable + args[] + working_dir + is_dangerous 纯数据结构），取代命令字符串
- platform 层 `TerminalLauncher::launch(LaunchPlan)` 用 `posix_spawn`/`CreateProcessW` 直接 exec，全程无 shell
- 删除 `core::Launcher::build_command`、`platform/terminal_command.*` 的字符串拼接函数
- `app.cpp` 不再拼接 `terminal.value()+" "+command`，改为构造 LaunchPlan
- `AppSettings` Glaze `meta::modify` alias 同时接受 `confirm_enabled`/`confirmEnabled`
- 启动操作移到后台线程，通过 `slint::invoke_from_event_loop` 回 UI 线程更新状态
- `ProcessHandle` RAII（析构自动释放句柄）
- mac/linux `SingleInstance` 用 lockfile 补齐
- `tests/platform` 移除 `if(WIN32)` 限制
- `.trellis/spec` 重建为 C++ layer

## Acceptance Criteria

- [ ] 全项目无 `std::system` 调用（mac/linux 启动器改 posix_spawn）
- [ ] 全项目无命令字符串拼接（`+` 拼接 `quote_arg` 结果的形式消失）
- [ ] `config/settings.json` 的 `confirmEnabled` 与 `confirm_enabled` 均能正确解析到 `AppSettings.confirm_enabled`
- [ ] macOS/Linux 启动不阻塞 UI 线程（后台线程 + invoke_from_event_loop）
- [ ] Windows 进程句柄无泄漏（RAII ProcessHandle）
- [ ] `cmake --build build/debug` 成功
- [ ] `ctest --test-dir build/debug` 全部通过
- [ ] mac/linux 有 SingleInstance 实现（lockfile）

## Notes

- 完整架构设计见 `.snow/plan/wt-launcher-roots-architecture-design.md`
- 深度审查报告见 `.snow/plan/wt-launcher-architecture-review-report.md`
- 拆分为 3 个子任务：A（安全/注入）、B（正确性/配置/线程/RAII）、C（完整性/跨平台/spec）
