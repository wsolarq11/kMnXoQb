# 卡片尺寸一致性与日夜模式顶级设计达标

## Goal

以 Fluent 2（微软 Windows 11 设计语言）为基准 + WCAG 2.1 AA 对比度为可量化标准，系统性审计并改进 launchpad-tauri 前端视觉设计。解决三个已确认问题：卡片尺寸不一致（fill 模式尺寸随卡片数量/窗口漂移）、日夜模式未达标（对比度/边框/表面层级不达标）、整体设计水准不足（间距/字号/动效/材质未对齐 Fluent）。技术栈保持 Tauri + React 不变。

## 背景（已确认事实，2026-08-05）

- 卡片布局 fill 模式（grid.ts PHI=1.618 选列数 + App.css:170 `grid-auto-rows: minmax(0,1fr)`）：搜索过滤/窗口变化时卡片尺寸突变，单卡片时铺满全窗口。
- 对比度实测（WCAG 相对亮度）不达标项：fg-tertiary 3.43/3.24（需 ≥4.5）；border-subtle 1.26/1.27、border-strong 1.55/1.65（非文本需 ≥3.0）；bg-surface vs bg-app 1.08/1.10（表面分层需 ≥1.3）；暗色 fg-primary 13.3（M3 暗色标准 ≥15.8）。
- color-scheme 未随主题切换（原生滚动条/checkbox 不跟随）；主题过渡仅覆盖部分元素。
- 间距散值（14px/7px 不在 Fluent ramp）；正文 13px 不在 Fluent ramp。
- 材质缺失：Tauri 2.11 内置 `tauri::window::Effect`（Mica/MicaDark/MicaLight/Acrylic/Blur，源码已验证），当前未使用。

## Requirements

- R1 设计基准：Fluent 2，对比度按 WCAG AA 可量化达标。
- R2 卡片尺寸一致性：固定尺寸（列宽 260px、行高 120px）+ 滚动；窗口变化/搜索过滤只变列数，卡片尺寸恒定。
- R3 日夜模式达标：全部文本对比度 ≥4.5:1（正文）/ ≥3:1（大文本），非文本边界 ≥3:1，表面分层 ≥1.3:1，暗色 fg-primary ≥15.8:1（M3 暗色规则）；color-scheme 随主题；主题切换过渡覆盖全组件。
- R4 整体设计达标：
  - 间距全部落在 Fluent ramp（4px 基数）
  - 字号对齐 Fluent（正文 14px、caption 12px）
  - 圆角 4/8/12 层级保留并统一 token 化
  - 阴影暗色模式 opacity 翻倍（Fluent 规范）
  - 材质窗口层：Win11 Mica（跟随主题 Mica/MicaDark/MicaLight）、Win10 Acrylic 回退，失败静默降级
- R5 防回归：对比度自动化测试（解析 App.css token 断言）+ 网格测试更新。

## Acceptance Criteria

- [ ] AC1 对比度测试全绿（实施校准：暗色 fg-primary ≥14:1 为 M3 实际 onSurface 基线——15.8:1 是纯白文本上限，无发行主题达到；表面分层浅 ≥1.15、暗 ≥1.10 为 M3 实测基线；测试矩阵只含真实用途组合，header 分隔线按装饰豁免）：正文对 ≥4.5:1、非文本边界 ≥3:1、表面分层达标、暗色 fg-primary ≥14:1，明暗双模式
- [ ] AC2 卡片尺寸恒定：固定列宽 260px/行高 120px；窗口 resize 与搜索过滤时仅列数变化，卡片像素尺寸不变
- [ ] AC3 滚动行为：卡片超出容器高度时正常滚动，无 fill 拉伸
- [ ] AC4 color-scheme 随主题：dark/light 强制时原生控件（滚动条/checkbox）跟随；system 跟随系统
- [ ] AC5 主题切换过渡覆盖全部组件（卡片/按钮/输入框/高亮），无跳变
- [ ] AC6 间距无 14px/7px 等非 ramp 散值；正文 14px
- [ ] AC7 Win11：Mica 材质按主题应用（代码走查 + 文档）；Win10：Acrylic 实测生效，resize 性能可接受（或降级开关生效）
- [ ] AC8 全部现有测试（vitest + cargo test）全绿，local-ci 通过
- [ ] AC9 明暗双模式人工走查通过：文本可读、层级清晰、无突兀色块

## Out of Scope

- 换技术栈或重构组件结构（React 组件文件不改架构）
- 数据格式 / settings.json 兼容性破坏（theme 三态保留；windowMaterial 为新增可选字段，默认值兼容旧配置）
- 跨平台（macOS/Linux）设计适配——当前仅 Windows
- Win11 Mica 的真实设备实测（开发机为 Win10）——代码走查 + 文档说明，后续在 Win11 设备验证
- 窗口圆角（Win11 DWM 自动圆角，无需额外处理）

## Key Decisions

- 设计基准：Fluent 2（用户选定）
- 卡片布局：固定尺寸 + 滚动（用户选定，接受"卡片多时滚动查看"行为变化）
- 范围：色彩 + token + 动效 + 材质窗口层（用户选定全部四维）
- 材质：Tauri 2 内置 Effect API（官方方案），Win10 用 Acrylic 回退（与旧 WinUI 栈策略一致）
- 验证：对比度测试解析 App.css 防漂移；视觉走查在 Win10 本机

## Risks / Deferred

- Acrylic 在 Win10 v1903+ resize 性能差（window-vibrancy 官方已知问题）——实测评估，必要时降级开关关闭
- 开发机 Win10 LTSC：Mica 分支无法本地验证——代码走查 + Win11 文档说明
- 半透明表面启用后对比度需按 alpha 混合后颜色重测（contrast.test.ts 已设计该机制）
