# 评估设计：候选路线、评估维度、验证方法

## 1. 候选路线（评估矩阵行）

| 代号 | 路线 | 语言组合 | 说明 |
|---|---|---|---|
| R1 | 纯 Rust + egui/eframe | Rust | 旧 POC（`archive/launchpad-rs`，egui 0.31）直接升级；单 exe 天然；需核验当前 egui 版本与 IME 状态 |
| R2 | 纯 Rust + Slint | Rust | 声明式 UI；注意 GPLv3/商业双许可；与旧版历史（曾用 Slint 失败迁移）对照 |
| R3 | Rust 核心 + TS 前端（Tauri v2） | Rust + TS | 前端 Web 技术栈；产物近单 exe；Windows 依赖系统 WebView2 Runtime（Win10 非自带，LTSC 需核验） |
| R4 | 纯 TS + Deno desktop（2.9+, experimental） | TS | 2026-06-25 新发布；单二进制捆绑运行时 + WebView/CEF 渲染；experimental 状态是主要风险 |
| R5 | 纯 TS + Bun compile + 自接 webview | TS | Bun 单 exe 可行但无官方 GUI 通道；预计排除，需给出排除理由 |

## 2. 评估维度（评估矩阵列，每项 可行/有差距/不可行 + 依据）

1. **单 exe 达成度**：产物形态（单文件/文件树）、体积量级、运行时依赖（无依赖 / WebView2 / 内置 Chromium）；Win10 LTSC 2021（可能无 WebView2）场景判断。
2. **配置字节兼容**：snake_case 序列化等价能力（Rust serde 天然 1:1；TS 手写 serializer 需对齐省略规则与缩进）；UnknownFields 写回保留的等价机制；`config.json.bak` 恢复流程。
3. **功能对齐工作量**：prd.md 基准 B/C/D 逐项映射到目标语言的端口/框架能力；纯函数 1:1 移植成本；UI 重构成本。
4. **UI 能力**：中文 IME 输入（决定性风险）、主题切换（Mica/Acrylic 回退）、系统文件夹选择对话框、多窗口/对话框、卡片列表 + 复选框交互、lucide 字形。
5. **测试资产复用**：现有单测断言 1:1 移植可行性；契约测试（真实 spawn）等价物；快照测试等价物（insta / snapshot 工具）。
6. **生态成熟度**：框架版本稳定性、维护活跃度、学习/排障资源；experimental 状态评估。
7. **构建/发布复杂度**：CI 可复现、交叉产物、发布脚本等价物（当前 `publish.ps1` 的复杂度是痛点之一）。
8. **风险与回退**：每路线的最大风险点与可回退性（能否在不重写核心的情况下换 UI）。

## 3. 验证方法

- **对齐契约 = 现有测试断言**：评估报告中每项"可行"结论必须能指回 `tests/launchpad.Core.Tests/` 的对应测试文件（如 `LaunchPlannerTests`、`ItemUseCaseTests`、`DangerousFlagTests`、`ConfigStoreTests`、`JsonRoundTripTests`），证明行为有断言约束可移植。
- **IME 验证**：不写代码。评估报告给出验证门（迁移计划阶段 0 的验收项）：用最小窗口 + 中文输入法实测候选 UI 框架（egui 当前版本 / webview 前端），输入"中文测试"确认候选词上屏与编辑。egui 已知历史 IME 问题（2025 调查提及），需查当前版本 changelog 判定风险等级。
- **WebView2 依赖核验**：查证 Win10 LTSC 2021 WebView2 Runtime 的可用性与部署要求（R3/R4 的 webview 后端相关）。
- **字节兼容验证**：现有 `*.verified.txt` 快照 + `JsonRoundTripTests` 即验收标准；评估报告说明目标方案的序列化测试如何对齐。

## 4. 关键决策点（评估报告必须回答）

- D1: 单 exe 约束下，TS 路线（R4/R5）是否真能达成"单文件 + 根目录配置"，还是实际会引入隐性依赖（WebView2 / 大体积 CEF）？
- D2: 中文 IME 在 R1（egui）是否构成不可接受风险？若构成，egui 是否仍可接受（编辑框输入受限 vs 其他 UI 全部达成）？
- D3: R3（Tauri）的"exe 旁生成配置"行为是否可实现（前端 fs 能力 + 目录定位），产物形态是否满足"一个 exe"？
- D4: 旧 Rust POC（R1 基础）与当前 C# 的功能差距清单（如 i18n、批量确认、探测缓存、ClampToVisible）——R1 并非零成本复用，差距要量化。
- D5: 若所有路线均不满足硬约束，结论应为"维持现状"而非强行迁移——评估报告需诚实地支持该结论（务实主义原则）。

## 5. 风险与回退

- 时间风险：deno desktop experimental 可能已变/被砍 → 结论标注核验日期（2026-08-01）与复核触发点。
- 信息风险：搜索摘要可能过时 → 关键结论（IME、WebView2、deno desktop）标注来源与验证门。
- 回退策略：评估报告推荐的路线若在阶段 0 验证门（IME / WebView2 / 单文件）失败，回退到当前 C# 实现继续演进（C# 代码不受影响，迁移计划是增量验证的）。
