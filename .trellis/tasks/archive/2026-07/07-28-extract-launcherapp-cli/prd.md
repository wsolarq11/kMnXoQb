# 提取 LauncherApp 共享业务层，添加 CLI 入口

## Goal

将当前 `App` 类中的业务逻辑提取为独立的 `LauncherApp` 类，GUI（`App`）和 CLI 入口共享同一个实例。在 `wt-launcher.exe` 上添加 `--check` 和 `launch` 两个命令行入口。

## Requirements

### R1: LauncherApp — 共享业务层

从 `App` 中分离出一个与 UI 框架无关的 `LauncherApp` 类，位于 `src/shell/`：

- 拥有 `ConfigIO`、`Launcher`、`SelectedStore`、`items_`、`settings_`
- 提供所有业务方法：`load_config()`、`save_config()`、`add_item(item)`、`edit_item(index, item)`、`delete_item(index)`、`launch(item_id)`、`dry_run(item_id)`、`validate_all()`
- 不依赖 Slint 或任何 UI 框架
- 通过构造函数注入 `FilesystemIface&`

### R2: GUI（App）退化为薄壳

- `App` 只持有 `LauncherApp&` + Slint 组件
- 所有回调直接委托：`launcher_app_.launch(item.id)`
- `app.cpp` < 80 行

### R3: CLI — `--check <config-path>`

- 加载配置文件，逐项 `validate_rules` + `build()` + `populate()`
- 输出结构化 JSON（glaze 序列化）
- 不初始化 Logger，不弹终端，不写任何文件
- 退出码：0 = 全通过，1 = 有问题

### R4: CLI — `launch <id>`

- 与 GUI 走同一个 `LauncherApp::launch()`
- stdout 简要信息，stderr 错误信息
- CLI 默认 skip_confirm

### R5: 依赖构造不重复

- `create_dependencies()` 函数，GUI 和 CLI 共用
- CLI 不创建 `SingleInstance`

## Acceptance Criteria

- [ ] `App` 不直接调用 `LaunchPlanBuilder` / `TerminalLauncher` / `ConfigIO` — 全部通过 `LauncherApp`
- [ ] `app.cpp` < 80 行
- [ ] `--check` 输出有效 JSON，覆盖全部条目
- [ ] `launch <id>` 与 GUI 共享完全相同的调用路径
- [ ] 全部 103 测试用例通过
- [ ] `wt-launcher`（无参数）行为不变
- [ ] 无新 CMake target，无新外部依赖
