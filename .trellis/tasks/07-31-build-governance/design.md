# Design — 构建治理

## CPM 迁移

`Directory.Packages.props` 放 `launchpad/` 根（sln 所在层级，覆盖 4 个项目）：

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <PackageVersion Include="CommunityToolkit.Mvvm" Version="8.4.2" />
    <PackageVersion Include="Microsoft.Extensions.DependencyInjection" Version="10.0.10" />
    <PackageVersion Include="Microsoft.WindowsAppSDK" Version="2.3.1" />
    <PackageVersion Include="Microsoft.Windows.SDK.BuildTools" Version="10.0.28000.2526" />
    <PackageVersion Include="ErrorOr" Version="2.x.y" />  <!-- 02 引入后并入 -->
  </ItemGroup>
</Project>
```

4 个 csproj 的 `<PackageReference>` 删除 Version 属性。

## Directory.Build.props

`launchpad/Directory.Build.props`（默认值全项目统一，csproj 可覆盖）：

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>preview</LangVersion>  <!-- 或 14.0，跟随 SDK -->
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
```

- csproj 中删除重复的 Nullable/ImplicitUsings（core 项目无 ImplicitUsings 声明的保留默认）
- 注意：`launchpad/` 根已有 config/、archive/ 等，Directory.Build.props 放 launchpad/ 子目录（sln 层），不影响仓库其他部分

## packages.lock.json

- `RestorePackagesWithLockFile=true` 后 `dotnet restore` 生成 `packages.lock.json`（每个项目）
- 提交入库；升级依赖时锁文件 diff 可见
- CI 缓存键：`hashFiles('launchpad/**/packages.lock.json')`

## CI

ci.yml 改造：

```yaml
- name: Cache NuGet packages
  uses: actions/cache@v4
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('launchpad/**/packages.lock.json') }}
    restore-keys: ${{ runner.os }}-nuget-

- name: Audit vulnerable packages
  run: dotnet list package --vulnerable --include-transitive
```

- 审计在 Restore 后跑；warning 级别（不 fail），输出留日志
- Restore 步骤利用缓存（actions/setup-dotnet 自带缓存或显式 actions/cache——显式更可控）

## publish.ps1

`launchpad/publish.ps1`（PowerShell，幂等）：

```powershell
param([string]$OutputDir = "publish")

$project = Join-Path $PSScriptRoot "src/launchpad/launchpad.csproj"
dotnet publish $project -c Release -p:RuntimeIdentifierOverride=win-x64 `
  -p:WindowsPackageType=None -p:WindowsAppSDKSelfContained=true `
  -o (Join-Path $PSScriptRoot $OutputDir)

# 复制 config 模板（首次发布创建 config/）
$configDest = Join-Path $PSScriptRoot (Join-Path $OutputDir "config")
New-Item -ItemType Directory -Force -Path $configDest | Out-Null
Copy-Item (Join-Path $PSScriptRoot "config/config.example.json") `
  (Join-Path $configDest "config.json") -Force
```

注意：`-o` 指定输出目录；RuntimeIdentifierOverride 用于 WASDK 自包含（官方 CLI 发布路径）。发布后 `ResolveConfigDir` 从 exe 向上找 config/——发布目录内放 config/ 模板，保证首次运行可读写。
