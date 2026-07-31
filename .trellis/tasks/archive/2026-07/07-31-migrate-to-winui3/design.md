# Design: WinUI 3 + C# 迁移

## 1. 技术选型

| 项 | 选择 | 理由 |
|----|------|------|
| 运行时 | .NET 10 LTS | 本机 SDK 10.0.301；LTS 支持到 2028-11；官方当前路线 |
| UI 框架 | WinUI 3（Windows App SDK） | 微软官方现代 UI，Fluent 设计语言，系统材质 |
| 部署 | unpackaged 自包含（WindowsAppSDKSelfContained） | 免 MSIX 证书，单目录可运行，适合工具类应用；`dotnet build` 命令行可构建 |
| MVVM | CommunityToolkit.Mvvm | 微软社区官方 MVVM 工具包（源生成器） |
| DI | Microsoft.Extensions.DependencyInjection | .NET 官方 DI 容器 |
| JSON | System.Text.Json | .NET 官方序列化 |
| 测试 | xUnit + 手写 fakes | 官方生态；fakes 遵守全局规则（命名 fake 类） |
| 图标 | lucide SVG path → PathIcon | 全局规则"图标库只用 lucide"；约 14 个图标手写 Geometry 常量 |
| Windows App SDK | **2.2.0**（Stable，2026-06-09 发布；2.0 支持到 2027-04，1.8 仅维护至 2026-09） | 2026-07 研究确认：2.x 面向 .NET 10 推荐；1.7 已出支持期 |

## 2. 工程结构

```
launchpad/
  src/launchpad.Core/               # 领域层类库（net10.0 纯函数，零 WinUI/IO 依赖，可被任意宿主引用）
    Models/  LaunchItem.cs, AppSettings.cs, WindowState.cs, LaunchPlan.cs
    Domain/  DangerousFlagDetector.cs, LaunchPlanner.cs, ItemValidator.cs
    Ports/   IConfigStore.cs, IProcessSpawner.cs, ITerminalDetector.cs,
             IDirectoryPicker.cs, IWindowService.cs
  src/launchpad/                    # WinUI 3 主程序（unpackaged，引用 launchpad.Core）
    App.xaml / App.xaml.cs          # 入口、DI 容器、单实例
    MainWindow.xaml / MainWindow.cs # 主窗口 + Mica/Acrylic + 窗口状态恢复
    Views/
      HomeView.xaml                 # 主页（列表/搜索/选择/统计/状态栏/空状态）
      EditDialog.xaml               # 编辑对话框（表单 + 校验 + 目录选择）
    ViewModels/
      HomeViewModel.cs              # 列表状态、命令、搜索过滤
      EditViewModel.cs              # 表单状态、校验、危险警告
    Application/                    # 应用层：用例编排（引用 Core，无 WinUI 依赖）
      ItemUseCase.cs, LaunchUseCase.cs, SettingsUseCase.cs
    Infrastructure/                 # 基础设施层：端口实现（依赖 Core.Ports）
      ConfigStore.cs, ProcessSpawner.cs, TerminalDetector.cs,
      DirectoryPickerService.cs, SingleInstance.cs, WindowStateService.cs
  tests/
    launchpad.Core.Tests/           # xUnit，只引用 launchpad.Core
      LaunchPlannerTests.cs, DangerousFlagTests.cs, ItemValidatorTests.cs,
      ConfigStoreTests.cs, LaunchUseCaseTests.cs
```

分层规则：依赖箭头永远向下（UI/App → Application → Core ← Infrastructure）。端口接口定义在 `Core/Ports`，由 Infrastructure 实现，DI 容器注入。Core 为纯类库（net10.0），测试不接触 WinUI。

> 修订注（2026-07-31，阶段 B 实现中）：原设计为单项目目录分层；实施时发现 xUnit 测试引用 WinUI 主项目会拉入 Windows 依赖，故领域层拆为独立类库 `launchpad.Core`。
> 修订注 2（2026-07-31，阶段 C 实现中）：`Launchpad.Application` 命名空间与 `Microsoft.UI.Xaml.Application` 类名冲突（CS0118），应用层重命名为 `Launchpad.UseCases`（目录 `launchpad.UseCases`）；应用层与基础设施层均拆为独立 net10.0 类库，测试项目引用全部三个纯库、零 WinUI 依赖。

## 3. 数据模型映射（Rust → C#）

| Rust (serde) | C# | JSON 字段 |
|---|---|---|
| `LaunchItem` | `LaunchItem` record | 字段同名（name/directory/command/confirm/id/selected/terminal/tag/group 均单字，无命名转换） |
| confirm: bool (default true) | `bool Confirm { get; init; } = true` | `"confirm"` |
| terminal/tag/group: Option | `string?` | 缺失时 null；写时省略（null 不序列化，对齐 serde skip_serializing_if） |
| `AppSettings` | `AppSettings` | confirm_enabled/theme/launch_history/window_state + alias "confirmEnabled"（System.Text.Json 不支持 alias，用自定义 converter 或直接读两个键；implement 时读实际 settings.json 定方案） |
| `WindowState` | `WindowState` | x/y/width/height + 默认 800x600 |
| `LaunchPlan` | `LaunchPlan` | 仅内存对象，不序列化 |

## 4. 端口契约

