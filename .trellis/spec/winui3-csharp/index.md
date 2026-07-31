# WinUI 3 + C# 规范（在役架构）

> 2026-07-31 起在役。取代 C++/Slint 与 Flutter/FRB 时代的全部包规范（cpp-core、slint-ui、cmake-build 等仅作历史参考）。

## 架构

```
launchpad/
  src/launchpad.Core/          领域层：纯函数（net10.0 类库，零 WinUI/IO 依赖）
    Models/    LaunchItem, AppSettings, WindowState, LaunchPlan（不可变 record）
    Domain/    DangerousFlagDetector, LaunchPlanner, ItemValidator
    Ports/     所有端口接口（IConfigStore 等）
  src/launchpad.UseCases/      应用层：ItemUseCase, LaunchUseCase, SettingsUseCase
  src/launchpad.Infrastructure/ 基础设施：ConfigStore, ProcessSpawner, TerminalDetector
  src/launchpad/               WinUI 3 外壳（unpackaged 自包含，Windows App SDK 2.2.0）
  tests/launchpad.Core.Tests/  xUnit（只引用三个纯库，零 WinUI 依赖）
```

规则：
1. 依赖箭头永远向下：UI → UseCases → Core ← Infrastructure。
2. 端口接口定义在 Core/Ports，由 Infrastructure 实现，DI 注入。
3. 纯函数核心：模型不可变、列表操作返回新列表、plan 纯决策。
4. 测试项目绝不引用 WinUI 主项目（否则拉入 Windows 依赖）。
5. 零 shell 启动：`Process.ArgumentList`，禁止字符串拼接执行。

## 关键坑（踩过的，别再踩）

1. **`Application.RequestedTheme` 运行时不可变**（COMException 0x80131515）。主题切换必须在内容根元素上设 `FrameworkElement.RequestedTheme`。
2. **命名空间冲突**：`Launchpad.Application` 与 `Microsoft.UI.Xaml.Application` 冲突（CS0118）。应用层命名空间用 `Launchpad.UseCases`。
3. **DI 按具体类型解析会失败**：`GetRequiredService<ConcreteClass>()` 在只注册接口映射时不 work。Attach 类方法用 `(Concrete)_services.GetRequiredService<IInterface>()`。
4. **unpackaged 应用窗口句柄**：`WinRT.Interop.WindowNative.GetWindowHandle`；AppWindow 用 `Window.AppWindow` 属性（Windows App SDK 1.4+），不要手写 WindowId 互操作。
5. **unpackaged FolderPicker** 必须 `WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd)`，否则抛异常。
6. **Mica 是 Win11 专属**：Win10 用 `DesktopAcrylicBackdrop` 回退（`OperatingSystem.IsWindowsVersionAtLeast(10,0,22000)` 分支）。
7. **XAML stowed exception（退出码 0xC000027B）**：启动即崩时用 `Application.UnhandledException` 写日志文件定位（本次迁移用它找到了 DI 和主题两个崩溃点）。
8. **lucide 图标**：字体方案（assets/fonts/Lucide.ttf + FontIcon Glyph），码点从 lucide_icons Flutter 包源码提取（本机 pub cache）。禁止引入其他图标库。
9. **配置目录不依赖 cwd**：从 `AppContext.BaseDirectory` 向上搜索含 `config/` 的祖先（开发布局命中项目根）。
10. **System.Text.Json**：读旧配置用 `PropertyNameCaseInsensitive=true` + `PropertyNamingPolicy.SnakeCaseLower`（写 snake_case 保持兼容）；record 相等性会被 JsonExtensionData 空字典破坏（测试用字段级断言）。
11. **x:Bind 在 DataTemplate 里访问外层**：用传统 `{Binding DataContext.X, ElementName=Root}` 或事件转发，x:Bind 默认绑定 item。
12. **构建**：WinUI 3 命令行构建可行（VS Build Tools + Windows SDK 26100 + `dotnet build`）；csproj 需 `WindowsPackageType=None`（unpackaged）+ `WindowsAppSDKSelfContained=true`。

## 约定

- 数据模型：`sealed record` + required/init，JSON 与旧 serde 格式字节兼容。
- 错误：配置解析失败抛 `ConfigParseException`（含路径），不静默丢弃。
- 测试：单命令 `dotnet test tests/launchpad.Core.Tests/launchpad.Core.Tests.csproj`。
- 每阶段一个提交（本次迁移 5 个：A 骨架 → B 数据层 → C 六边形 → D MVVM → E 稳定性+归档）。
