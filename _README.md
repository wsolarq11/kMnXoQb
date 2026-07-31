# WT Launcher

Windows 原生终端启动器。管理启动项列表，一键在终端中运行用户定义的命令。WinUI 3 + C#（.NET 10 LTS），六边形架构。

## 功能

- 启动项管理：可视化新增、编辑、删除、排序、多选、搜索过滤（name/directory/command 子串，不区分大小写）
- 卡片列表：搜索过滤、hover 操作按钮、危险命令红色标记
- 批量启动：多选后一键拉起多个终端，逐项确认
- 启动前确认：全局开关 + per-item confirm + 危险命令触发弹窗确认
- 危险命令检测：`dangerously` / `yolo` / `skip-permissions` / `bypass-approvals` / `bypass-sandbox` / `bypass.sandbox`，不区分大小写子串匹配
- 主题切换：深/浅双主题手动切换（Mica / Win10 Acrylic 回退），持久化到 settings.json
- 动态状态栏：显示最近启动项或 Ready
- Ctrl+A 全选/取消全选快捷键
- 配置持久化：选中状态、确认设置、主题偏好跨重启保留
- 零 shell 注入：所有子进程通过 `Process.ArgumentList` 以 argv 数组启动，无字符串拼接执行
- 目录存在性校验、运行时终端检测（wt.exe → pwsh.exe → cmd.exe）
- 单实例锁 + 窗口位置/大小恢复

## 前置要求

- **.NET 10 LTS SDK**（本机 10.0.301）
- **Windows 10 1809+**（应用目标平台 min 17763；主题材质需 Win11 Mica / Win10 Acrylic 回退）
- **Windows App SDK 2.x**（unpackaged 自包含部署，构建时自动还原）

## 构建与运行

```bash
cd launchpad
dotnet build src/launchpad/launchpad.csproj     # 构建
dotnet test tests/launchpad.Core.Tests/         # 单元 + 架构测试
dotnet run --project src/launchpad              # 运行
```

## 目录结构

```
launchpad/
├── src/
│   ├── launchpad.Core/          领域层：纯函数（Models/Domain/Ports/Serialization）
│   ├── launchpad.UseCases/      应用层：用例编排（ItemUseCase/LaunchUseCase/SettingsUseCase）
│   ├── launchpad.Infrastructure/ 基础设施：ConfigStore/ProcessSpawner/TerminalDetector
│   └── launchpad/               WinUI 3 外壳（Views/ViewModels/Themes/Infrastructure）
├── tests/
│   └── launchpad.Core.Tests/    xUnit（60 用例，只引用三个纯库）
├── config/                      配置目录（config.json 启动项 / settings.json 偏好）
└── archive/                     退役版本归档（Flutter/Rust/HTA 时代快照）
```

## 架构

```
src/launchpad.Core/   (零 I/O/UI 依赖)   纯决策：模型 + 校验 + plan + 危险检测
    ↑
src/launchpad.UseCases/                 用例编排（依赖端口接口）
    ↑
src/launchpad/ + src/launchpad.Infrastructure/   WinUI 3 外壳 + 端口实现
```

**关键架构决策**：
- **依赖方向**：UI → UseCases → Core ← Infrastructure；端口接口定义在 Core/Ports，DI 容器注入
- **零 shell 执行**：`Process.ArgumentList` argv 数组，禁止字符串拼接执行
- **纯函数核心**：模型不可变 record、`LaunchPlanner.PlanWindows` 纯决策、`DangerousFlagDetector` 纯检测
- **可测试性**：领域层全覆盖、应用层 fakes 断言 argv、配置序列化兼容性测试
- **配置兼容**：config.json/settings.json 与旧版格式字节兼容（snake_case，confirm 缺失默认 true，写前备份 .bak）

## 配置

- `config/config.json`：启动项数组（name/directory/command/confirm/terminal）
- `config/settings.json`：偏好（confirmEnabled/theme/launchHistory/windowState），运行时自动读写
- 配置目录：从 exe 位置向上搜索含 `config/` 的祖先目录（开发布局为项目根 config/）
- 首次使用：复制 `config/config.example.json` 为 `config/config.json` 并修改

## 安全边界

- 启动前校验目录是否存在、命令是否为空
- 零 shell 执行：所有子进程通过 argv 数组传递参数
- 确认逻辑：全局开关 `confirmEnabled` &&（item.confirm || 危险命令）
- 危险命令检测在编辑框/卡片/确认对话框三处展示
- 单实例 Mutex，第二实例激活已有窗口后退出

## 技术栈

- .NET 10 LTS + C# + WinUI 3（Windows App SDK 2.3.1，unpackaged 自包含）
- CommunityToolkit.Mvvm（MVVM 源生成器）+ Microsoft.Extensions.DependencyInjection
- System.Text.Json（snake_case 序列化）+ xUnit（测试）
- lucide 图标字体（Assets/Fonts/Lucide.ttf）
