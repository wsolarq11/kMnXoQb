# Implement — 外部进程契约测试

## 顺序

1. 创建测试项目 `tests/launchpad.IntegrationTests`（xUnit，引用 Core/UseCases/Infrastructure）
2. `TerminalContractTests`：pwsh 分支（目录含 `'`/`;`/`&`/空格 + `$PWD` 断言）
3. `TerminalContractTests`：cmd 分支（`%CD%` 断言）
4. `TerminalContractTests`：wt 分支（存在性检测 + 启动成功断言；命令执行层标记人工验证）
5. `SpawnerContractTests`：ProcessSpawner 启动 + 存活断言
6. ci.yml 增加集成测试步骤
7. 本机全量跑 + 人工验证 wt 命令执行 + 记录 CREATE_NEW_CONSOLE 结论到 spec

## 验证命令

```bash
dotnet test launchpad/tests/launchpad.IntegrationTests/
dotnet test launchpad/tests/launchpad.Core.Tests/   # 回归
```

## 审查关口

- [ ] 测试无 mock 真实进程；超时/清理完备
- [ ] CI 上 wt 分支 skip 有日志
- [ ] CREATE_NEW_CONSOLE 结论写进 spec（05 任务联动）

## 回滚点

- 独立提交；测试项目不触及其他代码
