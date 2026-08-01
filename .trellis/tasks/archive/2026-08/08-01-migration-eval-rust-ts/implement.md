# 执行计划：评估 C# → Rust/TS 再迁移方案

本任务是**评估任务**：不写产品代码，产出评估报告 + 迁移计划骨架。研究结论落盘 `research/` 目录。

## 步骤（顺序执行）

### 1. 整理功能基准（已完成 → prd.md）
- [x] 从 C# 源码提取功能基准清单（prd.md A–E 五组）
- [x] 确认约束（2026-08-01 修订：便携 zip/MSI 双形态 + WebView2 接受 + 双轨配置、中文环境、Rust/TS 限制）

### 2. 技术研究（写 research/ 下的研究笔记）
- [x] 路线清单初建 + 关键变量初查（deno desktop / egui / Tauri / bun）
- [ ] **R4 深挖**：deno desktop 当前文档（Deno 2.9+）——产物形态、体积、WebView 后端的 Windows 依赖、experimental 状态、bindings 机制（research/deno-desktop.md）
- [ ] **R1 深挖**：egui 当前版本（0.31+ → 最新）的 IME/中文输入状态（changelog / issues）、单 exe 发布、lucide 字形嵌入方案（research/egui-ime.md）
- [ ] **R3 深挖**：Tauri v2 Windows 产物形态与 WebView2 依赖、Win10 LTSC 2021 的 WebView2 Runtime 可用性（research/tauri-webview2.md）
- [ ] **R2 补充**：Slint 当前版本、许可条款确认（research/slint.md）
- [ ] 旧 Rust POC 差距清单：对照 prd.md 功能基准，列出 `archive/launchpad-rs` 缺什么（i18n/批量确认/探测缓存/ClampToVisible/语言字段等）（research/poc-gap.md）

### 3. 评估矩阵打分
- [ ] 按 design.md 的 5 条路线 × 8 个维度逐项打分（可行/有差距/不可行 + 依据，依据指回测试文件或来源）
- [ ] 回答 design.md D1–D5 五个决策点
- [ ] 形成推荐路线（或维持现状）+ 理由

### 4. 撰写评估报告
- [ ] 报告：背景、路线对比矩阵、功能对齐差距逐项表（prd.md A–E）、风险清单、推荐与理由
- [ ] 迁移计划骨架（若推荐迁移）：阶段 0 验证门（IME/WebView2/单文件实测）→ 核心层移植 → 契约测试对齐 → UI 层 → 发布形态；每阶段验收 + 回滚点
- [ ] 落盘：任务目录 `research/评估报告.md`（或仓库 docs，由结论决定）

### 5. 验收与收尾
- [ ] 对照 prd.md Acceptance Criteria 逐条自检
- [ ] 汇报结论给用户，征询是否进入后续（实施迁移任务 / 归档评估）

## 验证命令

```bash
# 无代码改动，本任务不涉及构建/测试；唯一验证是报告完整性自检
# 对照 prd.md Acceptance Criteria 逐条过
```

## 评审门

- 评估报告完成后、归档前，由用户审阅推荐路线（若推荐"维持现状"也需用户确认）。
- 报告结论如推荐迁移，迁移本身是**新任务**，本任务不实施。
