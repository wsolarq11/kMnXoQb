# 将 core_lib 重构为纯函数式核心, shell 层为命令式外壳

## Goal

重构架构，使 `core_lib` 成为零副作用、无全局状态的纯函数式核心；所有 I/O（文件系统、进程启动、注册表、系统 API）集中在命令式 shell 层。`core_lib` 中的任何函数对相同输入必须产生相同输出，不依赖任何外部状态。

## Requirements

### R1: core_lib 零副作用

`core_lib`（`src/core/`）不得包含以下任何内容：
- 文件系统 I/O（`std::ifstream`、`std::ofstream`、`std::filesystem::exists` 等）
- 全局/静态可变状态（包括 spdlog 全局 logger 注册）
- 进程创建操作
- 注册表或系统 API 调用
- 对 spdlog 的依赖（`#include <spdlog/...>`）

所有 I/O 操作必须由一个显式注入的接口处理，该接口在 shell 层实现。

### R2: 从 app.cpp 中提取纯业务逻辑

当前嵌入在 `app.cpp` UI 回调中的纯决策逻辑必须提取到 `core_lib` 中的独立函数：
- ID 去重逻辑（`on_dialog_save` 中的 `id = name; while (...) { id = name (n) }`）
- 搜索/筛选逻辑（`on_search_changed`）
- 启动项验证规则（当前在 `Launcher::validate_item` 和 `App::launch_item` 之间分散）

### R3: 平台依赖注入

所有平台服务（`TerminalLauncher`、`PathResolver`、`SingleInstance`、`ThemeDetector`）必须通过构造函数注入 shell 层。消除所有 `create()` 工厂方法在 shell/glue 层和 platform 层中的直接调用，以及 `get_theme_detector()` 函数内 static 单例。

### R4: 消除代码重复

- `split_by_whitespace` — 当前在 `terminal_launcher_win.cpp` 和 `rapidcheck_test.cpp` 中均存在 — 必须提取到 `core_lib` 中的单个公共函数。
- `join_args_for_createprocess` 中的反斜杠加倍逻辑必须在可行的情况下与 `core::quote_arg` 共享实现。

### R5: 平台特定 UI 代码提取

`app.cpp` 中 `on_dialog_browse()` 方法的 `#ifdef` 块必须提取到 `platform_lib` 中的一个新接口（例如 `DialogProvider`），每个平台有独立实现。

### R6: filesystem 抽象

`core::ConfigIO` 必须能够使用注入的 filesystem 适配器（接口 + 真实实现 + mock 实现）。`core::Launcher::validate_item` 不得直接调用 `std::filesystem::exists`。

### R7: logging 不在 core 中

`core_lib` 代码不得调用 `CORE_LOG_*` 宏。取而代之的是，函数通过返回值进行通信（`std::expected<T, Error>`），日志记录是 shell 层的关切。`logger.h/cpp` 从 `core_lib` 移动到 shell 层或平台层。

## Acceptance Criteria

- [ ] `src/core/` 中没有任何文件 `#include <spdlog/...>` 或调用 `CORE_LOG_*`
- [ ] `src/core/` 中没有任何文件调用 `std::filesystem::exists` 或直接执行文件 I/O
- [ ] `src/core/` 中或通过 `CORE_LOG_*` 宏（在移动之后）均无全局/静态可变状态
- [ ] 所有当前嵌入在 `app.cpp` 中的纯决策函数均作为 `core_lib` 中的独立函数存在，具备单元测试
- [ ] 所有平台服务通过构造函数注入 shell 层；零函数内 static 单例
- [ ] `split_by_whitespace` 在代码库中恰好存在一次，位于 `core_lib` 中
- [ ] `on_dialog_browse` 平台代码位于 `platform_lib` 中 `DialogProvider` 接口之后
- [ ] `ConfigIO` 接受一个显式的 filesystem 适配器（接口），具备 mock 和真实实现
- [ ] `core_lib` 仅链接 `glaze`（无 spdlog，无 `std::filesystem` 用于副作用）
- [ ] 所有现有 23 个测试通过（回归）
- [ ] 新增纯函数的单元测试达到与现有测试相同的覆盖率标准
- [ ] `app.cpp` 缩减到 60 行以下（纯胶水代码/生命周期管理，零业务逻辑）

## Constraints

- 零行为变更：`LaunchPlan` 的 argv 输出必须与重构前逐字节相同
- 所有平台（Windows、macOS、Linux）必须继续编译且功能相同
- 不引入新的外部依赖（用于接口进行抽象的原生 C++ 虚基类是可以的）
- 不改变 `LaunchPlanBuilder::build` 或 `TerminalLauncher::populate/launch` 的 public API

## Notes

- 这是一个架构重构——完全没有新功能。所有现有行为必须保留。
- `LaunchItem`、`LaunchPlan`、`Error`、`AppSettings`、`WindowState` 数据类已经是纯数据结构体；无需更改。
- `LaunchPlanBuilder::build`、`quote_arg`、`is_dangerous` 和 `SelectedStore` 已经是纯函数，保持不变。
- 主要工作量是：提取、接口化、重组——而非重写。
