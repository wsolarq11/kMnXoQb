# Implement — 构建治理

## 顺序

1. `Directory.Build.props`（统一属性 + RestorePackagesWithLockFile）
2. `dotnet new packagesprops` → 填入 4 个包版本；4 个 csproj 移除内联 Version
3. `dotnet restore` → 验证生成 packages.lock.json；提交入库
4. ci.yml：缓存步骤 + 审计步骤
5. publish.ps1 + 本机发布验证（含 config 模板复制）
6. 全量验证：restore/build/test

## 验证命令

```bash
cd launchpad
dotnet restore src/launchpad/launchpad.csproj        # CPM + lock 生成
dotnet build src/launchpad/launchpad.csproj -c Release
dotnet test tests/launchpad.Core.Tests/
powershell -File publish.ps1                          # 发布产物
./publish/launchpad.exe                               # 实战验证（本机）
```

## 审查关口

- [ ] csproj 无内联版本号（grep 验证）
- [ ] lock 文件入库（git status 确认）
- [ ] CI 缓存键基于锁文件（可复现）
- [ ] 发布产物运行正常：启动 + 保存 config（建目录生效）

## 回滚点

- CPM 单提交（删除 Directory.Packages.props 即回退）
- publish.ps1 独立提交
