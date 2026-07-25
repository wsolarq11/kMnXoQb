#include "platform/terminal_launcher.h"

#if defined(__APPLE__)

#include <string>
#include <cstdlib>

namespace pal {

// macOS 终端启动器：使用 open -a Terminal 打开新终端
class MacTerminalLauncher : public TerminalLauncher {
public:
    auto launch(const std::string& directory, const std::string& command)
        -> std::expected<ProcessHandle, core::Error> override;

    auto default_terminal_name() const -> std::string override {
        return "Terminal.app";
    }
};

auto MacTerminalLauncher::launch(const std::string& directory, const std::string& command)
    -> std::expected<ProcessHandle, core::Error>
{
    // 使用 AppleScript 在 Terminal 中打开新标签页并执行命令
    std::string script = "osascript -e 'tell app \"Terminal\" to do script "
        + core::quote_arg("cd " + core::quote_arg(directory) + "; " + command)
        + "' 2>/dev/null";

    int ret = std::system(script.c_str());
    if (ret != 0) {
        return std::unexpected(core::Error::LaunchFailed(
            "osascript failed with code " + std::to_string(ret)));
    }

    ProcessHandle handle;
    handle.pid = 0;
    return handle;
}

} // namespace pal

auto create_macos_launcher() -> std::unique_ptr<pal::TerminalLauncher> {
    return std::make_unique<pal::MacTerminalLauncher>();
}

#endif // __APPLE__
