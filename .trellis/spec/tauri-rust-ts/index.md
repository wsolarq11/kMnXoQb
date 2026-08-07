# Tauri + Rust + React 技术栈规范（launchpad-tauri）

在役架构规范。迁移自 C#/WinUI 3（`winui3-csharp/index.md` 为旧栈存档）。行为对齐以测试断言为准（`launchpad-tauri/src-tauri` cargo test + `src/` vitest）。

## 架构

- 分层：React 薄壳 → Tauri commands 薄壳 → app 编排层 → core 纯函数核心 ← config/infra 实现。
- 核心层（`core/`）零外部 I/O（仅 std + serde）：planner/danger/validator/window_pos/items/launch/settings/i18n/errors/models/ports。
- 端口（`core/ports.rs`）：`ProcessSpawner` / `TerminalAvailability` trait，由 infra 实现注入（AppState 组装）。
- 双轨配置：`config/paths.rs` —— 便携（exe 旁 `launchpad.portable` 标记 → 向上搜索含 config/ 的祖先，返回其 `config/` **子目录**，fallback `<exe>/config`）；MSI（`%APPDATA%\launchpad\config`）。
- 写入韧性：atomic_write（.tmp + rename）+ config.json.bak 备份 + 损坏自动恢复（恢复走文件复制，不走写路径）。

## 命令面（前端经 lib/invoke.ts 调用，禁止散落 invoke）

list_items / create_item / update_item / delete_item / move_item / set_select / toggle_select_all / needs_confirm / launch_item / launch_many / get_settings / toggle_theme / toggle_language / set_confirm_enabled / get_language / pick_directory / save_window_state / load_window_state / window_material。

`window_material` 返回 `"mica" | "acrylic" | "none"`（按 OS build 号判定，无副作用），前端据此翻转 `body.material` 类切换半透明 chrome（契约见下方视觉规范）。

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
15. **Tauri 2 窗口材质（Mica/Acrylic）走内置 Effect API**：`tauri::window::Effect`（`Mica`/`MicaDark`/`MicaLight`/`Acrylic`/`Blur`/`Tabbed*`，底层委托官方 window-vibrancy）。`WindowEffect` 在 `tauri::utils::config` 是**私有**的，公开路径是 `tauri::window::Effect`；配置对象是 `tauri::utils::config::WindowEffectsConfig`（pub）。用法：`window.set_effects(Some(WindowEffectsConfig { effects: vec![effect], ..Default::default() }))`。版本阈值：build ≥ 22000 用 Mica 系（theme 联动：dark→MicaDark / light→MicaLight / system→Mica），build ≥ 17763 用 Acrylic，其余/失败静默降级（`let _ =` 忽略 Result）。应用点：lib.rs setup（按持久化主题）+ `toggle_theme` command（窗口效果随主题切换）。实现见 `src-tauri/src/infra/effects.rs`。
16. **RtlGetVersion 在 windows crate 位于 `Wdk::System::SystemServices`**（不在 Win32 模块树），feature 名 `Wdk_System_SystemServices`；参数结构是 `OSVERSIONINFOW`（含 dwOSVersionInfoSize，需手动置 size）。`NTSTATUS::ok()` 返回 `Result<(), Error>` 而非 Option——在返回 `Option` 的函数里要 `.ok().ok()?` 双重转换。
17. **Rust std spawn 错误码行为（环境相关，勿假设稳定）**：缺失可执行文件时 `raw_os_error()` 为 None + `NotFound`；缺失可执行文件**且**工作目录缺失时历史上返回 3（ERROR_PATH_NOT_FOUND），但工具链行为可能变化（实测新 std 返回 None+NotFound，被 spawner 特判为 2）。`classify_spawn_error` 因此对路径类错误码（2/3/267）统一按注入的 `dir_exists` 归因：目录缺失 → `WorkingDirectoryMissing`，否则 → `ProcessNotFound`。新增 spawn 错误分类逻辑必须保留这个 dir_exists 归因，不要只按错误码直判。

## 前端视觉规范（2026-08-05 设计达标任务重写，Fluent 2 + WCAG AA 基准）

### 卡片网格：固定尺寸 + 滚动（2026-08-05 起替代旧的 φ=1.618 fill 模式，旧文档作废）

