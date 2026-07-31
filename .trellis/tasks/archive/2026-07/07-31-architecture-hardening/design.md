# Design — 架构固化

## S1 编排下沉

目标：HomeViewModel 从 215 行 → < 120 行，所有业务决策进入 UseCases。

下沉清单（从 HomeViewModel 移出）：

| 逻辑 | 去向 | 新签名 |
|---|---|---|
| LaunchSelected 确认决策 | LaunchUseCase | `RequireConfirm(settings, items)`（01 已建） |
| 批量启动 + history 合并 | LaunchUseCase | `LaunchMany(items)` → `(int succeeded, int failed)`（01 已建） |
| 保存策略（items+settings 原子保存） | ItemUseCase/SettingsUseCase 组合 → 新增 `WorkspaceUseCase`？ | 否——保持薄：HomeViewModel 保留 Save() 两行调用（无决策） |
| 状态栏文案生成 | LaunchUseCase | `LaunchSummary(int total, int failed)` 纯函数 |
| 主题/确认开关决策 | SettingsUseCase | 已有 SetTheme/SetConfirmEnabled |

关键决策：**不引入新 UseCase 类**（KISS）。`LaunchUseCase` 承担启动编排全部职责；ViewModel 只做：DI 字段、属性、调用、状态栏字符串赋值。

ViewModel 保留：
- ObservableCollection 管理（RefreshItems 调 Filter）
- 命令转发（一行调用）
- 主题切换（IsDark + RequestedTheme）

## S2 ErrorOr

引入 `ErrorOr` NuGet（launchpad.UseCases 或 Infrastructure？——**只在 Infrastructure 层引入**，Core 保持零依赖原则；UseCases 需要引用错误类型 → 在 Core 定义错误模型，ErrorOr 类型本身放 UseCases？）

设计决策（保持依赖方向）：
- Core 不引 ErrorOr（纯零依赖承诺，架构测试会查）
- **UseCases 定义 `LaunchResult` / `StoreResult` record 或引 ErrorOr**——UseCases 已引 Core+Ports，允许引第三方
- 方案 A（推荐）：UseCases 引 ErrorOr；Infrastructure 的 ConfigStore 抛异常（不变量破坏）→ UseCases 捕获转为 ErrorOr。但这把异常当控制流。
- 方案 B：Ports 接口返回值改 `ErrorOr<T>`——但 Core 不能引 ErrorOr（Core 引 Ports 接口）→ 不行。
- 方案 C：Ports 接口抛异常（保持现状），UseCases 层用 ErrorOr 包装（`TrySave` → `ErrorOr<Success>`）。错误模型：Core 定义 `enum StoreError { DirNotFound, WriteFailed, ParseFailed }` + UseCases 映射。

选 **方案 C**：
- `LaunchUseCase.TryLaunch` 返回 `ErrorOr<Unit>`（结构化：`LaunchError.ProcessNotFound/WorkingDirectoryMissing/Unknown`）
- `LaunchUseCase.TryLaunchMany` 返回 `(int succeeded, List<string> failures)`
- `ItemUseCase.Save` 返回 `ErrorOr<Unit>`（DirNotFound/WriteFailed）
- HomeViewModel：`TryLaunch` 失败 → 状态栏显示结构化消息（`error.Description` 含路径/异常）；`Save` 失败 → 状态栏"Save failed: ..."（不再静默）
- `UnhandledException` 保留（编程错误兜底 + crash log），但预期失败不再到达它

ErrorOr 包：`ErrorOr` 最新稳定（2.x）。

## S4 ArchUnitNET

新增测试文件 `tests/launchpad.Core.Tests/ArchitectureTests.cs`（xUnit 包 ArchUnitNET.xUnit）：

规则（5 条起步）：
1. Core 类型不得引用 Microsoft.UI.Xaml（WinUI）
2. Core 类型不得引用 System.IO（除 System.IO.xxx 白名单——实际 Core 无 IO）
3. UseCases 类型不得引用 Microsoft.UI.Xaml
4. UI 层类型不得依赖 Infrastructure 类型（例外：App.xaml.cs 的 DI 注册与 Attach 调用——用 `Except` 规则或命名空间过滤）
5. Core 类型不得引用 UseCases/Infrastructure 类型（依赖方向：Core 是最底层）

注意：ArchUnitNET 反射分析 WinUI 生成的 obj 文件（App.g.cs 等）会拖慢/误报——架构测试只加载 4 个程序集（Core/UseCases/Infrastructure/launchpad），用 `new ArchitectureLoader().LoadAssemblies(...)` 限定。

架构测试项目：现有 `launchpad.Core.Tests` 引了 Core/UseCases/Infrastructure（FakeTerminalDetector 已引）。launchpad（WinUI）程序集加载：测试项目引 WinUI 项目会拉 WASDK 依赖——**架构测试需要加载 launchpad 程序集才能查 UI 层规则**。方案：`ArchitectureLoader.LoadAssemblies("launchpad.dll")` 动态加载（不需要编译时引用，测试运行时从输出目录找 dll）；若 WASDK 加载失败（缺原生运行时），用条件跳过（UI 规则本机验证，CI 只跑纯库规则）——诚实方案：UI 相关规则在测试中 `Skip` 由环境变量控制？过度复杂。
简化：**架构测试只针对三个纯库程序集**（Core/UseCases/Infrastructure），UI 层规则用命名空间静态检查降级：断言"launchpad 项目源码中不得出现 `using Launchpad.Infrastructure`"（用源文件 grep 测试——读取 cs 文件断言，简单可靠）。这符合 KISS。

最终规则：
- ArchUnitNET：Core 零 WinUI、Core 零 System.IO、UseCases 零 WinUI、Core 不依赖上层（4 条）
- 源文件检查（xUnit 读文件）：launchpad/src/launchpad 下 cs 文件无 `using Launchpad.Infrastructure`（App.xaml.cs 除外，白名单）——1 条

## B2 卡片危险标记

- `LaunchItem` 增加 `[JsonIgnore] public bool IsDangerous => DangerousFlagDetector.IsDangerous(Command)`（Core 模型，纯计算，不序列化）
- HomeView 卡片模板：危险时显示 AlertTriangle 图标 + ToolTip 危险原因（`DangerousFlagDetector.DangerousReason`）
- DataTemplate 绑定：`IsDangerous` + 新 converter 或直接绑定 Brush。用现有 DangerBrush
- 危险原因文本：`LaunchItem` 增加 `[JsonIgnore] public string? DangerReason => DangerousFlagDetector.DangerousReason(Command)`

## B4 文案

- EditDialog placeholder："e.g. pwsh, gnome-terminal" → "e.g. pwsh, cmd, powershell"

## 测试计划

- 架构测试（4+1 条规则）
- LaunchUseCase.TryLaunch 结构化错误断言（ProcessNotFound/目录缺失）
- Save 失败路径（FakeStore 抛 IO 异常 → ErrorOr WriteFailed）——注意：ConfigStore 用 ErrorOr 后 FakeStore 返回 ErrorOr
- 卡片危险标记：模型层断言 IsDangerous/DangerReason
