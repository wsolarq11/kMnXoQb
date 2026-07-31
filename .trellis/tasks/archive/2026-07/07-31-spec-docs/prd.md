# PRD — spec 与文档治理

## 背景

审查确认（S4/B1-B5）：
- B1：CLAUDE.md 声称"60 个测试"，实际 52（50 Fact + 2 Theory）
- B3：CLAUDE.md 引用的 phase2-proposal 路径错误（漏 `archive/` 段，broken link）
- B2：CLAUDE.md 声称危险标记"三处展示（编辑框/卡片/确认框）"，卡片缺失（02 任务补齐实现后文档随之正确）
- B5：.trellis/spec 下 4 个过期 C++ 时代 spec（cmake-build/cpp-core/cross-platform/slint-ui）仍在可用索引列表
- 历史链接：CLAUDE.md"60 个测试"、提交信息"58 tests green"均需核对

## 需求

- [ ] 过期 spec 处理：4 个 C++ 时代 spec 目录移出可用索引（方案：目录改名 `_retired-*` 或加 deprecated 标记文件 + 从 session 索引/grep 面移除——以 get_context.py 的发现机制为准，检查后选最小方案）
- [ ] CLAUDE.md 修正：
  - 测试数改为实际值（实施后以 CI 输出为准，写"dotnet test 单命令全绿"不写死数字，或写实时数字）
  - phase2 链接改为 archive 实际路径
  - 危险标记描述与实际一致（三处→实际两处或实现补齐后保持三处）
  - PushHistory/Id 生成行为描述与实现一致（01 对齐后）
- [ ] spec 更新（winui3-csharp 或 guides）：新增内容——架构测试规则、ErrorOr 错误通道、契约测试结论（含 CREATE_NEW_CONSOLE 人工验证结论）、CPM/lock 文件构建约定
- [ ] 全库 grep 检查 broken link / 数值失配（测试数、版本号、路径）

## 验收标准

- [ ] `python .trellis/scripts/get_context.py --mode packages`（或等价命令）不再列出过期 spec
- [ ] CLAUDE.md 无 broken link、测试数一致、行为描述与代码一致
- [ ] 新增 spec 内容覆盖本次实施（架构/错误/构建/契约四块）
- [ ] 无"60 个测试"等失配数字残留（grep 验证）

## 约束

- 不改代码只改文档（代码行为由 01-04 任务负责）
- 不删除归档（archive 是历史，只改索引与引用）
