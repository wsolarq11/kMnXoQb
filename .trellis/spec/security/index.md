# 安全编码规范

## 核心原则

**禁止将用户可控文本通过 shell 解释器执行。**

## 命令注入防御

- ✅ 所有子进程通过 **reproc** 以 argv 数组启动（`reproc::process::start({...})`）
- ✅ Windows 底层走 `CreateProcessW`，macOS/Linux 走 `posix_spawn`，无 shell 介入
- ❌ 禁止使用 `std::system()`、`::popen()`、`::pclose()`
- ❌ 禁止字符串拼接构造命令（`cmd + " " + args`）
- ✅ 命令参数通过 `LaunchPlan.executable` + `LaunchPlan.args` 数组传递
- ✅ 终端目录路径使用引号包裹（win: `cd /d "dir"`, mac: `quoted form of`, linux: `cd 'dir'`）

## 安全规范自动化

### clang-tidy 门禁

`.clang-tidy` 配置包含以下 WarningsAsErrors 规则：

| 规则 | 目的 |
|------|------|
| `cert-msc30-c` / `cert-msc50-cpp` | 禁止使用不可预测的随机函数 |
| `cert-err34-c` | 禁止格式字符串漏洞 |
| `clang-analyzer-core.*` | 核心分析器 |
| `bugprone-suspicious-string-compare` | 字符串比较错误 |
| `clang-analyzer-deadcode.DeadStores` | 死代码检测 |
| `concurrency-*` | 线程安全问题 |

### pre-commit hooks

`.pre-commit-config.yaml` 包含：

| Hook | 触发时机 | 用途 |
|------|----------|------|
| `trailing-whitespace` | 提交前 | 尾随空白 |
| `cmake-lint` | 提交前 | CMake 语法检查 |
| `clang-format` | 提交前 | C++ 代码格式化 |
| `forbid-popen`（自定义） | 提交前 | 正则匹配 `::popen(` / `::system(` 并阻断 |
| `clang-tidy`（manual） | 手动触发 | 完整静态分析 |

## 配置安全

- `core::is_dangerous()` 对所有用户自定义命令进行黑名单 + 启发式判定
- `confirm_enabled` 控制是否在危险命令执行前弹出确认对话框
- 危险命令即使 `confirm_enabled=false` 也应记录日志

## 路径安全

- 启动目录必须校验 `std::filesystem::exists()`
- 配置目录由 `PathResolver::create()` 统一管理，不信任用户传入的相对路径

## 跨平台安全一致性

- Windows 和 POSIX 系统的安全等级应一致
- 安全修复必须覆盖所有平台实现
- 安全相关的测试必须在所有平台上运行
