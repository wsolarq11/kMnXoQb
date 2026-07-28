# C++ Core Layer 规范

## 职责

纯业务逻辑与数据结构，零外部框架依赖（仅 glaze）。

## 约束

- **零 Slint 依赖**：core 中的头文件不得引用 `<slint-*.h>` 或 `#include <slint.h>`
- **零 platform 依赖**：core 不得引用 `platform/` 目录下任何头文件
- **零系统调用**：core 不得直接调用操作系统 API（文件读写由上层 ConfigIO 封装）

## 代码规范

- 使用 C++23 标准
- 头文件使用 `#pragma once`
- 返回值使用 `std::expected<T, Error>` 表达可恢复错误
- struct 序列化使用 Glaze `meta::modify` 而非 `meta::value`（支持别名和迁移）

## 安全边界

- `is_dangerous()` 是唯一的安全判定函数，接受 `std::string_view`
- `quote_arg()` 用于 Shell 参数转义（仅用于显示场景，不用于运行时拼接）
- `LaunchPlanBuilder::build()` 输出纯数据结构，不含 shell 语法
