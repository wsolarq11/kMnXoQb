# 全图实施 — 执行计划

## 执行顺序（依赖约束）

1. **p0-xaml-bindings**（先修反馈通道，后续子任务的错误提示才有地方显示）
2. **p0-checkbox-events**（状态操作基础）
3. **p1-legacy-contract**（行为恢复 + 快照锁死，依赖 1/2 的状态栏/勾选正常）
4. **p1-config-resilience**（依赖 1 的状态栏）
5. **p1-build-governance**（独立，最后做构建产物布局调整，避免中间改动影响其它子任务验证）
6. **p1-launch-hardening**（独立）
7. **p2-eval-phase**（评估文档，随时可做）

## 验证命令（每子任务检查门禁）

```bash
cd launchpad
dotnet build src/launchpad/launchpad.csproj        # 零错误（P0 后零警告）
dotnet test tests/launchpad.Core.Tests/            # 全绿
dotnet test tests/launchpad.IntegrationTests/      # 契约（本机）
```

P0.1 额外断言：`grep -c RegisterPropertyChangedListener src/launchpad/obj/Debug/net10.0-windows10.0.19041.0/win-x64/Views/HomeView.g.cs` ≥ 6。

## 质检（父任务收尾）

1. 全量构建 + 全量测试（单元/架构 + 契约）
2. 编译产物断言
3. 实战实测验真质检清单：
   - 启动正常（含损坏 config 恢复流程：手工损坏 config.json → 启动 → 自动恢复提示）
   - 状态栏反馈：故意启动不存在目录 → 状态栏显示错误
   - ITEMS/选中计数/RECENT 实时刷新
   - 勾选：单点/快速双击/滚动后勾选（无错乱无回弹）
   - 搜索无结果 → 「无匹配」提示；清空搜索恢复
   - 删除：卡片删除弹确认；编辑框删除直接删
   - 批量启动后选中清除；主题三态循环；窗口位置恢复
4. 提交（每子任务独立 commit）

## 发布

`powershell -ExecutionPolicy Bypass -File publish.ps1` 产物可运行（xbf/pri 路径同步后验证）。
