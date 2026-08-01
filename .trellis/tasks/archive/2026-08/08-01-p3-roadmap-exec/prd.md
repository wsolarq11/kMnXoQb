# P3 路线图实施：MVVMTK 清理 + XAML 静态门禁

## Goal

执行 P3/P4 梳理中当前可做的两项，使构建达成真正零警告，并给 XAML 绑定默认值加上 CI 回归防线。

## Requirements

1. P3-1：MVVMTK0045（24 处，[ObservableProperty] 字段 → partial property 迁移）+ MVVMTK0034（10 处，字段直接引用清理）——构建零警告。
2. C-2：CI 加 x:DefaultBindMode 静态检查（新页面必须声明根默认绑定模式）。
3. 明确不做（记录理由）：P3-2 analyzer（等待官方 NuGet）、P3-3 CI artifacts 缓存（收益有限）、P4-1 UDF（等待触发）、P4-2 UI 自动化（等待生态）。

## Acceptance Criteria

- [ ] 构建零警告（MVVMTK 全部消失）
- [ ] 106 单元/架构/快照 + 17 契约全绿
- [ ] 主题开关往返无循环（_confirmEnabled 迁移风险点验证）
- [ ] CI 新步骤存在且本地等价命令通过（缺 x:DefaultBindMode 的 XAML 被抓出）
- [ ] 提交 + 独立质检 PASS

## Notes

- 风险点（梳理已识别）：_confirmEnabled 赋值链（OnSettingsChanged 直接写字段——迁移后需删除恒同步死分支）；构造函数属性赋值触发 OnConfirmEnabledChanged → 构造期 TrySave。
