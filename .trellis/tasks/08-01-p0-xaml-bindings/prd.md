# P0: x:DefaultBindMode 绑定种子修复（BUG-1/2/3/13）

## Goal

消灭 XAML 绑定默认值陷阱（x:Bind 默认 OneTime），让状态栏/计数/最近项/编辑框校验/搜索空态全部实时刷新，并让未来新绑定默认安全。

## Requirements

1. `HomeView.xaml` 根节点加 `x:DefaultBindMode="OneWay"`；卡片模板 11 处绑定显式 `Mode=OneTime`（6 处无 Mode + 5 处显式 OneWay）。
2. `EditDialog.xaml` 根节点加 `x:DefaultBindMode="OneWay"`（无模板，直接生效）。
3. 页面级绑定（ViewModel 属性）默认 OneWay 后订阅通知，BUG-1/2/3/13 修复。

## Acceptance Criteria

- [x] 构建零错误；WMC1506 归零（模板全部 OneTime）；MVVMTK0045 为既有警告（24 处，AOT 前瞻，记入 P2 评估，本任务不迁移）
- [ ] `HomeView.g.cs` 出现 >=6 个页面级 `RegisterPropertyChangedListener`（编译产物断言）
- [ ] `dotnet test tests/launchpad.Core.Tests/` 全绿
- [ ] 模板绑定行为不变（item 不可变，按实例求值语义等价）

## Notes

- 配方经 scratch 工程实证（WindowsAppSDK 2.3.1/.NET 10）：无 Mode 绑定被改写为 OneWay（WMC1506 证明）；显式 OneTime/TwoWay 被尊重。
- 不在本子任务范围：行为逻辑改动。
