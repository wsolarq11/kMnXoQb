# Implement: LauncherApp + CLI Entry

## Strategy

自顶向下：先建 LauncherApp（新类，零风险），再用 App 调用 LauncherApp（验证等价性），最后加 CLI 入口。每步后 `ctest` 全部通过。

---

## Step 1: 创建 LauncherApp

### 1.1 创建头文件

- [ ] 创建 `src/shell/launcher_app.h`
- [ ] 声明 `CheckResult` 结构体
- [ ] 声明 `LauncherApp` 类：构造函数接受 `ConfigIO`、`Launcher`、`ThemeDetector`
- [ ] 声明所有公共方法

### 1.2 创建实现文件

- [ ] 创建 `src/shell/launcher_app.cpp`
- [ ] 实现 `load_config()` / `save_config()`（从 `App` 搬移）
- [ ] 实现 `add_item()` — 用 `core::deduplicate_id()` 分配 ID，push 到 items_
- [ ] 实现 `edit_item()` — 按 index 替换，保留 id
- [ ] 实现 `delete_item()` — 按 index 删除
- [ ] 实现 `launch()` — LaunchPlanBuilder::build → TerminalLauncher::create()->populate → launch（同步，后台线程）
- [ ] 实现 `dry_run()` — build → populate，返回 LaunchPlan，不 launch
- [ ] 实现 `validate_all()` — 遍历 items，逐项 validate_rules + 目录检查 + build + populate，收集 CheckResult

### 1.3 更新 CMake

- [ ] `src/CMakeLists.txt` 的 `wt-launcher` target 添加 `shell/launcher_app.cpp`

---

## Step 2: 重写 App 为薄壳

### 2.1 重写 app.h

- [ ] 删除 `config_`、`launcher_`、`fs_`、`resolver_`、`theme_detector_`、`dialog_provider_`、`single_instance_`、`items_`、`settings_`、`selected_` 成员
- [ ] 添加 `shell::LauncherApp& app_` 成员
- [ ] 构造函数：`App(slint::ComponentHandle<MainWindow>, shell::LauncherApp&, pal::DialogProvider&)`
- [ ] 保留 UI 状态成员：`card_model_`、`current_edit_index_`、`pending_launch_index_`、`search_query_`

### 2.2 重写 app.cpp

- [ ] `App` 构造函数只存引用
- [ ] `run()` — 委托 `launcher_app` 的初始化，不再创建 SingleInstance/ConfigIO/Launcher
- [ ] `load_config()` / `save_config()` — 委托给 `app_`
- [ ] `refresh_ui()` — 读 `app_.items()` / `app_.settings()` / `app_.selected()` / `app_.theme_detector()`
- [ ] `update_card_model()` — 不变（纯 UI 操作，只是数据来源改为 `app_.items()`）
- [ ] 每个 `on_*` 回调 — 委托给 `app_`，1-3 行
- [ ] `on_dialog_save` — 调用 `app_.add_item()` 或 `app_.edit_item()`，不再内联 ID 去重
- [ ] 删除 `launch_item(int)` 私有方法（启动逻辑在 `LauncherApp::launch()` 中）

### 2.3 验证

- [ ] 编译 `wt-launcher`
- [ ] 运行全部测试（确保等价性）

---

## Step 3: 添加 CLI 入口

### 3.1 创建 `create_dependencies()`

- [ ] 在 `main.cpp` 中抽取依赖构造函数
- [ ] 逐项 `mkdir` 必要目录 / 初始化 Logger（仅 GUI 分支）

### 3.2 实现 `--check`

- [ ] 创建 `src/cli_check.cpp`
- [ ] 包含 `shell/launcher_app.h`、`shell/real_filesystem.h`
- [ ] 实现 `run_check(config_path)`：读取 config → validate_all → glaze JSON 输出
- [ ] 声明于 `src/cli_check.h`
- [ ] 更新 CMake

### 3.3 实现 `launch`

- [ ] 创建 `src/cli_launch.cpp`
- [ ] 实现 `run_launch(config_path, id)`：LauncherApp::launch() 同步调用
- [ ] 声明于 `src/cli_launch.h`
- [ ] 更新 CMake

### 3.4 修改 main.cpp

- [ ] argv 解析：`--check FILE` / `launch ID` / 无参数 → GUI
- [ ] GUI 分支使用 `create_dependencies()` + `LauncherApp` + `App`

---

## Step 4: 验证

### 4.1 等价性

- [ ] 全部 103 测试通过
- [ ] `wt-launcher`（无参数）打开 GUI，行为不变
- [ ] `wt-launcher --check <real-config.json>` 输出正确 JSON
- [ ] `wt-launcher launch "some-id"` 与 GUI 点击按钮行为一致

### 4.2 行数检查

- [ ] `app.cpp` < 80 行

---

## Verification Commands

```powershell
cmake --build build/debug --target wt-launcher
cmake --build build/debug --target core_tests
cmake --build build/debug --target platform_tests
ctest --test-dir build/debug --output-on-failure

# CLI test
./build/debug/src/wt-launcher.exe --check ./path/to/real-config.json
```

## Rollback

每个 Step 一个 commit。Step 1 创建新类（零破坏）。Step 2 重连 App（有破坏性，完整验证）。Step 3 添加 CLI（增量）。
