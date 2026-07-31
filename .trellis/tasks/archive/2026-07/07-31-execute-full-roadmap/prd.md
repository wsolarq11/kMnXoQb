# 按路线图全图实施（审查发现修复）

## Goal

将审查任务（07-31-audit-outdated-content）发现的全部可处理项规划为实施路线图并全部执行：文档修正、残留删除、依赖升级、settings.json 策略调整、CI 重写。全图生产就绪后实战实测 + 真质检，再预备后续 phase。

## 范围（用户已确认全部包含）

### Phase A: 文档与配置修正
- A1: `AGENTS.md` Quick Reference（23-32 行）从 Rust 时代更新为 WinUI 3 + dotnet 命令
- A2: `_README.md` 整篇重写为当前架构说明（WinUI 3 + C#，.NET 10 LTS）
- A3: `CLAUDE.md:39` "58 个测试" → 60
- A4: `CLAUDE.md:47` + `.trellis/spec/winui3-csharp/index.md:15` WindowsAppSDK 版本声明更新为升级后版本
- A5: `.pre-commit-config.yaml` popen 钩子（C++ 检查失效）处理 + mixed-line-ending 冲突（editorconfig crlf vs hook lf）统一
- A6: `.trellis/spec/guides/incremental-refactor-workflow.md:25,65` CMake 工具链引用更新为 dotnet

### Phase B: 文件删除（git rm）
- B1: `CMakePresets.json`
- B2: `.clang-format`
- B3: `WT Launcher.hta`
- B4: `.trellis/trash/` 中 C++ 残留（terminal_command.cpp/.h/_test.cpp）

### Phase C: 依赖升级（对照 NuGet 最新稳定）
- C1: Microsoft.WindowsAppSDK 2.2.0 → 2.3.1
- C2: Microsoft.NET.Test.Sdk 17.14.1 → 18.8.1
- C3: xunit.runner.visualstudio 3.1.0 → 3.1.5
- C4: Microsoft.Windows.SDK.BuildTools 10.0.26100.4654 → 10.0.28000.2526
- 升级后必须 build + test 全绿

### Phase D: settings.json 跟踪策略
- D1: `config/settings.json` 从 git 移除跟踪（git rm --cached），加 .gitignore 规则
- D2: `config/config.example.json` 保留为模板
- 注：settings.json 含运行时数据（launch_history/window_state），继续跟踪则每次运行都 dirty

### Phase E: CI 重写
- E1: `.github/workflows/ci.yml` 从 Rust 重写为 Windows + dotnet build + dotnet test

### Phase F: 磁盘清理（gitignore 项，非 git 操作）
- F1: `build/debug/`（CMake 时代构建产物）
- F2: `wt-last-err.txt`、`wt-last-out.txt`
- F3: `.cargo-cache/`、`.cpm-cache/`（如存在）

### Phase G: 实战实测验真质检
- G1: `dotnet build --release` 通过
- G2: `dotnet test` 全绿（60 个）
- G3: 实战运行应用（进程启动、窗口出现、无 crash log、配置读写正常）
- G4: 真质检：verification agent 全量复核

### Phase H: 预备后续 phase
- H1: CLAUDE.md 历史部分更新（记录本次全图实施）
- H2: 任务归档 + 提交
- H3: 后续 phase 提案（阶段 2：核心下沉 Rust + P/Invoke 或其他）

## 约束

- 只修改审查报告中列出的问题项，不做范围外重构（避免引入新问题）。
- 依赖升级后若出现构建/测试失败，回退到原版本并报告，不强行绕过。
- 删除文件使用 git rm 保持仓库干净；磁盘清理保留 gitignore 规则。
- 图标只用 lucide；代码无 emoji；风格遵守 CLAUDE.md。
- 提交遵循仓库风格（feat:/fix:/chore: 前缀，中文说明）。

## Acceptance Criteria

- [ ] Phase A 全部文档修正完成，声明与实测一致（测试数 60、版本号正确）
- [ ] Phase B 文件删除完成，git status 干净
- [ ] Phase C 依赖升级后 `dotnet build` + `dotnet test` 全绿
- [ ] Phase D settings.json 不再被跟踪，example 模板保留
- [ ] Phase E ci.yml 重写为 dotnet 命令（本机等价命令实测通过）
- [ ] Phase F 磁盘残留清理
- [ ] Phase G release 构建 + 60 测试 + 应用实战运行验证通过，verification agent 给出 PASS
- [ ] Phase H 提交完成、任务归档、后续 phase 提案产出

## Out of Scope

- 阶段 2（核心下沉 Rust + P/Invoke）的实际实施——仅产出提案
- 新功能开发
- 审查报告"已核实正常"项的修改
