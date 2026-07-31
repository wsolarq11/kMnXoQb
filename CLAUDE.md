# launchpad — Project Guide

## Overview

Windows 原生终端启动器（WT Launcher）：管理启动项列表，一键在终端中运行用户定义的命令。WinUI 3 + C#（.NET 10 LTS），六边形架构。

## Architecture

```
launchpad/
  src/launchpad.Core/         领域层：纯函数，零 I/O/UI 依赖（net10.0 类库）
    Models/    LaunchItem, AppSettings, WindowState, LaunchPlan
    Domain/    DangerousFlagDetector, LaunchPlanner, ItemValidator, WindowPosition
    Ports/     IConfigStore, IProcessSpawner, ITerminalDetector,
               IDirectoryPicker, IDirectoryChecker, IDialogService, IWindowService
    Serialization/  LauncherJson（共享序列化选项）
  src/launchpad.UseCases/     应用层：用例编排（ItemUseCase, LaunchUseCase, SettingsUseCase），
                              ErrorOr 结构化错误（预期失败显式返回，异常保留给编程错误）
  src/launchpad.Infrastructure/ 基础设施：ConfigStore, ProcessSpawner, TerminalDetector
  src/launchpad/              WinUI 3 外壳（unpackaged 自包含）
    Views/       HomeView, EditDialog
    ViewModels/  HomeViewModel, EditViewModel（CommunityToolkit.Mvvm，绑定转发薄壳）
    Infrastructure/  DialogService, DirectoryPickerService, SingleInstance,
                     WindowStateService（WinUI 专属端口实现）
    Themes/      Colors.xaml（双主题 token + 系统材质回退）
    Assets/Fonts/Lucide.ttf   lucide 图标字体
  tests/launchpad.Core.Tests/ xUnit 单元测试 + ArchUnitNET 架构测试（只引用三个纯库，零 WinUI 依赖）
  tests/launchpad.IntegrationTests/ 契约测试（真实 spawn pwsh/cmd/wt，验证 argv 转义与目录语义）
  publish.ps1                 官方 CLI 发布脚本（unpackaged 自包含 + config 模板 + XAML 资源补齐）
```

- **依赖箭头永远向下**：UI → UseCases → Core ← Infrastructure。端口接口定义在 Core/Ports，由 Infrastructure 实现，DI 容器（Microsoft.Extensions.DependencyInjection）注入。该约束由 ArchUnitNET 架构测试机器执行（Core 零 WinUI/System.IO、UseCases 零 WinUI、UI 源码不引用 Infrastructure）。
- **纯函数核心**：模型不可变 record、列表操作返回新列表、`LaunchPlanner.PlanWindows` 纯决策、`DangerousFlagDetector` 纯检测、`WindowPosition.ClampToVisible` 纯纠偏。
- **零 shell 启动**：`Process.ArgumentList` argv 数组，禁止字符串拼接执行。cmd.exe 不认标准 argv 引号转义（`\"`），目录一律经 `WorkingDirectory` 传递，不在命令串里 cd。
- **显式错误通道**：预期失败（写盘、启动失败）经 ErrorOr 返回结构化错误并由状态栏展示；异常仅用于编程错误。禁止用 UnhandledException 兜底预期失败。
- **配置兼容**：config.json/settings.json 与旧 Rust/serde 格式字节兼容（snake_case 键、confirm 缺失默认 true、写前备份 .bak）。ConfigStore 构造时确保目录存在。

## Quick Reference

```bash
cd launchpad
dotnet build src/launchpad/launchpad.csproj     # 构建
dotnet test tests/launchpad.Core.Tests/         # 单元 + 架构测试（89 个）
dotnet test tests/launchpad.IntegrationTests/   # 契约测试（pwsh/cmd 必跑，wt 本机）
dotnet run --project src/launchpad              # 运行
powershell -ExecutionPolicy Bypass -File publish.ps1   # 发布（输出 launchpad/publish/）
```

## Key Dependencies

