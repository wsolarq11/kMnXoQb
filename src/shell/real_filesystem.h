#pragma once

#include "core/fs_iface.h"
#include <expected>
#include <filesystem>
#include <fstream>
#include <string>

namespace shell {

class RealFilesystem final : public core::FilesystemIface {
public:
    auto read_file(const std::filesystem::path& path)
        -> std::expected<std::string, core::Error> override {
        std::ifstream file(path);
        if (!file.is_open()) {
            return std::unexpected(core::Error::ConfigNotFound(path.string()));
        }
        return std::string(std::istreambuf_iterator<char>(file),
                          std::istreambuf_iterator<char>());
    }

    auto write_file(const std::filesystem::path& path, const std::string& content)
        -> std::expected<void, core::Error> override {
        std::ofstream file(path);
        if (!file.is_open()) {
            return std::unexpected(
                core::Error::ConfigWriteError("Cannot open file for writing: " + path.string()));
        }
        file << content;
        return {};
    }

    auto file_exists(const std::filesystem::path& path) const -> bool override {
        return std::filesystem::exists(path) && std::filesystem::is_regular_file(path);
    }

    auto directory_exists(const std::filesystem::path& path) const -> bool override {
        std::error_code ec;
        return std::filesystem::is_directory(path, ec);
    }

    auto rename(const std::filesystem::path& from, const std::filesystem::path& to)
        -> std::expected<void, core::Error> override {
        std::error_code ec;
        std::filesystem::rename(from, to, ec);
        if (ec) {
            return std::unexpected(core::Error::ConfigWriteError(
                "Rename failed: " + from.string() + " -> " + to.string()));
        }
        return {};
    }

    auto copy_file(const std::filesystem::path& from, const std::filesystem::path& to)
        -> std::expected<void, core::Error> override {
        std::error_code ec;
        std::filesystem::copy_file(from, to, std::filesystem::copy_options::overwrite_existing, ec);
        if (ec) {
            return std::unexpected(core::Error::ConfigWriteError(
                "Copy failed: " + from.string() + " -> " + to.string()));
        }
        return {};
    }
};

} // namespace shell
