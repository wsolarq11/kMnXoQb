#include "platform/terminal_launcher.h"
#include "core/quote_arg.h"

#ifdef _WIN32

#include <windows.h>
#include <string>

namespace pal {

// Windows 终端启动器
// 优先级：wt（Windows Terminal）> pwsh（PowerShell conhost）> cmd
class WinTerminalLauncher : public TerminalLauncher {
public:
    enum class TerminalType { kWt, kPwsh, kCmd };

    WinTerminalLauncher();

    auto launch(const std::string& directory, const std::string& command)
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
    auto build_wt_command(const std::string& dir, const std::string& cmd) -> std::string;
    auto build_pwsh_command(const std::string& dir, const std::string& cmd) -> std::string;

    TerminalType type_;
};

WinTerminalLauncher::WinTerminalLauncher() {
    // 检测 wt 是否可用
    // 优先级：注册表 HKCU\Software\Microsoft\Windows\CurrentVersion\App Paths\wt.exe
    // 或检查 PATH 中是否有 wt.exe
    HKEY key;
    LONG result = RegOpenKeyExW(HKEY_CURRENT_USER,
        L"Software\\Microsoft\\Windows\\CurrentVersion\\App Paths\\wt.exe",
        0, KEY_READ, &key);
    if (result == ERROR_SUCCESS) {
        RegCloseKey(key);
        type_ = TerminalType::kWt;
    } else {
        // 检测 pwsh 是否可用
        if (GetModuleHandleW(L"pwsh.exe") ||
            SearchPathW(nullptr, L"pwsh", L".exe", 0, nullptr, nullptr)) {
            type_ = TerminalType::kPwsh;
        } else {
            type_ = TerminalType::kCmd;
        }
    }
}

auto WinTerminalLauncher::build_wt_command(const std::string& dir, const std::string& cmd) -> std::string {
    // wt -d "<directory>" pwsh -NoExit -Command "<command>"
    return "wt -d " + core::quote_arg(dir)
        + " pwsh -NoExit -Command " + core::quote_arg(cmd);
}

auto WinTerminalLauncher::build_pwsh_command(const std::string& dir, const std::string& cmd) -> std::string {
    // pwsh -NoExit -Command "cd <dir>; <command>"
    return "pwsh -NoExit -Command " + core::quote_arg("cd " + dir + "; " + cmd);
}

auto WinTerminalLauncher::launch(const std::string& directory, const std::string& command)
    -> std::expected<ProcessHandle, core::Error> {

    std::string cmd_line;
    switch (type_) {
        case TerminalType::kWt:
            cmd_line = build_wt_command(directory, command);
            break;
        case TerminalType::kPwsh:
            cmd_line = build_pwsh_command(directory, command);
            break;
        case TerminalType::kCmd:
            cmd_line = command;
            break;
    }

    // 用 CreateProcess 启动
    STARTUPINFOW si = { sizeof(si) };
    PROCESS_INFORMATION pi;

    // 转换到 UTF-16
    int len = MultiByteToWideChar(CP_UTF8, 0, cmd_line.c_str(), -1, nullptr, 0);
    std::wstring wcmd(len, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, cmd_line.c_str(), -1, &wcmd[0], len);

    // 转换目录路径到 UTF-16
    len = MultiByteToWideChar(CP_UTF8, 0, directory.c_str(), -1, nullptr, 0);
    std::wstring wdir(len, L'\0');
    MultiByteToWideChar(CP_UTF8, 0, directory.c_str(), -1, &wdir[0], len);

    BOOL success = CreateProcessW(
        nullptr,           // 可执行文件
        &wcmd[0],          // 命令行
        nullptr,           // 进程安全属性
        nullptr,           // 线程安全属性
        FALSE,             // 句柄继承
        0,                 // 创建标志
        nullptr,           // 环境变量
        &wdir[0],          // 工作目录
        &si,               // 启动信息
        &pi                // 进程信息
    );

    if (!success) {
        DWORD err = GetLastError();
        return std::unexpected(core::Error::LaunchFailed(
            "CreateProcess failed: error " + std::to_string(err)));
    }

    CloseHandle(pi.hThread);

    ProcessHandle handle;
    handle.handle = pi.hProcess;
    handle.pid = pi.dwProcessId;
    return handle;
}

// 工厂方法
auto TerminalLauncher::create() -> std::unique_ptr<TerminalLauncher> {
    return std::make_unique<WinTerminalLauncher>();
}

} // namespace pal

#endif // _WIN32
