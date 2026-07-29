#include "core/filter_items.h"
#include <algorithm>
#include <cctype>

namespace core {

namespace {

auto to_lower(std::string_view sv) -> std::string {
    std::string lower;
    lower.resize(sv.size());
    std::transform(sv.begin(), sv.end(), lower.begin(),
                   [](unsigned char c) { return static_cast<char>(std::tolower(c)); });
    return lower;
}

} // anonymous namespace

auto filter_items(std::span<const LaunchItem> items,
                  std::string_view query) -> std::vector<size_t> {
    if (query.empty()) {
        std::vector<size_t> indices;
        indices.reserve(items.size());
        for (size_t i = 0; i < items.size(); ++i) {
            indices.push_back(i);
        }
        return indices;
    }

    auto query_lower = to_lower(query);
    std::vector<size_t> matched;
    for (size_t i = 0; i < items.size(); ++i) {
        const auto& item = items[i];
        auto name_lower = to_lower(item.name);
        auto dir_lower = to_lower(item.directory);
        auto cmd_lower = to_lower(item.command);

        if (name_lower.find(query_lower) != std::string::npos ||
            dir_lower.find(query_lower) != std::string::npos ||
            cmd_lower.find(query_lower) != std::string::npos) {
            matched.push_back(i);
        }
    }
    return matched;
}

} // namespace core
