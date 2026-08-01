# 全图实施 — 技术设计

## 架构边界

依赖方向不变：UI → UseCases → Core ← Infrastructure。所有改动遵循 Functional Core, Imperative Shell：纯逻辑进 Core/UseCases（可单测），I/O 与 UI 交互进外壳。

## 改动域

| 域 | 文件 | 说明 |
|----|------|------|
| XAML 绑定 | `src/launchpad/Views/HomeView.xaml`、`EditDialog.xaml` | 根加 x:DefaultBindMode；模板绑定显式 OneTime |
| 勾选事件 | `src/launchpad/Views/HomeView.xaml(.cs)`、`ViewModels/HomeViewModel.cs`、`Core/Domain/ItemUseCase` | Click 事件 + Id 幂等 |
| 行为恢复 | `ViewModels/HomeViewModel.cs`（批量清选）、`Infrastructure/DialogService.cs`+`Ports/IDialogService.cs`（删除确认）、`HomeViewModel.cs`（搜索空态） | 对齐旧版 Rust 语义 |
| 行为契约 | `tests/launchpad.Core.Tests/` | Verify 快照套件 |
| config 韧性 | `UseCases/ItemUseCase.cs`、`App.xaml.cs`、`ConfigStore.cs` | .bak 自动恢复 + 错误提示 + 日志保护 |
| 构建治理 | `Core/Serialization/LauncherJson.cs`、`Directory.Build.props`（新建）、`publish.ps1` | JsonSerializerContext + UseArtifactsOutput |
| 启动加固 | `Infrastructure/TerminalDetector.cs`、`UseCases/LaunchUseCase.cs` | 探测缓存 + 错误归类 |

## 关键契约（跨子任务）

1. **P0.1 配方**（实证验证过）：根节点 `x:DefaultBindMode="OneWay"`；模板内所有绑定（卡片 11 处）显式 `Mode=OneTime`（不可变 record 按实例求值，语义等价且零 WMC1506 噪音）。页面级绑定（HomeViewModel 是 INPC）自动 OneWay，无警告。
2. **P0.2 配方**（WinUI 源码实证）：`Click` 仅用户输入触发；处理器内同步捕获目标状态 `IsChecked == true`，Defer 后按 **Id**（而非引用）解析索引，命令语义为「设置目标状态」而非「翻转」。
3. **P1.1 行为契约**：旧版语义（archive/launchpad-rs/src/app.rs 实证）：批量启动后 `selected=false`；卡片 Del 弹确认框（"This cannot be undone"）；空态只在全列表为空时显示（搜索无结果显示「无匹配」）。
4. **P1.2 恢复链**：`ReadItems` 抛 ConfigParseException → ItemUseCase.LoadItems 捕获 → 尝试 `config.json.bak` 读取 → 成功则写回 config.json 并返回（状态栏提示已恢复）→ 失败返回错误（UI 显示，不崩）。
5. **P1.3**：`JsonSerializerContext` 保持 snake_case + 未知字段保留语义（JsonRoundTripTests 锁定）；`UseArtifactsOutput` 后 `publish.ps1` binOutput 改为 `artifacts/bin/launchpad/release/` 布局（实际路径以构建后为准，实施时先构建再核对）。
6. **P1.4**：TerminalDetector 结果按进程生命周期缓存（单例，Key=name）；TryLaunch 分类前先 `Directory.Exists(plan.WorkingDirectory)` 排除工作目录因素再判 PathNotFound 归属。

## 兼容性

- settings.json/config.json 字节兼容不变（除 window_state 外无 schema 变化；P1.1 的批量清选只改内存状态 + 正常持久化）。
- 发布布局：publish.ps1 产物结构不变（xbf/pri 拷贝路径同步 artifacts 布局）。
- CI：无需改动（dotnet 命令通用；UseArtifactsOutput 后缓存路径优化为 P2 项）。

## 回滚

每子任务独立提交（git log 单点回滚）；P0.1/P0.2 是纯增量（XAML/事件改动，行为可逆）。
