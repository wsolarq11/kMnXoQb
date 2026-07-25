#include "platform/path_resolver.h"

#ifdef _WIN32
#include <windows.h>
#else
#include <unistd.h>
#include <climits>
#endif

namespace pal {

PathResolver::PathResolver() {
#ifdef _WIN32
    wchar_t path[MAX_PATH];
    GetModuleFileNameW(nullptr, path, MAX_PATH);
    exe_dir_ = std::filesystem::path(path).parent_path();
#elif defined(__APPLE__)
    char path[PATH_MAX];
    uint32_t size = PATH_MAX;
    if (_NSGetExecutablePath(path, &size) == 0) {
        exe_dir_ = std::filesystem::path(path).parent_path();
    }
#else
    char path[PATH_MAX];
    auto len = readlink("/proc/self/exe", path, sizeof(path) - 1);
    if (len != -1) {
        path[len] = '\0';
        exe_dir_ = std::filesystem::path(path).parent_path();
    }
#endif
}

auto PathResolver::config_directory() -> std::expected<std::filesystem::path, core::Error> {
    // 便携优先：exe 同级 config/
    auto portable = exe_dir_ / "config";
    if (std::filesystem::exists(portable)) {
        return portable;
    }

    // 平台标准回退
#ifdef _WIN32
    // Windows：便携模式优先，不存在时仍然返回 portable（创建前检查）
    return portable;
#elif defined(__APPLE__)
    auto home = std::getenv("HOME");
    return std::filesystem::path(home ? home : "") / "Library" / "Application Support" / "launchpad" / "config";
#else
    auto home = std::getenv("HOME");
    return std::filesystem::path(home ? home : "") / ".config" / "launchpad" / "config";
#endif
}

auto PathResolver::data_directory() -> std::filesystem::path {
#ifdef _WIN32
    return exe_dir_ / "data";
#elif defined(__APPLE__)
    auto home = std::getenv("HOME");
    return std::filesystem::path(home ? home : "") / "Library" / "Application Support" / "launchpad";
#else
    auto home = std::getenv("HOME");
    return std::filesystem::path(home ? home : "") / ".local" / "share" / "launchpad";
#endif
}

} // namespace pal
