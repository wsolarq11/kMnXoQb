#ifdef _WIN32

#include "platform/terminal_launcher.h"
#include <windows.h>
#include <winbase.h>
#include <consoleapi.h>
#include <processthreadsapi.h>

namespace pal {
namespace {

// CreatePseudoConsole: ordinal 233 (0xE9), ClosePseudoConsole: ordinal 141 (0x8D)
// Use ordinals because MinGW's import library may not include these exports.
struct ConPTYAPI {
    using CreateFn = HRESULT (WINAPI*)(COORD, HANDLE, HANDLE, DWORD, HPCON*);
    using CloseFn = VOID (WINAPI*)(HPCON);
    CreateFn create = nullptr;
    CloseFn close = nullptr;

    static auto load() -> ConPTYAPI {
        HMODULE k32 = GetModuleHandleW(L"kernel32.dll");
        if (!k32) return {};
        ConPTYAPI api;
        api.create = reinterpret_cast<CreateFn>(GetProcAddress(k32, "CreatePseudoConsole"));
        api.close  = reinterpret_cast<CloseFn>(GetProcAddress(k32, "ClosePseudoConsole"));
        return api;
    }
};

HRESULT prepare_startup_info(HPCON hpc, STARTUPINFOEXW* psi) {
    ZeroMemory(psi, sizeof(*psi));
    psi->StartupInfo.cb = sizeof(STARTUPINFOEXW);
    size_t bytes = 0;
    InitializeProcThreadAttributeList(nullptr, 1, 0, &bytes);
    psi->lpAttributeList = reinterpret_cast<PPROC_THREAD_ATTRIBUTE_LIST>(
        HeapAlloc(GetProcessHeap(), 0, bytes));
    if (!psi->lpAttributeList) return E_OUTOFMEMORY;
    if (!InitializeProcThreadAttributeList(psi->lpAttributeList, 1, 0, &bytes)) {
        HeapFree(GetProcessHeap(), 0, psi->lpAttributeList);
        return HRESULT_FROM_WIN32(GetLastError());
    }
    if (!UpdateProcThreadAttribute(psi->lpAttributeList, 0,
            PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
            hpc, sizeof(hpc), nullptr, nullptr)) {
        HeapFree(GetProcessHeap(), 0, psi->lpAttributeList);
        return HRESULT_FROM_WIN32(GetLastError());
    }
    return S_OK;
}

class ConPTYLauncher final : public TerminalLauncher {
public:
    auto populate(core::LaunchPlan plan) const
        -> std::expected<core::LaunchPlan, core::Error> override {
        // ConPTY runs headless — use cmd.exe directly, no relay
        plan.executable = "cmd.exe";
        plan.args = {"/c", plan.command};
        return plan;
    }

    auto launch(const core::LaunchPlan& plan)
        -> std::expected<ProcessHandle, core::Error> override {
        auto api = ConPTYAPI::load();
        if (!api.create || !api.close)
            return std::unexpected(core::Error::LaunchFailed("ConPTY not available — requires Win10 1809+"));

        HANDLE inRead = nullptr, inWrite = nullptr;
        HANDLE outRead = nullptr, outWrite = nullptr;
        if (!CreatePipe(&inRead, &inWrite, nullptr, 0))
            return std::unexpected(core::Error::LaunchFailed("CreatePipe failed"));
        if (!CreatePipe(&outRead, &outWrite, nullptr, 0)) {
            CloseHandle(inRead); CloseHandle(inWrite);
            return std::unexpected(core::Error::LaunchFailed("CreatePipe failed"));
        }

        COORD size = {80, 25};
        HPCON hPC = nullptr;
        HRESULT hr = api.create(size, inRead, outWrite, 0, &hPC);
        if (FAILED(hr) || hPC == nullptr) {
            CloseHandle(inRead); CloseHandle(inWrite);
            CloseHandle(outRead); CloseHandle(outWrite);
            auto msg = "CreatePseudoConsole failed hr=0x" + std::to_string(static_cast<unsigned>(hr));
            return std::unexpected(core::Error::LaunchFailed(msg));
        }

        STARTUPINFOEXW siEx;
        hr = prepare_startup_info(hPC, &siEx);
        if (FAILED(hr)) {
            api.close(hPC);
            CloseHandle(inRead); CloseHandle(inWrite);
            CloseHandle(outRead); CloseHandle(outWrite);
            return std::unexpected(core::Error::LaunchFailed("STARTUPINFOEX failed"));
        }

        std::string cmd_line = plan.executable.string() + " /c " + plan.command;
        int len = MultiByteToWideChar(CP_UTF8, 0, cmd_line.c_str(), -1, nullptr, 0);
        std::wstring wcmd(len, L'\0');
        MultiByteToWideChar(CP_UTF8, 0, cmd_line.c_str(), -1, &wcmd[0], len);

        PROCESS_INFORMATION pi{};
        BOOL ok = CreateProcessW(nullptr, &wcmd[0], nullptr, nullptr, FALSE,
            EXTENDED_STARTUPINFO_PRESENT, nullptr, nullptr,
            &siEx.StartupInfo, &pi);
        HeapFree(GetProcessHeap(), 0, siEx.lpAttributeList);

        CloseHandle(inRead);
        CloseHandle(outWrite);

        if (!ok) {
            api.close(hPC);
            CloseHandle(inWrite); CloseHandle(outRead);
            return std::unexpected(core::Error::LaunchFailed("CreateProcess failed"));
        }

        CloseHandle(pi.hThread);
        CloseHandle(pi.hProcess); // ConPTY owns the lifecycle

        ProcessHandle handle;
        handle.pid_ = pi.dwProcessId;
        handle.conpty_ = static_cast<void*>(hPC);
        handle.tracked_pid_ = pi.dwProcessId;
        return handle;
    }

    auto default_terminal_name() const -> std::string override {
        return "ConPTY";
    }
};

} // namespace

auto create_conpty_launcher() -> std::unique_ptr<TerminalLauncher> {
    return std::make_unique<ConPTYLauncher>();
}

} // namespace pal

#endif // _WIN32
