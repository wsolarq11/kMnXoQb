#pragma once

#include <string>

namespace core {

// 检测命令是否包含危险标志
// 与 HTA isDangerous 同规则
auto is_dangerous(const std::string& command) -> bool;

} // namespace core
