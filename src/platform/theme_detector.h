#pragma once

#include <memory>

namespace pal {

// 系统主题检测器。
// 检测操作系统当前是暗色还是亮色主题。
// - Windows: 注册表 HKCU\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize\AppsUseLightTheme
// - macOS: NSUserDefaults standardUserDefaults bool AppleInterfaceStyle（暗色时返回 "Dark"）
// - Linux: gsettings get org.gnome.desktop.interface color-scheme
class ThemeDetector {
public:
    virtual ~ThemeDetector() = default;

    // 返回 true 表示系统当前为暗色主题
    virtual auto is_system_dark() -> bool = 0;

    // 工厂方法，创建平台相关的 ThemeDetector 实例
    static auto create() -> std::unique_ptr<ThemeDetector>;
};

} // namespace pal
