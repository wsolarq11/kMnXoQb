# 路线 B：纯 Rust + Slint — 调研结论

调研日期：2026-08-01。核实版本：Slint 1.17.1（2026-07-07 发布；1.17.0 2026-06-24）。

## 总评（结论先行）

分发形态**完美契合**产品约束（单 exe、exe 旁配置、无 WebView2），Rust 侧 spawn/路径能力天然具备；但有两个独立于 egui 旧案的**新障碍**：

1. **许可证**：本项目仓库私有（闭源）。免费合法路径只有 Royalty-free 披露条款（UI 内显示 Slint 徽标），否则需付费商业订阅（按人/按 tier）；GPLv3 强制整个应用开源，不可用。
2. **中文 IME**：无官方支持保证，存在微软拼音冻结等已知报告（见 Q3），选型前必须实测。

结论：功能对齐可行、exe 形态最优；**若接受披露条款或付费，且 IME 实测通过，则路线 B 在分发约束上全面优于路线 A**。

## Q1 版本与成熟度

- 最新稳定版 **1.17.1**（2026-07-07）；1.17.0 2026-06-24；1.16.1 2026-04-23。发布节奏约 1–2 月一版，稳定。
- Rust 绑定为一等公民：`slint` crate + `slint-build` 编译期生成 `.slint` DSL 代码；渲染器可选 wgpu/skia/femtovg/软件渲染（软件渲染无 GPU 依赖）。1.17 官方定位 "another big step toward being genuinely desktop-ready"（新增系统托盘图标、单窗口拖放、tooltip 等）。
- 依赖：winit + wgpu/skia 等，纯 Rust 生态，无 WebView2/系统 WebView 依赖。

依据：https://github.com/slint-ui/slint/releases 、https://docs.rs/crate/slint/latest 、https://extenly.com/2026/07/09/slint-1-17-whats-new-in-the-modern-ui-toolkit

## Q2 许可证（关键决策项）

- 官方三选一（`slint/LICENSE.md` 原文）：
  1. **Royalty-free License**：专有桌面/移动/Web 应用**免费**，但必须**披露使用 Slint**（如 AboutSlint 组件/徽标）；嵌入式系统除外（嵌入式商业需付费）。
  2. **GPLv3**：开源应用免费（强 copyleft —— 整个应用需 GPL 兼容开源）。
  3. **Commercial License**：付费，专有应用（含嵌入式）。
- 商业订阅 tier（slint.dev/pricing）：Enterprise / Small Enterprise（≤50 员工、≤€10M 营收或资产）/ Startup & Individual（≤10 员工、≤€2M 营收、成立 <5 年；官网显示 "from $9/month"，具体金额在结账页）。注意："**所有参与设计/开发/测试的个人用户**均需许可"（按人头收费）。嵌入式设备另有一次性 royalty（$1/台起，量大打折）。
- **对本项目的结论**：闭源 → GPLv3 不可用；免费路径 = Royalty-free 披露条款；或付费商业订阅。若项目改为开源则 GPLv3 免费。
- 风险标注：披露义务（应用内显示 Slint 徽标）可能与产品 UI 冲突且不可移除（移除即需付费）；公司规模超限（员工/营收/成立年限）自动落入更高 tier；按人计费使团队规模直接进入成本。

依据：https://github.com/slint-ui/slint/blob/master/LICENSE.md 、https://slint.dev/pricing

## Q3 Windows 中文 IME 与单 exe 发布

- **IME（风险项）**：无官方文档承诺完整 IME 组合输入（preedit）支持。证据链：
  - #1706（2022，Win11 韩文 IME 切换失败，Skia 渲染器；已关闭，PR #1728）；
  - #8716（2025-06-17，**Win10 + 微软拼音**切换后 UI 完全冻结，需强杀进程；关闭原因标记 "duplicate of #5206"——#5206 为 IME 问题跟踪 issue）；
  - 1.17 changelog 仅笼统 "Skia renderer: Improvements to text input"，无 IME 专项修复声明。
  - 本项目表单输入以命令编辑为主（多 ASCII），但中文路径/名称输入可能触发。**必须**在目标环境（Win10 LTSC 2021 + 微软拼音）做 spike 实测后才能定案。
