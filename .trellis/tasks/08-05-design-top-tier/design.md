# design.md — 卡片尺寸一致性与日夜模式顶级设计达标

## 架构边界

| 层 | 改动点 | 说明 |
|---|---|---|
| 前端 CSS token（App.css） | 色彩/间距/字号/圆角/阴影/动效全套 token 重构 | 纯 CSS 变量，无逻辑 |
| 前端布局（grid.ts / useGridPlan） | 固定卡片尺寸 + 滚动 | 纯 TS 函数，可测 |
| 前端主题（theme.ts / App.css） | color-scheme 随主题、全组件过渡 | 少量逻辑 |
| 前端验证（新增 contrast.test.ts） | 解析 App.css token 断言对比度 | 纯 TS 测试 |
| Rust（lib.rs / app/settings.rs） | 窗口材质动态应用 + 主题联动 | Tauri 内置 `tauri::window::Effect` |

## 1. 色彩系统（R1 + R3）

### 对比度目标（WCAG 2.1 AA）
- 正文/小文本（≥12px）：≥ 4.5:1
- 大文本（≥18.5px bold 或 ≥24px）：≥ 3:1
- 非文本 UI 边界（边框/图标/控件轮廓）：≥ 3:1（SC 1.4.11）
- M3 暗色附加约束：暗色表面 + 白文本 ≥ 15.8:1（保证最高层级表面上仍 AA）——现行 fg-primary 13.3:1 不满足，需同步加深表面或提亮文本

### 实测不达标项与目标值（2026-08-05 计算）
| token | 现状（浅/深） | 目标 | 处理 |
|---|---|---|---|
| fg-tertiary | 3.43 / 3.24 | ≥4.5 | 浅 #6e6e78 深 #9a9aa4 方向（具体值以对比度脚本验证为准） |
| fg-tertiary on header | 3.06 / 3.36 | ≥4.5 | 同上，header 底色需配合微调 |
| border-subtle | 1.26 / 1.27 | ≥3.0（非文本） | 浅 #8f8f98 深 #5a5a66 方向 |
| border-strong | 1.55 / 1.65 | ≥3.0 | 浅 #6f6f78 深 #6f6f7a 方向 |
| bg-surface vs bg-app | 1.08 / 1.10 | ≥1.3 视觉分层 | 表面层级：暗色按 M3 规则"层级越高越亮"，浅色加深 app 背景（#f0f0f2 方向）或表面提白 |
| fg-primary on surface（暗色） | 13.27 | ≥15.8（M3 暗色） | 表面加深（#1f1f24 方向）或文本提亮 |

### 层级体系（Fluent 两层 + M3 暗色提亮）
- 背景层 bg-app：暗色最暗（#17171c 方向）
- 内容层 bg-surface：暗色略亮（#1f1f24 方向），hover 再亮一级
- 弹层 bg-elevated：暗色最亮（#26262c 保持/微调）
- 半透明层（材质启用后）：header/status 用带 alpha 的表面色

### 验证机制
新增 `src/lib/contrast.test.ts`：正则解析 App.css 的 `:root` 与 `:root[data-theme="dark"]` 块 → 提取变量 → 按用途矩阵断言（正文对 ≥4.5、边界对 ≥3.0、表面分层 ≥1.3）。token 值只存在于 CSS，测试解析 CSS 防漂移。

## 2. 间距/字号/圆角/阴影（R4）

### 间距（Fluent 2 ramp：2/4/6/8/10/12/16/20/24/28/32...，4px 基数）
| 位置 | 现状 | 修正 |
|---|---|---|
| item-grid padding | 14px | 12px |
| search-input padding | 7px 12px | 8px 12px |
| 其余 10/6/2px | 合法（size100/60/20） | 保留 |
| icon-btn 28px | 合法（size280） | 保留 |

### 字号（Fluent 2 ramp，Segoe UI）
- body 正文：13px → **14px**（Fluent body 基准；13px 不在 ramp）
- caption/次要：12px 保留（Fluent caption）
- title：14px 加粗保留（Fluent subtitle 14 semibold）
- 行高：正文 1.5 左右明确声明

### 圆角（Fluent 2：控件 4、卡片 8、大表面/对话框 12）
现状已符合（4/8/12），保留，统一为 token 引用。

### 阴影（Fluent elevation + 暗色适配）
- 层级对齐：卡片 elevation 8（shadow-sm 现状接近）、hover 12、对话框 128（shadow-lg 现状接近）
- **暗色模式阴影 opacity 翻倍**（Fluent 规范：light 14% → dark 28%）：`[data-theme=dark]` 覆盖 --shadow-* 变量
- 卡片 elevation 用"阴影 + 表面分层"组合，弱化对边框的依赖（边框仍满足 3:1 作为兜底）

