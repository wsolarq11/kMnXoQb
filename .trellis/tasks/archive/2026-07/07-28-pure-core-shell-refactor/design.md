# Design: Pure Core + Imperative Shell

## 1. Target Architecture

```
┌─────────────────────────────────────────────────────────┐
│  Shell Layer (src/shell/)                                │
│                                                          │
│  App (lifecycle, wiring, ~50 lines)                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────────────────┐  │
│  │ ConfigIO │  │ Launcher │  │ DialogProvider       │  │
│  │(real FS) │  │(real FS) │  │(platform impl)       │  │
│  └──────────┘  └──────────┘  └──────────────────────┘  │
│                                                          │
│  Owns all I/O, injected into core functions as params    │
├─────────────────────────────────────────────────────────┤
│  Core Layer (src/core/) — PURE, NO I/O, NO GLOBALS      │
│                                                          │
│  LaunchPlanBuilder  quote_arg       is_dangerous        │
│  SelectedStore      split_by_ws     validate_rules      │
│  deduplicate_id     filter_items    merge_items         │
│                                                          │
│  All functions: (input...) → output, no side effects     │
├─────────────────────────────────────────────────────────┤
│  Platform Layer (src/platform/) — I/O IMPLEMENTATIONS    │
│                                                          │
│  TerminalLauncher   PathResolver     ThemeDetector       │
│  SingleInstance     DialogProvider                       │
│                                                          │
│  All abstract interfaces + platform-specific impls       │
└─────────────────────────────────────────────────────────┘
```

Dependency direction: `core_lib` ← `platform_lib` ← `shell` (and GUI links to all three)

## 2. Interface Contracts

### 2.1 Filesystem Abstraction

```cpp
// src/core/fs_iface.h — in core_lib, defines the contract
namespace core {

class FilesystemIface {
public:
    virtual ~FilesystemIface() = default;

    virtual auto read_file(const std::filesystem::path& path)
        -> std::expected<std::string, Error> = 0;

    virtual auto write_file(const std::filesystem::path& path,
                            const std::string& content)
        -> std::expected<void, Error> = 0;

    virtual auto file_exists(const std::filesystem::path& path) const
        -> bool = 0;

    virtual auto directory_exists(const std::filesystem::path& path) const
        -> bool = 0;

    virtual auto rename(const std::filesystem::path& from,
                        const std::filesystem::path& to)
        -> std::expected<void, Error> = 0;
};

} // namespace core
```

ConfigIO 在构造时接受 `FilesystemIface&`。Launcher 在构造时（或 `validate_item` 在调用时）接受它。Shell 提供 `RealFilesystem` 实现；测试提供 `MockFilesystem`。

**权衡：** 一个集中式的 filesystem 接口与多个细粒度接口。选择集中式，因为 launcher 使用点极其有限——ConfigIO 做读写，Launcher 做存在性检查。每个调用点拆一个微接口是过度工程。

### 2.2 DialogProvider（新增）

```cpp
// src/platform/dialog_provider.h
namespace pal {

class DialogProvider {
public:
    virtual ~DialogProvider() = default;

    /// Opens a native directory-picker dialog.
    /// Returns the selected path, or nullopt if cancelled.
    virtual auto browse_directory() -> std::optional<std::filesystem::path> = 0;

    static auto create() -> std::unique_ptr<DialogProvider>;
};

} // namespace pal
```

三个平台实现：`WinDialogProvider`（COM `IFileDialog`）、`MacDialogProvider`（osascript via reproc）、`LinuxDialogProvider`（zenity/kdialog via reproc）。从 `app.cpp` 原样移出 `on_dialog_browse` 代码块。

### 2.3 Logger 迁移

`logger.h` / `logger.cpp` 从 `src/core/` 移动到 `src/shell/`。宏（`CORE_LOG_*`）重命名为 `APP_LOG_*`。core_lib 不调用任何日志宏——函数通过 `std::expected<T, Error>` 返回值向上传播失败信息。

## 3. 提取的纯函数

### 3.1 `deduplicate_id`

```cpp
// src/core/deduplicate_id.h
namespace core {

/// Given a desired ID and the existing item list, returns a unique ID.
/// Appends " (2)", " (3)", etc. if collisions exist.
auto deduplicate_id(std::string_view desired_id,
                    std::span<const LaunchItem> existing)
    -> std::string;

} // namespace core
```

提取自 `app.cpp` ~line 120-130。

### 3.2 `filter_items`

```cpp
// src/core/filter_items.h
namespace core {

/// Returns items whose name contains query (case-insensitive).
/// Returns all items if query is empty.
auto filter_items(std::span<const LaunchItem> items,
                  std::string_view query)
    -> std::vector<LaunchItem>;

} // namespace core
```

提取自 `app.cpp` `on_search_changed` 回调。

### 3.3 `split_by_whitespace`

```cpp
// src/core/split_whitespace.h
namespace core {

/// Splits a string by whitespace, respecting double-quoted regions.
/// Compatible with Windows CommandLineToArgvW semantics.
auto split_by_whitespace(std::string_view input)
    -> std::vector<std::string>;

} // namespace core
```

