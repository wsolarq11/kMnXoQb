# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

Launchpad：Windows 原生启动器，用统一界面启动 AI CLI 工具（snow / codex / claude / opencode 等）。

**技术栈状态（2026-08-01）**：R3 全图实施完成——新栈为 **Rust 核心 + Tauri 2.11 + React**（`launchpad-tauri/`，阶段 0-5 验收通过，质检自动化全 PASS，含便携 zip + MSI 双产物与双轨配置）。新栈规范/踩坑见 `.trellis/spec/tauri-rust-ts/index.md`；迁移决策链见 `archive/2026-08/08-01-migration-eval-rust-ts`。C# 主线（`launchpad/`，WinUI 3）保持可回滚，其行为断言是迁移对齐的契约。旧实现归档在 `archive/`（`launchpad-rs`、`launchpad_flutter`）。注意：安装实测（per-user 双产物）已于 8-01 完成；人工场景走查为下一迭代待办，干净环境安装实测为 R4 W3 工作项。双栈退役条件与归档步骤见 R4 W4 决策（.trellis/tasks/08-03-r4-w4-retirement/retirement-criteria.md）。

### 验证状态矩阵（项目当前状态唯一真源，滞后检测见 AGENTS.md）

| 工作项 | 状态 | 证据 | 更新日期 |
|---|---|---|---|
| W2 新栈 CI 门禁（launchpad-tauri） | 已完成 | `.github/workflows/ci-tauri.yml` 全绿（run 30820279223 success）；本地 cargo 125 例 + vitest 35 例全绿 | 2026-08-03 |
| W1 人工场景走查 | 未开始（清单已就绪） | `08-03-r4-w1-walkthrough/walkthrough.md`（14 场景） | 2026-08-03 |
| W3 干净环境安装实测 | 进行中（zip/便携双轨 PASS，MSI 部分待用户） | `08-03-r4-w3-install-validation/install-form-validation.md`（CI artifact 三件套 + 便携双轨正负对照验证） | 2026-08-03 |
| W4 双栈退役条件决策 | 已完成 | `archive/2026-08/08-03-r4-w4-retirement/retirement-criteria.md`（触发条件/归档步骤/处置，提交 f334124） | 2026-08-03 |
| W5 状态真源与文档同步 | 已完成 | 本矩阵 + commit-msg hook（提交 e70254c，无前缀拒绝/带前缀通过实测） | 2026-08-03 |

## 常用命令（均在 `launchpad/` 目录下执行）

```bash
# 构建（Release）
dotnet build src/launchpad/launchpad.csproj --configuration Release

# 单元测试 + 架构测试（ArchUnitNET），提交前必须全绿
dotnet test tests/launchpad.Core.Tests/

# 契约测试（真实 spawn pwsh/cmd/wt；无 Windows Terminal 时 wt 用例自动跳过）
dotnet test tests/launchpad.IntegrationTests/

# 发布（产物在 launchpad/publish/，含 config/ 模板与 XAML xbf/pri 补齐）
powershell -ExecutionPolicy Bypass -File publish.ps1
```

- 依赖版本由 `Directory.Packages.props`（CPM）集中管理；恢复图锁在 `packages.lock.json`（入库，CI 缓存键）。
- pre-commit hooks（`.pre-commit-config.yaml`）：行尾 CRLF 强制、尾随空白、大文件阻断。
- CI（`.github/workflows/ci.yml`）额外执行 XAML binding-mode 门禁：每个 Page/UserControl/ContentDialog 根元素必须声明 `x:DefaultBindMode`（x:Bind 默认 OneTime 是 BUG-1/2/3 类缺陷的根源）。

## 架构：Clean Architecture + Functional Core / Imperative Shell

