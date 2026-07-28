#include "platform/terminal_launcher.h"

#ifdef _WIN32

#include <windows.h>
#include <string>
#include <vector>

namespace pal {
namespace detail {

// 构造 wt argv: new-tab -d <dir> pwsh -NoExit -Command <cmd>
auto build_wt_argv(const std::string& dir, const std::string& cmd) -> std::vector<std::string> {
    return {"new-tab", "-d", dir, "pwsh", "-NoExit", "-Command", cmd};
}
// 构造 pwsh argv: -NoExit -Command "cd '<dir>'; <cmd>"
auto build_pwsh_argv(const std::string& dir, const std::string& cmd) -> std::vector<std::string> {
    return {"-NoExit", "-Command", "cd '" + dir + "'; " + cmd};
}

// 构造 cmd argv: /k "cd /d <dir> && <cmd>" (dir quoted)
auto build_cmd_argv(const std::string& dir, const std::string& cmd) -> std::vector<std::string> {
    return {"/k", "cd /d \"" + dir + "\" && " + cmd};
}

// 按空白拆分（用于 terminal_override）。
// 不支持引号嵌套/转义：`"C:\Program Files\term.exe" --flag` 会被拆分为 5 段。
// 如需支持引号，应升级为完整命令行解析器（如 CommandLineToArgvW）。
auto split_by_whitespace(const std::string& s) -> std::vector<std::string> {
    std::vector<std::string> tokens;
    std::string current;
    for (char ch : s) {
        if (ch == ' ' || ch == '\t') {
            if (!current.empty()) {
                tokens.push_back(std::move(current));
                current.clear();
            }
        } else {
            current += ch;
        }
    }
    if (!current.empty()) {
        tokens.push_back(std::move(current));
    }
    return tokens;
}

// 按 CreateProcessW 的 lpCommandLine 规则序列化 argv：
// 每个 arg 用双引号包裹，内部反斜杠遇引号或末尾翻倍。
// 参考: Microsoft "Parsing C++ command-line arguments"
auto join_args_for_createprocess(const std::vector<std::string>& args) -> std::wstring {
    std::wstring result;
    for (size_t i = 0; i < args.size(); ++i) {
        if (i > 0) result += L' ';

        // UTF-8 -> UTF-16
        int len = MultiByteToWideChar(CP_UTF8, 0, args[i].c_str(), -1, nullptr, 0);
        std::wstring warg(len > 0 ? len - 1 : 0, L'\0');
        if (len > 1) {
            MultiByteToWideChar(CP_UTF8, 0, args[i].c_str(), -1, &warg[0], len);
        }

        // 引号化
        result += L'"';
        int backslashes = 0;
        for (wchar_t wc : warg) {
            if (wc == L'\\') {
                backslashes++;
                continue;
            }
            if (wc == L'"') {
                result.append(backslashes * 2, L'\\');
                result += L'\\';
                result += L'"';
                backslashes = 0;
                continue;
            }
            result.append(backslashes, L'\\');
            result += wc;
            backslashes = 0;
        }
        result.append(backslashes * 2, L'\\');
        result += L'"';
    }
    return result;
}

// UTF-8 string -> wstring
auto to_wstring(const std::string& s) -> std::wstring {
    if (s.empty()) return {};
    int len = MultiByteToWideChar(CP_UTF8, 0, s.c_str(), -1, nullptr, 0);
    std::wstring ws(len > 0 ? len - 1 : 0, L'\0');
    if (len > 1) {
        MultiByteToWideChar(CP_UTF8, 0, s.c_str(), -1, &ws[0], len);
    }
    return ws;
}

} // namespace detail

// Windows 终端启动器
// 优先级：wt（Windows Terminal）> pwsh（PowerShell conhost）> cmd
class WinTerminalLauncher : public TerminalLauncher {
public:
    enum class TerminalType { kWt, kPwsh, kCmd };

    WinTerminalLauncher();

    auto populate(core::LaunchPlan plan) const
        -> std::expected<core::LaunchPlan, core::Error> override;

