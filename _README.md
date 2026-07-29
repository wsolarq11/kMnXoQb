# WT Launcher

跨平台终端启动器。统一管理并一键启动各类 AI 编码代理（snow、codex、claude、opencode 等），在指定项目目录中拉起终端执行命令。

## 功能

- 启动项管理：可视化新增、编辑、删除启动项
- 卡片列表：搜索过滤、hover 操作按钮、危险命令红色标记
- 批量启动：多选后一键拉起多个终端，逐项确认
- 启动前确认：per-item confirm + 危险命令触发弹窗确认
- 主题切换：Light / Dark / System 循环切换，持久化到 settings.json
- 动态状态栏：显示最近启动项或 Ready
- Ctrl+A 全选/取消全选快捷键
- 配置持久化：选中状态、确认设置、主题偏好跨重启保留
- 命令行安全转义：按 Windows 终端规则正确引号化
- 运行时检测：启动前校验目录是否存在
- 自定义终端覆盖：per-item terminal 字段覆盖默认终端
- **零 shell 注入**：所有子进程通过 reproc 以 argv 数组启动，无 system/popen

## 快速开始

### 前置要求

- **CMake 3.29+**
- **C++23 编译器**：GCC 14+ (MinGW/MSYS2) 或 Clang 16+ 或 MSVC 2022+
- **Ninja** 构建系统
- **Rust toolchain**（Slint 编译需要）

### 构建

```powershell
# 配置
cmake --preset debug

# 构建主程序
cmake --build build/debug --target wt-launcher

# 构建并运行测试
cmake --build build/debug --target core_tests --target platform_tests
ctest --test-dir build/debug
```

> 所有依赖（reproc/glaze/fmt/spdlog/doctest/trompeloeil）由 CPM.cmake 自动下载和编译，无需 vcpkg。

### 配置

将 `config/config.example.json` 复制为 `config/config.json`，按实际环境修改目录与命令。

## 目录结构

```
launchpad/
├── src/
│   ├── core/          核心业务逻辑（Config/Launcher/LaunchPlanBuilder/quote_arg/is_dangerous）
│   ├── platform/      平台抽象层（PathResolver/TerminalLauncher/SingleInstance/ThemeDetector）
│   └── ui/            Slint 界面（main_window + lucide 图标）
├── tests/
│   ├── core/          core_lib 单元测试（doctest，19+ 用例）
│   └── platform/      平台抽象测试（Trompeloeil mock，4+ 用例）
├── cmake/
│   ├── CPM.cmake      声明式依赖管理
│   └── CompileOptions.cmake  编译选项
├── config/
├── .github/workflows/ CI/CD 流水线
├── .clang-tidy        静态分析配置
├── .pre-commit-config.yaml  提交钩子
├── CMakeLists.txt
├── CMakePresets.json
└── vcpkg.json         （归档，现已由 CPM 替代）
```

## 架构

```
src/core/   (zero Slint dep)   核心逻辑
    ↑
src/platform/                  平台抽象（虚基类 + 工厂 + reproc 进程启动）
    ↑
src/app.cpp + ui/              Slint 界面层
```

**关键架构决策**：
- **依赖方向**：`core_lib` -> `platform_lib` -> `wt-launcher`
- **零 shell 执行**：所有进程通过 reproc argv 启动，`::popen`/`::system` 被 clang-tidy 禁止
- **可测试性**：平台抽象通过虚基类 + 工厂函数实现，使用 Trompeloeil 进行 mock 测试
- **RAII 生命周期**：`std::jthread` 替代 `std::thread::detach`，支持协作取消

## 依赖管理

所有第三方依赖通过 CPM.cmake 声明式管理（从源码构建，自动适配编译器）：

| 依赖 | 用途 | 类型 |
|------|------|------|
| reproc | 跨平台进程启动 | 静态库 |
| glaze | JSON 序列化 | 静态库 |
| fmt | 格式化 | 静态库 |
| spdlog | 日志 | 静态库 |
| trompeloeil | Mock 框架 | header-only |
| doctest | 测试框架 | header-only |
| Slint | UI 框架 | FetchContent (Rust) |

## 安全边界

- 启动前校验目录是否存在、命令是否为空
- **零 shell 执行**：所有子进程通过 reproc 以 argv 数组传递参数
- 启动确认由全局开关 `settings.json` 的 `confirmEnabled` 控制
- 危险命令检测：`dangerously`、`yolo`、`skip-permissions`、`bypass-approvals`、`bypass-sandbox`
- 批量启动逐项检查确认设置
- 命令行参数按 Windows 终端规则正确引号化
- CI 中 clang-tidy 静态分析自动禁止 `::popen` / `::system` / 字符串拼接命令

## 验证

```powershell
# 全部测试
ctest --test-dir build/debug --output-on-failure

# 构建测试目标
cmake --build build/debug --target core_tests
cmake --build build/debug --target platform_tests

# 静态分析（需安装 clang-tidy）
pre-commit run clang-tidy --all-files
```

## 技术栈

- C++23 + CMake 3.29+ + Ninja
- Slint 1.x（UI 框架，Rust）
- reproc 14.x（跨平台进程启动）
- glaze 4.x（JSON 序列化）
- spdlog + fmt（日志 + 格式化）
- doctest + Trompeloeil（测试 + mock）
- CPM.cmake（依赖管理）
- Windows / macOS / Linux（跨平台）
