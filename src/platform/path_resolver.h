#pragma once

#include <filesystem>
#include <expected>

#include "core/error.h"

namespace pal {

class PathResolver {
public:
    PathResolver();

    // 配置目录：便携优先（exe 同级 config/），否则平台标准回退
    auto config_directory() -> std::expected<std::filesystem::path, core::Error>;

    // 应用程序数据目录（日志、缓存等）
    auto data_directory() -> std::filesystem::path;

private:
    std::filesystem::path exe_dir_;
};

} // namespace pal
