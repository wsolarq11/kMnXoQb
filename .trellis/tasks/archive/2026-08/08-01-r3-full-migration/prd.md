# R3 全图实施：Rust核心 + Tauri2 + React 迁移（评估→生产就绪→实战质检）

## Goal

按评估结论（`archive/2026-08/08-01-migration-eval-rust-ts`：R3 首选）实施完整迁移：阶段 0 验证门 → 核心层 → 配置层 → 用例桥接 → 前端 → 发布与质检。全图生产就绪后**实战实测验真质检**（真实使用场景验收），再**预备后续 phase**（规划下一迭代）。

上游设计：`archive/2026-08/08-01-migration-eval-rust-ts/research/软件设计与架构模式.md`（v0.1，11 个决策点按建议默认采纳，见下）。

## 阶段路线图（顺序执行，每阶段独立可验收）

| 阶段 | 任务 | 交付 | 验收要点 |
|---|---|---|---|
| 0 验证门 | `r3-phase0-gate` | Tauri 2.11 最小工程（React + Rust core）；WebView2 在 Win10 LTSC 实测；便携 exe 旁 config/ 读写验证；中文输入实测 | 三要素通过才进入阶段 1；失败 → 回退 C# 或转 R1（egui 备选） |
| 1 核心层 | `r3-phase1-core` | Rust 纯函数 core/（models/planner/danger/validator/window_pos/items/launch/i18n/errors） | 现有 Core.Tests 断言 1:1 全绿（cargo test）；零外部依赖 |
| 2 配置层 | `r3-phase2-config` | config/（双轨路径解析 + atomic_write/.bak/损坏恢复 + 未知字段保留 + 探测缓存） | ConfigStoreTests + JsonRoundTrip 快照字节兼容（insta） |
| 3 用例与桥接 | `r3-phase3-bridge` | UseCases 移植（launch/items/settings 编排）+ 16 个 Tauri commands + 契约测试（真实 spawn pwsh/cmd/wt） | 用例单测 + 契约测试全绿（WtFact 跳过逻辑保留） |
| 4 前端 | `r3-phase4-frontend` | React UI（卡片/搜索/多选/对话框/主题三态/语言热切换/状态栏/lucide-react/i18n）+ vitest | 手动走查 C# 行为对照清单 + 前端测试全绿 |
| 5 发布与质检 | `r3-phase5-release` | MSI（per-user WiX）+ Portable zip 双产物；**实战实测验真质检**；spec 更新；**预备后续 phase** | 干净机器双形态跑通；质检清单全过；spec/路线图沉淀 |

## 依赖与顺序约束

- 阶段严格顺序执行（0→1→2→3→4→5）；后续阶段在阶段 5 的"预备后续 phase"中规划。
- 阶段 0 是硬门：未通过不进入实施（回滚点：维持 C# 现状或转 R1 备选）。
- 阶段 1/2 可并行验证（纯逻辑），但按顺序实施以保持单一主线。
- 行为对齐以 C# 现有测试断言为准（不信任注释）；`archive/launchpad-rs` 仅参考结构，禁止复用其启动分支（含 2 个已修复 bug）。

## 决策点采纳记录（设计文档 v0.1 默认采纳，实施中如遇重大问题再向用户提出）

D1 纯决策进 Rust 核心；D2 双轨配置（单一解析器 + 形态标记）；D3 文件 I/O 全收 Rust command；D4 WebView2 分发（MSI downloadBootstrapper + 便携启动检测指引）；D5 zustand；D6 i18n 双端一致（构建期同步 + 键完整性测试）；D7 tauri-plugin-single-instance；D8 crt-static；D9 `launchpad-tauri/` 新工程并行（C# 主线不动）；D10 前端测试（组件+store+键表完整性）；D11 前端持有全量键表。

## 全图生产就绪标准（阶段 5 验收）

1. 功能对齐：prd.md 基准 A–E 全量达成（行为断言 1:1）。
2. 双形态分发：MSI（per-user）+ Portable zip 干净机器跑通；便携版 exe 旁 config/ 自动生成、目录可移动；MSI 版 %APPDATA% 配置。
3. WebView2：缺失时启动检测 + 安装指引；Win10 LTSC 实测结论记录。
4. 测试全绿：cargo test（核心/配置/契约）+ vitest（前端）+ CI 接入。
5. 实战实测验真质检：真实场景清单（见下）逐项通过。

## 实战实测验真质检定义（阶段 5 核心交付）

质检 = 真实使用场景端到端验收，非仅单元测试：

1. **真实数据**：使用真实 config.json/settings.json（仓库 config/ 现有数据迁移验证字节兼容）。
2. **真实启动**：真实 spawn 三种终端路径（有 wt 用 wt，无 wt 用 pwsh/cmd fallback），验证窗口实际弹出、工作目录正确、命令执行。
3. **真实中文**：中文目录/命令名/项目名端到端；编辑框中文 IME 输入（候选/组词/上屏）；中英语言切换热刷新。
4. **真实场景走查**：按 C# 行为对照清单全量手动走查（新建/编辑/删除/移动/搜索/多选/批量/确认/危险警告/主题/窗口状态/单实例/损坏恢复）。
5. **真实发布**：双产物在干净机器（无 Rust/Node 环境）验证；便携版移动目录后配置仍被找到；MSI 版卸载后 %APPDATA% 残留检查。
6. **质检报告**：逐项记录 PASS/FAIL + 证据（截图/日志），FAIL 项进修复循环。

## 预备后续 phase 定义（阶段 5 收尾）

- 沉淀质检结论与遗留缺口 → 规划下一迭代（phase 6+）：候选方向（按优先级）：自动更新（Tauri updater）、Mica/Acrylic 增强、每工具配置适配器（P12）、跨平台（macOS/Linux）、CI 发布流水线。
- 更新 `.trellis/spec/`（新建 tauri 技术栈 spec 或并入既有文档）+ CLAUDE.md/README 迁移说明。
- 评估任务与实施任务的结论互链（archive 索引）。

## Acceptance Criteria

- [ ] 6 阶段全部完成，每阶段验收标准达成。
- [ ] 阶段 5 质检报告：实战清单逐项 PASS（或 FAIL 已修复并有回归测试）。
- [ ] 双形态产物可在干净机器跑通；WebView2 指引文档化。
- [ ] spec 更新 + 后续 phase 预备文档落盘。
- [ ] C# 主线未被破坏（保持可回滚）。

## Notes

- 约束修订（2026-08-01）：便携 zip/MSI 双形态 + WebView2 接受 + 双轨配置。
- 决策点如遇实施中的事实冲突（如 WebView2 实测失败），立即向用户报告并暂停该路径。
