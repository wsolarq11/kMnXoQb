# Archive

本目录是 WT Launcher 项目的退役制品与版本历史归档目录，用于保留不再在役但需可追溯的历史快照。

## 归档制品

| 目录/文件 | 说明 |
| --- | --- |
| `launchpad_flutter/` | 2026-07-30 的 Flutter + Rust（flutter_rust_bridge）实现，含 Awwwards 级视觉重设计；2026-07-31 被 WinUI 3 版取代 |
| `launchpad-rs/` | 更早的纯 Rust + egui 实现；被 Flutter 版取代后保留 |
| `_retired.zip` | 项目早期完整快照（2026-07-25，HTA 时代） |
| `winui3-screenshots/` | WinUI 3 迁移验收截图（深/浅主题对照） |

## 在役实现

当前在役实现为 `launchpad/`（WinUI 3 + C#，.NET 10 LTS）。参见根 `CLAUDE.md`。

## 入库策略

归档目录中的二进制压缩包不入库（见根 `.gitignore` 的 `archive/*.zip` 规则）；源码目录随 git 跟踪，构建产物由各子项目的 `.gitignore` 排除。
