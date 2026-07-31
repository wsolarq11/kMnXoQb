# Design — 止血修复

## A2 窗口状态（纯函数化）

- Core 新增 `Domain/WindowPosition.cs`（纯函数）：
  - `public static WindowState ClampToVisible(WindowState state, int virtualWidth, int virtualHeight, int minVisible = 100)`
    - 若 `X/Y` 导致窗口主体在虚拟屏外（含 -32000 最小化坐标），重置为默认（100, 100）
    - 若 `Width/Height` 过小（< 200x100 或 0），重置默认 800x600
  - 默认常量与 `WindowState` 模型默认一致
- `MainWindow.RestoreWindowState`：先 `ClampToVisible`（虚拟屏用 `SystemParameters.VirtualScreenWidth/Height`）再 `MoveAndResize`
- `MainWindow.OnClosed`：保存前检查 `OverlappedPresenter.State == OverlappedPresenterState.Minimized` → 不写坏坐标（跳过保存或写上次正常状态——选跳过保存，保持旧值）
  - 注意：`OnClosed` 中 `_appWindow.Presenter` 在关闭瞬间状态可能已不可靠；保守方案：`Window.Activated`/`SizeChanged` 追踪最后一次"正常状态"的位置缓存，OnClosed 用缓存值保存。实现：MainWindow 维护 `_lastNormalRect`，在 `AppWindow.Changed` 事件（Presenter 状态）里更新；OnClosed 保存 `_lastNormalRect ?? 当前值`
  - 更简单方案：OnClosed 时检查 `_appWindow.Presenter is OverlappedPresenter p && p.State != Minimized`；若最小化则用缓存。缓存由 `AppWindow.Changed`（或 `PositionChanged/SizeChanged`）持续更新，仅当 `OverlappedPresenter.State == Normal` 时记录

## A1 批量启动确认

- `LaunchUseCase` 新增：
  - `public IReadOnlyList<LaunchItem> RequireConfirm(AppSettings settings, IReadOnlyList<LaunchItem> items)` → 返回需要确认的子集（纯函数，复用 NeedsConfirm）
  - `public void LaunchMany(IReadOnlyList<LaunchItem> items)` → 批量 spawn + 批量 PushHistory（合并 history 更新）
- `HomeViewModel.LaunchSelected` 改为：
  1. `confirmItems = _launchUseCase.RequireConfirm(_settings, selected)`
  2. 若非空 → `_dialogs.ConfirmBatchAsync(confirmItems)`（DialogService 新增批量确认对话框，显示每项命令）
  3. 确认通过或无需确认 → `_launchUseCase.LaunchMany(selected)` → `_settings = PushHistory` 合并
- 状态栏文案保留现有格式（Launched N / N of M failed）

## C1 ConfigStore 建目录

- `ConfigStore` 构造函数 `configDir` 处：`Directory.CreateDirectory(configDir)`（幂等，无副作用成本）
- 保留现有读缺失返回默认值行为

## A3 PushHistory 去重

```csharp
public static List<string> PushHistory(List<string> history, string name, int max = 10)
{
    var deduped = history.Where(h => h != name).ToList();   // 移除旧同名
    deduped.Insert(0, name);
    return deduped.Take(max).ToList();
}
```
- 与旧 Rust 版 `retain(|h| h != name)` → `insert(0)` → `truncate(10)` 一致
- 更新注释（删除"no deduplication"错误声称）

## A4 Id 生成

```csharp
public static string GenerateId(IReadOnlyList<LaunchItem> items, string name)
{
    var baseId = name.Trim().ToLowerInvariant().Replace(' ', '_');
    if (!items.Any(i => i.Id == baseId)) return baseId;
    for (var n = 2; ; n++)
    {
        var candidate = $"{baseId}_{n}";
        if (!items.Any(i => i.Id == candidate)) return candidate;
    }
}
```
- `NewItem` 签名不变（`NewItem(string name, string directory, string command, bool confirm, string? terminal)` 无 items 参数），改为 `NewItem(..., IReadOnlyList<LaunchItem> existing)` 传入集合用于冲突检测；或拆 `NewItem` 纯生成 + 调用方检测
  - 决策：`NewItem` 增加 `existing` 参数（调用方 EditViewModel/HomeViewModel 传入当前 `_all`）。编辑路径保留 `_originalId`（稳定标识，行为优于旧版，PRD 已声明）
- 注意：已有数据 id 与 name 相同且无空格，小写化不影响存量

## 测试计划

- `WindowPositionTests`：最小化坐标、越界坐标、过小尺寸 → 默认值；合法状态不变
- `LaunchUseCaseTests` 扩展：RequireConfirm 危险/confirm/全局关；LaunchMany 批量 spawn + history 合并去重
- `ConfigStoreTests` 扩展：构造后目录已创建（嵌套路径）
- `LaunchUseCaseTests`：PushHistory 去重（同名在中间/头部/重复多次）
- `ItemUseCaseTests` 扩展：GenerateId 冲突后缀、小写化、空格转下划线
