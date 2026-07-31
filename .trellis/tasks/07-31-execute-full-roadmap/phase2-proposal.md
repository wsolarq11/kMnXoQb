# 后续 phase 提案：阶段 2 核心下沉 Rust + P/Invoke

> 来源：migrate-to-winui3 任务 D5 规划（2026-07-31）。本提案供用户决策，**不在当前任务执行范围**。

## 背景

当前（阶段 1）：全部 C#，领域层 `launchpad.Core`（纯函数）+ 应用层 `launchpad.UseCases` + 基础设施 + WinUI 3 外壳。60 个测试全绿，生产就绪。

阶段 2 动因（D5 原规划）：将核心（模型/校验/plan/危险检测）下沉 Rust，通过 P/Invoke 调用。动机是"核心语言与旧 Rust 版对齐"，但需重新评估其收益。

## 建议先做的决策点（逐一确认）

1. **下沉范围**：只下沉领域层纯函数（DangerousFlagDetector/LaunchPlanner/ItemValidator/序列化），还是连配置读写也下沉？
2. **Rust crate 形态**：cdylib + C ABI 导出（FFI 边界手写），还是用 C#/Rust 互操作库（如 csharpscript 类方案或 uniffi-rs）？注意：uniffi-rs 是社区方案，需确认符合"官方 LTS 稳定优先"原则。
3. **性能/收益验证**：当前纯 C# 领域层在 60 测试下毫秒级完成，下沉 Rust 的实际收益是什么？（启动性能瓶颈在 WinUI 3 外壳，不在领域层）
4. **风险**：FFI 边界（字符串所有权、panic 跨边界、序列化格式双实现漂移）是新增风险面；双语言调试成本；测试策略需分层（Rust 侧 cargo test + C# 侧 P/Invoke 冒烟）。
5. **替代方案**：保持全 C#（零新风险），把精力转向功能增强（如启动项分组、模板、导入导出）。

## 若执行（草案路线）

- Phase 2A：Rust cdylib crate（launchpad-core-rs）承载纯函数，cargo test 覆盖
- Phase 2B：C# P/Invoke 薄封装（launchpad.Core 内部换实现，接口不变）
- Phase 2C：C# 侧测试改为验证 P/Invoke 结果与纯函数等价（差分测试）
- 验收：60 测试全绿 + Rust 侧等价测试全绿 + release 构建 + 实战启动验证

## 决策建议（供用户参考）

- 若用户核心诉求是"与旧 Rust 版 1:1 对齐"：已通过兼容性测试达成（配置字节兼容、行为 1:1），无需物理下沉。
- 若诉求是"核心语言统一 Rust"：需要明确业务价值后立项，否则建议保持全 C#（KISS）。
