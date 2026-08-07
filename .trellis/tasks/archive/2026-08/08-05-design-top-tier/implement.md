# implement.md — 卡片尺寸一致性与日夜模式顶级设计达标

## 前置

- 设计基准：Fluent 2 + WCAG AA（design.md）
- 范围：色彩 / 间距字号圆角 token / 动效过渡 / 材质窗口层 / 卡片固定尺寸
- 技术栈不变：Tauri 2.11 + React + CSS

## 实施清单（有序）

### 1. 色彩与 token 重构（App.css）
- [ ] 1.1 调整 fg-tertiary / border-subtle / border-strong / bg-app / bg-surface（浅+深）到对比度目标，具体值由 1.4 脚本验证迭代
- [ ] 1.2 暗色阴影变量覆盖（opacity 翻倍）
- [ ] 1.3 间距修正：grid padding 14→12、search-input 7→8
- [ ] 1.4 新增对比度验证脚本（临时 node 脚本，最终固化进测试）

### 2. 对比度自动化测试
- [ ] 2.1 新建 `src/lib/contrast.test.ts`：解析 App.css 的 :root 与 :root[data-theme=dark] 块 → 用途矩阵断言（正文 ≥4.5 / 非文本 ≥3.0 / 表面分层 ≥1.3）
- [ ] 2.2 半透明表面（header/status）颜色按 alpha 混合后计算有效对比度

### 3. 卡片固定尺寸 + 滚动
- [ ] 3.1 重写 `src/lib/grid.ts`：planGrid 简化为固定列宽除法（260px + gap 10），删除 PHI 与 scroll fallback
- [ ] 3.2 更新 `src/lib/grid.test.ts`：新断言（列数计算、边界：窗口过窄 1 列、0 项）
- [ ] 3.3 更新 `src/hooks/useGridPlan.ts`：去掉 data-scroll 逻辑，只输出 columns
- [ ] 3.4 更新 App.css：item-grid 固定行高 120px + 常驻滚动；App.tsx 移除 data-scroll 属性

### 4. 主题增强
- [ ] 4.1 App.css：color-scheme 随 data-theme 切换（dark/light 强制，system 保持 light dark）
- [ ] 4.2 App.css：过渡覆盖全部组件（卡片/按钮/输入框/高亮）
- [ ] 4.3 字号：13px → 14px（正文类）

### 5. 材质与窗口层（Rust）
- [ ] 5.1 `src-tauri/src/` 新增窗口效果模块（如 infra/effects.rs）：detect 系统版本 + 按 theme 应用 Effect（Win11 Mica/MicaDark/MicaLight，Win10 Acrylic），失败静默降级
- [ ] 5.2 lib.rs setup 时应用一次；app/settings.rs toggle_theme 时更新
- [ ] 5.3 前端半透明化：.app / header / status 用 color-mix 半透明表面
- [ ] 5.4 降级开关：settings.json 增加 windowMaterial 字段（或默认开 + 失败自动关）
- [ ] 5.5 Rust 单测：效果选择函数（版本/主题矩阵）

### 6. 全量验证
- [ ] 6.1 `npm run test`（vitest 全绿，含新 contrast.test.ts）
- [ ] 6.2 `cargo test`（Rust 单测全绿）
- [ ] 6.3 `npm run tauri build` 构建成功
- [ ] 6.4 Win10 本机运行：Acrylic 效果 + 对比度目测 + resize 性能评估
- [ ] 6.5 视觉走查：明/暗两模式、搜索过滤时卡片尺寸恒定、滚动正常

## 验证命令

```bash
cd launchpad-tauri
npm run test                    # vitest（含新对比度/网格测试）
cargo test --manifest-path src-tauri/Cargo.toml   # Rust 单测
npm run tauri build             # 全量构建
pwsh -ExecutionPolicy Bypass -File ../scripts/local-ci.ps1 -Stack tauri  # 本地门禁
```

## 风险文件 / 回滚点

- `src/lib/grid.ts` + `grid.test.ts`：布局行为变化，单 commit（可回滚）
- `src/App.css`：视觉集中改动，单 commit
- `src-tauri/src/infra/effects.rs`（新增）：材质，单 commit；降级开关保证可关
- `src/lib/contrast.test.ts`（新增）：防回归

## 完成标准

- 对比度测试全绿（正文 ≥4.5、非文本 ≥3.0、表面分层 ≥1.3）
- 卡片尺寸恒定：窗口变化/搜索过滤只变列数
- 明暗双模式视觉走查通过
- Win10 材质（Acrylic）实测无阻塞问题；Win11（Mica）代码走查 + 文档说明
- local-ci 全绿
