# Tauri + Rust + React 技术栈规范（launchpad-tauri）

在役架构规范。迁移自 C#/WinUI 3（`winui3-csharp/index.md` 为旧栈存档）。行为对齐以测试断言为准（`launchpad-tauri/src-tauri` cargo test + `src/` vitest）。

## 架构

- 分层：React 薄壳 → Tauri commands 薄壳 → app 编排层 → core 纯函数核心 ← config/infra 实现。
- 核心层（`core/`）零外部 I/O（仅 std + serde）：planner/danger/validator/window_pos/items/launch/settings/i18n/errors/models/ports。
- 端口（`core/ports.rs`）：`ProcessSpawner` / `TerminalAvailability` trait，由 infra 实现注入（AppState 组装）。
- 双轨配置：`config/paths.rs` —— 便携（exe 旁 `launchpad.portable` 标记 → 向上搜索含 config/ 的祖先，返回其 `config/` **子目录**，fallback `<exe>/config`）；MSI（`%APPDATA%\launchpad\config`）。
- 写入韧性：atomic_write（.tmp + rename）+ config.json.bak 备份 + 损坏自动恢复（恢复走文件复制，不走写路径）。

## 命令面（前端经 lib/invoke.ts 调用，禁止散落 invoke）

list_items / create_item / update_item / delete_item / move_item / set_select / toggle_select_all / needs_confirm / launch_item / launch_many / get_settings / toggle_theme / toggle_language / set_confirm_enabled / get_language / pick_directory / save_window_state / load_window_state。

## 发布

- `tauri build --no-bundle`（exe 内嵌前端资源）→ `scripts/pack-portable.ps1`（zip 输出到 `release/`）→ `tauri build --bundles msi`（per-user WiX）。
- 便携 zip 必须含 `launchpad.portable` 空标记文件（形态判定）。

## 踩坑记录

