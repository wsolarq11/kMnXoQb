# PRD — 外部进程契约测试

## 背景

审查确认（S3）：`LaunchPlanner` 生成的 argv 只经纯函数断言测试，真实进程行为无验证——wt.exe 的 argv join/引号语义、pwsh `cd '...'; command` 转义边界（含 `'`/`;`/`&` 的目录）、ProcessSpawner 删掉 CREATE_NEW_CONSOLE 的声称（"GUI host 无 console 可继承，Windows 自动开新窗口"）全部无凭据。微软官方文档确认 wt `new-tab` commandline 是 "Executable with optional arguments"（argv 契约可测试）。

## 需求

- [ ] 新建集成测试项目 `tests/launchpad.IntegrationTests`（xUnit，引用 Core/UseCases/Infrastructure，真实 spawn 进程）
- [ ] pwsh 分支契约：含 `'`、`;`、`&` 的目录路径 + 简单命令，spawn 后验证进程可执行命令并返回（用可回显的哨兵命令，如 `echo marker` 通过输出重定向验证）
- [ ] cmd 分支契约：同上（`&`、`"` 边界）
- [ ] wt 分支契约（本机/有条件跳过）：`wt.exe new-tab` argv 传递，含引号命令可执行
- [ ] ProcessSpawner 行为：从无 console 父进程 spawn pwsh 能启动（退出码/进程存活）；CREATE_NEW_CONSOLE 的"新窗口"视觉行为人工验证并记录结论
- [ ] 集成测试可在 CI 跑（pwsh/cmd 分支）；wt 分支用环境变量/检测跳过（CI 无 wt.exe 时 skip）

## 验收标准

- [ ] `dotnet test tests/launchpad.IntegrationTests/` 本机全绿（含 wt 分支）
- [ ] CI 上 pwsh/cmd 分支执行通过（wt 分支 skip 有日志说明）
- [ ] 测试用哨兵命令验证"命令真正被执行"（不只是进程启动）
- [ ] CREATE_NEW_CONSOLE 结论记录到 spec（winui3-csharp 或 guides）

## 约束

- 测试必须可重复、不依赖用户环境（用 %TEMP% 建测试目录，哨兵输出重定向到临时文件）
- 不 mock 真实进程（契约测试的意义就是真实验证）
- 每次 spawn 超时保护（WaitForExit(timeout)）
