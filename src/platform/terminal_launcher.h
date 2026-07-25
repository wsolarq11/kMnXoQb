#pragma once

#include <memory>
#include <string>
#include <expected>

#include "core/error.h"

namespace pal {

struct ProcessHandle {
#ifdef _WIN32
    void* handle = nullptr;
    unsigned long pid = 0;
#else
    int pid = -1;
#endif
};

// 终端启动器抽象接口
class TerminalLauncher {
public:
    virtual ~TerminalLauncher() = default;

    // 在指定目录启动终端并执行命令
    virtual auto launch(const std::string& directory, const std::string& command)
        -> std::expected<ProcessHandle, core::Error> = 0;

    // 获取默认终端名称
    virtual auto default_terminal_name() const -> std::string = 0;

    // 创建平台默认实现
    static auto create() -> std::unique_ptr<TerminalLauncher>;
};

} // namespace pal
