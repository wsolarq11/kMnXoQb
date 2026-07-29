#include "core/deduplicate_id.h"
#include <algorithm>

namespace core {

auto deduplicate_id(std::string_view desired_id,
                    std::span<const LaunchItem> existing) -> std::string {
    std::string candidate(desired_id);

    int n = 2;
    while (std::any_of(existing.begin(), existing.end(),
                       [&](const LaunchItem& i) { return i.id == candidate; })) {
        candidate = std::string(desired_id) + "_" + std::to_string(n++);
    }
    return candidate;
}

} // namespace core
