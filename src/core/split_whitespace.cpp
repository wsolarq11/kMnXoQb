#include "core/split_whitespace.h"

namespace core {

// 按空白拆分。不支持引号嵌套/转义。
// 如需支持引号，应升级为完整命令行解析器（如 CommandLineToArgvW）。
auto split_by_whitespace(const std::string& s) -> std::vector<std::string> {
    std::vector<std::string> tokens;
    std::string current;
    for (char ch : s) {
        if (ch == ' ' || ch == '\t') {
            if (!current.empty()) {
                tokens.push_back(std::move(current));
                current.clear();
            }
        } else {
            current += ch;
        }
    }
    if (!current.empty()) {
        tokens.push_back(std::move(current));
    }
    return tokens;
}

} // namespace core
