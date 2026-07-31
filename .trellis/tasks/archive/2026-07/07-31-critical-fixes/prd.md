# PRD — 止血修复：窗口状态/批量确认/存储健壮性

## 背景

审查确认 5 项可独立验证的缺陷，全部有旧 Rust 版可对照行为：

1. **A2 窗口状态**：`MainWindow.OnClosed` 保存最小化坐标（-32000,-32000）到 settings.json（当前文件已是坏状态），下次启动 `MoveAndResize` 恢复到屏幕外。旧 Rust 版从不恢复 x/y。
2. **A1 批量启动绕过确认**：`HomeViewModel.LaunchSelected` 对选中项直接启动，绕过 `NeedsConfirm` 且不更新 launch history。旧 Rust 版有 batch_confirm 二次确认。
3. **C1 存储不建目录**：`ConfigStore` 写前不 `CreateDirectory`，发布场景（无 config/ 祖先）首次保存抛 DirectoryNotFoundException，被 UnhandledException 吞掉 → 静默丢数据。
4. **A3 PushHistory 无去重**：`LaunchUseCase.PushHistory` 注释声称 "matches legacy behavior: no deduplication"——**错误**。旧 Rust 版去重（retain 移除同名 → insert(0) → truncate）。当前 settings.json 有 "codex_temp" 重复证据。
5. **A4 Id 生成不对齐**：`ItemUseCase.NewItem` 用 `name.Replace(' ', '_')`，旧 Rust 版 `name.to_lowercase().replace(' ', "_")` + 冲突时追加 `_2`/`_3` 后缀去重。

## 需求

- [ ] A2：关闭时若窗口处于最小化状态，不保存坏坐标（保存关闭前正常状态或默认位置）；恢复时 clamp 到虚拟屏幕边界，保证窗口可见
- [ ] A1：`LaunchSelected` 走确认策略（与单卡一致：全局开关 && (item.confirm || 危险)）；需要确认的项进确认流程；同时批量启动也更新 launch history（与单卡一致）
- [ ] C1：`ConfigStore` 构造或写前确保目录存在（CreateDirectory）
- [ ] A3：`PushHistory` 对齐旧版：移除同名旧条目 → 头部插入 → 截断 max；更新错误注释
- [ ] A4：`NewItem` 对齐旧版：小写化 + 空格转下划线 + 集合内冲突时 `_2`/`_3` 后缀去重

## 验收标准

- [ ] 每项修复有回归测试（A2 的 clamp 逻辑提取为可测纯函数；A1 确认决策可测；A3 去重断言；A4 冲突断言）
- [ ] `dotnet test` 全绿（新增 + 原有）
- [ ] 现有 settings.json 的坏窗口状态被修复逻辑覆盖（恢复时 clamp 保证窗口可见；或首次启动自动纠偏）
- [ ] 注释与实现一致（删除/修正"no deduplication"等错误声称）

## 约束

- 窗口状态的判断/纠偏逻辑放纯函数层（Core），WinUI 层只调用——保证可测
- 行为对齐以旧 Rust 归档代码为基准（archive/launchpad-rs/src/app.rs）
