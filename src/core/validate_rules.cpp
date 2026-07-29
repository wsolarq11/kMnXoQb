#include "core/validate_rules.h"

namespace core {

auto validate_rules(const LaunchItem& item) -> std::expected<void, Error> {
    if (item.name.empty()) {
        return std::unexpected(Error::InvalidItem("name is empty"));
    }
    if (item.directory.empty()) {
        return std::unexpected(Error::InvalidItem("directory is empty"));
    }
    if (item.command.empty()) {
        return std::unexpected(Error::CommandEmpty());
    }
    return {};
}

} // namespace core
