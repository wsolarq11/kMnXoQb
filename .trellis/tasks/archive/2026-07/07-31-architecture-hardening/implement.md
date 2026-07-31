# Implement — 架构固化

## 顺序

1. **ErrorOr 引入 + UseCases 改造**：
   - `launchpad.UseCases` 引 ErrorOr NuGet
   - `LaunchUseCase.TryLaunch` → `ErrorOr<Unit>`（错误枚举 `LaunchError`）
   - `LaunchUseCase.TryLaunchMany` → `(int succeeded, List<string> failures)`
   - `ItemUseCase.Save` → `ErrorOr<Unit>`（`StoreError`）
   - 测试：Fake 实现返回/抛出 → ErrorOr 断言
2. **HomeViewModel 下沉**：
   - 移除 LaunchSelected 内联确认/启动/history → 调 UseCases
   - Save 失败状态栏显示
   - 目标行数 < 120
3. **模型危险标记**：`LaunchItem.IsDangerous`/`DangerReason`（[JsonIgnore]）
4. **HomeView 卡片**：危险标记（AlertTriangle + DangerBrush + ToolTip）
5. **EditDialog 文案**修正
6. **ArchUnitNET 架构测试**（ArchitectureTests.cs + SourceFileRuleTests.cs）
7. **回归**：全套测试 + 功能清单手测

## 验证命令

```bash
dotnet test launchpad/tests/launchpad.Core.Tests/launchpad.Core.Tests.csproj
dotnet build launchpad/src/launchpad/launchpad.csproj
```

## 审查关口

- [ ] Core 零新增依赖（ErrorOr 只在 UseCases）
- [ ] ViewModel < 120 行，无内联业务决策
- [ ] 架构测试全绿（含源文件规则）
- [ ] 52+ 测试全绿无回归

## 回滚点

- 每步独立提交；ErrorOr 引入单独提交（可回退）
