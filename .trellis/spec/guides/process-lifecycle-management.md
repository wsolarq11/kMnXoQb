# Process Lifecycle Management

## Principle

**真实进程测试必须可控，否则不测。** Windows Terminal 的 relay 行为（`wt.exe` 向持久 `WindowsTerminal.exe` 发消息后退出）使窗口生命周期无法从外部管理，但 pwsh/cmd 的**命令执行契约**（argv 转义、工作目录、错误码）可以在自动化测试中安全验证——2026-07-31 契约测试项目（`tests/launchpad.IntegrationTests`）证明这是可行的。

## 分层策略

| 层 | 验证方式 | 位置 |
|----|---------|------|
| 纯决策（PlanWindows argv/转义） | 单元测试断言 | launchpad.Core.Tests |
| 进程边界契约（pwsh/cmd 真实执行、目录语义、错误码） | 契约测试（真实 spawn） | launchpad.IntegrationTests |
| wt.exe 窗口/命令执行 | 本机人工验证 + 启动成功断言（CI 自动 Skip） | IntegrationTests + 发布版冒烟 |

## 契约测试规则（2026-07-31 起）

### R1: 真实 spawn 必须有超时与清理

- 交互式命令（`-NoExit`/`/k`）用 settle 窗口（默认 2s）后 `Kill(entireProcessTree: true)`，禁止裸 `WaitForExit()`（会挂死）。
- 输出用异步 `ReadToEndAsync`（防管道死锁）。
- 测试目录用 `%TEMP%` 下唯一目录，`Dispose` 删除；wt 场景目录可能被终端进程短暂持有，删除失败忽略（临时残留可接受）。

### R2: 环境不满足时跳过，不假绿不假红

- wt 依赖环境：`WtFactAttribute` 在反射期检测 `wt.exe` 可用性，不可用时设置 `Skip`（CI runner 无 Windows Terminal，自动跳过；本机自动执行）。

### R3: 断言真实语义而非进程存在

- pwsh：命令输出 `$PWD` 验证 cd 生效（单引号转义 `''`）；cmd：无参 `cd` 输出当前目录（**不要用 `%CD%`**——含 `&` 的目录名会触发 cmd 二次解析，`'and' is not recognized`）。
- Windows 路径比较用 `OrdinalIgnoreCase`（pwsh 会规范化 `TEMP` → `Temp` 显示）。
- 进程错误码：目录缺失是 267（ERROR_DIRECTORY）不是 3；缺 exe 是 2。

### R4: 单元测试止于 PlanWindows

纯决策路径（`LaunchPlanner.PlanWindows` → `LaunchPlan`）由单元测试覆盖 argv/转义/目录语义；契约测试只验证真实进程边界行为，两者不重叠。

## 历史教训（wt relay）

- `wt.exe` 是 relay 客户端：向 `WindowsTerminal.exe` 发 JSON 后退出，tab 生命周期由后者控制。外部进程无法关闭 wt tab（详见 `wt-lifecycle-analysis.md`）。
- 所以 wt 的自动化契约止于"argv 被接受 + 进程启动成功"，命令执行层靠本机人工验证（打开窗口可见命令运行）。

## 相关链接

- `tests/launchpad.IntegrationTests/TerminalContractTests.cs`（pwsh/cmd/wt 契约）
- `tests/launchpad.IntegrationTests/SpawnerContractTests.cs`（ProcessSpawner 错误码契约）
- `tests/launchpad.IntegrationTests/WtFactAttribute.cs`（wt 条件跳过）
