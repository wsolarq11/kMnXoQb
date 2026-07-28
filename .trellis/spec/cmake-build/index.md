# CMake Build 规范

## Target 分层

- `core_lib` (STATIC)：核心业务逻辑，零 Slint/platform 依赖
- `platform_lib` (STATIC)：平台抽象，依赖 `core_lib` (PRIVATE)，链接 `reproc` `reproc++`
- `wt-launcher` (EXECUTABLE)：主程序，依赖 `core_lib` + `platform_lib` + `Slint::Slint`

依赖方向严格单向：`core_lib` -> `platform_lib` -> `wt-launcher`

## 编译要求

- C++23 标准（`set(CMAKE_CXX_STANDARD 23)`）
- `CMAKE_CXX_STANDARD_REQUIRED ON`
- 编译器：GCC 14+ (MinGW)、Clang 16+、MSVC 2022+
- 构建系统：Ninja（推荐）或 MinGW Makefiles
- 警告级别：`-Wall -Wextra -Wpedantic`（GCC/Clang）
- 配置通过 `CompileOptions.cmake` 集中管理

## 依赖管理

- 使用 **CPM.cmake** 声明式管理所有第三方依赖（`cmake/CPM.cmake`）
- 依赖从源码构建，自动适配当前编译器，无需 vcpkg
- 当前依赖清单：

| 依赖 | 版本 | 用途 | 集成方式 |
|------|------|------|----------|
| reproc | 14.2.5 | 跨平台进程启动 | CPMAddPackage (REPROC++ ON) |
| glaze | 4.4.3 | JSON 序列化 | CPMAddPackage |
| fmt | 11.1.4 | 格式化 (spdlog 依赖) | CPMAddPackage (FMT_TEST OFF) |
| spdlog | 1.15.3 | 日志 | CPMAddPackage (SPDLOG_FMT_EXTERNAL OFF) |
| trompeloeil | v47 | 测试 mock | CPMAddPackage (header-only) |
| doctest | 2.4.11 | 测试框架 | CPMAddPackage (header-only) |
| Slint | release/1 | UI 框架 | FetchContent (Rust) |

- `POST_BUILD` 使用 `$<TARGET_RUNTIME_DLLS:target>` 自动复制运行时 DLL

### 构建优化

- **FMT_TEST=OFF** 在 CMakePresets 层设置，避免编译 fmt 测试二进制（节省 ~478 MB）
- **CARGO_HOME** 指向独立缓存目录（`C:/.cargo-cache`），跨构建复用 Rust crate 下载
- **CPM_SOURCE_CACHE** 缓存依赖源码，避免重复下载
- 增量编译 ~7 秒，保持 build 目录不清除

## CMakePresets

- `debug` 预设：MinGW + Debug + GCC 16.1.0 + FMT_TEST OFF + CARGO_HOME
- `release` 预设：MinGW + Release + GCC + FMT_TEST OFF
- 所有预设直接指定 `CMAKE_C_COMPILER` 和 `CMAKE_CXX_COMPILER`，不依赖 vcpkg toolchain

## 测试

- 每个 core_lib 和 platform_lib 有对应的 test target
- test target 通过 `add_test()` 注册到 CTest
- 测试框架：doctest（单元测试）+ Trompeloeil（mock）
- 平台测试不应有 `if(WIN32)` 限制，应在条件编译源文件层面隔离
- 运行测试：`ctest --test-dir build/debug --output-on-failure`
