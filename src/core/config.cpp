#include "core/config.h"
#include "core/fs_iface.h"
#include <glaze/glaze.hpp>

namespace core {

ConfigIO::ConfigIO(std::filesystem::path config_dir, FilesystemIface& fs)
    : config_dir_(std::move(config_dir)), fs_(fs) {}

auto ConfigIO::read_items() -> std::expected<std::vector<LaunchItem>, Error> {
    auto path = config_dir_ / "config.json";
    auto content = fs_.read_file(path);
    if (!content) {
        return std::unexpected(content.error());
    }

    auto result = glz::read_json<std::vector<LaunchItem>>(*content);
    if (!result) {
        return std::unexpected(Error::ConfigParseError(glz::format_error(result.error(), *content)));
    }
    return std::move(*result);
}

auto ConfigIO::write_items(const std::vector<LaunchItem>& items) -> std::expected<void, Error> {
    auto path = config_dir_ / "config.json";

    auto write_result = glz::write_json(items);
    if (!write_result) {
        return std::unexpected(Error::ConfigWriteError(glz::format_error(write_result.error())));
    }

    // Backup existing file
    if (fs_.file_exists(path)) {
        auto backup = config_dir_ / "config.json.bak";
        auto copy_result = fs_.copy_file(path, backup);
        if (!copy_result) {
            return copy_result;
        }
    }

    return fs_.write_file(path, *write_result);
}

auto ConfigIO::read_settings() -> std::expected<AppSettings, Error> {
    auto path = config_dir_ / "settings.json";
    auto content = fs_.read_file(path);
    if (!content) {
        // settings.json is optional; return defaults on missing
        return AppSettings{};
    }

    auto result = glz::read_json<AppSettings>(*content);
    if (!result) {
        return std::unexpected(Error::ConfigParseError(glz::format_error(result.error(), *content)));
    }
    return std::move(*result);
}

auto ConfigIO::write_settings(const AppSettings& settings) -> std::expected<void, Error> {
    auto path = config_dir_ / "settings.json";

    auto write_result = glz::write_json(settings);
    if (!write_result) {
        return std::unexpected(Error::ConfigWriteError(glz::format_error(write_result.error())));
    }

    return fs_.write_file(path, *write_result);
}

} // namespace core
