#pragma once

#include <filesystem>
#include <expected>
#include <memory>

#include "core/error.h"

namespace pal {

// 路径解析器，提供配置目录和数据目录的跨平台实现。
// Windows: 便携优先（exe 同级 config/）
// macOS: ~/Library/Application Support/launchpad/config
// Linux: ~/.config/launchpad/config
class PathResolver {
public:
    virtual ~PathResolver() = default;

    PathResolver(const PathResolver&) = delete;
    PathResolver& operator=(const PathResolver&) = delete;

    // 配置目录：便携优先（exe 同级 config/），否则平台标准回退
    virtual auto config_directory() -> std::expected<std::filesystem::path, core::Error> = 0;

    // 应用程序数据目录（日志、缓存等）
    virtual auto data_directory() -> std::filesystem::path = 0;

    // 工厂方法，创建平台相关的 PathResolver 实例
    static auto create() -> std::unique_ptr<PathResolver>;

protected:
    PathResolver() = default;
};

} // namespace pal
