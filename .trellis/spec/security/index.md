# 安全编码规范

## 核心原则

**禁止将用户可控文本通过 shell 解释器执行。**

## 命令注入防御

- ✅ 所有子进程通过 **`Process.ArgumentList`** 以 argv 数组启动（`ProcessSpawner`，零字符串拼接）
- ✅ Windows 底层走 `CreateProcessW`，无 shell 介入
- ❌ 禁止 `Process.Start(string)` 传入拼接命令字符串
- ❌ 禁止字符串拼接构造命令（`cmd + " " + args`）
- ✅ 命令参数通过 `LaunchPlan.Executable` + `LaunchPlan.Args` 数组传递（`LaunchPlanner.PlanWindows` 纯决策）
- ✅ 终端目录路径参数化传递（wt.exe `-d <dir>`；pwsh fallback 单引号转义 `''`；cmd fallback 双引号转义 `""`）

## 安全规范自动化

### 单元测试覆盖（主要门禁）

零 shell 约束由单测覆盖，提交前 `dotnet test tests/launchpad.Core.Tests/` 必须全绿：

| 测试文件 | 覆盖内容 |
|----------|----------|
| `LaunchPlannerTests` | 断言 `PlanWindows` 产出的 argv 数组逐项（wt/pwsh/cmd 三路径、目录转义） |
| `LaunchUseCaseTests` | fakes 断言 `IProcessSpawner` 收到的 argv，验证拒绝路径（空命令、未知项）不触达启动 |
| `ItemValidatorTests` | 目录存在性、命令非空校验 |

### pre-commit hooks

`.pre-commit-config.yaml` 包含（基础检查）：

| Hook | 触发时机 | 用途 |
|------|----------|------|
| `trailing-whitespace` | 提交前 | 尾随空白 |
| `end-of-file-fixer` | 提交前 | 文件末尾换行 |
| `check-yaml` | 提交前 | YAML 语法 |
| `check-added-large-files` | 提交前 | 大文件（>500KB）阻断 |
| `check-merge-conflict` | 提交前 | 合并冲突标记 |
| `mixed-line-ending`（--fix=crlf） | 提交前 | 行尾统一 CRLF（Windows 项目） |

## 配置安全

- `DangerousFlagDetector.IsDangerous()` 对用户自定义命令做 6 个 flag 子串匹配（dangerously / yolo / skip-permissions / bypass-approvals / bypass-sandbox / bypass.sandbox，不区分大小写）
- 确认逻辑：`confirmEnabled && (item.Confirm || IsDangerous(command))`，危险命令在三处展示警告（编辑框/卡片/确认对话框）
- 危险原因通过 `DangerousReason()` 返回供 UI 展示

## 路径安全

- 启动目录必须通过 `ItemValidator` 校验（存在性 + 非空）
- 配置目录由 `ResolveConfigDir()` 从 exe 位置向上搜索含 `config/` 的祖先目录，不依赖进程工作目录

## 平台说明

- 当前仅 Windows（WinUI 3 原生应用，unpackaged 自包含）。安全修复与测试均以 Windows 为唯一目标平台。