```
launchpad/
  src/launchpad.Core/           领域层：纯函数（net10.0 类库，零 WinUI/System.IO 依赖）
    Models/     LaunchItem, AppSettings, WindowState, LaunchPlan（不可变 record）
    Domain/     DangerousFlagDetector, LaunchPlanner, ItemValidator, WindowPosition（纯决策）
    Ports/      所有端口接口（IConfigStore, IProcessSpawner, IDialogService …）
    Localization/ LanguageKey（稳定文案键）+ Translations（中英纯语言表）
    Serialization/ LauncherJson（System.Text.Json，snake_case 序列化，与旧 serde 格式字节兼容）
  src/launchpad.UseCases/       应用层：ItemUseCase, LaunchUseCase, SettingsUseCase
  src/launchpad.Infrastructure/ 基础设施：ConfigStore, ProcessSpawner, TerminalDetector
  src/launchpad/                WinUI 3 外壳：App.xaml.cs（DI 组装）、ViewModels、Views、
                                Localization/LanguageService（语言状态 + 热切换）
  tests/launchpad.Core.Tests/   xUnit 单元 + ArchUnitNET 架构测试 + Verify 快照测试
  tests/launchpad.IntegrationTests/ 契约测试（真实进程）
```

规则（ArchUnitNET `ArchitectureTests` + `SourceFileRuleTests` 机器执行）：

1. 依赖箭头永远向下：UI → UseCases → Core ← Infrastructure。测试项目绝不引用 WinUI 主项目。
2. 端口接口定义在 `Core/Ports`，由 Infrastructure 实现，DI 注入（`App.xaml.cs` 的 `ConfigureServices` 全部按接口注册——**DI 按具体类型解析会失败**）。需要窗口宿主的服务（DialogService/DirectoryPickerService）构造注入 `IWindowHandleProvider`/`IXamlRootProvider`，宿主状态由组合根持有的 `WindowHost` 在 `Activate` 后填充；禁止后置 Attach 与具体类型解析。
3. 纯函数核心：模型不可变（`sealed record` + with 表达式），列表操作返回新列表（`ItemUseCase` 的 Upsert/Delete/Move/SetSelect 均为 static 纯方法），plan 纯决策（`LaunchPlanner.PlanWindows`）。
4. 编排逻辑不进 ViewModel：确认策略、批量启动、历史合并、保存时机全部在 UseCases；ViewModel 只做绑定转发 + 状态栏赋值。
5. 预期失败用 ErrorOr（`LaunchErrors`/`StoreErrors`）结构化返回给状态栏；异常仅用于编程错误。禁止 UnhandledException 兜底预期失败。
6. 零 shell 启动：`Process.ArgumentList`（argv 数组，底层 CreateProcessW），禁止 `Process.Start(string)` 或字符串拼接构造命令。

## 关键坑（详细版见 `.trellis/spec/winui3-csharp/index.md`，改对应层前先读）

