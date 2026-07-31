# Design — 外部进程契约测试

## 项目结构

```
tests/launchpad.IntegrationTests/
  launchpad.IntegrationTests.csproj   （xUnit；引用 Core/UseCases/Infrastructure）
  TerminalContractTests.cs            pwsh/cmd/wt 分支契约
  SpawnerContractTests.cs             ProcessSpawner 行为
```

csproj 要点：
- TargetFramework net10.0（纯 .NET 类库测试，不引 WinUI）
- 引 `launchpad.Infrastructure`（ProcessSpawner/TerminalDetector 真实实现）

## 哨兵命令设计

pwsh 分支：
```
命令：Write-Output CONTRACT_MARKER
验证：pwsh -NoExit 是交互式的——用 -Command "cd 'DIR'; Write-Output marker" 后进程退出？
```
pwsh -Command 执行完会退出（无 -NoExit）。但 LaunchPlanner 的 pwsh fallback 带 `-NoExit`——进程不退出，测试要 kill。
设计：**契约测试不直接跑 LaunchPlanner 的 argv**（-NoExit 挂住进程），而是验证等价语义：
1. 用 `pwsh -NoExit -Command "cd 'DIR'; Write-Output MARKER"` 启动，重定向输出，等 2s，读输出含 MARKER，kill 进程
2. 目录边界：DIR 含 `'`（转义 `''`）、含 `;`、含 `&`、含空格——验证 cd 成功（输出 `PWD` 路径验证当前目录正确）
3. cmd 分支：`cmd /k "cd /d \"DIR\" && echo MARKER"` 同理（/k 不退出，kill）

改进：**用窗口标题或进程环境验证**太重；用输出验证最可靠：
- pwsh：`Write-Output $PWD` → 断言输出 == 期望目录（含转义边界）
- cmd：`echo %CD%` → 断言输出 == 期望目录

## wt 分支

- `TerminalDetector.TerminalAvailable("wt.exe")` 为 false 时 `[Fact(Skip=...)]` 或 Assert.Skip（xUnit v3 有 Skip）——本机 wt 存在则跑
- 验证：`wt.exe new-tab -d <tmpdir> pwsh -NoExit -Command "Write-Output MARKER"`——wt 打开 GUI 窗口，无法捕获输出（wt 是 GUI）！
- **wt 分支契约验证方案**：验证 wt.exe 进程成功启动（Start 不抛、进程存活数秒）+ `--window-id` 参数 + 用 `-w new` 语义。实际"命令执行成功"在 wt 场景无法自动化断言（GUI 进程无 stdout）→ 改为：
  - 断言 1：wt.exe 启动成功（进程创建）
  - 断言 2：`wt.exe -w <name> new-tab ...` 后查询 wt 是否存活
  - 结论：**wt 的"命令真正执行"契约记录为本机人工验证项**（打开窗口可见），自动化覆盖到"启动成功"层
- 诚实标注：wt 分支自动化只能到启动层，命令执行层靠人工

## ProcessSpawner / CREATE_NEW_CONSOLE

- 断言：从测试进程（有 console 的 dotnet test host）spawn pwsh，能启动且能执行命令（继承 console 场景）
- GUI 无 console 场景：测试进程本身有 console——无法模拟。验证方案改为：
  - 用 `CreateProcess` 语义文档记录（ProcessSpawner 注释更新：从无 console 父进程启动 console 子进程 → Windows 分配新 console）
  - 人工验证记录：本机启动 launchpad（双击 exe）→ 点启动项 → 观察 pwsh 是否开新窗口 → 结论写进 spec
- 测试断言：ProcessSpawner.Launch(plan) 不抛、进程存活

## 超时与清理

- 所有 spawn 用 `process.WaitForExit(5000)` 或等待输出的并行读取 + `Kill()` 兜底
- 测试目录 %TEMP%\launchpad-int-tests-\<guid>，Dispose 删除
- 输出重定向用 `RedirectStandardOutput` + 异步读（防死锁）

## CI

- ci.yml 增加：`dotnet test tests/launchpad.IntegrationTests/`（windows-latest 有 pwsh，无 wt → wt 测试 Skip 并输出原因）
- pwsh 在 windows-latest 存在（GitHub 托管 runner 自带），cmd 必在