| 依赖 | 用途 |
|------|------|
| WinUI 3（Windows App SDK 2.3.1） | 原生 UI，unpackaged 自包含部署 |
| CommunityToolkit.Mvvm | MVVM 源生成器（ObservableObject/RelayCommand） |
| Microsoft.Extensions.DependencyInjection | DI 容器 |
| ErrorOr | 结构化错误（UseCases 层） |
| TngTech.ArchUnitNET | 架构规则测试 |
| System.Text.Json | 序列化（snake_case 命名策略） |
| xUnit | 测试 |

依赖版本由 `Directory.Packages.props` 集中管理（CPM），恢复图锁定于 `packages.lock.json`（入库，CI 缓存键）。

## 关键行为（与旧版 1:1 对齐，差异处注明）

- 终端优先序：wt.exe → pwsh.exe → cmd.exe（`LaunchPlanner.PlanWindows`）。pwsh fallback 目录单引号转义（`'` → `''`）；cmd fallback 目录经 WorkingDirectory 传递（**与旧版差异**：cmd 不认 `\"` 转义，cd 拼接在含引号/空格目录下失效，契约测试验证）。
- 危险检测：6 个 flag 子串匹配（dangerously/yolo/skip-permissions/bypass-approvals/bypass-sandbox/bypass.sandbox），三处展示（编辑框/卡片/确认框）。
- 确认逻辑：全局 Confirm 开关 &&（item.confirm || 危险）。批量启动同样走确认（`LaunchUseCase.RequireConfirm` + 批量确认对话框）。
- 配置目录：从 exe 位置向上搜索含 `config/` 的祖先（开发布局为项目根 config/，发布布局为 publish/config/）。
- 主题：Mica（Win11）/ Acrylic（Win10 19044 回退），深/浅切换走内容根 RequestedTheme。
- 单实例：命名 Mutex，第二实例激活已有窗口后退出。
- 窗口状态：关闭时仅保存最后一次正常（非最小化）位置，最小化关闭不写离屏坐标；启动恢复前经 `WindowPosition.ClampToVisible` 纠偏到虚拟屏。
- 启动历史：同名去重后前插、上限 10 条（与旧版一致）；批量启动按成功项合并入历史。
- Id 生成：小写化 + 空格转下划线 + 冲突追加 `_2/_3` 后缀（与旧版一致）；编辑时保留原 Id。

## Testing

```bash
dotnet test tests/launchpad.Core.Tests/launchpad.Core.Tests.csproj   # 单元 + 架构，单命令全部
dotnet test tests/launchpad.IntegrationTests/                       # 契约（真实进程，勿在无网络/无 pwsh 环境跑）
```

## Maintenance

本文件必须随项目演进更新。更新时机：模块增删、依赖变化、架构调整、关键行为变化。

## 历史

- 2026-07-31：C++ → egui → Flutter+FRB 之后第四次重写，迁移到 WinUI 3 + C#。旧代码归档于 `archive/launchpad_flutter/` 与 `archive/launchpad-rs/`。
- 2026-07-31（同日）：全图实施（审查发现修复）——CI 重写为 dotnet、文档/spec 全面更新为 C# 时代、删除 C++/HTA 残留、依赖升级（WindowsAppSDK 2.3.1、Test.Sdk 18、BuildTools 28000）、settings.json 停止跟踪。经三轮独立质检 PASS（实战运行验证）。
- 2026-07-31（同日，第二轮全图实施）：止血修复（窗口状态/批量确认/存储建目录/legacy 对齐）+ 架构固化（编排下沉 UseCases、ErrorOr 显式错误、ArchUnitNET 架构测试、卡片危险标记）+ 构建治理（CPM、packages.lock.json、CI 缓存与供应链审计、publish.ps1）+ 契约测试（捕获并修复 cmd fallback 引号陷阱）+ spec/文档治理。测试 52 → 106（89 单元/架构 + 17 契约，wt 分支本机）。阶段 2 提案（核心下沉 Rust + P/Invoke）经评估无业务价值，保持全 C#（KISS），提案见 `.trellis/tasks/archive/2026-07/07-31-execute-full-roadmap/phase2-proposal.md`。
