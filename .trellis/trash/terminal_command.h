#pragma once

#include <string>

#ifdef _WIN32

namespace pal {

// 构建 Windows Terminal (wt) 命令字符串
// 格式: wt -d "<directory>" pwsh -NoExit -Command "<command>"
// 使用 core::quote_arg 对路径和命令进行引号化
auto build_wt_command_string(const std::string& directory, const std::string& command) -> std::string;

// 构建 PowerShell (pwsh) 命令字符串
// 格式: pwsh -NoExit -Command "cd <dir>; <command>"
// 使用 core::quote_arg 对复合命令进行引号化
auto build_pwsh_command_string(const std::string& directory, const std::string& command) -> std::string;

} // namespace pal

#endif // _WIN32
