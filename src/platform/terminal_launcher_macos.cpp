#include "platform/terminal_launcher.h"

#if defined(__APPLE__)

#include <string>
#include <vector>
#include <spawn.h>
#include <unistd.h>
#include <cerrno>
#include <cstring>

extern char** environ;

namespace pal {

// macOS 终端启动器：用 posix_spawn 启动 osascript，全程无 shell
class MacTerminalLauncher : public TerminalLauncher {
public:
    auto populate(core::LaunchPlan plan) const
        -> std::expected<core::LaunchPlan, core::Error> override;

    auto launch(const core::LaunchPlan& plan)
        -> std::expected<ProcessHandle, core::Error> override;

    auto default_terminal_name() const -> std::string override {
        return "Terminal.app";
    }
};

namespace detail {

// 转义 AppleScript 字符串中的反斜杠和引号
auto escape_applescript(const std::string& s) -> std::string {
    std::string out;
    out.reserve(s.size() + 8);
    for (char ch : s) {
        if (ch == '\\') {
            out += "\\\\";
        } else if (ch == '"') {
            out += "\\\"";
        } else {
            out += ch;
        }
    }
    return out;
}

} // namespace detail

auto MacTerminalLauncher::populate(core::LaunchPlan plan) const
    -> std::expected<core::LaunchPlan, core::Error> {

    if (plan.terminal_override.has_value() && !plan.terminal_override->empty()) {
        // 自定义终端：executable = override，args = [command]
        // 简单按首个空格拆分
        auto& override_str = *plan.terminal_override;
        auto space_pos = override_str.find(' ');
        if (space_pos == std::string::npos) {
            plan.executable = override_str;
            plan.args = {plan.command};
        } else {
            plan.executable = override_str.substr(0, space_pos);
            std::string rest = override_str.substr(space_pos + 1);
            plan.args = {rest, plan.command};
        }
        return plan;
    }

    // 默认: osascript -e 'tell app "Terminal" to do script "cd " & quoted form of "<dir>" & "; <cmd>"'
    // 使用 quoted form of 确保路径中的空格和特殊字符被正确转义
    std::string dir = plan.working_dir.string();
    std::string script =
        "tell app \"Terminal\" to do script \"cd \" & quoted form of \"" +
        detail::escape_applescript(dir) + "\" & \"; \" & quoted form of \"" +
        detail::escape_applescript(plan.command) + "\"";

    plan.executable = "/usr/bin/osascript";
    plan.args = {"-e", script};
    return plan;
}

auto MacTerminalLauncher::launch(const core::LaunchPlan& plan)
    -> std::expected<ProcessHandle, core::Error> {

    // 构造 argv 数组（C 风格，posix_spawn 需要）
    std::vector<std::string> owned;
    owned.push_back(plan.executable.string());
    for (const auto& a : plan.args) {
        owned.push_back(a);
    }

    std::vector<char*> argv;
    argv.reserve(owned.size() + 1);
    for (auto& s : owned) {
        argv.push_back(s.data());
    }
    argv.push_back(nullptr);

    // 工作目录
    posix_spawn_file_actions_t actions;
    posix_spawn_file_actions_init(&actions);
    if (!plan.working_dir.empty()) {
        posix_spawn_file_actions_addchdir_np(&actions, plan.working_dir.c_str());
    }

    pid_t pid = 0;
    int ret = posix_spawnp(&pid, plan.executable.c_str(), &actions, nullptr, argv.data(), environ);
    posix_spawn_file_actions_destroy(&actions);

    if (ret != 0) {
        return std::unexpected(core::Error::LaunchFailed(
            "posix_spawn failed: " + std::string(std::strerror(ret))));
    }

    ProcessHandle handle;
    handle.pid_ = pid;
    return handle;
}

} // namespace pal

auto create_macos_launcher() -> std::unique_ptr<pal::TerminalLauncher> {
    return std::make_unique<pal::MacTerminalLauncher>();
}

#endif // __APPLE__
