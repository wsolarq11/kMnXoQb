# PRD — 全图实施：修复审查发现并加固架构

## 背景

2026-07-31 全面审查（只读）发现 12 项问题，根因 6 类种子：

| 种子 | 根因 | 受害者 |
|---|---|---|
| S1 | 编排逻辑（确认策略、保存时机）住在不可测层 | 批量启动绕过确认、窗口状态 bug |
| S2 | 错误通道是"吞掉"而非"显式" | UnhandledException 静默丢数据 |
| S3 | 外部进程边界（wt/pwsh/cmd）无契约验证 | wt argv 语义、CREATE_NEW_CONSOLE 声称无凭据 |
| S4 | 架构约束只存在于文档 | 分层漂移、过期 spec、文档失配 |
| S5 | 版本/构建/发布无治理 | 依赖散落、无发布布局、CI 无缓存 |
| S6 | legacy 对齐只有口头承诺 | PushHistory 去重、Id 生成、窗口位置三处漂移 |

## 目标

按实施路线图执行全图实施，每个可处理项都有可验证的交付物；全部完成后实战运行验证 + 独立质检 PASS，并预备后续 phase。

## 任务地图

```
07-31-remediate-all-findings（父：需求集、跨子任务验收、最终整合审查）
├── 07-31-critical-fixes          A2 窗口状态 / A1 批量确认 / C1 存储建目录 / A3 PushHistory / A4 Id 生成
├── 07-31-architecture-hardening  S1 编排下沉+确认单一入口 / S2 ErrorOr / S4 ArchUnitNET / 危险标记卡片 / 文案
├── 07-31-build-governance        CPM / Directory.Build.props / lock 文件 / CI 缓存+审计 / publish.ps1
├── 07-31-contract-tests          pwsh/cmd 分支集成测试（CI 可跑）/ wt 本机契约 / CREATE_NEW_CONSOLE 验证
└── 07-31-spec-docs               过期 C++ spec 索引移除 / CLAUDE.md 修正 / spec 更新
```

## 约束

1. KISS：不引入过度工程（不引入状态机库、BuildXL、不换框架、不做 phase2 Rust 下沉）。
2. 官方 LTS/稳定方案优先（CPM、lock 文件、NuGet 审计均为官方特性；ErrorOr 为社区成熟库）。
3. 纯函数核心 + 命令式外壳：所有业务决策进 Core/UseCases，UI 只做绑定与薄编排。
4. 与旧 Rust 版行为对齐以"可验证的机器对比"为准（测试），不再靠注释声称。
5. 依赖箭头永远向下：UI → UseCases → Core ← Infrastructure（由架构测试机器执行）。
6. 不引入 emoji、代码风格遵守 CLAUDE.md。

## 验收标准（父级）

- [ ] 每个子任务按顺序完成，各自验收通过（build + test 绿）
- [ ] 修复前已知问题全部有对应测试（回归测试），无新引入行为回归
- [ ] 架构测试存在且 CI 必跑：Core 零 WinUI/IO、UseCases 零 WinUI、UI 不直连 Infrastructure
- [ ] `dotnet build`（Debug+Release）、`dotnet test` 单命令全绿
- [ ] 实战运行验证：启动、列表渲染、搜索、确认流程（含危险项）、批量启动、编辑新增删除、主题切换、窗口状态恢复（正常关闭）、单实例、配置读写
- [ ] 独立质检（verification agent）PASS，含命令级证据
- [ ] spec 与文档（CLAUDE.md、.trellis/spec）与实现一致，无 broken link、无数值失配
- [ ] 后续 phase 预案（基于阶段 2 提案的评估结论）写入任务收尾

## 实施顺序与依赖

1. critical-fixes（无依赖，先行；稳定行为基线）
2. architecture-hardening（依赖 1 的确认入口修复作为基线）
3. build-governance（与 2 并行无冲突，顺序执行以简化）
4. contract-tests（依赖 1 的 ConfigStore 建目录行为）
5. spec-docs（最后，所有实现尘埃落定后同步文档）

每个子任务完成后立即跑该子任务验收；全部完成后父级整合验证。
