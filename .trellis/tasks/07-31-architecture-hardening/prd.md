# PRD — 架构固化：编排下沉 + ErrorOr + 架构测试

## 背景

审查确认的种子问题：

1. **S1 编排在不可测层**：确认策略、保存时机、批量启动逻辑全部住在 `HomeViewModel`（WinUI 项目），测试项目无法引用 → 批量确认绕过 bug 无测试。微软官方 MVVM 教程推荐 ViewModels/services 放独立类库提升可测性。
2. **S2 错误通道是"吞掉"**：`TryLaunch` 返回 `string?`（ex.Message 字符串），`App.OnUnhandledException` 把一切异常 `Handled = true` → 保存失败静默。
3. **S4 架构约束只在文档**：CLAUDE.md 声称依赖方向"UI → UseCases → Core ← Infrastructure"，无机器执行。
4. **B2 危险标记缺失**：CLAUDE.md 声称"三处展示（编辑框/卡片/确认框）"，实际卡片没有危险标记。

## 需求

- [ ] S1：`HomeViewModel` 编排逻辑下沉 `LaunchUseCase`/`ItemUseCase`/`SettingsUseCase`；ViewModel 变绑定薄壳；所有启动路径（单卡/批量）确认决策单一入口
- [ ] S2：预期失败改显式 Result 通道——`ConfigStore` 读写返回 `ErrorOr<T>`（I/O、解析失败）；`TryLaunch` 返回结构化错误；`UnhandledException` 仅兜底编程错误（保留日志，不再作为预期失败路径）
- [ ] S4：ArchUnitNET 架构测试（xUnit）：Core 零 WinUI 依赖、Core 零 System.IO、UseCases 零 WinUI、UI 层（launchpad 项目）不直接调用 Infrastructure 类型（除 DI 注册与 Attach 例外）
- [ ] B2：卡片增加危险标记（DangerBrush 图标 + 危险原因），编辑框/确认框保留

## 验收标准

- [ ] 架构测试至少 5 条规则，`dotnet test` 全绿
- [ ] `HomeViewModel` 无内联业务决策（确认/批量/历史/保存全部经 UseCases，仅保留绑定转发与状态栏赋值），所有编排逻辑有测试。行数目标修订：262 行是列表页 MVVM 壳的合理规模（<120 过严，属性壳+命令转发占大头），以"决策不在 UI 层"为实质验收
- [ ] `TryLaunch`/保存失败路径错误可被状态栏显式展示（含结构化信息）
- [ ] 卡片危险标记可见（危险项带 AlertTriangle + 危险原因）
- [ ] 现有功能无回归（单卡/批量/搜索/编辑全流程）
- [ ] EditDialog terminal placeholder 修正为 Windows 示例（B4 一并处理）

## 约束

- 不引入状态机库（确认策略是单函数，KISS）
- ErrorOr 仅用于预期失败；异常保留给编程错误
- ViewModel 下沉不改变 UI 行为（仅位置移动 + 测试覆盖）
