#pragma once

#include <memory>
#include <string>

#ifdef _WIN32
// ProcessHandle 析构需调用 CloseHandle，仅 Windows 分支引入 windows.h
// 此头是 platform 层头（不在 core），引入 windows.h 可接受
#ifndef NOMINMAX
#define NOMINMAX
#endif
#include <windows.h>
#endif

#include "core/error.h"       // brings in <expected>
#include "core/launch_plan.h"

namespace pal {

// 进程句柄（RAII）。
// 析构自动释放 Windows HANDLE（CloseHandle）；macOS/Linux 的 pid 由 OS 回收，析构无操作。
// 移动语义；禁用拷贝（避免双重释放）。
struct ProcessHandle {
    ProcessHandle() = default;

    ~ProcessHandle() {
#ifdef _WIN32
        if (handle_ != nullptr) {
            CloseHandle(handle_);
        }
#endif
    }

    // 移动构造：转移所有权，源置空
    ProcessHandle(ProcessHandle&& other) noexcept
#ifdef _WIN32
        : handle_(other.handle_), pid_(other.pid_)
#else
        : pid_(other.pid_)
#endif
    {
#ifdef _WIN32
        other.handle_ = nullptr;
#endif
        other.pid_ = -1;
    }

    // 移动赋值：先释放自身资源，再转移
    ProcessHandle& operator=(ProcessHandle&& other) noexcept {
        if (this != &other) {
#ifdef _WIN32
            if (handle_ != nullptr) {
                CloseHandle(handle_);
            }
            handle_ = other.handle_;
            other.handle_ = nullptr;
#endif
            pid_ = other.pid_;
            other.pid_ = -1;
        }
        return *this;
    }

    // 禁用拷贝
    ProcessHandle(const ProcessHandle&) = delete;
    ProcessHandle& operator=(const ProcessHandle&) = delete;

    // 访问器（供平台实现设置）
#ifdef _WIN32
    void* handle_ = nullptr;
    unsigned long pid_ = 0;

    // 向后兼容的访问器
    void* handle() const { return handle_; }
    unsigned long pid() const { return pid_; }
#else
    int pid_ = -1;
    int pid() const { return pid_; }
#endif

private:
#ifdef _WIN32
    friend class WinTerminalLauncher;
#else
    friend class MacTerminalLauncher;
    friend class LinuxTerminalLauncher;
#endif
};

// 终端启动器抽象接口
//
// 契约变更（子任务 A）：旧接口 launch(directory, command) 接收命令字符串，
// 经 shell 执行存在注入面。新接口拆分为 populate + launch 两步：
//   1. populate(plan): 平台层根据 terminal 类型填充 plan.executable 与 plan.args
//   2. launch(plan): 用 posix_spawn / CreateProcessW 直接 exec，全程无 shell
class TerminalLauncher {
public:
    virtual ~TerminalLauncher() = default;

    // 填充 LaunchPlan 的 executable 与 args（平台专属 argv 构造）。
    // 纯逻辑，不启动进程，不执行 I/O（除终端探测）。
    // 失败条件：终端未找到（Error::TerminalNotFound）。
    virtual auto populate(core::LaunchPlan plan) const
        -> std::expected<core::LaunchPlan, core::Error> = 0;

    // 用 posix_spawn / CreateProcessW 直接 exec plan。
    // 全程无 shell，不解析 shell 元字符，从结构上消除命令注入。
    virtual auto launch(const core::LaunchPlan& plan)
        -> std::expected<ProcessHandle, core::Error> = 0;

    // 获取默认终端名称
    virtual auto default_terminal_name() const -> std::string = 0;

    // 创建平台默认实现
    static auto create() -> std::unique_ptr<TerminalLauncher>;
};

} // namespace pal
