# design.md — 系统化地址有效状态处理

## 架构边界

| 层 | 改动点 | 说明 |
|---|---|---|
| Rust 命令（commands/misc.rs） | 新增 `check_directory` / `check_directories` | 纯读操作，无副作用 |
| Rust 启动（commands/launch.rs） | launch_item 前检查目录存在性 | 提前返回 WorkingDirectoryMissing |
| 前端 store（useAppStore.ts） | 新增 `dirStatus` 字段与 check action | 暂存运行时状态，不持久化 |
| 前端 App.tsx | init 后调用批量检查 | 启动时扫描 |
| 前端 ItemCard | 条件渲染"目录不存在"文字 | 灰色 fg-tertiary，固定高度 120px 内布局 |
| 前端 EditDialog | 保存时检查目录并警告 | 不阻止保存 |

## Rust 命令设计

### 命令签名

```rust
// 单条检查（前端可调用，也用于批量检查的内部循环）
#[tauri::command]
pub fn check_directory(path: String) -> bool {
    path.is_empty() || std::path::Path::new(&path).exists()
}

// 批量检查（启动时一次调用，减少 IPC 次数）
#[tauri::command]
pub fn check_directories(paths: Vec<String>) -> Vec<bool> {
    paths.into_iter().map(|p| check_directory_impl(&p)).collect()
}
```

注意：`check_directory` 返回 `true` 当路径为空（豁免）或目录存在（有效）。前端使用 `dirStatus[id] === false` 判断。

### launch 修改

`commands/launch.rs` 的 `launch_item_impl` 在 spawn 前检查目录：
```rust
// 如果目录非空且不存在，提前返回 WorkingDirectoryMissing
if !item.directory.is_empty() && !std::path::Path::new(&item.directory).exists() {
    return Err(AppError::WorkingDirectoryMissing(item.directory.clone()));
}
// 否则继续 spawn（现有逻辑）
```

`launch_many_impl` 同理：对每个条目提前检查，失效项计入失败计数。

### 编辑保存时验证

前端 EditDialog 调用 `check_directory`，不是 Rust 命令层修改——前端在 save 函数内 invoke 检查并显示警告。

## 前端 Store

```typescript
interface AppState {
  // ... 现有字段
  dirStatus: Record<string, boolean>;  // itemId -> exists (true=存在, false=不存在)
}

// 新增 action
checkAllDirectories: () => Promise<void>;
```

初始化流程：
```typescript
init: async () => {
  await loadItems();
  await checkAllDirectories();
}
```

## 卡片渲染

ItemCard 新增状态文字行，位于 card-dir 下方、card-cmd 上方，仅在目录失效时渲染：

```tsx
<div className="card-main">
  <div className="card-title">...</div>
  <div className="card-dir">{highlight(item.directory, query)}</div>
  {dirStatus[item.id] === false && (
    <div className="card-dir-status">{t("DirectoryMissing", language)}</div>
  )}
  <code className="card-cmd">...</code>
</div>
```

CSS：`.card-dir-status { font-size: 12px; color: var(--fg-tertiary); }`——充分利用卡片 120px 固定高度的余量。

## 编辑对话框

EditDialog 的 save 函数：
```typescript
async function save() {
  // 现有 name/command 非空验证...
  // 新增：检查目录存在性
  if (directory.trim() && !(await checkDirectory(directory.trim()))) {
    setWarning(t("DirectoryMissing", language));
    // 不阻止保存——用户可继续
  }
  // 继续保存逻辑...
}
```

警告文本用 `.error-text`（红色）或新增 `.warning-text`（黄色/橙色？）——用 `.error-text`（红色，但注意"不阻止保存"——红色警告+保存可继续，可能让用户困惑。更好的方案：用 `.danger-warning` 样式（现有用于危险命令的警告）但颜色不同？或者新增 `.warning-text` 用 fg-tertiary 色。

推荐：复用 `.error-text`（红色），因为这是需要用户注意的问题。保存按钮仍然可用，但红色文字提示"目录不存在，保存后仍可运行"。——或者更简洁：`.error-text` 显示"目录不存在，建议先设置目录"。

## 国际化

需要在 `src/i18n/keys.ts` 新增 `DirectoryMissing` 键，中英翻译。

## 风险

- 卡片 120px 固定高度：加一行文字后长标题+状态文字可能溢出。状态文字本身只有 12px 1 行，现有布局有 60px 余量，安全。
- 启动时批量检查大量条目（50+）时 IPC 延迟——`check_directories` 单次命令返回所有结果，只有一次 IPC 开销。
- 目录状态只在启动时扫描，不实时更新。用户可能启动后移动目录——折中：启动前检查 + 每次启动时检查。如果用户打开编辑对话框后移动目录，保存时会再次检查。