从 `terminal_launcher_win.cpp` 提取。移除测试文件中的副本。WinTerminalLauncher 从 core_lib 调用它（此方向已存在——platform_lib 已依赖 core_lib）。

### 3.4 `validate_rules`

```cpp
// src/core/validate_rules.h
namespace core {

/// Pure validation rules that don't need filesystem access.
/// Returns Error if the item violates any rule (empty command, etc).
auto validate_rules(const LaunchItem& item) -> std::expected<void, Error>;

} // namespace core
```

与基于文件系统的 `validate_item` 拆分——`validate_rules` 是纯函数，`validate_item` 变为 `validate_rules(item).and_then([&] { return fs->directory_exists(item.directory) ? ... })`。

## 4. 依赖注入模式

Shell 的 `App` 构造函数变为：

```cpp
App(std::unique_ptr<pal::TerminalLauncher> terminal,
    std::unique_ptr<pal::PathResolver> resolver,
    std::unique_ptr<pal::SingleInstance> single_instance,
    std::unique_ptr<pal::ThemeDetector> theme_detector,
    std::unique_ptr<pal::DialogProvider> dialog_provider,
    std::unique_ptr<core::FilesystemIface> fs);
```

工厂方法留在 `platform_lib` 中，但仅在 `main.cpp` 中调用以构造 App。App 本身对具体平台类零感知。

## 5. 文件布局（目标）

```
src/
  core/
    error.h              (不变)
    launch_item.h        (不变)
    launch_plan.h        (不变)
    launch_plan_builder.h/.cpp  (不变)
    quote_arg.h/.cpp     (不变)
    is_dangerous.h/.cpp  (不变)
    selected_store.h/.cpp (不变)
    launcher.h/.cpp      (修改：fs 注入，移除 filesystem::exists)
    config.h/.cpp        (修改：fs 注入，移除直接 I/O)
    fs_iface.h           (新增)
    deduplicate_id.h/.cpp (新增)
    filter_items.h/.cpp  (新增)
    split_whitespace.h/.cpp (新增)
    validate_rules.h/.cpp (新增)

  platform/
    terminal_launcher.h  (不变)
    terminal_launcher_win.cpp    (修改：使用 core::split_by_whitespace)
    terminal_launcher_macos.cpp  (不变)
    terminal_launcher_linux.cpp  (不变)
    terminal_launcher_factory.cpp (不变)
    path_resolver.h/.cpp  (不变)
    theme_detector.h/.cpp  (不变)
    single_instance.h/.cpp (不变)
    dialog_provider.h      (新增)
    dialog_provider_win.cpp    (新增，从 app.cpp 移出)
    dialog_provider_macos.cpp  (新增，从 app.cpp 移出)
    dialog_provider_linux.cpp  (新增，从 app.cpp 移出)
    dialog_provider_factory.cpp (新增)

  shell/
    app.h/.cpp           (重写：薄胶水层，~50 行)
    logger.h             (从 core/ 移出)
    real_filesystem.h/.cpp (新增)

  main.cpp               (修改：构造依赖并注入)

tests/
  core/
    (现有测试 + 新增：deduplicate_id, filter_items, split_whitespace, validate_rules)
  platform/
    (现有测试 + 新增：DialogProvider mock 测试)
  shell/
    (新增：使用 mock 依赖的集成测试)
```

## 6. CMake 变更

```
core_lib (STATIC)    — 移除 spdlog 依赖，移除 std::filesystem 副作用使用
platform_lib (STATIC) — 添加 dialog_provider_*.cpp
shell_lib (STATIC)   — 新增：app, logger, real_filesystem, real_* 实现
wt-launcher (EXEC)   — 链接 shell_lib + core_lib + platform_lib + Slint
```

## 7. 兼容性与回滚

- 每个 git commit 必须是原子性的，所有测试通过——不允许"中间"状态
- 重构顺序：自底向上（先 core，再 platform，最后 shell）
- 回滚：任何提交都可以被独立 revert，因为行为不应改变
- `LaunchPlan` argv 签名的逐字节等效性通过运行完整的 `tools/verify.ps1 --strict` 和比较 `--dry-run` 输出进行验证，覆盖所有现有配置项

## 8. 选择不做的权衡

| 不做 | 原因 |
|--------|--------|
| 将 Slint UI 替换为 CLI | 在本次重构范围之外；纯核心在 GUI 和 CLI 中均可用，所以 CLI 可以稍后添加 |
| 使用概念/模板替代虚接口 | 会增加构建时耦合（使用方的模板实例化）。虚函数在此规模下（最多 3-4 个实现）是最简单的 |
| 将 `spdlog` 替换为其他 logger | 成本过高，没有收益；只需将其移出 core |
| 将 `glaze` 替换为自定义 JSON | glaze 只是编译时反射——无运行时副作用 |
| 为每个 I/O 函数拆微接口 | 对如此小的代码库来说是过度工程 |
