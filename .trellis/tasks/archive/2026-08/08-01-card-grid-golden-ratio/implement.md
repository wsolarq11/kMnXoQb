# 实施:卡片自适应黄金比例网格 + 整卡点击 + 主题全面升级

## 实施纪律(用户 2026-08-01 明确:不将就不妥协任何一点标准,无一例外)

- 任何指标不达标即返工,不允许"先这样以后再改"。
- 对比度以实测为准(工具抽查),不靠估算;正文 ≥ 7:1、次要 ≥ 4.5:1,不达标就调 token 直到达标。
- 布局算法边界全覆盖:单卡片/空态/超窄/超多卡片,一律有断言。
- 交互细节零妥协:焦点环、禁用态、hover/active、键盘操作、危险流程、双击防抖、搜索高亮全部保留且对齐新体系。
- 视觉走查以顶级软件(Windows 11 Fluent / VS Code / Linear 级)为对照,观感不过关就继续打磨,不因"差不多"收尾。

## 实施顺序(ordered checklist)

1. `src/lib/grid.ts` — planGrid 纯函数(黄金比例枚举 + 滚动兜底),含 JSDoc 与示例。
2. `src/lib/grid.test.ts` — planGrid 单测(单卡片/多卡片黄金比例/超窄滚动兜底/边界)。
3. `src/hooks/useGridPlan.ts` — ResizeObserver + 100ms 防抖,返回 { ref, columns, scroll }。
4. `src/App.tsx` — 网格容器接入 useGridPlan,CSS Grid 渲染(1fr 行高 / 滚动兜底两模式)。
5. `src/components/ItemCard.tsx` — 整卡点击启动:根 onClick;checkbox/actions stopPropagation;根 Enter 仅 target===currentTarget 时启动;focus-visible 焦点环。
6. `src/App.css` — 主题 token 重构(浅/深两套新变量体系)+ 网格 CSS + 组件样式对齐(HeaderBar/StatBar/StatusBar/对话框/空态/滚动条)。
7. 组件微调:HeaderBar/StatBar/StatusBar/对话框/空态 的类名与 token 对齐。
8. 验证:`npx vitest run` 全绿 + `npm run build`(tsc + vite)零错误 + 手工 resize 走查。

## 验证命令

```bash
cd launchpad-tauri
npx vitest run          # 单测(新 grid 测试 + 现有全量)
npm run build           # tsc 类型检查 + vite 生产构建
# 手工走查:启动 tauri dev 或 release exe,拖动窗口 480x360 ~ 全屏,
# 验证:列数实时变化/卡片全部可见/无横向滚动/整卡点击/按钮不触发启动/深浅主题对比度
```

## 检查门(review gates)

- 单测通过后进入组件接入。
- tsc + vitest 全绿后进入视觉走查。
- 走查通过后过 `trellis-check` 全面质检,再提交。

## 风险文件与回滚点

- `src/lib/grid.ts`(新):算法核心,单测保护。
- `src/App.css`(大改):token 重构波及全部样式 —— 每个提交点 git diff 复查,防止意外丢失现有行为(危险警示、搜索高亮、boot screen)。
- 回滚:`git revert` 该任务 commit;全部改动在前端,不影响 Rust/配置。

## 提交前检查

- [ ] vitest 全绿(新 grid 测试 + 现有 6 文件测试)
- [ ] tsc 零错误
- [ ] 手工走查:resize/整卡点击/主题三态/危险流程/搜索高亮 均正常
- [ ] 深色对比度抽查(primary ≥ 7:1、secondary ≥ 4.5:1)
- [ ] prd.md AC1-AC6 逐条对照
