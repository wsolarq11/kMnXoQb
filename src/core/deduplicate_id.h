#pragma once

#include <span>
#include <string>
#include <string_view>

#include "core/launch_item.h"

namespace core {

/// Given a desired ID and a list of existing items, returns a unique ID.
/// If the desired ID does not collide, it is returned as-is.
/// Otherwise, appends "_2", "_3", etc. until a unique ID is found.
///
/// Example:
///   deduplicate_id("VS Code", [{"id": "VS Code"}]) -> "VS Code_2"
auto deduplicate_id(std::string_view desired_id,
                    std::span<const LaunchItem> existing) -> std::string;

} // namespace core
