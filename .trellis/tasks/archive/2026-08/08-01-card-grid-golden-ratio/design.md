# 设计:卡片自适应黄金比例网格 + 整卡点击 + 主题全面升级

## 1. 布局算法(核心纯函数)

新建 `src/lib/grid.ts`,导出纯函数:

```ts
interface GridOptions {
  gap: number;      // 卡片间距(px)
  minWidth: number; // 最小可读卡片宽,默认 240
  phi: number;      // 黄金比例,默认 1.618
}

interface GridPlan {
  columns: number; // 列数
  scroll: boolean; // true = 滚动兜底模式
}

function planGrid(width: number, height: number, count: number, opts: GridOptions): GridPlan
```

算法(两模式):

```
W = width - 2*padX, H = height - 2*padY   // 网格容器内容区
模式一(黄金比例枚举):for c in 1..=count:
  r = ceil(count / c)
  w = (W - gap*(c-1)) / c
  h = (H - gap*(r-1)) / r
  若 w < minWidth 则跳过
  记分 score = |w/h - phi|,取 score 最小者
若有解 → { columns: c*, scroll: false }
模式二(滚动兜底,仅当模式一无解):
  columns = max(1, floor((W + gap) / (minWidth + gap)))
  → { columns, scroll: true }
```

要点:
- 模式一下所有卡片恰好填满网格区域(行高 1fr 平分),"用实时窗口大小显示所有卡片"由此实现。
- 模式二:列宽 ≈ minWidth,行高为内容舒适高度,垂直滚动。
- count=0(空态)由调用方短路,不调用 planGrid。

## 2. React 集成

- 新建 `src/hooks/useGridPlan.ts`:`useGridPlan(count)` 返回 `{ ref, columns, scroll }`。用 ResizeObserver 观察网格容器(比 window resize 更准,可捕获任何布局变化),防抖 100ms,回调内调用 `planGrid(rect.width, rect.height, count, opts)`。
- App.tsx:网格容器改 CSS Grid:非滚动模式 `grid-template-columns: repeat(c, 1fr); grid-auto-rows: minmax(0, 1fr); overflow: hidden`;滚动模式 `grid-template-columns: repeat(c, minmax(0, 1fr)); overflow-y: auto; grid-auto-rows: auto`。
- 卡片在 1fr 行高下内容截断:卡片内部 flex column,标题区不压缩,目录/命令区 `min-height: 0` + ellipsis(现有 .card-dir/.card-cmd 已有 ellipsis,需加 overflow 保护)。

## 3. 整卡点击启动(ItemCard.tsx)

- 点击事件从 `.card-main` 上移到卡片根元素 `onClick → launchOne(item.id)`。
- 排除区域:`label.card-check` 与 `div.card-actions` 上 `onClick stopPropagation`(checkbox 与 4 个操作按钮不触发启动)。
- 键盘:Enter 启动逻辑上移到根元素(`tabIndex={0}`),排除区域内的按钮天然可 Tab 聚焦、Enter 触发自身语义,冒泡到根的 keydown 需对 button 场景豁免(按钮 Enter 冒泡问题:button 的 keydown Enter 会冒泡到根并触发启动 —— 处理:根 onKeyDown 仅当 `e.target === e.currentTarget` 时启动)。
- 危险确认、双击防抖沿用现有 `launchOne` 链路,零改动。
- 无障碍:整卡可点击且内部有 checkbox/button 时,根元素用 `role="button"` 语义存疑 —— 保守做法:保留卡片内可聚焦控件,根元素不加 role,仅作为点击区域(与现有 card-main 行为一致,只是区域扩大)。focus-visible 焦点环样式加到根元素。

## 4. 主题体系重构(App.css)

### 4.1 Design Tokens(新分层变量)

间距:--space-1..4 = 4/8/12/16px;圆角:--radius-sm/md/lg = 4/8/12px;阴影:--shadow-sm/md/lg;焦点环:--focus-ring = 0 0 0 2px color-mix(accent 40%)。