    auto launch(const core::LaunchPlan& plan)
        -> std::expected<ProcessHandle, core::Error> override;

    auto default_terminal_name() const -> std::string override {
        switch (type_) {
            case TerminalType::kWt: return "Windows Terminal";
            case TerminalType::kPwsh: return "PowerShell";
            case TerminalType::kCmd: return "Command Prompt";
        }
        return "Unknown";
    }

private:
    TerminalType type_;
};

WinTerminalLauncher::WinTerminalLauncher() {
    HKEY key;
    LONG result = RegOpenKeyExW(HKEY_CURRENT_USER,
        L"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\wt.exe",
        0, KEY_READ, &key);
    if (result == ERROR_SUCCESS) {
        RegCloseKey(key);
        type_ = TerminalType::kWt;
    } else {
        if (GetModuleHandleW(L"pwsh.exe") ||
            SearchPathW(nullptr, L"pwsh", L".exe", 0, nullptr, nullptr)) {
            type_ = TerminalType::kPwsh;
        } else {
            type_ = TerminalType::kCmd;
        }
    }
}

auto WinTerminalLauncher::populate(core::LaunchPlan plan) const
    -> std::expected<core::LaunchPlan, core::Error> {
    const std::string& dir = plan.working_dir.string();
    const std::string& cmd = plan.command;

    if (plan.terminal_override.has_value() && !plan.terminal_override->empty()) {
        // 自定义终端覆盖：按空白拆分为 executable + args 前缀
        auto override_parts = detail::split_by_whitespace(*plan.terminal_override);
        if (override_parts.empty()) {
            return std::unexpected(core::Error::TerminalNotFound("empty terminal_override"));
        }
        plan.executable = override_parts[0];
        plan.args.assign(override_parts.begin() + 1, override_parts.end());
        plan.args.push_back(cmd);
        return plan;
    }

    switch (type_) {
        case TerminalType::kWt:
            plan.executable = "wt.exe";
            plan.args = detail::build_wt_argv(dir, cmd);
            break;
        case TerminalType::kPwsh:
            plan.executable = "pwsh.exe";
            plan.args = detail::build_pwsh_argv(dir, cmd);
            break;
        case TerminalType::kCmd:
            plan.executable = "cmd.exe";
            plan.args = detail::build_cmd_argv(dir, cmd);
            break;
    }
    return plan;
}

auto WinTerminalLauncher::launch(const core::LaunchPlan& plan)
    -> std::expected<ProcessHandle, core::Error> {

    // 构造 lpCommandLine: executable + " " + args
    std::vector<std::string> all_parts;
    all_parts.push_back(plan.executable.string());
    for (const auto& a : plan.args) {
        all_parts.push_back(a);
    }
    std::wstring cmd_line = detail::join_args_for_createprocess(all_parts);
    std::wstring wdir = detail::to_wstring(plan.working_dir.string());

    STARTUPINFOW si = { sizeof(si) };
    PROCESS_INFORMATION pi;

    BOOL success = CreateProcessW(
        nullptr,           // lpApplicationName（nullptr 让 lpCommandLine 第一段作 exe）
        &cmd_line[0],      // lpCommandLine（argv 序列化）
        nullptr, nullptr,  // 安全属性
        FALSE,             // 句柄继承
        0,                 // 创建标志
        nullptr,           // 环境变量
        wdir.empty() ? nullptr : &wdir[0],  // 工作目录
        &si, &pi
    );

    if (!success) {
        DWORD err = GetLastError();
        return std::unexpected(core::Error::LaunchFailed(
            "CreateProcess failed: error " + std::to_string(err)));
    }

    CloseHandle(pi.hThread);

    ProcessHandle handle;
    handle.handle_ = pi.hProcess;
    handle.pid_ = pi.dwProcessId;
    return handle;
}

} // namespace pal

auto create_win_launcher() -> std::unique_ptr<pal::TerminalLauncher> {
    return std::make_unique<pal::WinTerminalLauncher>();
}

#endif // _WIN32