- `IConfigStore`：`IReadOnlyList<LaunchItem> ReadItems()`、`void WriteItems(...)`、`AppSettings ReadSettings()`、`void WriteSettings(...)`；写入 items 前备份 `.bak`；解析失败抛类型化 `ConfigParseException`（含路径与原因），不静默丢弃。
- `IProcessSpawner`：`void Launch(LaunchPlan plan)`——`Process.Start` + `ArgumentList`，零 shell。
- `ITerminalDetector`：`bool TerminalAvailable(string name)`（`where` 命令）；`LaunchPlan` 的终端选择逻辑放领域层 `LaunchPlanner`（纯函数），探测结果作为输入。
- `IDirectoryPicker`：原生文件夹选择器（unpackaged 需 `InitializeWithWindow`）。
- `IWindowService`：位置/大小保存与恢复（`AppWindow.MoveAndResize`）。

## 5. 关键技术点

### 5.1 Mica/Acrylic 与 Win10 回退（风险项）
- 目标机器是 **Win10 19044 LTSC**：Mica 是 Win11 专属，Win10 上不可用。
- 方案：Win11 用 `MicaBackdrop`；Win10 回退 `AcrylicBackdrop`（Win10 1809+ 支持 Background Acrylic）。
- SDK 2.x 新增 `SystemBackdropElement`（FrameworkElement，可放任意 XAML 位置、带 CornerRadius）——卡片级 Acrylic 用这个，替代自绘模糊。
- `OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)` 分支。implement 时以实际 SDK 能力为准验证。

### 5.2 lucide 图标方案
- 当前 UI 用到的图标约 14 个：zap、plus、search、moon、sun、pencil、trash2、chevronUp、chevronDown、alertTriangle、checkCircle、folderOpen、folder、arrowUp。
- 方案：从 lucide 官方 SVG 取 path data，写 `Core/LucideIcons.cs` 静态类，返回 `PathGeometry`，XAML 用 `<PathIcon Data="{x:Static ...}" />`。一次性手工转换，不引入第三方包。

### 5.3 启动逻辑（plan_windows 1:1 移植）
- 终端优先序：wt.exe → pwsh.exe → cmd.exe（与 Rust `plan_windows` 一致）。
- wt 路径：`new-tab -d <dir> <terminal> -NoExit -Command <cmd>`（纯 argv）。
- pwsh fallback：`-NoExit -Command "cd '<dir>'; <cmd>"`；cmd fallback：`/k "cd /d "<dir>" && <cmd>"`——这两个是传给终端的脚本字符串（产品语义），但**修复 Rust 版的目录单引号未转义 bug**（pwsh: `''` 转义；cmd: 目录含引号时用 `""` 包裹转义）。修复行为记录在实现提交信息与 CLAUDE.md。
- 危险检测行为与 Rust 版 1:1（子串小写匹配）；token 级优化列为后续改进，不在本任务。

### 5.4 单实例
- 命名 `Mutex`（`Global\WT_Launcher` 或本地命名）；第二实例启动时尝试激活已有窗口（按进程名查找主窗口句柄 + `SetForegroundWindow`），找不到则仅提示退出。

### 5.5 窗口状态
- 退出时把位置/大小写入 settings.json `window_state`；启动时恢复。用 `AppWindow.MoveAndResize`（WinUI 3 窗口定位正路）。

### 5.6 配置目录
- 保留 `../config` 相对路径（用户决策 D2），解析为相对**当前工作目录**（与 Flutter 版语义一致：从项目根运行）。

## 6. 兼容性要点

- `config.json` 字段与 C# 属性同名，直接兼容；`confirm` 缺失默认 true（对齐 serde `default_bool_true`）。
- `settings.json` 需确认实际字段名（`confirm_enabled` 或 `confirmEnabled`），写前读实际文件取证，两种都能读。
- 旧 `.bak` 文件已存在时覆盖前重新备份（行为一致）。

## 7. 风险与缓解

| 风险 | 缓解 |
|------|------|
| Windows App SDK 版本与 .NET 10 兼容性 | 已确认：2.2.0 Stable 面向 .NET 10 推荐（2026-07 研究） |
| Win10 19044 无 Mica | §5.1 回退链（AcrylicBackdrop / SystemBackdropElement） |
| unpackaged WinUI 3 文件夹选择器需 hwnd | `InitializeWithWindow` 桥接 |
| 命令行构建 WinUI 3（无 VS IDE） | VS Build Tools 18 已装；阶段 A 最小工程冒烟验证 |
| 视觉与 Flutter 版有差异 | 验收用截图对照 + 差异记录（PRD 已允许） |
| settings.json 字段名不确定性 | §6 取证后定 |
| WinUI 3 已知 API 缺口（如程序化窗口最大化需 native API，社区反馈） | 窗口控制走 Win32 `AppWindow` API；遇到缺口时记录为决策而非绕行 |

## 8. 决策记录

- D1（2026-07-31，用户）：视觉效果用系统材质 Mica/Acrylic，不复刻 Flutter 玻璃拟态。
- D2（2026-07-31，用户）：配置目录保留 `../config` 相对路径。
- D3（2026-07-31，用户）：`launchpad_flutter/` 与 `launchpad-rs/` 全部归档到 `archive/`。
- D4（2026-07-31，用户）：单实例锁 + 窗口位置/大小恢复，两个都做。
- D5（规划）：阶段 1 全 C#，阶段 2 核心下沉 Rust + P/Invoke（后续任务）。
- D6（规划）：WinUI 3 unpackaged 自包含部署（免 MSIX）。
