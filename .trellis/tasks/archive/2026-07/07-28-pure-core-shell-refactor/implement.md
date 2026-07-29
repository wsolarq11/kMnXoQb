# Implement: Pure Core + Imperative Shell

## Strategy

自底向上，每个步骤完成后 `ctest --test-dir build/debug --output-on-failure` 必须全部通过。步骤顺序是为了保证每次提交都是绿色的，不会产生"一半重构"的状态。

---

## Phase A: Core 层纯化（不涉及 shell 改动）

### A1. 提取 `split_by_whitespace` → core_lib

- [ ] 创建 `src/core/split_whitespace.h` + `src/core/split_whitespace.cpp`
- [ ] 从 `src/platform/terminal_launcher_win.cpp` 移除本地副本，改为 include 新 header
- [ ] 从 `tests/core/rapidcheck_test.cpp` 移除本地副本，改为 include 新 header
- [ ] 创建 `tests/core/split_whitespace_test.cpp`（从 rapidcheck 测试迁移 + 添加 doctest 用例）
- [ ] 运行 `core_tests` + `platform_tests`，确认全部通过

### A2. 创建 `FilesystemIface` 接口

- [ ] 创建 `src/core/fs_iface.h`（纯虚接口：read_file, write_file, file_exists, directory_exists, rename）
- [ ] 不需要任何实现类——接口是 core 唯一需要的东西

### A3. 提取纯验证规则 `validate_rules`

- [ ] 创建 `src/core/validate_rules.h` + `src/core/validate_rules.cpp`
- [ ] 实现：空命令检查、名称长度、目录格式验证（纯语法，不做 `exists()`）
- [ ] `Launcher::validate_item` 改为：调用 `validate_rules` → 然后通过注入的 fs 接口检查目录存在性
- [ ] 创建 `tests/core/validate_rules_test.cpp`
- [ ] 运行 `core_tests`，确认通过

### A4. 提取 `deduplicate_id` 和 `filter_items`

- [ ] 创建 `src/core/deduplicate_id.h` + `src/core/deduplicate_id.cpp`
- [ ] 创建 `src/core/filter_items.h` + `src/core/filter_items.cpp`
- [ ] 创建 `tests/core/deduplicate_id_test.cpp`
- [ ] 创建 `tests/core/filter_items_test.cpp`
- [ ] 运行 `core_tests`，确认通过

### A5. 从 core_lib 移除 spdlog 依赖

- [ ] 创建 `src/shell/logger.h`（移动现有 `src/core/logger.h`，重命名宏为 `APP_LOG_*`）
- [ ] 删除 `src/core/logger.h`
- [ ] 更新所有非 core 文件中的 `#include`，指向新路径
- [ ] 从 `core_lib` CMake target 中移除 spdlog 链接
- [ ] 运行完整构建 + 测试套件

---

## Phase B: ConfigIO 和 Launcher 重构

### B1. ConfigIO 接受 FilesystemIface

- [ ] 修改 `ConfigIO` 构造函数：`ConfigIO(fs::path config_dir, FilesystemIface& fs)`
- [ ] 将 `read_items` / `write_items` / `read_settings` / `write_settings` 中的所有直接 I/O 替换为 `fs_` 调用
- [ ] 使用真实 filesystem 更新 `tests/core/config_test.cpp`
- [ ] 运行 `core_tests`，确认通过

### B2. Launcher 接受 FilesystemIface

- [ ] 修改 `Launcher` 构造函数：`Launcher(string config_dir, FilesystemIface& fs)`
- [ ] 用 `fs_.directory_exists()` 替换 `validate_item` 中的 `filesystem::exists`
- [ ] 更新 `tests/core/launcher_test.cpp`
- [ ] 运行 `core_tests`，确认通过

---

## Phase C: 平台层提取

### C1. 创建 DialogProvider 接口和实现

