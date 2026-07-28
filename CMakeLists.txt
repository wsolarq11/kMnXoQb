cmake_minimum_required(VERSION 3.29)

# 必须在 project() 之前设置，禁用 C++ 模块扫描避免 CMake 4.x + Ninja 的 $Config 问题
set(CMAKE_CXX_SCAN_FOR_MODULES OFF)

project(WTLauncher VERSION 0.1.0 LANGUAGES CXX)

include(cmake/CompileOptions.cmake)

# 启用 C 语言 (reproc 原生 C 库依赖)
enable_language(C)

# CPM.cmake: 声明式依赖管理 (用于 header-only 和无需 vcpkg 的依赖)
include(cmake/CPM.cmake)

# Trompeloeil: C++14 header-only mock 框架
# 用于跨平台测试，在 Windows 上模拟 macOS/Linux 平台代码
CPMAddPackage(
  NAME trompeloeil
  GITHUB_REPOSITORY rollbear/trompeloeil
  GIT_TAG v47
  VERSION 47
  DOWNLOAD_ONLY YES
)
if(trompeloeil_ADDED)
  add_library(trompeloeil INTERFACE)
  target_include_directories(trompeloeil INTERFACE
    "${trompeloeil_SOURCE_DIR}/include"
  )
endif()

# reproc: 跨平台进程启动库 (替代 popen/shell 拼接)
# C library target: reproc, C++ wrapper target: reproc++
CPMAddPackage(
  NAME reproc
  GITHUB_REPOSITORY DaanDeMeyer/reproc
  GIT_TAG v14.2.5
  VERSION 14.2.5
  OPTIONS "REPROC++" "ON"
)

# glaze: 高性能 JSON 序列化库
CPMAddPackage(
  NAME glaze
  GITHUB_REPOSITORY stephenberry/glaze
  GIT_TAG v4.4.3
  VERSION 4.4.3
)

# fmt: 格式化库 (spdlog 依赖)
CPMAddPackage(
  NAME fmt
  GITHUB_REPOSITORY fmtlib/fmt
  GIT_TAG 11.1.4
  VERSION 11.1.4
  OPTIONS
    "FMT_TEST" "OFF"
    "FMT_DOC" "OFF"
)

# spdlog: 高性能日志库
CPMAddPackage(
  NAME spdlog
  GITHUB_REPOSITORY gabime/spdlog
  GIT_TAG v1.15.3
  VERSION 1.15.3
  OPTIONS
    "SPDLOG_FMT_EXTERNAL" "OFF"
    "SPDLOG_BUILD_TESTING" "OFF"
    "SPDLOG_BUILD_EXAMPLE" "OFF"
)

# doctest: 轻量 C++ 测试框架 (header-only)
CPMAddPackage(
  NAME doctest
  GITHUB_REPOSITORY doctest/doctest
  GIT_TAG v2.4.11
  VERSION 2.4.11
  DOWNLOAD_ONLY YES
)
if(doctest_ADDED)
  add_library(doctest INTERFACE)
  add_library(doctest::doctest ALIAS doctest)
  target_include_directories(doctest INTERFACE
    "${doctest_SOURCE_DIR}"
  )
endif()

# RapidCheck: 属性测试框架 (QuickCheck for C++, builds as static library)
CPMAddPackage(
  NAME rapidcheck
  GITHUB_REPOSITORY emil-e/rapidcheck
  GIT_TAG master
  VERSION 0
)

enable_testing()

# Slint: 先找已安装的，找不到就从源码构建
find_package(Slint QUIET)
if (NOT Slint_FOUND)
    message(STATUS "Slint not found via find_package. Fetching from GitHub...")
    include(FetchContent)
    FetchContent_Declare(
        Slint
        GIT_REPOSITORY https://github.com/slint-ui/slint.git
        GIT_TAG release/1
        SOURCE_SUBDIR api/cpp
    )
    set(BUILD_SHARED_LIBS ON CACHE BOOL "Build Slint as shared library" FORCE)
    FetchContent_MakeAvailable(Slint)
endif()

add_subdirectory(src)
add_subdirectory(tests)
