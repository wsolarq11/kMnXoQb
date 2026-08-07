# 系统化地址有效状态处理

## Goal

使应用以顶级软件设计标准系统化处理条目目录地址的有效性状态：用户能直观看到哪些条目的目录不存在（灰色状态文字），编辑时得到验证反馈，启动时被阻止并告知原因——而不是仅靠启动失败后的状态栏错误。

## 需求

- R1 **视觉指示**：目录不存在的条目在卡片上显示灰色状态文字（"目录不存在"），位于目录路径行下方，不与其他卡片标记（危险感叹号、选中边框）混淆
- R2 **保存时验证**：编辑对话框保存时调用 Rust 命令检查目录存在性，不存在时在对话框底部显示警告文本（但不阻止保存——用户可能先保存后修复路径）
- R3 **启动时批量扫描**：应用启动初始化时，Rust 命令批量检查所有条目的目录状态，结果存入前端 store（`dirStatus: Record<id, boolean>`），驱动卡片状态文字渲染
- R4 **启动前拒止**：点击目录不存在的卡片时，不启动进程，在状态栏显示"目录不存在"错误（复用/扩展 `WorkingDirectoryMissing` 语义）
- R5 **空目录字段豁免**：directory 字段为空字符串时不检查、不显示、"目录不存在"文字
- R6 **防回归**：新命令有 Rust 单测 + 前端可选测试

## 技术方案概要

### Rust 层
- 新增纯函数命令 `check_directory(path: &str) -> bool`（`std::path::Path::exists`）
- 新增批量命令 `check_directories(paths: Vec<String>) -> Vec<bool>`（循环调用，无状态改变）
- 启动命令 `launch_item` / `launch_many` 在 spawn 前检查目录存在性，若不存在直接返回 `WorkingDirectoryMissing`（现有错误分类就绪，无需新增错误类型）

### 前端层
- `useAppStore`：新增 `dirStatus: Record<string, boolean>` 字段 + `checkAllDirectories` action
- `App.tsx`：`init()` 成功后调用 `checkAllDirectories()`
- `ItemCard`：当 `dirStatus[id] === false` 时，在卡片目录行下方显示灰色状态文字
- `EditDialog`：保存时调用 `checkDirectory`，若不存在则在底部显示警告（保存仍允许）
- `useAppStore` 的 `launchOne` / `launchMany`：启动前检查目录状态，若 false 则设状态栏错误并 return

### 数据流
```
App init → list_items → check_directories → store.dirStatus
                                    ↓
                              ItemCard 渲染 → 灰色状态文字

编辑保存 → check_directory(path) → false → 警告提示（仍可保存）

点击启动 → dirStatus[id] === false → 状态栏"目录不存在" → 不启动
```

## Acceptance Criteria

- [x] AC1 目录不存在的卡片在目录路径行下方显示灰色"目录不存在"文字（空目录字段豁免）
- [x] AC2 目录存在的卡片/空目录卡片不显示该文字
- [x] AC3 编辑对话框保存时，目录不存在则显示警告（保存仍允许）
- [x] AC4 点击目录不存在的卡片，不启动进程，状态栏显示"目录不存在"
- [x] AC5 应用启动时批量扫描，目录状态准确反映当前文件系统（不缓存/持久化）
- [x] AC6 危险命令（红色感叹号）与目录失效（灰色文字）互不干扰
- [x] AC7 全部现有测试全绿（vitest 39/39、cargo 137 例；修复了 launch_many failed_indexes 的 HashSet 随机顺序回归）

## Out of Scope

- 目录变化的实时监听（文件系统 watcher）——仅启动时/保存时/启动前检查
- 命令可执行性（exe 是否存在）的检查——仅目录存在性
- 跨平台（macOS/Linux）适配——当前仅 Windows
- 目录状态的持久化/缓存——每次启动重新扫描
