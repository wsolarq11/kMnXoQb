# launchpad — Project Guide

## Overview

Windows 原生终端启动器（WT Launcher）：管理启动项列表，一键在终端中运行用户定义的命令。WinUI 3 + C#（.NET 10 LTS），六边形架构。

## Architecture

```
launchpad/
  src/launchpad.Core/         领域层：纯函数，零 I/O/UI 依赖（net10.0 类库）
    Models/    LaunchItem, AppSettings, WindowState, LaunchPlan
    Domain/    DangerousFlagDetector, LaunchPlanner, ItemValidator
    Ports/     IConfigStore, IProcessSpawner, ITerminalDetector,
               IDirectoryPicker, IDirectoryChecker, IDialogService, IWindowService
    Serialization/  LauncherJson（共享序列化选项）
  src/launchpad.UseCases/     应用层：用例编排（ItemUseCase, LaunchUseCase, SettingsUseCase）
  src/launchpad.Infrastructure/ 基础设施：ConfigStore, ProcessSpawner, TerminalDetector
  src/launchpad/              WinUI 3 外壳（unpackaged 自包含）
    Views/       HomeView, EditDialog
    ViewModels/  HomeViewModel, EditViewModel（CommunityToolkit.Mvvm）
    Infrastructure/  DialogService, DirectoryPickerService, SingleInstance,
                     WindowStateService（WinUI 专属端口实现）
    Themes/      Colors.xaml（双主题 token + 系统材质回退）
    Assets/Fonts/Lucide.ttf   lucide 图标字体
  tests/launchpad.Core.Tests/ xUnit（只引用三个纯库，零 WinUI 依赖）
```

- **依赖箭头永远向下**：UI → UseCases → Core ← Infrastructure。端口接口定义在 Core/Ports，由 Infrastructure 实现，DI 容器（Microsoft.Extensions.DependencyInjection）注入。
- **纯函数核心**：模型不可变 record、列表操作返回新列表、`LaunchPlanner.PlanWindows` 纯决策、`DangerousFlagDetector` 纯检测。
- **零 shell 启动**：`Process.ArgumentList` argv 数组，禁止字符串拼接执行。
- **配置兼容**：config.json/settings.json 与旧 Rust/serde 格式字节兼容（snake_case 键、confirm 缺失默认 true、写前备份 .bak）。

## Quick Reference

```bash
cd launchpad
dotnet build src/launchpad/launchpad.csproj     # 构建
dotnet test tests/launchpad.Core.Tests/         # 58 个测试
dotnet run --project src/launchpad              # 运行
```

## Key Dependencies

| 依赖 | 用途 |
|------|------|
| WinUI 3（Windows App SDK 2.2.0） | 原生 UI，unpackaged 自包含部署 |
| CommunityToolkit.Mvvm | MVVM 源生成器（ObservableObject/RelayCommand） |
| Microsoft.Extensions.DependencyInjection | DI 容器 |
| System.Text.Json | 序列化（snake_case 命名策略） |
| xUnit | 测试 |

## 关键行为（与旧版 1:1 对齐）

- 终端优先序：wt.exe → pwsh.exe → cmd.exe（`LaunchPlanner.PlanWindows`，pwsh/cmd fallback 已修复目录单引号转义）。
- 危险检测：6 个 flag 子串匹配（dangerously/yolo/skip-permissions/bypass-approvals/bypass-sandbox/bypass.sandbox），三处展示（编辑框/卡片/确认框）。
- 确认逻辑：全局 Confirm 开关 &&（item.confirm || 危险）。
- 配置目录：从 exe 位置向上搜索含 `config/` 的祖先（开发布局为项目根 config/）。
- 主题：Mica（Win11）/ Acrylic（Win10 19044 回退），深/浅切换走内容根 RequestedTheme。
- 单实例：命名 Mutex，第二实例激活已有窗口后退出。
- 窗口状态：关闭时写入 settings.json window_state，启动时恢复。

## Testing

```bash
dotnet test tests/launchpad.Core.Tests/launchpad.Core.Tests.csproj   # 单命令全部
```

## Maintenance

本文件必须随项目演进更新。更新时机：模块增删、依赖变化、架构调整、关键行为变化。

## 历史

- 2026-07-31：C++ → egui → Flutter+FRB 之后第四次重写，迁移到 WinUI 3 + C#。旧代码归档于 `archive/launchpad_flutter/` 与 `archive/launchpad-rs/`。
