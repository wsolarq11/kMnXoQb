# Archive

本目录是 WT Launcher 项目的退役制品与版本历史归档目录，用于保留不再在役但需可追溯的历史快照。

## 归档制品

### _retired.zip

项目早期完整快照，归档于 2026-07-25。压缩包内顶层目录为 `_retired/`，包含：

| 文件 | 说明 |
| --- | --- |
| `WT Launcher.hta` | 早期 HTA 实现（22694 字节，早于当前在役版本） |
| `config/config.json` | 早期配置 |
| `config/config.json.bak` | 早期配置备份 |
| `design/winforms-launcher.md` | 早期 WinForms 退役记录（3218 字节，早于当前 design 目录版本） |
| `README.md` | 早期说明文档 |

该快照在 Slint 跨平台迁移启动前归档，作为回溯参照。当前在役实现仍为根目录的 `WT Launcher.hta`。

## 入库策略

归档目录中的二进制压缩包不入库（见根目录 `.gitignore` 的 `archive/*.zip` 规则），仅本索引文件入库以保持可追溯。