- [ ] 创建 `src/platform/dialog_provider.h`（抽象接口：`browse_directory()`）
- [ ] 创建 `src/platform/dialog_provider_win.cpp`（从 `app.cpp` 移出 COM IFileDialog 代码）
- [ ] 创建 `src/platform/dialog_provider_macos.cpp`（从 `app.cpp` 移出 osascript 代码）
- [ ] 创建 `src/platform/dialog_provider_linux.cpp`（从 `app.cpp` 移出 zenity/kdialog 代码）
- [ ] 创建 `src/platform/dialog_provider_factory.cpp`（`#ifdef` 平台分发）
- [ ] 编译验证所有三个平台路径（在单独的编译测试中）

---

## Phase D: Shell 层重构

### D1. 将 App 重写为薄胶水层

- [ ] 修改 `App` 构造函数，通过构造函数注入接受所有依赖
- [ ] 移除函数内 static 单例 `get_theme_detector()`
- [ ] 用 `core::deduplicate_id()` 替换内联 ID 去重逻辑
- [ ] 用 `core::filter_items()` 替换内联搜索筛选逻辑
- [ ] 用 `dialog_provider_->browse_directory()` 替换内联 `on_dialog_browse` 平台代码
- [ ] 确保 `app.cpp` 在 60 行以下

### D2. 更新 main.cpp

- [ ] 在 `main()` 中构造所有平台依赖
- [ ] 将它们注入到 `App` 构造函数中
- [ ] 编译 + 运行

### D3. 更新 CMakeLists.txt

- [ ] 添加 `shell_lib` STATIC target（app.cpp, logger.cpp, real_filesystem.cpp）
- [ ] 将 `wt-launcher` 目标更新为链接 `shell_lib`
- [ ] 更新所有 `#include` 路径以反映移动的文件

---

## Phase E: 验证与清理

### E1. 全量回归测试

- [ ] `ctest --test-dir build/debug --output-on-failure` — 所有现有 23 个测试通过
- [ ] 所有新增测试通过
- [ ] 完整的 Release + Debug 构建，零警告（`-Wall -Wextra -Wpedantic`）

### E2. LaunchPlan 兼容性验证

- [ ] 使用现有配置文件运行 `tools/verify.ps1`（JSON 解析、字段完整性、ID 唯一性、危险命令检测、quote_arg 规则断言）
- [ ] 手动启动项并验证终端窗口行为与重构前相同

### E3. 最终检查

- [ ] `app.cpp` < 60 行
- [ ] `src/core/` 零 `#include <spdlog/...>`
- [ ] `src/core/` 零直接 `std::filesystem` I/O 调用
- [ ] `CLAUDE.md` 已更新以反映新架构

---

## Verification Commands

```powershell
# 每个步骤之后运行：
cmake --preset debug
cmake --build build/debug --target core_tests
cmake --build build/debug --target platform_tests
ctest --test-dir build/debug --output-on-failure

# Phase E 中的完整验证：
cmake --build build/debug --target wt-launcher
ctest --test-dir build/debug --output-on-failure
./build/debug/tests/core/core_tests
./build/debug/tests/platform/platform_tests
powershell -File tools/verify.ps1
```

## Rollback Points

回滚发生在提交边界处。如果在步骤 N 之后出现问题，恢复到上一个提交并调查。任何提交都不应留下"一半完成"的状态。

Git 提交计划：
1. A1: `refactor: extract split_by_whitespace to core_lib`
2. A2-A3: `refactor: add FilesystemIface and validate_rules to core_lib`
3. A4: `refactor: extract deduplicate_id and filter_items to core_lib`
4. A5: `refactor: move logger from core_lib to shell`
5. B1: `refactor: inject FilesystemIface into ConfigIO`
6. B2: `refactor: inject FilesystemIface into Launcher`
7. C1: `refactor: extract DialogProvider from app.cpp to platform_lib`
8. D1-D3: `refactor: rewrite App as thin shell with DI`
9. E1-E3: `chore: verify and update CLAUDE.md`