- `src/lib/grid.ts`：`planGrid(width, count) => columns`，列数 = `max(1, floor((width + 10) / 270))`（固定列宽 260px + gap 10px）。卡片尺寸**恒定**：窗口 resize / 搜索过滤只变列数，不变卡片像素尺寸；行高固定 120px（App.css `grid-auto-rows: 120px`），超出滚动（`overflow-y: auto` 常驻）。0 项 → 0 列。
- `src/hooks/useGridPlan.ts`：ResizeObserver + 100ms 防抖，只输出 columns（无 scroll 状态）。
- 改布局参数时同步改 `grid.test.ts` 断言 + App.css 的 `grid-auto-rows`（两处有对照测试）。
- **`overflow: hidden` 塌缩陷阱（Chromium 实测，仍有效）**：flex 卡片用 `overflow: hidden` 时固有高度被算成仅 padding+border；用 `overflow: clip`。新增网格卡片务必用 clip 而非 hidden。

### 主题 token 与对比度门槛（实测约束，勿凭感觉调色）

- CSS 变量分层（`App.css` 顶部三块：浅色默认 / `[data-theme="dark"]` / `prefers-color-scheme` 深色）。背景 7 层、文本 3 级（primary/secondary/tertiary）、边框 2 级、accent/danger 各带 hover 变体。暗色表面随层级升高变亮（M3 暗色规则），阴影 opacity 翻倍（Fluent 暗色规范）。
- **对比度门槛（WCAG 2.1 AA）**：正文 ≥ 4.5:1、非文本 UI 边界 ≥ 3:1（SC 1.4.11）、表面分层 ≥ 1.15（浅）/ ≥ 1.10（暗，M3 实测基线）、暗色 fg-primary ≥ 14:1（M3 on-surface 基线——15.8:1 是纯白文本上限，无发行主题达到）。深色主题亮强调色（`#4d8aff` 白字仅 3.28）必须配深色文字——按钮文字走 `--on-accent` / `--on-danger` 变量，不要硬编码 `#fff`。
- **调色后必须跑 `contrast.test.ts`**（vitest）：该测试解析 App.css 的 `:root` 与 `:root[data-theme="dark"]` 块提取 token，按用途矩阵断言。**矩阵必须覆盖真实渲染配对**——分隔线画在 bg-header 上（`.header-bar`/`.status-bar` 用 border-strong 而非 border-subtle，否则浅色 2.55:1 破 3:1）、stat-bar 分隔线在 bg-app 上、kbd 键帽边框在 bg-code 上。新增任何带背景色的元素时同步补矩阵配对。
- **间距必须落在 Fluent 4px ramp**（0/2/4/6/8/10/12/16/20/24/28/32...）：14px/7px/22px 都不合法（14px→16px、7px→8px、22px→24px）。唯一豁免：mark 搜索高亮 `padding: 0 1px`（文本装饰贴边，非组件）。
- **color-scheme 随主题**：`:root[data-theme="dark"] { color-scheme: dark }`、`:root[data-theme="light"] { color-scheme: light }`，system（无 data-theme）保持 `light dark`——否则原生滚动条/checkbox 不跟随主题。
- **窗口材质契约（body.material）**：Rust `window_material` 命令返回 "mica"|"acrylic"|"none"（按 OS build），前端启动时 invoke → `document.body.classList.toggle("material", state !== "none")`。`body.material` 下：body 透明、`.app`/`.header-bar`/`.status-bar`/`.stat-bar`/`.boot-screen` 用 color-mix 80-88% 半透明（材质透出）；无该类时 body 保持实色（无材质系统降级）。半透明只降对比度余量不破门槛（base 色对比按不透明对算，更保守）。

## 测试

- 单测：core 内联 `#[cfg(test)]` + `tests/`（config_store/json_round_trip/spawner_contract/terminal_contract）。
- 契约测试真实 spawn pwsh/cmd/wt（无 wt 跳过）；目录用例覆盖空格/引号/分号/&/中文。
- **警告：契约测试会弹出真实终端窗口**（spawner_contract / terminal_contract 设计如此，用于验证真实进程行为）。跑 `cargo test` 时桌面会短暂弹出 pwsh/cmd/wt 窗口——这是预期测试行为，**不是应用 bug**。循环复现 flaky 测试（如 `for` 循环反复跑 cargo test）会持续弹窗，可能被用户误判为"应用不停创建终端"；复现 flaky 时优先用 `--test <name>` 单测过滤，并在跑测试前告知用户会有窗口弹出。
- 前端 vitest：键表完整性（62 键 × 2 语言）、danger 工具、store 流转（mock invoke）、grid 布局算法。
- 提交前：`cargo test` + `npx vitest run` + `npx tsc --noEmit` 全绿。