- **单 exe 发布**：是，且是最优形态。纯 Rust 编译为单个静态 exe，无运行时依赖、无安装器需求；体积 Windows GUI 应用约 **3–4MB**（官方讨论 #9570；Rust hello-world 基线 ~4MB），软渲染器可进一步免 GPU 依赖。
- **i18n**：Slint 内置翻译机制（`tr!()` + Fluent `.ftl` 文件，编译期接入），文案键表可行；但**运行时语言热切换**能力需验证（本项目 LanguageService 热切换需求，官方文档未见明确承诺）。

依据：https://github.com/slint-ui/slint/issues/1706 、https://github.com/slint-ui/slint/issues/8716 、https://github.com/slint-ui/slint/issues/5206 、https://github.com/slint-ui/slint/blob/master/CHANGELOG.md 、https://github.com/slint-ui/slint/discussions/9570 、https://slint.dev/docs/translations

## Q4 项目历史参考（egui vs Slint）

- **事实澄清**：`archive/launchpad-rs` 是 **egui/eframe 0.31** POC（Cargo.toml 原文：`eframe = "0.31"`、`egui = "0.31"`），**不是 Slint**。归档 `_README.md` 仅记录取代次序（egui 版 → Flutter 版 2026-07-30 → WinUI 3 版 2026-07-31），**未记录放弃原因**——不得臆测"放弃原因"为事实。
- egui vs Slint 差异（影响本次评估）：
  | 维度 | egui（旧案） | Slint |
  | --- | --- | --- |
  | 架构 | 立即模式（immediate mode），全 Rust 代码构建 UI | 保留模式声明式 DSL（`.slint` + 编译期生成），与 WinUI XAML 心智模型更接近 |
  | 许可证 | MIT/Apache-2.0 宽松 | GPLv3 / 商业 / 披露式 Royalty-free（**新障碍**） |
  | 生态 | 社区大、示例多 | 公司制（SixtyFPS GmbH）维护，嵌入式+桌面双线 |
- **评估影响**：旧案放弃的是 egui（立即模式在复杂布局/主题/热切换场景开发效率低是常见共识，但本项目无归档文字佐证，仅作参考）；Slint 的声明式模型与现有 XAML 绑定/模板结构映射度更高。但需注意：**本项目 30 天内已两度更换 UI 框架**（egui → Flutter → C#/WinUI 3），若再迁需明确动机与止损点——许可证与 IME 是新引入的硬障碍，与 egui 无关，不能被"换掉 egui"的惯性掩盖。

## 风险清单

1. **[高] 许可证合规**：闭源免费 = Royalty-free 披露条款（UI 内置 Slint 徽标）；移除披露或规模超限即触发付费（按人/tier）；GPLv3 要求整体开源，与闭源定位冲突。
2. **[高] 中文 IME 无保证**：#5206 系列（含微软拼音冻结 #8716），必须 Win10 LTSC 2021 实测后才能定案。
3. **[中] 迁移工作量**：XAML → `.slint` 全量重写 UI（卡片、对话框、主题切换、危险命令三处警告等）；i18n 键表需迁移到 Fluent 体系，热切换需自验。
4. **[低] 单窗口应用** Slint 完全覆盖；1.17 起托盘/多窗口能力已具备，无功能缺口。
5. **[低] CI 简化**：仅 Rust 工具链（cargo），比路线 A（Node+Rust）和当前 C#（dotnet）都简单；无安装器/签名复杂度（可加分项）。

## 依据汇总

- https://github.com/slint-ui/slint/releases （1.17.1, 2026-07-07）
- https://github.com/slint-ui/slint/blob/master/LICENSE.md 、https://slint.dev/pricing
- https://github.com/slint-ui/slint/issues/1706 、#8716 、#5206 、https://github.com/slint-ui/slint/discussions/9570
- https://github.com/slint-ui/slint/blob/master/CHANGELOG.md
- https://slint.dev/docs/translations 、https://extenly.com/2026/07/09/slint-1-17-whats-new-in-the-modern-ui-toolkit
- 本项目存档：`archive/launchpad-rs/Cargo.toml`（egui/eframe 0.31）、`archive/_README.md`（取代次序，无原因记录）