- **`Application.RequestedTheme` 运行时不可变**（COMException）。主题切换必须在内容根元素上设 `FrameworkElement.RequestedTheme`；"system" 映射 `ElementTheme.Default`。
- **命名空间冲突**：`Launchpad.Application` 与 `Microsoft.UI.Xaml.Application`（CS0118）。应用层命名空间叫 `Launchpad.UseCases`，不要新建 `Launchpad.Application`。
- **cmd.exe 引号陷阱**：`cmd /k` 不用标准 argv 引号规则（`\"` 是字面量），cd 前缀命令在含引号/空格目录下整条不执行。目录一律经 `ProcessStartInfo.WorkingDirectory` 传递（`Args = ["/k", command]`）；pwsh `-Command` 走标准 argv 解析，单引号转义 `'` → `''`。
- **dotnet publish 不复制 XAML 编译产物**（xbf/pri 缺失 → 发布版启动即 XamlParseException）。必须用 `publish.ps1`（从 `artifacts/bin/launchpad/release_win-x64` 复制）。
- **最小化窗口 Position 是 -32000**（离屏）。`OnClosed` 只保存最后一次 `OverlappedPresenterState.Restored` 的坐标，恢复前经 `WindowPosition.ClampToVisible` 纠偏。枚举值是 `Restored` 不是 `Normal`。
- **unpackaged FolderPicker 必须 `WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd)`**，否则抛异常；XamlRoot 在 Activate 之后才可用（DialogService 经 `IXamlRootProvider` 每次 show 时懒解析）。
- **Mica 是 Win11 专属**：Win10 用 `DesktopAcrylicBackdrop` 回退（`OperatingSystem.IsWindowsVersionAtLeast(10,0,22000)` 分支）。
- **启动目录不存在抛 `Win32Exception`**，NativeErrorCode=267（ERROR_DIRECTORY）而非 3。TryLaunch 把 267/3 都映射为 WorkingDirectoryMissing。
- **x:Bind 在 DataTemplate 里访问外层**：用 `{Binding DataContext.X, ElementName=Root}` 或事件转发，x:Bind 默认绑定 item。
- **System.Text.Json**：`PropertyNameCaseInsensitive=true` + `PropertyNamingPolicy.SnakeCaseLower`（写 snake_case 保持旧配置兼容）；record 相等性会被 JsonExtensionData 空字典破坏（测试用字段级断言）。
- **i18n**：文案一律键引用（`Core/Localization/LanguageKey` + `Translations` 中英纯表），`settings.json` 的 `language` 字段（`auto` 跟随系统 / `zh-CN` / `en-US`）三层优先级：显式设置 > 系统语言 > 英文兜底。领域层返回 `LanguageKey?` 而非文案（`DangerousReason`、表单校验错误、`LastRecoveryNoteKey`）；XAML 用 `ViewModel.Texts` 文案属性（`HomeTexts`）或 `LanguageKeyTextConverter`；LanguageService 热切换触发 `HomeTexts` 通知重估 + `RefreshItems()` 重建卡片。**ErrorOr 错误描述保持英文内部诊断**（有意决策，前缀本地化）。注意：`FirstOrDefault` 对值类型元组返回非空默认枚举（安全命令误报危险的坑，详见 spec 坑 #13）。

## 数据与配置

- 配置目录从 exe 位置**向上搜索含 `config/` 的祖先**（`ResolveConfigDir`），不依赖进程工作目录；`ConfigStore` 构造时 `CreateDirectory`。
- `config.json` 写前备份到 `config.json.bak`；解析损坏时自动从备份恢复（`LastRecoveryNote` 上状态栏），双坏才抛 `ConfigParseException`（含路径）。settings.json 在 `launchpad/config/settings.json` 仓库内即应用真实配置。
- 配置目录下的 `config.json` 示例在 `config/config.example.json`（仓库根）；发布时作为模板拷入产物。
- 危险命令确认：`DangerousFlagDetector` 对命令做 6 个 flag 子串匹配（dangerously / yolo / skip-permissions / bypass-approvals / bypass-sandbox / bypass.sandbox），危险项在编辑框/卡片/确认对话框三处警告。
- 图标只用 lucide：`Assets/Fonts/Lucide.ttf` + `FontIcon Glyph`（`LucideGlyph.cs` 码点表）。禁止引入其他图标库。
- 应用单实例：`SingleInstance` 非主实例直接 Exit。

## 测试约定

- 每个修复必须有回归测试；契约行为对齐旧 Rust 版以测试断言为准，注释不得声称未验证的行为。
- 架构测试在 `launchpad.Core.Tests/ArchitectureTests.cs`（ArchUnitNET）；`SourceFileRuleTests` 断言 UI 源码不引用 Infrastructure（App.xaml.cs 白名单）。
- 快照测试用 Verify.Xunit（`*.verified.txt`），行为变更需审核后更新快照。
- 集成测试用 `WtFact` 特性标记需要 Windows Terminal 的用例。

## 规范文档（Trellis）

- `.trellis/spec/winui3-csharp/index.md` — 在役架构规范 + 15 条踩坑记录（改对应层前先读）。
- `.trellis/spec/security/index.md` — 命令注入/路径安全约定与测试覆盖映射。
- 工作流遵循 `.trellis/workflow.md`（Trellis 任务驱动，见 AGENTS.md）。
