# PRD — 构建治理：CPM + 锁文件 + CI 缓存/审计 + 发布脚本

## 背景

审查确认（S5）：依赖版本散落 4 个 csproj（升级易漏，提交 de60781 证明手工多文件操作）；无发布布局（发布后 ConfigStore 找不到 config/ 且不建目录 → 静默丢数据）；CI 无缓存（WASDK 依赖每次全量下载）；无依赖供应链审计。

调研结论（2026-07-31 deepsearch）：CPM 官方特性、packages.lock.json 官方特性、NuGetAudit 默认开、`dotnet package update --vulnerable`（.NET 10）、WinUI 3 自包含不能单文件（官方文档）。

## 需求

- [ ] CPM：`Directory.Packages.props`（`dotnet new packagesprops`），4 个 csproj 的 PackageReference 去掉版本号，全部版本集中管理
- [ ] `Directory.Build.props`：统一 Nullable/ImplicitUsings/LangVersion（当前 4 个 csproj 重复声明）
- [ ] packages.lock.json：启用 `RestorePackagesWithLockFile`，锁文件提交入库（可复现恢复 + CI 缓存键）
- [ ] CI（.github/workflows/ci.yml）：NuGet 缓存步骤（actions/cache，键基于锁文件）+ `dotnet list package --vulnerable --include-transitive` 审计步骤（不阻塞，warning 级别或报告）
- [ ] `publish.ps1`：发布脚本（dotnet publish -c Release + WindowsAppSDKSelfContained，输出固定目录，复制 config 模板），README 说明发布布局

## 验收标准

- [ ] `dotnet restore/build/test` 全绿（CPM 迁移后）
- [ ] `Directory.Packages.props` 存在且含全部 4 个包版本；csproj 无内联版本号
- [ ] `packages.lock.json` 生成且入库（4 个项目各一份）
- [ ] ci.yml 含缓存步骤与审计步骤
- [ ] publish.ps1 执行成功，输出目录包含 launchpad.exe + WASDK 原生文件 + config 模板
- [ ] 发布产物运行验证（本机）：启动、读默认配置、修改项保存成功（ConfigStore 建目录后不崩溃）

## 约束

- 全部官方特性（CPM/lock 文件/审计），无第三方构建工具（不引入 BuildXL）
- 审计步骤不阻塞构建（先报告，可后续升级为 fail）
