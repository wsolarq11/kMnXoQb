# Launchpad

Windows 原生启动器：在一个界面里集中管理并一键启动 AI CLI 工具（snow / codex / claude / opencode 等），每个条目在指定目录下以指定终端启动。

## 功能

- 启动项列表：增删改、排序、筛选、单选/全选、批量启动
- 终端自动回退：Windows Terminal（`wt.exe`）→ PowerShell（`pwsh.exe`）→ `cmd.exe`，按可用性逐级降级
- 危险命令确认：命令含 `dangerously` / `yolo` / `skip-permissions` / `bypass-approvals` / `bypass-sandbox` 等 flag 时，在编辑框、卡片、确认对话框三处警告
- 主题切换：跟随系统 / 浅色 / 深色（Win11 Mica，Win10 自动回退 Acrylic）
- 中英双语界面：默认跟随系统语言，可手动循环切换（自动 → 中文 → English），实时生效
- 单实例运行；窗口位置记忆（最小化离屏坐标自动纠偏）
- 配置写盘前自动备份到 `config.json.bak`，损坏时自动恢复并在状态栏提示
- 启动历史记录（最近启动的条目）

## 技术栈

> **2026-08-01 迁移状态**：R3 全图实施（Rust 核心 + Tauri 2 + React）已完成（`launchpad-tauri/`，阶段 0-5 验收通过，质检自动化项全 PASS）。新栈产物：便携 zip（`launchpad-tauri/release/`）+ MSI；配置双轨（便携 exe 旁 config/，安装版 %APPDATA%）。规范见 `.trellis/spec/tauri-rust-ts/index.md`。人工场景走查与安装实测为下一迭代项；C# 主线（`launchpad/`）保持可回滚。

### 新栈（进行中，推荐）

- Rust 核心（纯函数分层）+ Tauri 2.11 + React/TS/Vite + zustand + lucide-react
- serde snake_case 配置（字节兼容旧格式）；cargo test + vitest + 契约测试

### 旧栈（存档）

- .NET 10 + WinUI 3（Windows App SDK 2.3.1），unpackaged 自包含部署
- CommunityToolkit.Mvvm（MVVM）、Microsoft.Extensions.DependencyInjection（DI）
- ErrorOr（预期失败结构化错误）、xUnit + ArchUnitNET + Verify（测试）
- lucide 图标（字体方案）

## 构建与运行

需要 .NET 10 SDK（Windows 10 1809+）。

```bash
cd launchpad

# 构建
dotnet build src/launchpad/launchpad.csproj --configuration Release

# 运行
dotnet run --project src/launchpad/launchpad.csproj
```

## 测试

```bash
cd launchpad

# 单元测试 + 架构测试（依赖方向、分层约束由 ArchUnitNET 机器执行）
dotnet test tests/launchpad.Core.Tests/

# 契约测试（真实 spawn pwsh/cmd/wt；无 Windows Terminal 时 wt 用例自动跳过）
dotnet test tests/launchpad.IntegrationTests/
```

## 发布

WinUI 3 无法单文件发布（Windows App SDK 原生依赖以独立文件部署），发布脚本会补齐 dotnet publish 不产出的 XAML 编译产物（xbf/pri）并放置 config 模板：

```bash
cd launchpad
powershell -ExecutionPolicy Bypass -File publish.ps1
# 产物在 launchpad/publish/，运行 publish\launchpad.exe
```

## 配置

配置目录从可执行文件位置向上搜索含 `config/` 的祖先目录（不依赖工作目录），首次运行自动创建。

`config/config.json`（启动项列表，snake_case，缺失 `confirm` 默认 `true`）：

```json
[
  {
    "name": "claude-example",
    "directory": "D:\\projects\\your-project",
    "command": "claude --dangerously-skip-permissions",
    "confirm": true,
    "id": "claude-example",
    "selected": false
  }
]
```

字段：`name`（必填）、`directory`（必填）、`command`（必填）、`id`（必填，历史遗留格式：小写、空格转下划线，冲突时追加数字后缀）、`confirm`（启动前确认，默认 true）、`selected`（批量启动选中态）、`terminal` / `tag` / `group`（可选）。

`config/settings.json`（应用设置）：

```json
{
  "confirm_enabled": false,
  "theme": "system",
  "language": "auto",
  "launch_history": [],
  "window_state": { "x": 0, "y": 0, "width": 800, "height": 600 }
}
```

`theme` 取值 `system` / `dark` / `light`；`language` 取值 `auto`（跟随系统语言）/ `zh-CN` / `en-US`；未知字段在写回时保留，不丢未来版本数据。

仓库根 `config/config.example.json` 为启动项模板，发布时拷入产物目录。

## 架构

Clean Architecture，依赖方向只向下：UI → UseCases → Core ← Infrastructure。领域层（Core）是零 I/O 的纯函数核心（不可变 record + 纯决策），命令执行经 `Process.ArgumentList` argv 数组启动（零 shell 拼接，防命令注入）。详见 `CLAUDE.md` 与 `.trellis/spec/winui3-csharp/index.md`。

## 历史

2026-07-31 起由 Rust/Slint 与 Flutter 版本迁移至 WinUI 3 + C#（.NET 10），旧实现保留在 `archive/`（`launchpad-rs`、`launchpad_flutter`），行为对齐以测试断言为准。
