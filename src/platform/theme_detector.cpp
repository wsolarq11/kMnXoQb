#include "platform/theme_detector.h"

#ifdef _WIN32
#include <windows.h>
#else
#include <array>
#include <cstdlib>
#include <string>
#include <system_error>
#include <reproc++/reproc.hpp>
#endif

namespace pal {

namespace {

// ThemeDetector 的真实实现，封装平台特定的主题检测逻辑。
class RealThemeDetector final : public ThemeDetector {
public:
    auto is_system_dark() -> bool override {
        return is_system_dark_impl();
    }

private:
    static auto is_system_dark_impl() -> bool {
#ifdef _WIN32
        // Windows: 查注册表 AppsUseLightTheme
        // 0 = 暗色, 1 = 亮色
        HKEY key;
        LONG result = RegOpenKeyExW(HKEY_CURRENT_USER,
            L"Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize",
            0, KEY_READ, &key);
        if (result != ERROR_SUCCESS) {
            return false;  // 默认亮色
        }

        DWORD value = 1;
        DWORD size = sizeof(value);
        DWORD type = 0;
        result = RegQueryValueExW(key, L"AppsUseLightTheme", nullptr, &type,
            reinterpret_cast<LPBYTE>(&value), &size);
        RegCloseKey(key);

        if (result != ERROR_SUCCESS) {
            return false;
        }
        return value == 0;

#elif defined(__APPLE__)
        // macOS: defaults read -g AppleInterfaceStyle
        // 暗色模式返回 "Dark"，亮色返回空
        // 使用 reproc 替代 popen，零 shell 执行
        reproc::process process;
        std::error_code ec = process.start(
            {"/usr/bin/defaults", "read", "-g", "AppleInterfaceStyle"},
            reproc::environment::empty  // 不传递环境变量，防止注入
        );
        if (ec) return false;

        std::array<char, 256> buf{};
        auto [bytes_read, read_ec] = process.read(reproc::stream::out, buf.data(), buf.size());
        process.wait(reproc::infinite);
        process.stop();

        if (read_ec || bytes_read <= 0) return false;

        std::string style(buf.data(), static_cast<size_t>(bytes_read));
        // 去除尾部换行
        while (!style.empty() && (style.back() == '\n' || style.back() == '\r')) {
            style.pop_back();
        }
        return style == "Dark";

#else
        // Linux: gsettings get org.gnome.desktop.interface color-scheme
        // 'prefer-dark' = 暗色, 其他 = 亮色
        // 使用 reproc 替代 popen，零 shell 执行
        reproc::process process;
        std::error_code ec = process.start(
            {"gsettings", "get", "org.gnome.desktop.interface", "color-scheme"},
            reproc::environment::empty
        );
        if (ec) return false;

        std::array<char, 256> buf{};
        auto [bytes_read, read_ec] = process.read(reproc::stream::out, buf.data(), buf.size());
        process.wait(reproc::infinite);
        process.stop();

        if (read_ec || bytes_read <= 0) return false;

        std::string scheme(buf.data(), static_cast<size_t>(bytes_read));
        return scheme.find("dark") != std::string::npos;
#endif
    }
};

} // anonymous namespace

auto ThemeDetector::create() -> std::unique_ptr<ThemeDetector> {
    return std::make_unique<RealThemeDetector>();
}

} // namespace pal
