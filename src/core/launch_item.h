#pragma once

#include <string>
#include <optional>
#include <vector>

#include <glaze/glaze.hpp>

namespace core {

// 主题枚举（light / dark / system）
enum class Theme { light, dark, system };

struct LaunchItem {
    std::string name;
    std::string directory;
    std::string command;
    bool confirm = true;
    std::string id;
    bool selected = false;
    // 扩展字段（向后兼容）
    std::optional<std::string> terminal;
    std::optional<std::string> tag;
    std::optional<std::string> group;
};

struct WindowState {
    int x = -1;
    int y = -1;
    int width = 700;
    int height = 500;
};

struct AppSettings {
    bool confirm_enabled = false;
    std::string theme = "system";
    std::vector<std::string> launch_history;
    std::optional<WindowState> window_state;
};

} // namespace core

// Glaze 反射（放在 core 命名空间外，因为模板特化需要在命名空间外）
template<>
struct glz::meta<core::LaunchItem> {
    using T = core::LaunchItem;
    static constexpr auto value = glz::object(
        "name", &T::name,
        "directory", &T::directory,
        "command", &T::command,
        "confirm", &T::confirm,
        "id", &T::id,
        "selected", &T::selected,
        "terminal", &T::terminal,
        "tag", &T::tag,
        "group", &T::group
    );
};

template<>
struct glz::meta<core::WindowState> {
    using T = core::WindowState;
    static constexpr auto value = glz::object(
        "x", &T::x,
        "y", &T::y,
        "width", &T::width,
        "height", &T::height
    );
};

template<>
struct glz::meta<core::AppSettings> {
    using T = core::AppSettings;
    // modify 系统提供向后兼容：
    // - "confirm_enabled" 是新 key（蛇形，与 struct 成员名一致，单一源）
    // - "confirmEnabled" 是旧 key（驼峰，来自旧 HTA settings.json），alias 指向同一成员
    // 读入任一 key 均生效；写出统一用 "confirm_enabled"
    static constexpr auto modify = glz::object(
        "confirm_enabled", &T::confirm_enabled,
        "confirmEnabled",  [](auto& s) -> auto& { return s.confirm_enabled; },
        "theme",           &T::theme,
        "launch_history",  &T::launch_history,
        "window_state",    &T::window_state
    );
};
