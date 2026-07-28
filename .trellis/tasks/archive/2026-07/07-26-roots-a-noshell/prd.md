# 子任务A：LaunchPlan + platform 非 shell 执行根除注入

## Goal

根除命令注入源头：把"字符串拼接 + shell 执行"反模式改为"LaunchPlan argv 结构 + 直接 exec"。本子任务专注注入根除，不涉及配置/线程/RAII（子任务 B）与跨平台补齐（子任务 C）。

## Requirements

- 新增 `core/launch_plan.h`：`struct LaunchPlan { path executable; vector<string> args; path working_dir; optional<string> terminal_override; bool is_dangerous; }`
- 新增 `core::LaunchPlanBuilder`：从 `LaunchItem` 构造 `LaunchPlan`（纯逻辑，零平台依赖）
- `platform/terminal_launcher.h` 接口改为 `launch(const LaunchPlan&) -> expected<ProcessHandle, Error>`，并拆分 `populate(plan)`（构造平台 argv）与 `launch(plan)`（直接 exec）
- Windows：`CreateProcessW` 用 `lpApplicationName` + `lpCommandLine` 分离，argv 按 MS argc 规则序列化
- macOS：`posix_spawn` 启动 `/usr/bin/osascript`，AppleScript 作为参数
- Linux：`posix_spawnp` + `access(X_OK)` 探测终端，`posix_spawn_file_actions_addchdir_np` 设工作目录
- 删除 `core::Launcher::build_command`（硬编码 wt+pwsh 拼接）
- 删除 `platform/terminal_command.{h,cpp}`（字符串拼接函数）
- `app.cpp:200` 的 `terminal.value()+" "+command` 改为构造 LaunchPlan
- `quote_arg` 保留但不再用于主路径（向后兼容）
- `tests/platform/CMakeLists.txt` 移除 `if(WIN32)` 限制

## Acceptance Criteria

- [ ] 全项目无 `std::system` 调用
- [ ] 全项目无命令字符串拼接（`+` 拼接 `quote_arg` 结果的形式消失）
- [ ] `core::Launcher::build_command` 已删除
- [ ] `platform/terminal_command.{h,cpp}` 已删除
- [ ] `app.cpp` 不再拼接 terminal+command
- [ ] `cmake --build build/debug` 成功
- [ ] `ctest --test-dir build/debug` 全部通过
- [ ] macOS/Linux 编译可行（实机回归移交子任务 C）

## Notes

- 技术设计见 `design.md`
- 执行计划见 `implement.md`
- 本子任务不涉及 Glaze alias / 后台线程 / RAII（子任务 B）
- 本子任务不涉及 SingleInstance / ThemeDetector / spec 重建（子任务 C）
- mac/linux 实机不可测（本机 Windows），仅保证编译可行性
