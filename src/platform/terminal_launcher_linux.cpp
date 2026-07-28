#include "platform/terminal_launcher.h"

#if defined(__linux__)

#include <string>
#include <vector>
#include <spawn.h>
#include <unistd.h>
#include <cerrno>
#include <cstring>

extern char** environ;

namespace pal {

// Linux 终端启动器：用 posix_spawnp 启动终端模拟器，全程无 shell
class LinuxTerminalLauncher : public TerminalLauncher {
public:
    auto populate(core::LaunchPlan plan) const
        -> std::expected<core::LaunchPlan, core::Error> override;

    auto launch(const core::LaunchPlan& plan)
        -> std::expected<ProcessHandle, core::Error> override;

    auto default_terminal_name() const -> std::string override {
        return "GNOME Terminal";
    }
};

auto LinuxTerminalLauncher::populate(core::LaunchPlan plan) const
    -> std::expected<core::LaunchPlan, core::Error> {

    if (plan.terminal_override.has_value() && !plan.terminal_override->empty()) {
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

    // 按优先级探测终端模拟器（用 access(X_OK)，不走 shell）
    const char* terminals[] = {
        "gnome-terminal",
        "konsole",
        "xfce4-terminal",
        "xterm",
    };

    for (auto term : terminals) {
        if (access(term, X_OK) == 0) {
            plan.executable = term;
            std::string dir = plan.working_dir.string();
            // 转义单引号以安全使用 sh -c "cd '<dir>' && ..."
            std::string escaped_dir;
            for (char c : dir) {
                if (c == '\'')
                    escaped_dir += "'\\''";  // end quote, escaped quote, resume
                else
                    escaped_dir += c;
            }
            std::string inner = "cd '" + escaped_dir + "' && " + plan.command + "; exec bash";
            plan.args = {"--", "bash", "-c", inner};
            return plan;
        }
    }

    return std::unexpected(core::Error::TerminalNotFound(
        "No supported terminal emulator found"));
}

auto LinuxTerminalLauncher::launch(const core::LaunchPlan& plan)
    -> std::expected<ProcessHandle, core::Error> {

    // 构造 argv 数组
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

auto create_linux_launcher() -> std::unique_ptr<pal::TerminalLauncher> {
    return std::make_unique<pal::LinuxTerminalLauncher>();
}

#endif // __linux__
