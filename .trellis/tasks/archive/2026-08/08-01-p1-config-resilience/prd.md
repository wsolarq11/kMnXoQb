# P1: config 韧性 + 崩溃日志保护（BUG-6/8）

## Goal

损坏 config 不再静默启动死亡；崩溃日志写入自身不崩。

## Requirements

1. `config.json` 解析失败 → 自动尝试 `config.json.bak` 恢复（读 bak → 写回 config.json → 继续）→ 状态栏提示「已从备份恢复」。
2. 恢复也失败 → 返回结构化错误 → 状态栏显示，应用正常启动（空列表）。
3. `OnUnhandledException` 日志写入包 try/catch（写失败不影响进程）。

## Acceptance Criteria

- [ ] 损坏 config + 有效 bak → 自动恢复 + 提示（测试覆盖 + 实战验证）
- [ ] 损坏 config + 无 bak → 空列表 + 错误提示，不崩
- [ ] 日志写入失败不抛出（模拟只读目录测试或代码审查）
- [ ] `dotnet test` 全绿

## Notes

- 恢复逻辑放 ItemUseCase.LoadItems（UseCases 层编排，可单测）；错误文案中文友好。
