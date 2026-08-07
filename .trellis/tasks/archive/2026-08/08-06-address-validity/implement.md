# implement.md — 系统化地址有效状态处理

## 前置

- 设计基准：design.md（Rust 命令 + 前端 store + 卡片状态文字 + 编辑验证 + 启动拒止）
- 技术栈不变：Tauri 2.11 + React + Rust
- 决策：范围——卡片标记 + 保存时验证 + 启动阻止；视觉——灰色状态文字；验证时机——启动时批量 + 保存时 + 启动前

## 实施清单（有序）

### 1. Rust 命令（check_directory / check_directories）
- [ ] 1.1 `commands/misc.rs` 新增 `check_directory` 命令（单条，纯 `Path::exists`，空路径豁免返回 true）
- [ ] 1.2 新增 `check_directories` 命令（批量，循环调用，单次 IPC 返回所有结果）
- [ ] 1.3 `lib.rs` invoke_handler 注册 `check_directory` / `check_directories`
- [ ] 1.4 Rust 单测：空路径 true、存在路径 true、不存在路径 false、批量长度一致

### 2. 启动拒止（launch 前检查）
- [ ] 2.1 `commands/launch.rs` 的 `launch_item_impl`：spawn 前检查目录存在性，不存在则直接返回 WorkingDirectoryMissing
- [ ] 2.2 `launch_many_impl`：同理，对每个条目提前检查，失效项计入失败
- [ ] 2.3 测试：集成测试覆盖"目录缺失 → 拒止 + WorkingDirectoryMissing"

### 3. 前端 store
- [ ] 3.1 `useAppStore.ts`：`dirStatus` 字段 + `checkAllDirectories` action（调用 `check_directories`）
- [ ] 3.2 `init()` 成功后调用 `checkAllDirectories()`
- [ ] 3.3 `launchOne` / `launchMany` 启动前检查 `dirStatus[id]`，false 则设状态栏错误并 return

### 4. 卡片状态文字
- [ ] 4.1 `ItemCard.tsx`：`dirStatus[item.id] === false` 时渲染状态文字行（获取 `dirStatus` 从 store）
- [ ] 4.2 `App.css`：新增 `.card-dir-status { font-size: 12px; color: var(--fg-tertiary); }`

### 5. 编辑对话框验证
- [ ] 5.1 `EditDialog.tsx`：save 时调用 `checkDirectory`，false 则显示警告（不阻止保存）
- [ ] 5.2 警告文本复用 `.error-text` 红色样式

### 6. 国际化
- [ ] 6.1 `src/i18n/keys.ts` 新增 `DirectoryMissing` 键 + 中英翻译（"目录不存在" / "Directory missing"）
- [ ] 6.2 更新 `keys.test.ts`（键表完整性测试会抓缺失键）

### 7. 全量验证
- [ ] 7.1 `npm run test`（vitest 全绿）
- [ ] 7.2 `cargo test`（Rust 单测 + 集成测试全绿）
- [ ] 7.3 `npm run tauri build` 构建成功
- [ ] 7.4 人工走查：启动应用 → 卡片状态文字显示 → 编辑时验证 → 失效卡片启动拒止

## 验证命令

```bash
cd launchpad-tauri
npm run test
cargo test --manifest-path src-tauri/Cargo.toml
npm run tauri build
```

## 风险文件 / 回滚点

- `src-tauri/src/commands/misc.rs`（新增命令，可独立回滚）
- `src-tauri/src/commands/launch.rs`（启动拒止逻辑，改动小）
- `src/stores/useAppStore.ts`（核心状态管理，谨慎修改）
- `src/components/ItemCard.tsx`（渲染逻辑，新增一行）
- `src/lib/invoke.ts`（新增 API 封装）

## 完成标准

- 目录不存在的卡片显示灰色状态文字，目录存在/空目录不显示
- 编辑保存时目录不存在则警告（保存仍允许）
- 点击失效卡片不启动，状态栏报错
- 危险命令标记与目录失效状态互不干扰
- 全部现有测试全绿
- local-ci 全绿
