# Implement — 止血修复

## 顺序

1. **Core 纯函数**（先做，独立可测）：
   - `Domain/WindowPosition.cs`（ClampToVisible）
   - `LaunchUseCase.RequireConfirm` / `LaunchMany` / `PushHistory` 去重
   - `ItemUseCase.GenerateId` + `NewItem` 签名调整
2. **Core 测试**：WindowPositionTests、LaunchUseCaseTests 扩展、ItemUseCaseTests 扩展、ConfigStoreTests 扩展
3. **Infrastructure**：`ConfigStore` 构造 CreateDirectory
4. **WinUI 层**：`MainWindow`（缓存正常位置 + clamp + OnClosed 检查）、`HomeViewModel.LaunchSelected` 走确认 + history、`DialogService.ConfirmBatchAsync`

## 验证命令

```bash
dotnet build launchpad/src/launchpad.Core/launchpad.Core.csproj
dotnet test launchpad/tests/launchpad.Core.Tests/launchpad.Core.Tests.csproj
dotnet build launchpad/src/launchpad/launchpad.csproj
```

## 审查关口

- [ ] 行为对齐旧版（PushHistory/Id）有对应测试且与归档 Rust 逻辑一致
- [ ] 无新引入的未测试路径（新增逻辑全部有测试）
- [ ] 现有 52 测试不回归

## 回滚点

- 提交粒度：纯函数层 → 存储层 → UI 层 分三次提交，任一层可独立回退