## 3. 卡片固定尺寸 + 滚动（R2）

### 尺寸
- 列宽固定 **260px**（含内容三行可读宽度，Fluent 卡片建议）
- 行高固定 **120px**（标题 20 + 目录 18 + 命令 24 + padding 32 + 余量，均 4px 对齐）
- gap 10px 保留（Fluent size100）

### planGrid 重写
```ts
planGrid(width, count, { cardWidth: 260, gap: 10 }) => columns
// columns = max(1, floor((width + gap) / (cardWidth + gap)))
```
- 删除 PHI 黄金比例逻辑（fill 模式不再存在）
- 删除 scroll fallback 分支（固定尺寸后滚动是常态，不需要 data-scroll 开关）
- `grid-auto-rows` 固定 120px，超出自然滚动（overflow-y: auto 常驻）
- 搜索过滤/窗口变化 → 仅列数变化，卡片尺寸恒定

### 行为变化
- 卡片多时滚动查看（原 fill 模式保证全部可见）——用户已确认
- EmptyState 逻辑保留

## 4. 材质与窗口层（R4）

### 方案：Tauri 2 内置窗口效果（官方，v2.11.2 已验证源码）
`tauri::window::Effect` 枚举（Windows）：`Mica` / `MicaDark` / `MicaLight` / `Acrylic` / `Blur` / `Tabbed*`，底层委托官方 window-vibrancy。配置经 `WindowEffectsConfig`（tauri.conf.json 静态或 Rust 动态）。

### 平台策略（与旧 WinUI 栈一致：Mica 是 Win11 专属，Win10 用 Acrylic 回退）
| 系统 | 应用主题 | 效果 |
|---|---|---|
| Windows 11 22000+ | system | Mica（跟随系统） |
| Windows 11 22000+ | dark / light | MicaDark / MicaLight |
| Windows 10 1809+ | 任意 | Acrylic（可选 tint color） |

### 实施要点
- Rust 侧 `setup` 时按当前 settings.theme 应用一次；`toggle_theme` 命令内更新窗口效果（主题状态本就在 Rust 侧）
- 应用失败静默降级（不 panic，保持纯色背景）——window-vibrancy 在版本不支持时返回 Err
- 检测：`std::env::var` 不适用，用 `windows` crate 的 RtlGetVersion 或 `windows_version`（评估）——不引入新依赖优先，可用现有 windows crate（已依赖）
- 前端配合：body 透明（现状已 transparent），`.app` 与 header/status 改半透明表面（`color-mix(in srgb, var(--bg-header) 70%, transparent)` 方向），卡片保持实色表面（内容可读性优先）
- 关闭材质分支：如果 Acrylic 在 Win10 上 resize 性能问题不可接受（官方已知：Win10 v1903+ / Win11 22000 resize 卡顿），提供降级开关（settings.json `windowMaterial` 或环境变量，默认开）

### 风险
- 开发机是 Windows 10 LTSC 2021（19044）：Mica 效果无法本地验证，只能验证 Acrylic 分支；Mica 分支代码走查 + 文档说明
- Acrylic resize 性能（官方已知问题），需实测评估
- 半透明表面后对比度需重测（alpha 混合后的有效对比度）——contrast.test.ts 用混合后颜色计算

## 5. 主题与 color-scheme（R3）

- `color-scheme: light dark` → 随主题切换：`:root[data-theme="dark"] { color-scheme: dark }`、`:root[data-theme="light"] { color-scheme: light }`、system 保持 `light dark`（原生滚动条/checkbox 跟随）
- 主题过渡覆盖全组件：item-card、icon-btn、primary/ghost/danger-btn、search-input、modal input/textarea、mark.match 加入 transition（0.18s，现状部分缺失）

## 6. 动效（R4）

- 时长对齐 Fluent：标准 150ms（现状 0.15s ✓）、主题 180ms（✓）、modal fade 120ms（✓）
- 补充 hover/active 反馈到全部可交互元素（现状 icon-btn/按钮有，卡片 hover 有 elevation+位移）

## 兼容性与回滚

- 纯视觉 + 布局改动，无数据格式变化；settings.json 不变（theme 三态保留）
- 回滚：单 commit 粒度（CSS / grid / Rust 材质可分 commit），任一步可 revert
- Win10 上材质分支自动降级为纯色，功能不受影响
