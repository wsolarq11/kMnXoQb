# P2: analyzer 试点 + UDF 评估 + 后续 phase 预备

## Goal

为后续 phase 准备：评估官方 analyzer 落地价值、UDF 演进路径、产出路线图文档。

## Requirements

1. win-dev-skills analyzer 试点：临时接入（不提交），跑现有 XAML，记录规则命中（尤其 x:Bind without Mode），评估接入成本。
2. UDF 评估：对 HomeViewModel 状态面做 UDF 改造可行性评估（reducer 化收益 vs 样板成本）。
3. 后续 phase 路线图文档：P3+ 候选（analyzer 正式接入、UDF 实施、artifacts CI 缓存、行为契约扩展）。

## Acceptance Criteria

- [ ] 评估报告（analyzer 试点结果 + UDF 可行性结论 + 路线图）写入任务目录
- [ ] 报告含决策建议（接入/不接入 + 理由 + 置信度）

## Notes

- 本子任务只产出文档，不强制改代码。
