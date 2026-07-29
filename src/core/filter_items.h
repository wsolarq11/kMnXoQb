#pragma once

#include <cstddef>
#include <span>
#include <string>
#include <string_view>
#include <vector>

#include "core/launch_item.h"

namespace core {

/// Returns indices of items whose name, directory, or command contain
/// the query string (case-insensitive match). Returns indices of all items
/// when query is empty.
///
/// Example:
///   filter_items([{"VS Code", "D:\\", "code ."}, {"Terminal", "/tmp", "bash"}], "code")
///   -> {0}  (only VS Code matches)
auto filter_items(std::span<const LaunchItem> items,
                  std::string_view query) -> std::vector<size_t>;

} // namespace core
