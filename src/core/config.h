#pragma once

#include <expected>
#include <filesystem>
#include <vector>

#include "core/error.h"
#include "core/launch_item.h"

namespace core {

class ConfigIO {
public:
    explicit ConfigIO(std::filesystem::path config_dir);

    auto read_items() -> std::expected<std::vector<LaunchItem>, Error>;
    auto write_items(const std::vector<LaunchItem>& items) -> std::expected<void, Error>;
    auto read_settings() -> std::expected<AppSettings, Error>;
    auto write_settings(const AppSettings& settings) -> std::expected<void, Error>;

private:
    std::filesystem::path config_dir_;
};

} // namespace core
