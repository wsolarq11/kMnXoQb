# WT Launcher

Windows Terminal 启动器。统一管理并一键启动各类 AI 编码代理（snow、codex、claude、opencode 等），在指定项目目录中以 Windows Terminal 拉起对应命令。

## 功能

- 启动项管理：可视化新增、编辑、删除启动项
- 批量启动：多选后一键拉起多个终端
- 启动前确认：每个启动项可单独配置是否需要二次确认
- 危险命令识别：含 `dangerously`、`yolo`、`skip-permissions` 等标志的命令会被标记；启用「启动确认」开关时强制弹窗确认
- 配置持久化：选中状态与确认设置跨重启保留
- 命令行安全转义：目录与命令参数按 Windows 终端规则正确引号化，避免注入与解析错误

## 快速开始

双击 `WT Launcher.hta` 即可运行。首次使用时，将 `config/config.example.json` 复制为 `config/config.json`，并按实际环境修改其中的目录与命令。

## 目录结构

```
launchpad/
├── WT Launcher.hta            启动器应用，双击运行
├── config/
│   ├── config.example.json    配置模板，随仓库分发
│   ├── config.json            本地工作配置，不入库（含机器相关绝对路径）
│   ├── config.json.bak        保存时自动生成，不入库
│   └── settings.json          UI 偏好（启动确认开关），入库
├── design/
│   └── winforms-launcher.md   历史架构经验记录
├── tools/
│   └── verify.ps1             配置与规则验证脚本
├── .gitignore
└── .gitattributes
```

## 启动项配置契约

`config/config.json` 是一个 JSON 数组，每个元素为一个启动项：

| 字段        | 类型    | 说明                                          |
| ----------- | ------- | --------------------------------------------- |
| `name`      | string  | 显示名称                                      |
| `directory` | string  | 启动目录，运行时校验必须存在                  |
| `command`   | string  | 在该目录中执行的命令                          |
| `confirm`   | boolean | 为 true 时启动前弹窗确认                      |
| `id`        | string  | 稳定标识，用于持久化选中状态，建议等于 `name` |
| `selected`  | boolean | 是否处于批量选中状态，跨重启保留              |

最终执行的命令形如：

```
wt -d "<directory>" pwsh -NoExit -Command "<command>"
```

## 安全边界

- 启动前校验目录是否存在、命令是否为空。
- 启动确认由全局开关 `config/settings.json` 的 `confirmEnabled` 控制，默认 `false`（直接启动，不弹窗）。打开后，per-item `confirm` 与危险命令（含 `dangerously`、`yolo`、`skip-permissions`、`bypass-approvals`、`bypass-sandbox`）会触发确认。可在界面顶栏的「启动确认」开关切换，状态持久化。
- `config.json` 含本机绝对路径，默认不入库；随仓库分发的是脱敏模板 `config.example.json`。

## 验证

在仓库根目录执行：

```powershell
pwsh tools/verify.ps1
```

校验配置可解析、字段完整、危险命令识别规则与命令行转义逻辑。