### 4.2 浅色 token

```
--bg-app: #f6f6f7       窗口底色(网格背景)
--bg-surface: #ffffff    卡片/输入/模态
--bg-surface-hover: #f0f0f3
--bg-header: #f2f2f4
--bg-input: #ffffff
--bg-code: #f6f6f3
--fg-primary: #1f1f1f   对比度约 16:1
--fg-secondary: #5f5f66 约 6.5:1
--fg-tertiary: #8a8a92  弱化文本/提示(约 3.6:1,非必需文本可接受)
--fg-mono: #8a6d1f      代码文本
--border-subtle: #e5e5e9
--border-strong: #cfcfd6
--accent: #2f6fed       按钮白字约 4.6:1
--accent-hover: #2456c4
--danger: #d13438       危险语义(按钮白字约 5.9:1)
--danger-hover: #b02e32
```

### 4.3 深色 token

```
--bg-app: #1b1b1f
--bg-surface: #232329
--bg-surface-hover: #2c2c33
--bg-header: #202024
--bg-input: #2a2a30
--bg-code: #2a2a26
--fg-primary: #ececf1   约 13:1
--fg-secondary: #a3a3ab 约 6.8:1
--fg-tertiary: #71717b
--fg-mono: #e0b95c
--border-subtle: #34343c
--border-strong: #45454f
--accent: #4d8aff
--accent-hover: #3f76e8
--danger: #f05555
--danger-hover: #ff6b6b
```

对比度目标:正文(primary)≥ 7:1,次要(secondary)≥ 4.5:1,弱化(tertiary)≥ 3:1 且仅用于非必需文本。实施后用对比度工具抽查确认,断言写入检查清单。

### 4.4 动效与交互规范

- 统一过渡:colors/shadows 0.15s ease;主题切换 0.18s(保留现有)。
- hover:卡片 shadow-sm → shadow-md + translateY(-1px);按钮 hover 背景提升。
- active:按钮 scale(0.97)(现有)。
- focus-visible:2px accent 焦点环,radius 匹配,应用于按钮/输入/卡片。
- 禁用态:opacity 0.35 + cursor: default + 无 hover 反馈(现有,保留)。
- 滚动条:thumb radius 6px、hover 变亮(现有,对齐 token)。

### 4.5 组件打磨点(全应用)

- HeaderBar:间距对齐 8px 基准;新建按钮 primary 层级。
- StatBar:文本层级 fg-tertiary。
- StatusBar:层级统一。
- 对话框(编辑/删除/确认/关于):radius-lg、shadow-lg、按钮层级(primary/ghost/danger 体系现有,对齐新 token)。
- 空态:图标 fg-tertiary、主文案 fg-primary、次文案 fg-secondary。
- 危险警示:danger token 统一。

## 5. 测试

- `src/lib/grid.test.ts`(vitest,新函数必须有测试):
  - 单卡片 → 1 列非滚动;
  - 大窗口 N 张卡 → 列数使 |w/h - φ| 最小(断言具体输入的具体列数);
  - 超窄窗口 → scroll=true 且列数 = 兜底公式;
  - 边界:刚好达到 minWidth 的窗口。
- 现有测试全绿:`npx vitest run`;`npm run build`(tsc + vite)零错误。

## 6. 风险与回滚

- 风险 1:1fr 行高下卡片内容(标题 1 行 + 目录 + 命令)溢出 —— 以 ellipsis + min-height:0 控制,手工走查各窗口尺寸。
- 风险 2:ResizeObserver 频繁回调 —— 100ms 防抖 + 纯计算,开销可忽略。
- 风险 3:深色对比度偏差 —— 实施后对比度抽查,不达标调 token。
- 回滚:改动全部在前端(src/ 下),git revert 单 commit 即可;不动 Rust/配置。
