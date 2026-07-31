# Implement: 迁移执行计划

学习路线映射：每阶段 = 一课，一提交。验证命令统一：`dotnet build`、`dotnet test`、启动冒烟。

## 阶段 A：环境验证（第 0 课：工具链）

1. WebSearch 确认 Windows App SDK 最新稳定版（1.7.x / 2.x）与 .NET 10 兼容性，锁定版本。
2. 用模板创建一个最小 WinUI 3 unpackaged 工程，`dotnet build` + `dotnet run` 冒烟（验证无 VS IDE 命令行构建可行，验证 Windows SDK 工作负载在位）。
3. 验证 Mica/Acrylic 在 Win10 19044 的实际可用性，确定 §5.1 回退链。
   - 验证命令：`dotnet build` / `dotnet run`
   - 退出条件：最小工程可构建可运行

## 阶段 B：工程骨架 + 数据层（第 1 课：XAML + 数据模型）

1. 建 `launchpad/src/launchpad` 工程（unpackaged，WindowsAppSDK，D6）。
2. 主窗口 + 双主题资源字典 + Mica/Acrylic 回退（§5.1）。
3. `Core/Models`：LaunchItem / AppSettings / WindowState / LaunchPlan 移植（§3）。
4. JsonModels + 序列化往返：字段映射、confirm 默认 true、null 省略。
5. 读实际 `settings.json` 取证字段名，定 converter 方案（§6）。
   - 验证：`dotnet build` + 模型序列化往返单测

## 阶段 C：六边形分层（第 2-3 课：领域 + 端口）

1. 领域层（纯函数）：`DangerousFlagDetector`（flag 表 1:1）、`LaunchPlanner`（plan_windows 1:1 + 单引号转义修复）、`ItemValidator`。
2. 端口接口：`IConfigStore` / `IProcessSpawner` / `ITerminalDetector` / `IDirectoryPicker` / `IWindowService`。
3. 基础设施实现：`ConfigStore`（含 .bak 备份、类型化解析错误）、`ProcessSpawner`（ArgumentList 零 shell）、`TerminalDetector`。
4. 应用层：`ItemUseCase`（CRUD/排序/搜索/批量）、`LaunchUseCase`（确认判定 + 启动 + 历史）、`SettingsUseCase`。
   - 验证：领域层 xUnit 全覆盖；`LaunchUseCaseTests` 用 FakeSpawner 断言 argv 精确值

## 阶段 D：MVVM + DI + 视图（第 4 课：MVVM）

1. DI 容器（App.xaml.cs）+ ViewModel 注入链。
2. `HomeViewModel`：列表状态、搜索过滤、选择、批量、统计、状态栏。
3. `HomeView`：卡片网格（WinUI ItemsRepeater/GridView）、空状态、统计卡、搜索框、主题切换、Confirm 开关。
4. `EditDialog` + `EditViewModel`：表单校验、目录存在性验证、危险实时警告、文件夹选择器（InitializeWithWindow）。
5. `LucideIcons.cs`：14 个 PathIcon 常量（§5.2）；颜色 token 对齐 theme.dart（§PRD-5）。
   - 验证：启动冒烟 + 手动功能走查（对照 Flutter 版逐项）

## 阶段 E：稳定性 + 收尾（第 5 课：稳定性 + 归档）

1. `SingleInstance`（Mutex + 激活已有窗口，§5.4）+ `WindowStateService`（§5.5）。
2. 全功能对比清单逐项验证（PRD 10 条），深/浅主题截图存档。
3. 归档：`git mv launchpad_flutter/ launchpad-rs/ → archive/`（D3）。
4. 更新根 `CLAUDE.md`（WinUI 3 架构）、`_README.md`、AGENTS.md（如存在架构描述）。
5. 根 `config/` 保留原位（D2 语义：从项目根运行）。
   - 验证：`dotnet build --release` + `dotnet test` + 全功能走查 + 双实例测试 + 窗口状态恢复测试

## 验证命令

```bash
cd launchpad
dotnet build --release
dotnet test                      # 单命令跑全部
dotnet run                       # 冒烟
```

## 回滚点

- 每阶段结束一个提交（`feat: winui3 <阶段名>`）；任意阶段失败可 `git checkout` 回上一提交。
- 归档用 `git mv`（历史保留，可逆）。
- 阶段 C 完成后（六边形 + 测试就位）是主要回滚决策点：此前任何问题都可低成本放弃，改回 Flutter 版。

## 验收对照

PRD Acceptance Criteria 10 项逐项打勾；每项有验证命令或截图存档。
