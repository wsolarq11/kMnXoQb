# WinUI 3 + C# 规范（在役架构）

> 2026-07-31 起在役。取代 C++/Slint 与 Flutter/FRB 时代的全部包规范（`_cpp-core`、`_slint-ui`、`_cmake-build`、`_cross-platform` 已停用，仅作历史参考）。

## 架构

```
launchpad/
  src/launchpad.Core/          领域层：纯函数（net10.0 类库，零 WinUI/IO 依赖）
    Models/    LaunchItem, AppSettings, WindowState, LaunchPlan（不可变 record）
    Domain/    DangerousFlagDetector, LaunchPlanner, ItemValidator, WindowPosition
    Ports/     所有端口接口（IConfigStore 等）
    Localization/ LanguageKey（稳定文案键）+ Translations（中英纯语言表，可测）
  src/launchpad.UseCases/      应用层：ItemUseCase, LaunchUseCase, SettingsUseCase（ErrorOr 结构化错误）
  src/launchpad.Infrastructure/ 基础设施：ConfigStore, ProcessSpawner, TerminalDetector
  src/launchpad/               WinUI 3 外壳（unpackaged 自包含，Windows App SDK 2.3.1）
    Localization/LanguageService  语言状态（settings + 系统侦测，auto 跟随系统，热切换）
  tests/launchpad.Core.Tests/  xUnit 单元 + ArchUnitNET 架构测试（只引用三个纯库，零 WinUI 依赖）
  tests/launchpad.IntegrationTests/ 契约测试（真实 spawn pwsh/cmd/wt）
  publish.ps1                  发布脚本（官方 CLI + config 模板 + XAML 资源补齐）
```

规则：
1. 依赖箭头永远向下：UI → UseCases → Core ← Infrastructure。
2. 端口接口定义在 Core/Ports，由 Infrastructure 实现，DI 注入。
3. 纯函数核心：模型不可变、列表操作返回新列表、plan 纯决策。
4. 测试项目绝不引用 WinUI 主项目（否则拉入 Windows 依赖）。
5. 零 shell 启动：`Process.ArgumentList`，禁止字符串拼接执行。
6. **架构约束机器执行**：`ArchitectureTests`（ArchUnitNET）断言 Core 零 WinUI/System.IO、UseCases 零 WinUI、依赖方向向下；`SourceFileRuleTests` 断言 UI 源码不引用 Infrastructure（App.xaml.cs 白名单）。
7. **编排逻辑不进 ViewModel**：确认策略、批量启动、历史合并、保存时机全部在 UseCases（ViewModel 只做绑定转发 + 状态栏赋值，保证可测）。
8. **预期失败用 ErrorOr**：写盘/启动失败返回结构化错误（`LaunchErrors`/`StoreErrors`），状态栏展示；异常仅用于编程错误。禁止 UnhandledException 兜底预期失败。
9. 依赖版本由 `Directory.Packages.props`（CPM）集中管理；恢复图锁在 `packages.lock.json`（入库，CI 缓存键）。

## 关键坑（踩过的，别再踩）

