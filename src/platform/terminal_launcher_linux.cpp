#include "platform/terminal_launcher.h"
#include "core/quote_arg.h"

#if defined(__linux__)

#include <string>
#include <cstdlib>

namespace pal {

// Linux 终端启动器：尝试多个终端模拟器
class LinuxTerminalLauncher : public TerminalLauncher {
public:
    auto launch(const std::string& directory, const std::string& command)
        -> std::expected<ProcessHandle, core::Error> override;

    auto default_terminal_name() const -> std::string override {
        return "GNOME Terminal";
    }
};

auto LinuxTerminalLauncher::launch(const std::string& directory, const std::string& command)
    -> std::expected<ProcessHandle, core::Error>
{
    // 构造命令：cd 到目录后执行
    std::string cmd = "cd " + core::quote_arg(directory) + " && " + command;

    // 按优先级选择终端模拟器
    const char* terminals[] = {
        "gnome-terminal",   // GNOME
        "konsole",          // KDE
        "xfce4-terminal",   // XFCE
        "xterm",            // 回退
    };

    for (auto term : terminals) {
        std::string full_cmd = std::string("which ") + term + " 2>/dev/null && "
            + term + " -- " + core::quote_arg("bash -c " + core::quote_arg(cmd + "; exec bash"))
            + " 2>/dev/null";
        int ret = std::system(full_cmd.c_str());
        if (ret == 0) {
            ProcessHandle handle;
            handle.pid = 0;
            return handle;
        }
    }

    return std::unexpected(core::Error::TerminalNotFound(
        "No supported terminal emulator found"));
}

} // namespace pal

#endif // __linux__
