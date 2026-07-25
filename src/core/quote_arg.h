#pragma once

#include <string>

namespace core {

// Windows 终端引号化规则：
// 中间反斜杠不翻倍；遇引号或位于尾部时翻倍
// 输出格式为 "..."（两端加引号）
// 逻辑与 verify.ps1 的 QuoteArg 完全一致
auto quote_arg(const std::string& arg) -> std::string;

} // namespace core
