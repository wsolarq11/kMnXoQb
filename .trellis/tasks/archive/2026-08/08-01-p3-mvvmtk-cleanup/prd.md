# P3-1: MVVMTK 警告清理（partial property 迁移）

## Goal

构建零警告：MVVMTK0045（[ObservableProperty] 字段的 AOT 兼容警告）与 MVVMTK0034（字段直接引用警告）全部消除。

## Requirements

1. HomeViewModel 5 个字段、EditViewModel 9 个字段：`[ObservableProperty] private T _x;` → `[ObservableProperty] public partial T X { get; set; }`。
2. 字段引用点逐一迁移：HomeViewModel `_settings`（7 处读取）、`_confirmEnabled`（3 处）；EditViewModel `_name`/`_directory` 等（构造函数赋值、PickDirectoryAsync 读取）。
3. 风险链处理：`OnSettingsChanged` 里 `_confirmEnabled` 直接写字段 → 删除（恒同步死分支，调研确认）；构造函数 `_confirmEnabled = ...` → 属性赋值（评估触发 OnConfirmEnabledChanged → TrySave 构造期写盘，若发生需抑制或接受）。

## Acceptance Criteria

- [ ] `dotnet build` 零警告零错误
- [ ] 106 + 17 测试全绿
- [x] 主题开关往返冒烟（ToggleTheme × 2 + Confirm 开关 × 2 无循环无多余写盘）——实战实测环节执行
- [x] HomeViewModelTests 方案放弃（测试项目只引用三个纯库约束），守卫逻辑由代码审查 + 冒烟覆盖
- [ ] MVVMTK0034/0045 在构建输出中计数为 0

## Notes

- partial property 需要 C# 13+（net10 默认 C# 14，满足）。
- CommunityToolkit.Mvvm 8.4.2 支持 [ObservableProperty] partial property。
