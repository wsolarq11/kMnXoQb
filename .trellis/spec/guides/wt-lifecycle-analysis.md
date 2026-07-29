# Bug Analysis: WindowsTerminal Tab 生命周期不可自动化管理

## 1. Root Cause Category

- **Category**: E - Implicit Assumption + B - Cross-Layer Contract
- **Specific Cause**: 假设 `CreateProcessW(wt.exe)` 返回的 ProcessHandle 拥有终端窗口的生命周期控制权。实际上 `wt.exe` 是一个 relay 客户端——它向持久运行的 `WindowsTerminal.exe` 发送 JSON 消息后立即退出。tab 的创建、销毁、`-NoExit` 行为完全由 `WindowsTerminal.exe` 内部状态机控制，外部进程无法干预。

### Bayesian Prior Analysis

| 假设 | 先验概率 | 事后概率 | 证据 |
|------|---------|---------|------|
| H1: ProcessHandle = 窗口生命周期 | 60% | ~0% | wt.exe 在 CreateProcess 后立即退出 |
| H2: WindowsTerminal 掌管 tab | 20% | ~95% | 8 种方案全部失效；微软#15747 closed |
| H3: Window messaging 可控制 | 20% | ~5% | `PostMessage(WM_CLOSE)` 被 WT 忽略 |

## 2. Why Fixes Failed

| # | 尝试 | 为什么失败 | 教训 |
|---|------|-----------|------|
| 1 | `TerminateProcess(wt_handle)` | wt.exe 已退出，handle 指向僵尸进程 | 没有验证进程是否存活 |
| 2 | `TerminateProcess(pwsh)` | pwsh 被杀，`-NoExit` 让 tab 显示"[已退出]" | 不理解 WT 的 shell exit 行为 |
| 3 | `keybd_event(Ctrl+Shift+W)` | UIPI 阻止合成按键 | 对 Windows 安全模型认知不足 |
| 4 | `SendInput(Ctrl+Shift+W)` | WT 忽略合成快捷键 | 同上 |
| 5 | `PostMessage(WM_CLOSE)` | WT 窗口不响应外部 WM_CLOSE | 假设 WT 是标准 Win32 窗口 |
| 6 | `SetForegroundWindow + Alt+F4` | `SetForegroundWindow` 被焦点限制拦 | 未使用 Alt 解锁技巧 |
| 7 | `GenerateConsoleCtrlEvent(CTRL_C)` | `FreeConsole/AttachConsole` 在测试进程中崩溃 | 控制台 API 的使用限制 |
| 8 | `wt -w -1` 独立窗口 | 新窗口仍然有 `-NoExit` | 架构问题，非参数问题 |
| 9 | `cmd.exe /c` 绕过 wt | 测试了 cmd，没测 wt | 改变了测试目标 |
| 10 | ConPTY 工厂注入（最终方案） | 成功 | 测试 LauncherApp 逻辑，不测试 WT 窗口 |

**模式识别**：前 9 次修复都在"如何关闭窗口"层面进行 surface fix。第 10 次切换视角为"如何在不依赖窗口关闭的情况下验证 launch() 逻辑"。

## 3. Prevention Mechanisms

| 优先级 | 机制 | 具体动作 | 状态 |
|--------|------|---------|------|
| P0 | 架构 | `ProcessHandle` 支持 `conpty_` 成员，move 构造必须拷贝 | DONE |
| P0 | 测试 | shell_tests 使用 ConPTY 工厂注入验证完整 `LauncherApp::launch()` 路径 | DONE |
| P1 | 文档 | 本文件——记录 WT 架构约束 | DONE |
| P1 | 代码 | `terminal_launcher_conpty.cpp` 作为参考实现存档 | DONE |
| P2 | 编译期 | `TerminalLauncherFactory` 类型确保接口一致性 | DONE |

## 4. Systematic Expansion

- **类似问题**：macOS `osascript` 和 Linux `gnome-terminal` 也有 relay 模式——`populate` 的 terminal_override 路径应该支持 ConPTY 类似的 headless 模式用于跨平台测试。
- **设计改进**：`LauncherApp` 现在通过 `TerminalLauncherFactory` 接受注入；CLI 和 GUI 入口也应使用相同的工厂模式而非硬编码 `TerminalLauncher::create()`。
- **流程改进**：当外部进程行为不明时，先查该项目的 GitHub issues（#15747 在第一次尝试前就应该查到）。

## 5. 有效方案总结

| 方案 | 能自动化？ | 测生产代码？ | 关窗口？ |
|------|-----------|------------|---------|
| wt.exe + TerminateProcess | 是 | 是 | 否 |
| wt.exe + SendInput | 不可靠 | 是 | 否 |
| cmd.exe 绕过 wt | 是 | **否** | 是 |
| ConPTY + 工厂注入 | **是** | **是** | N/A（无窗口） |
| dry_run 验证 populate | 是 | 是 | N/A |

**结论**：ConPTY + 工厂注入是唯一同时满足"可自动化"和"测生产代码"的方案。`dry_run` 覆盖 wt `populate` 路径。`launch` 中的 `CreateProcessW` 是无法进一步拆解的薄 I/O 调用，不需要自动化验证。
