# 预备后续 phase（R3 全图实施收尾）

日期：2026-08-01。R3 全图实施（阶段 0-5）完成，质检自动化项全 PASS。以下为下一迭代（phase 6+）候选方向，按优先级排序。

## P1 候选（建议优先）

1. **人工场景走查验收**（质检报告第 5 节）：新建/编辑/删除/搜索/多选/批量/确认/主题/窗口恢复/单实例/损坏恢复/IME 中文输入，逐项勾选 → 收集缺陷进修复循环。这是"实战实测验真质检"的人工补充。
2. **MSI 安装形态实测**：干净机器（或虚拟机）安装 → 验证 %APPDATA%\launchpad\config 生成与双轨切换 → 卸载残留检查。

## P2 候选

3. **自动更新**：Tauri updater（bundle createUpdaterArtifacts 已启用配置项，未接前端）；需 GitHub Release 流水线配合。
4. **Mica/Acrylic 背景**：window-vibrancy 插件（Win11 Mica / Win10 Acrylic 回退）；性能实测（eframe 类透明窗口坑）。
5. **CI 发布流水线**：GitHub Actions（windows-latest + Node + Rust 双链；Cargo.lock + package-lock.json 缓存键）；tag 触发 build + 双产物发布。
6. **窗口状态的 Rust 侧接线**：当前前端 restoreWindow best-effort；改为 Rust 侧 window 事件（close → Restored 判定 → save_window_state）。

## P3 候选

7. **每工具配置适配器**（评估报告 P12）：管理各 AI CLI 配置（cc-switch 模式），需先明确产品需求。
8. **跨平台**（macOS/Linux）：评估报告确认 Windows 优先；egui 备选路线无此需求。
9. **图标体系**：lucide-react 已满足约束；如需品牌图标（窗口图标 .ico）→ tauri.conf icon 替换。

## 交接说明

- 后续开发入口：`launchpad-tauri/`（Rust: src-tauri/src/{core,config,infra,app,commands}；前端: src/）。
- 规范：`.trellis/spec/tauri-rust-ts/index.md`（含 10 条踩坑记录）。
- 双栈并行期：C# 主线（launchpad/）保持可回滚；切换决策由用户定（质检人工项通过后）。
