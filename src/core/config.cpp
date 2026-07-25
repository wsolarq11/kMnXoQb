#include "core/config.h"
#include <fstream>
#include <glaze/glaze.hpp>

namespace core {

ConfigIO::ConfigIO(std::filesystem::path config_dir)
    : config_dir_(std::move(config_dir)) {}

auto ConfigIO::read_items() -> std::expected<std::vector<LaunchItem>, Error> {
    auto path = config_dir_ / "config.json";
    std::ifstream file(path);
    if (!file.is_open()) {
        return std::unexpected(Error::ConfigNotFound(path.string()));
    }
    std::string content((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());

    auto result = glz::read_json<std::vector<LaunchItem>>(content);
    if (!result) {
        return std::unexpected(Error::ConfigParseError(glz::format_error(result.error(), content)));
    }
    return std::move(*result);
}

auto ConfigIO::write_items(const std::vector<LaunchItem>& items) -> std::expected<void, Error> {
    auto path = config_dir_ / "config.json";

    auto write_result = glz::write_json(items);
    if (!write_result) {
        return std::unexpected(Error::ConfigWriteError(glz::format_error(write_result.error())));
    }

    // 备份现有文件
    if (std::filesystem::exists(path)) {
        std::error_code ec;
        std::filesystem::copy_file(path, config_dir_ / "config.json.bak",
            std::filesystem::copy_options::overwrite_existing, ec);
    }

    std::ofstream file(path);
    if (!file.is_open()) {
        return std::unexpected(Error::ConfigWriteError("Cannot open file for writing"));
    }
    file << *write_result;
    return {};
}

auto ConfigIO::read_settings() -> std::expected<AppSettings, Error> {
    auto path = config_dir_ / "settings.json";
    std::ifstream file(path);
    if (!file.is_open()) {
        // settings.json 可选，不存在时返回默认值
        return AppSettings{};
    }
    std::string content((std::istreambuf_iterator<char>(file)), std::istreambuf_iterator<char>());

    auto result = glz::read_json<AppSettings>(content);
    if (!result) {
        return std::unexpected(Error::ConfigParseError(glz::format_error(result.error(), content)));
    }
    return std::move(*result);
}

auto ConfigIO::write_settings(const AppSettings& settings) -> std::expected<void, Error> {
    auto path = config_dir_ / "settings.json";

    auto write_result = glz::write_json(settings);
    if (!write_result) {
        return std::unexpected(Error::ConfigWriteError(glz::format_error(write_result.error())));
    }

    std::ofstream file(path);
    if (!file.is_open()) {
        return std::unexpected(Error::ConfigWriteError("Cannot open file for writing"));
    }
    file << *write_result;
    return {};
}

} // namespace core
