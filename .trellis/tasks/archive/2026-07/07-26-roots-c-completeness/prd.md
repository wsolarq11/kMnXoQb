# 子任务C：跨平台补齐 SingleInstance/ThemeDetector/tests/spec

## Goal

补齐跨平台完整性：mac/linux SingleInstance lockfile、system 主题检测、tests 跨平台编译、.trellis/spec 重建为 C++ layer。

## Requirements

- macOS/Linux `SingleInstance` 用 lockfile + flock 实现（崩溃 OS 自动释放）
- `app.cpp` 主题切换的 "system" 模式实现真正的系统主题检测（Win 注册表 / mac NSUserDefaults / linux gsettings）
- `tests/platform/CMakeLists.txt` 移除 `if(WIN32)` 限制，跨平台编译
- `.trellis/spec` 删除 Web/React 模板，新建 cpp-core / slint-ui / cmake-build / cross-platform / security layer

## Acceptance Criteria

- [ ] mac/linux `SingleInstance` 有 lockfile 实现
- [ ] "system" 主题模式跟随系统（非当作 light）
- [ ] `tests/platform` 跨平台编译（移除 if(WIN32)）
- [ ] `.trellis/spec` 不再有 database-guidelines / hook-guidelines 等 Web 模板
- [ ] `cmake --build build/debug` 成功
- [ ] `ctest --test-dir build/debug` 全部通过

## Notes

- 子任务 A（注入根除）与 B（配置/线程/RAII）已完成
- mac/linux 实机不可测（本机 Windows），仅保证编译可行性
