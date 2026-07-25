#include "core/is_dangerous.h"
#include <regex>

namespace core {

auto is_dangerous(const std::string& command) -> bool {
    static const std::regex pattern(
        R"((?:dangerously|yolo|skip.permissions|bypass.approvals|bypass.sandbox))",
        std::regex::icase | std::regex::optimize
    );
    return std::regex_search(command, pattern);
}

} // namespace core
