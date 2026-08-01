# P1: 启动加固（TerminalDetector 缓存 / PathNotFound 误报）

## Goal

降低启动延迟抖动与错误误报。

## Requirements

1. TerminalDetector 结果按进程生命周期缓存（单例注入，Key=name；wt/pwsh 探测各一次，后续启动复用）。
2. TryLaunch 错误归类：先 `Directory.Exists(plan.WorkingDirectory)` 排除工作目录因素，再判 PathNotFound 归属可执行路径 vs 工作目录。

## Acceptance Criteria

- [ ] 缓存生效（同一实例第二次 Plan 不再调用 where——测试用假探测器或计数器验证）
- [ ] 错误文案准确（可执行缺失 vs 工作目录缺失区分）
- [ ] `dotnet test` 全绿（含新增错误归类测试）

## Notes

- 缓存需处理 PATH 环境变化：进程生命周期内缓存可接受（KISS，Windows 下 PATH 改动需重启应用属正常）。
