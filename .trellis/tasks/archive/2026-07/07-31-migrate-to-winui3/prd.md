# Migrate to WinUI 3 + C# (Windows Native)

## Goal

把启动器从 Flutter + Rust（flutter_rust_bridge）迁移到 Windows 原生 WinUI 3 + C#（.NET 10 LTS）。

本任务执行**阶段 1：全 C# 实现**（含领域层），学习目标：XAML、MVVM、依赖注入、六边形架构、xUnit 测试。**阶段 2（核心下沉 Rust + P/Invoke）不在本任务范围**，是后续任务。

## Requirements

### 功能需求（与 Flutter 版 1:1 对齐）

1. **列表管理**：新增、删除、编辑、上移/下移排序、多选、Select All、搜索过滤（name/directory/command 子串，不区分大小写）。
2. **启动流程**：点击卡片启动；确认逻辑 = 全局 `confirmEnabled && (item.confirm || isDangerous)`；确认对话框展示 name/command/directory + 危险原因；批量启动选中项；启动后写 launchHistory（最近 10 条）。
3. **危险检测**：flag 表与 Rust 版完全一致（dangerously / yolo / skip-permissions / bypass-approvals / bypass-sandbox / bypass.sandbox，不区分大小写子串匹配）；三处展示：编辑框实时警告、卡片警告、确认对话框警告。
4. **配置**：`config.json`（LaunchItem 数组）+ `settings.json`（AppSettings），读写与旧格式完全兼容；写 items 前备份 `config.json.bak`；配置目录**保持 `../config` 相对路径**（用户决策 D2）。
5. **主题**：深/浅双主题手动切换，主色 Indigo #6366F1，颜色 token 与 theme.dart 对齐（dark base #0A0A0A / light base #F8FAFC 等）；使用**系统材质**（Mica / Acrylic 回退，用户决策 D1）。
6. **历史与统计**：顶部 ITEMS / RECENT 统计卡。
7. **图标**：lucide 图标集（PathIcon 方案），禁止其他图标库。
8. **编辑对话框**：Name / Directory（存在性验证：绿勾/橙警告）/ Command（危险实时警告）/ Terminal（可选）/ Confirm 复选框；目录选择用 Windows 原生文件夹选择器。
9. **其他 UI**：空状态（无 item 提示 + New 按钮）、底部状态栏（操作反馈）、窗口标题 "WT Launcher"。
10. **稳定性**：单实例锁（用户决策 D4）+ 窗口位置/大小恢复（window_state）。

### 架构需求

- MVVM（CommunityToolkit.Mvvm）+ 依赖注入（Microsoft.Extensions.DependencyInjection）。
- 六边形分层：领域层（纯函数：模型 + 校验 + plan + 危险检测）/ 应用层（用例编排）/ 基础设施层（ConfigStore、ProcessSpawner、TerminalDetector 等端口实现）。
- 零 shell 启动：`System.Diagnostics.Process` + `ArgumentList`（argv 数组），禁止字符串拼接执行。
- 错误处理：类型化错误；配置解析失败不得静默丢弃（保留 .bak、错误提示）。
- 测试：xUnit + 手写 fakes；领域层全覆盖，应用层主路径。

### 约束

- .NET 10 LTS（本机 SDK 10.0.301）+ Windows App SDK（版本见 design.md）。
- 官方 LTS 稳定方案，不用社区偏方。
- 图标只用 lucide；代码无 emoji。
- 代码风格：函数 4-20 行、文件 <500 行、显式类型、无重复、命名具体。

## Acceptance Criteria

- [ ] `dotnet build --release` 通过，应用可启动。
- [ ] 功能逐项对比清单（10 条）全部 1:1 覆盖 Flutter 版。
- [ ] 旧 `config.json` / `settings.json` 直接可用（数据兼容，不丢）。
- [ ] 零 shell：启动路径全部使用 `Process.ArgumentList`，无字符串拼接执行。
- [ ] `dotnet test` 单命令通过，领域层全覆盖、应用层主路径覆盖。
- [ ] 深/浅双主题截图与 Flutter 版对照，差异记录在案。
- [ ] `launchpad_flutter/` 与 `launchpad-rs/` 移入 `archive/`；根 CLAUDE.md 更新为 WinUI 3 架构。
- [ ] 单实例锁 + 窗口状态恢复实测通过。

## Out of Scope

- 阶段 2：核心下沉 Rust + P/Invoke（后续任务）。
- 跨平台、内嵌终端、Web 版。

## Notes

- 用户决策记录（D1-D4）详见 design.md §8。
- 视觉差异允许存在（Mica 版与 Flutter 版），以截图对照 + 记录为准。
