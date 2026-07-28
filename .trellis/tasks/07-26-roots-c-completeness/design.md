# 子任务C 架构决策记录

## 决策 1: 用 CPM.cmake 替代 vcpkg 进行依赖管理

**背景**: 项目原本使用 vcpkg 管理第三方依赖，但本机环境使用 MinGW (GCC 16.1.0) 而非 MSVC，vcpkg 仅支持 MSVC 的 x64-windows triplet，导致工具链冲突。

**方案对比**:
| 方案 | 优点 | 缺点 |
|------|------|------|
| 安装 MSVC Build Tools | 兼容现有配置 | 下载 >5GB, 安装时间长 |
| vcpkg MinGW triplet (x64-mingw-dynamic) | 保留 vcpkg | 需额外配置 overlay triplets |
| **CPM.cmake (选定)** | 声明式, 从源码构建, 自动适配编译器 | 冷构建需编译所有依赖 |

**结论**: CPM.cmake 从源码构建，自动适配 MinGW。所有依赖 (trompeloeil, reproc, glaze, fmt, spdlog, doctest) 通过 CPM 统一管理。

## 决策 2: 用 reproc 替代 popen / system / 字符串拼接

**背景**: QA 评审发现 4 处 popen 调用和 3 处 shell 字符串拼接的安全隐患。

**方案对比**:
| 方案 | 优点 | 缺点 |
|------|------|------|
| 手写 posix_spawn / CreateProcessW | 无外部依赖 | 每平台 100+ 行样板代码 |
| **reproc (选定)** | 跨平台统一 API, argv 数组传参 | 需 CPM 引入 |

**结论**: reproc 提供跨平台统一进程启动 API，参数以 `std::vector<std::string>` 传递，零 shell 调用。

## 决策 3: 跨平台抽象使用虚基类 + 工厂函数

**背景**: ThemeDetector/SingleInstance/PathResolver 原为静态函数或具体类，不可 mock 测试。

**方案对比**:
| 方案 | 优点 | 缺点 |
|------|------|------|
| `ifdef` 条件编译 | 简单直接 | 不可测试 |
| **虚基类 + 工厂 (选定)** | 可 mock, Windows 可测试全平台逻辑 | 少量虚函数开销 |

**结论**: 三平台抽象统一为 `virtual ~X() = default` + `static auto create() -> std::unique_ptr<X>` 模式。

## 决策 4: 用 std::jthread 替代 std::thread::detach

**背景**: `app.cpp` 的 `launch_item()` 使用 `std::thread([]{}).detach()`，退出时无法等待线程。

**方案对比**:
| 方案 | 优点 | 缺点 |
|------|------|------|
| std::thread + join | 可等待 | 阻塞 UI, 无法取消 |
| **std::jthread (选定)** | RAII, 自动 join, stop_token 支持取消 | C++20 最低要求 |
| Taskflow | 任务图编排 | 此场景过度设计 |

**结论**: std::jthread 提供 RAII 线程生命周期 + stop_token 协作取消。

## 决策 5: 用 Trompeloeil 替代 Google Mock

**背景**: 需要跨平台 mock 框架测试平台抽象接口。

**方案对比**:
| 方案 | 优点 | 缺点 |
|------|------|------|
| Google Mock | 功能丰富 | 编译慢, 需 vcpkg |
| **Trompeloeil (选定)** | header-only, C++14, 与 doctest 无缝集成 | 相对较新 |

**结论**: Trompeloeil header-only 零运行时开销，与 doctest 同属轻量测试方案。