1. **`Application.RequestedTheme` 运行时不可变**（COMException 0x80131515）。主题切换必须在内容根元素上设 `FrameworkElement.RequestedTheme`。
2. **命名空间冲突**：`Launchpad.Application` 与 `Microsoft.UI.Xaml.Application` 冲突（CS0118）。应用层命名空间用 `Launchpad.UseCases`。
3. **DI 按具体类型解析会失败**：`GetRequiredService<ConcreteClass>()` 在只注册接口映射时不 work。Attach 类方法用 `(Concrete)_services.GetRequiredService<IInterface>()`。
4. **unpackaged 应用窗口句柄**：`WinRT.Interop.WindowNative.GetWindowHandle`；AppWindow 用 `Window.AppWindow` 属性（Windows App SDK 1.4+），不要手写 WindowId 互操作。
5. **unpackaged FolderPicker** 必须 `WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd)`，否则抛异常。
6. **Mica 是 Win11 专属**：Win10 用 `DesktopAcrylicBackdrop` 回退（`OperatingSystem.IsWindowsVersionAtLeast(10,0,22000)` 分支）。
7. **XAML stowed exception（退出码 0xC000027B）**：启动即崩时用 `Application.UnhandledException` 写日志文件定位。
8. **lucide 图标**：字体方案（assets/fonts/Lucide.ttf + FontIcon Glyph），码点从 lucide_icons Flutter 包源码提取。禁止引入其他图标库。
9. **配置目录不依赖 cwd**：从 `AppContext.BaseDirectory` 向上搜索含 `config/` 的祖先；`ConfigStore` 构造时 `CreateDirectory`（发布布局首次写盘不崩）。
10. **System.Text.Json**：读旧配置用 `PropertyNameCaseInsensitive=true` + `PropertyNamingPolicy.SnakeCaseLower`（写 snake_case 保持兼容）；record 相等性会被 JsonExtensionData 空字典破坏（测试用字段级断言）。
11. **x:Bind 在 DataTemplate 里访问外层**：用传统 `{Binding DataContext.X, ElementName=Root}` 或事件转发，x:Bind 默认绑定 item。
12. **cmd.exe 引号陷阱（契约测试捕获）**：cmd `/k` 不用标准 argv 引号规则（`\"` 是字面量），`cd /d "DIR" && ...` 在含引号/空格目录下整条命令不执行。目录一律经 `ProcessStartInfo.WorkingDirectory` 传递，`Args = ["/k", command]`。pwsh `-Command` 走标准 argv 解析（`\"` 有效），单引号转义 `'` → `''` 可用。
13. **`FirstOrDefault` 对值类型元组返回非空默认值**：`DangerousFlagDetector.DangerousReason` 曾用 `FirstOrDefault(...).Reason`，LanguageKey 是枚举，默认元组给 `(null, ToggleConfirm)` —— 安全命令被误标为危险。无匹配必须显式返回 null（显式循环）。
14. **枚举在条件表达式里需显式可空**：`condition ? LanguageKey.X : null` 编译报 CS0173，写 `(LanguageKey?)LanguageKey.X`。
13. **dotnet publish 不复制 XAML 编译产物**（xbf/pri 缺失 → 发布版启动即 XamlParseException）。publish.ps1 从 Release bin 复制 `*.xbf`、`launchpad.pri`、`Views/`、`Themes/`。
14. **窗口最小化坐标**：最小化窗口的 Position 是 -32000（离屏）。`OnClosed` 只保存最后一次 `OverlappedPresenterState.Restored` 的位置（`AppWindow.Changed` 追踪）；恢复前经 `WindowPosition.ClampToVisible` 纠偏。OverlappedPresenter 的枚举值是 `Restored` 不是 `Normal`。
15. **Process.Start 目录错误码**：目录不存在抛 `Win32Exception`，NativeErrorCode=267（ERROR_DIRECTORY）而非 3。TryLaunch 把 267/3 都映射为 WorkingDirectoryMissing。

## 约定

- 数据模型：`sealed record` + required/init，JSON 与旧 serde 格式字节兼容。
- 错误：配置解析失败抛 `ConfigParseException`（含路径），不静默丢弃；写盘失败 ErrorOr 返回（`Store.WriteFailed`）。
- 测试：单命令 `dotnet test tests/launchpad.Core.Tests/`（单元 + 架构）；契约 `dotnet test tests/launchpad.IntegrationTests/`。
- 每个修复必须有回归测试；行为对齐旧 Rust 版以测试断言为准，注释不得声称未验证的行为。
- 发布：`powershell -ExecutionPolicy Bypass -File publish.ps1`，产物在 `launchpad/publish/`。

## 国际化（i18n，2026-08-01 起）

- **语言状态**：settings.json `language` 字段（`"auto"` / `"zh-CN"` / `"en-US"`，缺失默认 `"auto"`）。`auto` 跟随系统：`GlobalizationPreferences.Languages[0]` 以 `zh*` 前缀判定中文，其余回退英文（`Translations.FromSystemLanguage`）。
- **三层优先级**：用户显式设置 > auto 跟随系统 > 英文兜底。顶栏语言按钮循环 auto → zh-CN → en-US。
- **纯语言表**：`Core/Localization/Translations`（zh/en 两个字典 + `Resolve`/`Effective`/`T`/`Format` 纯函数）；键枚举 `LanguageKey`。新文案 = 新键 + 两语言值 + TranslationsTests 完整性断言自动覆盖。
- **可翻译文案一律键引用**：XAML 绑定 ViewModel 文案属性（如 `NewButtonText`）或 `LanguageKeyTextConverter`（LanguageKey → 当前语言文案，经 `LanguageService.Instance`）；对话框（DialogService/EditDialog）显示时经注入的 LanguageService 翻译。
- **热切换**：LanguageService 变更触发全量 `OnPropertyChanged(string.Empty)` + `RefreshItems()`（卡片重建使 converter 重新求值）。模态对话框在打开期间语言固定（语言按钮在主界面，模态下不可切换）。
- **领域层语义化**：`DangerousFlagDetector.DangerousReason`、`ItemValidator` 错误、`ConfigStore.LastRecoveryNoteKey` 均返回 `LanguageKey?`（不返回文案）。
- **ErrorOr 错误描述保持英文内部诊断**（含路径/异常等技术细节，属诊断信息），状态栏前缀本地化（如"启动失败：{desc}"）。这是有意决策，不是遗漏。
- 品牌名 "WT Launcher"（窗口标题、主页标题）不翻译。
