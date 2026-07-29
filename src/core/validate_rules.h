#pragma once

#include <expected>

#include "core/error.h"
#include "core/launch_item.h"

namespace core {

/// Pure validation rules for a LaunchItem.
/// Does NOT access the filesystem. For directory existence checks,
/// the caller must validate separately via injected filesystem abstraction.
auto validate_rules(const LaunchItem& item) -> std::expected<void, Error>;

} // namespace core
