# Design: LauncherApp + CLI Entry

## 1. 目标架构

```
main()
  ├─ 读到 argv → CLI 分支
  │   ├─ --check FILE  → run_check(fs, config_path)
  │   └─ launch ID     → run_launch(fs, config_path, id)
  │
  └─ 无参数 → GUI 分支
      └─ build_app(window)
          └─ App(window, launcher_app)  ← 薄 Slint 壳

LauncherApp（核心，只此一份）
  ├─ items_, settings_, selected_
  ├─ ConfigIO&, Launcher&
  ├─ add_item(), launch(), dry_run(), validate_all(), ...
  └─ 被 GUI 和 CLI 同时调用
```

## 2. LauncherApp 设计

```cpp
// src/shell/launcher_app.h

namespace shell {

class LauncherApp {
public:
    LauncherApp(std::unique_ptr<core::ConfigIO> config,
                std::unique_ptr<core::Launcher> launcher,
                std::unique_ptr<pal::ThemeDetector> theme_detector);

    // 数据访问
    auto items() -> std::vector<core::LaunchItem>& { return items_; }
    auto settings() -> core::AppSettings& { return settings_; }
    auto selected() -> core::SelectedStore& { return selected_; }

    // 持久化
    auto load_config() -> void;
    auto save_config() -> void;

    // 业务操作
    auto add_item(core::LaunchItem item) -> std::string; // returns assigned id
    auto edit_item(size_t index, core::LaunchItem item) -> bool;
    auto delete_item(size_t index) -> bool;
    auto launch(const std::string& id) -> std::expected<void, core::Error>;
    auto dry_run(const std::string& id) -> std::expected<core::LaunchPlan, core::Error>;
    auto validate_all() -> std::vector<CheckResult>;

    auto theme_detector() -> pal::ThemeDetector& { return *theme_detector_; }

private:
    std::unique_ptr<core::ConfigIO> config_;
    std::unique_ptr<core::Launcher> launcher_;
    std::unique_ptr<pal::ThemeDetector> theme_detector_;
    core::SelectedStore selected_;
    std::vector<core::LaunchItem> items_;
    core::AppSettings settings_;
};

struct CheckResult {
    std::string id;
    std::string name;
    bool valid;
    std::vector<std::string> errors;
    std::optional<core::LaunchPlan> plan;
};

} // namespace shell
```

`LauncherApp` 不依赖 Slint。不依赖 spdlog。不包含任何 UI 代码。

## 3. App 简化后

```cpp
class App : public std::enable_shared_from_this<App> {
public:
    App(slint::ComponentHandle<MainWindow> window, shell::LauncherApp& app);
    int run();

private:
    void bind_callbacks();  // 每个回调 1-3 行，只做委托
    void refresh_ui();      // 读取 LauncherApp 状态 → 更新 Slint 属性

    slint::ComponentHandle<MainWindow> window_;
    shell::LauncherApp& app_;   // 引用，不拥有
    std::shared_ptr<slint::VectorModel<LaunchCardData>> card_model_;
    int current_edit_index_ = -1;
    int pending_launch_index_ = -1;
    std::string search_query_;
};
```

每个回调示例：

```cpp
// 之前 (10+ 行):
void App::on_launch(int index) {
    if (index < 0 || ...) return;
    const auto& item = items_[index];
    if (settings_.confirm_enabled && ...) { ... return; }
    launch_item(index);
}

// 之后 (3 行):
void App::on_launch(int index) {
    if (index < 0 || index >= static_cast<int>(app_.items().size())) return;
    app_.launch(app_.items()[index].id);
}
```

## 4. CLI 入口设计

```cpp
// main.cpp

// 共用依赖构造（GUI 和 CLI 都用）
struct Dependencies {
    std::unique_ptr<core::FilesystemIface> fs;
    std::unique_ptr<pal::PathResolver> resolver;
    std::unique_ptr<pal::ThemeDetector> theme_detector;
    std::unique_ptr<pal::DialogProvider> dialog_provider;
};

auto create_dependencies() -> Dependencies;

// CLI 子入口
auto run_check(const std::string& config_path) -> int;
auto run_launch(const std::string& config_path, const std::string& id) -> int;
```

### `run_check` 流程

1. 创建 `Dependencies`
2. 用 `config_path` 创建 `ConfigIO`
3. 读取 items
4. 对每个 item：
   - 跑 `validate_rules()` + `filesystem::directory_exists()`
   - 如果 valid，跑 `LaunchPlanBuilder::build()` + `TerminalLauncher::populate()`
   - 收集到 `CheckResult`
5. 用 glaze 序列化为 JSON 输出到 stdout
6. 返回退出码

### `run_launch` 流程

1. 创建 `Dependencies`（含 `TerminalLauncher`）
2. 创建 `LauncherApp`
3. `load_config()`
4. `app.launch(id)`
5. 成功输出 "Launched: <name>"，失败输出错误到 stderr

### `main()` 调度

```cpp
int main(int argc, char* argv[]) {
    if (argc >= 3 && std::string(argv[1]) == "--check")
        return run_check(argv[2]);
    if (argc >= 3 && std::string(argv[1]) == "launch")
        return run_launch(argv[2]);

    // GUI
    auto window = MainWindow::create();
    auto deps = create_dependencies();
    auto app = std::make_shared<shell::LauncherApp>(...);
    auto gui = std::make_shared<App>(window, *app);
    return gui->run();
}
```

## 5. 文件布局

```
src/shell/
  launcher_app.h        (新增 — LauncherApp 类)
  launcher_app.cpp      (新增 — 业务方法实现)
  app.h                 (修改 — 引用 LauncherApp)
  app.cpp               (重写 — 薄 Slint 壳)
  logger.h              (不变)

src/
  main.cpp              (修改 — argv 分流 + create_dependencies)
  cli_check.cpp         (新增 — --check 实现)
  cli_launch.cpp        (新增 — launch 实现)
```

## 6. 取舍

| 不做 | 原因 |
|------|------|
| LauncherApp 用 interface 抽象 | 只有两个消费者（GUI 和 CLI），虚接口过度工程 |
| CLI 用子命令框架 | 只有两个命令，手写 argv 解析更轻量 |
| `--check` 跑 launch() | 不需要弹终端窗口就能验证 |
| `launch` 加 --dry-run flag | dry_run 是 LauncherApp 的独立方法，不走 launch 路径 |
