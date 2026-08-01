# 阶段 1 核心层：Rust 纯函数移植（models/planner/danger/validator/position/items/launch/i18n/errors）+ 单测 1:1

## Goal

在 `launchpad-tauri/src-tauri/src/core/` 建立零外部依赖（纯 std + serde）的 Rust 纯函数核心，行为与 C# `launchpad.Core`/`launchpad.UseCases` 纯函数 1:1 对齐（以现有测试断言为准），cargo test 全绿。

## 模块清单（对应 C# 源）

| Rust 模块 | C# 源 | 说明 |
|---|---|---|
| models.rs | Core/Models/* | LaunchItem/AppSettings/WindowState/LaunchPlan（serde snake_case 字节兼容） |
| planner.rs | Core/Domain/LaunchPlanner.cs | wt→pwsh→cmd 三级回退 + EscapePwshQuotes |
| danger.rs | Core/Domain/DangerousFlagDetector.cs | 6 flag → LanguageKey |
| validator.rs | Core/Domain/ItemValidator.cs | 名称/命令必填、目录存在性 |
| window_pos.rs | Core/Domain/WindowPosition.cs | ClampToVisible（-32000 纠偏） |
| items.rs | UseCases/ItemUseCase.cs 纯函数 | GenerateId/Filter/Upsert/Delete/Move/SetSelect/SetSelectById/ClearSelection/ToggleSelectAll |
| launch.rs | UseCases/LaunchUseCase.cs 纯函数 | NeedsConfirm/RequireConfirm/PushHistory/PushHistoryMany |
| i18n.rs | Core/Localization/LanguageKey.cs + Translations.cs | 键枚举 + 中英表 + 三层优先级解析 |
| errors.rs | UseCases/Errors.cs + Core/Ports | AppError 枚举（错误分类语义） |

## 验收标准

- [ ] cargo test 全绿；每个 C# 单测文件（LaunchPlannerTests/DangerousFlagTests/ItemValidatorTests/WindowPositionTests/ItemUseCaseTests/LaunchUseCaseTests/TranslationsTests/JsonRoundTripTests 对应部分）断言逐条移植
- [ ] core/ 零外部依赖（仅 std + serde/serde_json），无 std::fs/process/windows 调用
- [ ] 序列化字节兼容：字段顺序/缩进/snake_case/可选字段省略与 C# 输出一致（JsonRoundTripTests + Verify 快照为验收）
- [ ] 边界用例完整：GenerateId 冲突 _2 后缀、Move 越界、History 去重上限、pwsh 引号转义、flag 各匹配、WindowPosition 各异常坐标

## Notes

- 行为对齐以测试断言为准；`archive/launchpad-rs` 仅参考结构，不搬代码（cmd/pwsh 分支用 C# 修复版）。
- i18n 键表与前端 TS 同构（D6）：本阶段产出 Rust 侧枚举 + 表，TS 侧在阶段 4 生成。
