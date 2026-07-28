# 子任务B：Glaze alias + 后台线程 + ProcessHandle RAII

## Goal

根除三个正确性问题：(1) 配置字段名错位导致 confirm_enabled 持久化失效；(2) 启动操作阻塞 UI 线程；(3) Windows 进程句柄泄漏。

## Requirements

- `AppSettings` 的 Glaze meta 从 `value` 改为 `modify`，增加 `confirmEnabled`（旧驼峰 key）alias 指向同一成员，实现向后兼容
- `app.cpp::launch_item` 的启动操作移到 `std::thread`，通过 `slint::invoke_from_event_loop` 回 UI 线程更新状态
- `ProcessHandle` 改为 RAII：析构自动 `CloseHandle`（Win）/ 无操作（mac/linux，pid 由 OS 回收）；移动语义；禁用拷贝

## Acceptance Criteria

- [ ] `config/settings.json` 的 `confirmEnabled` 能正确解析到 `AppSettings.confirm_enabled`
- [ ] `launch_item` 不在 UI 线程同步等待进程启动
- [ ] `ProcessHandle` 析构自动释放句柄
- [ ] `cmake --build build/debug` 成功
- [ ] `ctest --test-dir build/debug` 全部通过

## Notes

- 子任务 A 已完成（LaunchPlan + 非 shell 执行），本任务在其基础上改正确性
- 不涉及 SingleInstance / ThemeDetector / tests 跨平台（子任务 C）
