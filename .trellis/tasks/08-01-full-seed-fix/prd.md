# 全图实施：种子级修复 + 生产就绪 + 实测质检 + 后续 phase 预备

## Goal

把调研发现的全部可处理项按路线图实施：P0 x:DefaultBindMode 绑定种子修复 + 勾选框 Click 事件种子修复；P1 旧版行为恢复 + Verify 行为契约 + config 韧性 + 构建治理 + 启动加固；生产就绪后实战实测验真质检（构建+测试+运行验证）；预备 P2 phase（analyzer 试点/UDF 评估）文档。

## Requirements（可处理项）

### P0 种子修复（消灭 bug 产生机制）

- P0.1 x:DefaultBindMode 绑定种子：HomeView/EditDialog 根加 `x:DefaultBindMode="OneWay"`，模板绑定显式 OneTime。修复 BUG-1（状态栏不可见）、BUG-2（计数/最近项停滞）、BUG-3（编辑框校验不可见）、BUG-13（搜索空态误导）。
- P0.2 勾选框事件种子：CheckBox 改 `Click` 事件 + `ToggleSelect` 按 Id 幂等。修复 BUG-4（快速双击丢切换）、BUG-5（绑定驱动回弹）。

### P1 行为契约与韧性

- P1.1 旧版行为恢复 + Verify 快照契约：批量启动后清除选中（BUG-11）、卡片删除确认（BUG-12）、搜索空态区分（BUG-13 行为面）。
- P1.2 config 韧性：损坏 config 自动从 .bak 恢复 + 错误提示（BUG-6）；崩溃日志写入保护（BUG-8）。
- P1.3 构建治理：System.Text.Json 源生成（去反射）、UseArtifactsOutput（bin/obj 集中 + CI 缓存单点）。
- P1.4 启动加固：TerminalDetector 结果缓存（BUG-9 同步阻塞）、TryLaunch 错误归类（BUG-10 PathNotFound 误报）。

### P2 评估与后续 phase

- P2.1 win-dev-skills analyzer 试点评估。
- P2.2 UDF 演进评估。
- P2.3 后续 phase 路线图文档。

## Acceptance Criteria

- [ ] 构建：`dotnet build src/launchpad/launchpad.csproj` 零错误、干净构建零警告
- [ ] 测试：`dotnet test tests/launchpad.Core.Tests/` 全绿（89 存量 + 新增回归）
- [ ] 契约测试：`dotnet test tests/launchpad.IntegrationTests/` 全绿（本机）
- [ ] 编译产物断言：HomeView.g.cs 出现 6 个页面级 `RegisterPropertyChangedListener`
- [ ] 实战实测验真质检：启动/主题/勾选/搜索/增删改/批量启动/删除确认/损坏 config 恢复逐一验证
- [ ] 文档：P2 评估报告 + 后续 phase 路线图入库

## Notes

- 不做：UI 自动化测试栈（WinAppDriver 停维护）、WinUIEx/MSBuildCache 引入（调研结论收益为负）、换 UI 框架。
- 子任务 7 个，独立规划/实施/检查/归档；父任务承载跨子任务验收与最终集成质检。
