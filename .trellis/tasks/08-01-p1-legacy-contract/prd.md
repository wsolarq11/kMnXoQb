# P1: 旧版行为恢复 + Verify 行为契约（BUG-11/12/13）

## Goal

把与旧版 Rust 的行为漂移拉回 1:1，并用快照测试锁死，防止再次漂移。

## Requirements

1. 批量启动后清除所有选中态（旧版 app.rs:637-641 实证）。
2. 卡片删除弹确认框（"This cannot be undone"，旧版 app.rs:476-495）；编辑框内 Delete 保持直接删（旧版一致）。
3. 搜索空态区分：全列表为空显示「No items yet」；搜索无结果显示「无匹配」提示（旧版只在全空时显示）。
4. Verify 快照契约：LaunchPlanner 输出（3 终端路径 × 引号目录）、PushHistory/PushHistoryMany 序列、删除确认行为、批量清选行为。

## Acceptance Criteria

- [ ] 三处行为与旧版对齐（代码 + 可操作验证）
- [ ] Verify 快照测试入库（新增测试项目引用 Verify.Xunit，xUnit 兼容）
- [ ] `dotnet test tests/launchpad.Core.Tests/` 全绿（含快照）
- [ ] 快照首次生成经人工 diff 确认（机器审查）

## Notes

- IDialogService 需新增删除确认方法（对话框属于 UI 壳层，错误仍走 ErrorOr）。
