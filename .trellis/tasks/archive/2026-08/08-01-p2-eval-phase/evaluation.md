# P2 评估报告：analyzer 试点 + UDF 评估 + 后续 phase 路线图

## 1. win-dev-skills analyzer 试点评估

### 试点过程（实证记录）

1. **规则目录实证**（调研轮）：`Microsoft.WindowsAppSDK.Analyzers` 的 `WUI2xxx` 类别明确含 **"`x:Bind` without `Mode`"** 规则——正是本项目此前 BUG-1/2/3 的静态门禁形态。
2. **预构建分发物接线尝试**：从官方仓库下载 `plugins/winui/skills/winui-dev-workflow/analyzer/` 下的 `Microsoft.WindowsAppSDK.Analyzers.dll`（49,664 字节，MZ 头完整）+ 配套 `.targets`（注入 XAML 为 AdditionalFiles），挂载到 launchpad 构建 → **CS8034 加载失败**（"Could not load file or assembly 'Microsoft.WindowsAppSDK.Analyzers, Version=1.0.0.0'"）。
3. **源码构建尝试**：稀疏 clone `microsoft/win-dev-skills` 的 `src/tools/winui-analyzer`，`.NET 10 SDK` 构建 `Microsoft.WindowsAppSDK.Analyzers.slnx -c Release` 成功（0 错误）→ 挂载源码产物 → **同样 CS8034**。该 analyzer 引用 `Microsoft.CodeAnalysis.CSharp 4.12.0`（PrivateAssets=all，不随包分发），与 .NET 10 SDK 内置 Roslyn（5.x）加载不兼容。
4. **分发现状**：README 明确「预构建 DLL 提交在 skill 的 analyzer/ 载荷下，每次源码变更后需 scripts/build-tools.ps1 刷新」——**无官方 NuGet 分发**，仅随 win-dev-skills skill 走。

### 评估结论：当前阶段不引入（理由 + 置信度）

| 维度 | 置信度 | 依据 |
|------|--------|------|
| 机器 | 85% | 规则目录实证存在；接线失败已实证（Roslyn 版本兼容） |
| 用户 | 80% | 规则面（无 Mode 绑定）已被 x:DefaultBindMode + 显式 OneTime 配方从源头覆盖 |
| 我 | 85% | 接线成本高（源码构建/版本兼容），收益边际化 |

**触发再评估的条件**：微软发布官方 NuGet 包；或 XAML 规模显著增长（新页面/复杂模板）。

## 2. UDF（单向数据流）评估

### 现状

HomeViewModel 经本轮改造后状态面已大幅收敛：所有集合变更走纯函数（ItemUseCase）、勾选走「目标状态 + 按 Id」幂等语义、绑定全部 OneWay（页面级）/OneTime（模板级）、路由事件全部 Defer。**回弹/竞态类 bug 的种子已被工程措施压制**。

### 评估结论：本阶段不引入完整 UDF/reducer 框架

- 单页应用 + 已收敛状态面：UDF 的样板成本（Action 类型、reducer 分发、Store 接线）> 收益。
- 现有结构已是「准 UDF」：命令（Action）→ 纯函数（Reducer）→ 状态（ObservableProperty）→ OneWay 绑定（View）。缺的只是形式化（Action 枚举/统一 Store），无当前痛点驱动。
- 触发再评估：多页面共享状态、撤销/重做需求、状态时序 bug 复发。

| 维度 | 置信度 | 依据 |
|------|--------|------|
| 机器 | 90% | 现有 106 测试 + 快照契约已覆盖状态面 |
| 用户 | 85% | 当前无 UDF 可消除的新 bug 类 |
| 我 | 85% | 引入即重写 VM 层，违背 KISS |

## 3. 后续 phase 路线图（P3+ 候选）

| 优先级 | 项 | 触发条件 | 预期收益 |
|--------|----|---------|---------|
| P3 | MVVMTK0045/0034 清理（partial property 迁移） | 启用 AOT/Trim 前必须；当前为推荐性警告 | 警告归零、AOT 就绪 |
| P3 | analyzer 正式接入 | 官方 NuGet 发布 | XAML 静态门禁 |
| P3 | CI bin/obj 缓存（artifacts/ 单一目录） | 构建时间成为瓶颈 | CI 提速 |
| P4 | UDF 形式化 | 多页面/状态共享需求 | 状态架构演进 |
| P4 | UI 自动化（NovaWindows Driver 成熟后） | 生态成熟评估 | 端到端覆盖 |
| 持续 | 行为契约快照扩展 | 新行为面出现时 | 防漂移 |
| 持续 | XAML 静态检查（无 Mode 绑定 grep 进 CI） | 立即可行（零成本） | 绑定默认值回归防线 |

### 立即可行的零成本项（可并入下次提交）

- CI 加一条 `grep -L "x:DefaultBindMode" **/*.xaml` 检查：新页面必须显式声明默认绑定模式（防「新页面忘了根属性」回归）。