1. **cargo build 的 exe 不内嵌前端资源**：页面加载失败 → 必须 `tauri build`（build.rs 嵌入 frontendDist）。
2. **Rust std 缺失可执行文件时 `raw_os_error()` 为 None**（`ErrorKind::NotFound`，"program not found"）——Win32 错误码 2 丢失；spawner 必须补 `NotFound → ERROR_FILE_NOT_FOUND(2)` 映射，否则 TryLaunch 错误分类失效。
3. **struct 与 trait 不能同名**（Rust 单命名空间）：实现类 `SystemProcessSpawner` / `TerminalDetector`，端口 trait `ProcessSpawner` / `TerminalAvailability`。
4. **vite 的 `beforeBuildCommand` 清空 dist/**：打包产物不要放 `dist/`（被清掉），用独立目录（`release/`）。
5. **WebView2 首次冷启动 ~35s**（LTSC + Runtime 150 初始化）：自动化验证等待需 >30s；常驻使用无感。
6. **WiX 下载超时**（github 网络）：手动下载 wix314-binaries.zip 解压到 `%LOCALAPPDATA%\tauri\WixTools314` 后 `tauri build --bundles msi` 可用。
7. **Tauri 2.11 运行时无 on_page_load**（WebviewEvent 只有 DragDrop）：页面就绪逻辑放前端（React useEffect），不要用 Rust eval 注入。
8. **Windows `executableDir()` Not supported**：文件 I/O 全部收进 Rust command（`current_exe()` 定位），前端不直接用 fs 插件。
9. **配置目录语义**：`ResolveConfigDir` 返回祖先的 `config/` **子目录**（不是祖先本身）；fallback `<exe>/config`——与 C# 一致，写错会导致 config.json 散落仓库根。
10. **dialog 插件 pick_folder 是回调式**：用 `blocking_pick_folder()`（命令线程池执行，不阻塞 webview 线程）。
11. **Tauri 默认 MSI 模板是 perMachine + ProgramFilesFolder**（安装需提权、目录不可写）：per-user 需自定义 wix 模板（`src-tauri/wix/per-user-main.wxs`）：`InstallScope="perUser"` + `InstallPrivileges="limited"` + INSTALLDIR 放 `LocalAppDataFolder\Programs`；**组件 KeyPath 必须是 HKCU 注册表键**（ICE38：用户 profile 组件不允许文件 KeyPath）；用户 profile 目录需 RemoveFile 条目（ICE64）。
12. **自定义 wix 模板必须保留默认模板的 Feature 结构与 WixUI 引用**：组件不在任何 Feature 会 ICE21 失败；WixUI 依赖 tauri 的 light `-ext WixUIExtension`（自动带）。现做法：`src-tauri/wix/per-user-main.wxs` 直接用 tauri handlebars 变量（`{{main_binary_path}}` / `{{icon_path}}`）与 candle 变量（`$(var.Win64)`），随 `tauri build` 直接渲染，不再依赖"渲染后拷贝再修改"。`patch_wix.py` 仍在 `src-tauri/`，但仅作历史生成参考（其匹配的默认渲染输出结构已过时，勿再按其流程重新生成）。
13. **WiX light 的 `-cultures` 参数**：本环境用冒号语法（`-cultures:en-US`）；空格语法会把文化名当输入文件（"cannot find the file 'en-US' with type 'Source'"）。tauri CLI 内部用冒号，无碍。
14. **MSI 静默安装要在 PowerShell 里跑**：Git Bash 会把 `/i` `/qn` 当路径转换导致 msiexec 挂起。

## 前端视觉规范（2026-08-02 卡片网格任务确立）

- 卡片网格：`src/lib/grid.ts` 纯函数 `planGrid`（枚举列数使卡片宽高比最接近 φ=1.618，卡片同时受最小宽 240px / 最小高 96px 约束，无解转滚动兜底）+ `src/hooks/useGridPlan.ts`（ResizeObserver + 100ms 防抖）。改布局参数时同步改 `grid.test.ts` 的断言（用例注释含逐步数学）。
- 主题 token：CSS 变量分层（`App.css` 顶部三块：浅色默认 / `[data-theme="dark"]` / `prefers-color-scheme` 深色）。背景 7 层、文本 3 级（primary/secondary/tertiary）、边框 2 级、accent/danger 各带 hover 变体。
- **对比度门槛（实测约束，勿凭感觉调色）**：正文 ≥ 7:1、次要 ≥ 4.5:1、弱化 ≥ 3:1、按钮文字 ≥ 4.5:1。浅色主题 accent `#2f6fed` 白字 4.55 达标；但深色主题亮强调色（`#4d8aff` 白字仅 3.28）必须配深色文字——按钮文字走 `--on-accent` / `--on-danger` 变量（浅色 `#fff`，深色 `#0d1017`），不要硬编码 `#fff`。
- **`overflow: hidden` 塌缩陷阱（Chromium 实测）**：CSS Grid `grid-auto-rows: auto` 下，`overflow: hidden` 的 flex 容器卡片其固有高度贡献被计算为仅 padding+border（480x360 窗口 + 10 条目实测卡片 26px，内容 61px 被裁切）。改用 `overflow: clip`（同样裁切溢出，但不参与固有尺寸计算，卡片恢复 87px 自然高度）。新增网格卡片务必用 clip 而非 hidden。
- **planGrid 必须收 content-box 尺寸**：ResizeObserver 回调用 `entry.contentRect`；初始计算用 `getBoundingClientRect()` 减 `.item-grid` padding（16x2 / 14x2，常量与 App.css 同步注释）。传 border-box 会使 minWidth/minHeight 阈值偏移 32x28px（例：计划 240px 宽实际 208px）。

## 测试

- 单测：core 内联 `#[cfg(test)]` + `tests/`（config_store/json_round_trip/spawner_contract/terminal_contract）。
- 契约测试真实 spawn pwsh/cmd/wt（无 wt 跳过）；目录用例覆盖空格/引号/分号/&/中文。
- 前端 vitest：键表完整性（62 键 × 2 语言）、danger 工具、store 流转（mock invoke）、grid 布局算法。
- 提交前：`cargo test` + `npx vitest run` + `npx tsc --noEmit` 全绿。
