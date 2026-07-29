#pragma once

#include <string>
#include <vector>

namespace core {

/// Splits a string by whitespace (space, tab).
/// Known limitation: does not handle double-quoted regions.
/// For full parsing, use platform API (CommandLineToArgvW on Windows).
///
/// Example:
///   split_by_whitespace("wt.exe -d /tmp pwsh") -> {"wt.exe", "-d", "/tmp", "pwsh"}
auto split_by_whitespace(const std::string& s) -> std::vector<std::string>;

} // namespace core
