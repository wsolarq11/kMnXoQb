#pragma once

#include <expected>
#include <filesystem>
#include <string>

#include "core/error.h"

namespace core {

/// Abstract filesystem interface for pure-core separation.
/// Real implementation lives in the shell layer; tests provide mock implementations.
class FilesystemIface {
public:
    virtual ~FilesystemIface() = default;

    virtual auto read_file(const std::filesystem::path& path)
        -> std::expected<std::string, Error> = 0;

    virtual auto write_file(const std::filesystem::path& path,
                            const std::string& content)
        -> std::expected<void, Error> = 0;

    virtual auto file_exists(const std::filesystem::path& path) const -> bool = 0;

    virtual auto directory_exists(const std::filesystem::path& path) const -> bool = 0;

    virtual auto rename(const std::filesystem::path& from,
                        const std::filesystem::path& to)
        -> std::expected<void, Error> = 0;

    virtual auto copy_file(const std::filesystem::path& from,
                           const std::filesystem::path& to)
        -> std::expected<void, Error> = 0;
};

} // namespace core
