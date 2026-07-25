# WT Launcher

跨平台终端启动器。统一管理并一键启动各类 AI 编码代理（snow、codex、claude、opencode 等），在指定项目目录中拉起终端执行命令。

## 功能

- 启动项管理：可视化新增、编辑、删除启动项
- 卡片列表：搜索过滤、hover 操作按钮、危险命令红色标记
- 批量启动：多选后一键拉起多个终端，逐项确认
- 启动前确认：per-item confirm + 危险命令触发弹窗确认
- 主题切换：Light / Dark / System 循环切换，持久化到 settings.json
- 动态状态栏：显示最近启动项或 Ready
- Ctrl+A 全选/取消全选快捷键
- 配置持久化：选中状态、确认设置、主题偏好跨重启保留
- 命令行安全转义：按 Windows 终端规则正确引号化
- 运行时检测：启动前校验目录是否存在
- 自定义终端覆盖：per-item terminal 字段覆盖默认终端

## 快速开始

### 构建

需要 Visual Studio 2022+ BuildTools（或完整 VS）与 CMake。使用构建助手脚本：

```powershell
pwsh tools/build.ps1
```

或手动：

```powershell
# 在 VS Developer Command Prompt 中执行
cmake --preset debug
cmake --build build/debug
```

### 配置

将 `config/config.example.json` 复制为 `config/config.json`，按实际环境修改目录与命令。

## 目录结构

```
launchpad/
├── src/
│   ├── core/          核心业务逻辑（Config/Launcher/is_dangerous/quote_arg/SelectedStore）
│   ├── platform/      平台抽象层（PathResolver/TerminalLauncher/SingleInstance）
│   └── ui/            Slint 界面（main_window/launch_card/dialog/theme + lucide 图标）
├── tests/core/        单元测试（config/quote_arg/is_dangerous/launcher/selected_store）
├── config/
│   ├── config.example.json   配置模板，随仓库分发
│   ├── config.json           本地工作配置，不入库
│   ├── config.json.bak       保存时自动生成
│   └── settings.json         UI 偏好（confirmEnabled/theme）
├── tools/
│   ├── build.ps1             构建助手脚本（自动初始化 MSVC 环境）
│   └── verify.ps1            配置与规则验证脚本
├── CMakeLists.txt
├── CMakePresets.json
└── vcpkg.json
```

## 启动项配置契约

`config/config.json` 是一个 JSON 数组，每个元素为一个启动项：

| 字段 | 类型 | 说明 |
|------|------|------|
| `name` | string | 显示名称 |
| `directory` | string | 启动目录，运行时校验必须存在 |
| `command` | string | 在该目录中执行的命令 |
| `confirm` | boolean | 为 true 时启动前弹窗确认 |
| `id` | string | 稳定标识，用于持久化选中状态 |
| `selected` | boolean | 是否处于批量选中状态 |
| `terminal` | string? | 可选，覆盖默认终端 |
| `tag` | string? | 可选，标签分类 |
| `group` | string? | 可选，分组 |

## 安全边界

- 启动前校验目录是否存在、命令是否为空
- 启动确认由全局开关 `settings.json` 的 `confirmEnabled` 控制
- 危险命令检测：`dangerously`、`yolo`、`skip-permissions`、`bypass-approvals`、`bypass-sandbox`
- 批量启动逐项检查确认设置
- 命令行参数按 Windows 终端规则正确引号化

## 验证

```powershell
pwsh tools/verify.ps1     # 配置与规则验证
ctest --test-dir build/debug  # 单元测试
```

## 技术栈

- C++23 + CMake + Ninja
- Slint 1.17（UI 框架）
- Glaze 7.9（JSON 序列化）
- doctest 2.5.3（单元测试）
- vcpkg（依赖管理）
- Windows / macOS / Linux（跨平台）
