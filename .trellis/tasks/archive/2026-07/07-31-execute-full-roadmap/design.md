# 设计：按路线图全图实施

## 1. 边界

- 实施对象严格限定为审查报告列出的问题项（A-F 共 6 组）。
- 不触碰：`launchpad/src/**` 业务代码逻辑（依赖升级仅改 csproj 版本号）、archive/ 内容、spec 中"历史"标注条目。
- 文档更新只改声明内容，不重写风格结构。

## 2. 关键决策与权衡

### 2.1 依赖升级（Phase C）

| 包 | 版本变化 | 风险 | 缓解 |
|---|---|---|---|
| WindowsAppSDK | 2.2.0 → 2.3.1 | 中：runtime 变更可能影响 unpackaged 部署、Mica/Acrylic 行为 | 升级后 release 构建 + 实战启动验证；失败即回退 |
| Microsoft.NET.Test.Sdk | 17.14.1 → 18.8.1 | 低：测试基础设施，跨 major | dotnet test 全绿即通过 |
| xunit.runner.visualstudio | 3.1.0 → 3.1.5 | 低：patch | dotnet test 验证 |
| SDK.BuildTools | 26100.4654 → 28000.2526 | 低：工具链 | dotnet build 验证 |

回滚形态：每个包版本单行 diff，`git checkout` 单文件即可回退。升级顺序：BuildTools → Test.Sdk/runner → WindowsAppSDK（风险递增，每步独立验证）。

### 2.2 settings.json 跟踪策略（Phase D）

- `git rm --cached config/settings.json`：文件保留在磁盘（应用继续读写），仅停止版本跟踪。
- .gitignore 新增 `config/settings.json`（保留 `config/config.json` 已有规则；example 文件仍跟踪）。
- 影响：clone 到新机器后 settings.json 缺失——应用首次启动应自动创建默认值（需确认 ConfigStore 行为；若无默认创建逻辑，保留 config.example.json 复制说明到文档）。
- 权衡：不跟踪则丢失"用户偏好版本控制"，换取无 dirty 工作树。

### 2.3 CI 重写（Phase E）

- 目标结构：`windows-latest` 单平台（项目为 Windows 原生）+ `dotnet build --release` + `dotnet test`（working-directory: launchpad）。
- 不用 3-OS matrix（WinUI 3 仅 Windows）。
- 本地验证：在本机等价执行 CI 的每一条命令，记录输出作为证据。
- GitHub Actions 本身无法本地运行，标注"待 push 后首次运行确认"。

### 2.4 pre-commit 钩子（A5）

- `forbid-popen`（types: [c++]）在无 C++ 源码后失效：方案为替换为 C# 侧"零 shell"检查（pygrep 正则查 `Process.Start(` + 字符串拼接模式过于复杂不可靠）——**决策**：删除该钩子，保留基础 hooks（trailing-whitespace 等），安全约束由 tests 覆盖（已有 LaunchPlannerTests 断言 argv）。
- `mixed-line-ending` fix=lf 与 .gitattributes/.editorconfig 的 crlf 冲突：改为 `fix=crlf` 统一。

### 2.5 实战验证（Phase G3）

- GUI 应用冒烟：启动 exe（`dotnet run --project src/launchpad` 或直接 exe），等待窗口进程存活 ≥5s，检查：
  - 进程存活无退出
  - 无 `%TEMP%\launchpad-crash.log` 新写入
  - config/settings.json 被正常读取（launch_history 更新行为）
  - 单实例 Mutex 行为（第二实例启动应退出）
- 深/浅主题、窗口状态恢复需人工目测，标注为"人工验收项"。

## 3. 数据流与影响面

- 无运行时行为变更：所有修改均为配置/文档/CI/依赖版本，业务代码零改动（除 csproj 版本号）。
- ConfigStore 对 settings.json 缺失的处理：需在实施前读 ConfigStore.cs 确认（若抛异常则 D1 需改为保留空模板文件）。

## 4. 兼容性

- 依赖升级对配置格式零影响（snake_case 序列化不变）。
- settings.json 停止跟踪后，现有用户磁盘文件不受影响。
- CI 从 Rust 改 dotnet 后，push 将触发新的 Windows 构建（首个 push 前 CI 处于"旧定义"状态，重写提交后生效）。

## 5. 回滚形态

- 每个 Phase 独立提交（或分阶段提交），单点回滚：`git revert <commit>`。
- 依赖升级失败：`git checkout -- launchpad/src/launchpad/launchpad.csproj`（+ 测试项目）回退版本号。
- 删除文件可恢复：`git checkout <commit> -- <file>`（git rm 未推远时）。
