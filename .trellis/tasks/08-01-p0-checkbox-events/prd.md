# P0: 勾选框 Click 事件 + 按 Id 幂等（BUG-4/5）

## Goal

消灭勾选框的状态回弹与快速双击丢切换（BUG-4/5）——勾选错乱家族的根因修复。

## Requirements

1. 卡片 CheckBox 事件从 `Checked/Unchecked` 改为 `Click`（WinUI 源码实证：Click 仅用户输入触发，绑定驱动不触发）。
2. Click 处理器内同步捕获目标状态（`IsChecked == true`），Defer 后执行。
3. `ToggleSelect` 语义从「翻转」改为「设置目标状态」，按 **Id**（而非引用）解析索引——快速双击的第二次操作不失效。

## Acceptance Criteria

- [ ] 新增回归测试：按 Id 幂等（旧引用/旧状态不破坏目标状态）；双击语义（ToggleSelect 幂等）
- [ ] 构建零错误；`dotnet test tests/launchpad.Core.Tests/` 全绿
- [ ] 实战验证：单点/快速双击/滚动后勾选无错乱、无回弹

## Notes

- 配方经 WinUI 官方源码（ToggleButton_Partial.cpp:177-182）实证。
- HomeViewModel.ToggleSelectCommand 参数类型需调整（item + target）；保持命令签名兼容或同步改 XAML 绑定。
