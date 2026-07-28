#pragma once

#include <filesystem>
#include <optional>
#include <string>
#include <vector>

namespace core {

// LaunchPlan 是 core 与 platform 之间的唯一启动契约。
// 它是纯数据结构，不携带任何平台语义，由 LaunchPlanBuilder 从 LaunchItem 构造，
// 再由 platform::TerminalLauncher::populate 填充 executable 与 args。
//
// 设计动机：根除"字符串拼接 + shell 执行"反模式。
// 旧路径把 directory/command 通过 quote_arg 拼成命令字符串送入 shell 解释器，
// 存在命令注入面。LaunchPlan 改为 argv 数组 + 直接 exec，从结构上消除注入。
struct LaunchPlan {
    // 原始命令文本（来自 LaunchItem.command），供 platform 层 populate 构造 argv 时使用。
    // 不直接送入 shell，仅作为构造 argv 的输入。
    std::string command;

    // 工作目录（来自 LaunchItem.directory），posix_spawn_file_actions_addchdir_np
    // 或 CreateProcessW 的 lpCurrentDirectory 使用。
    std::filesystem::path working_dir;

    // 可选的自定义终端覆盖（来自 LaunchItem.terminal）。
    // 非空时 platform 层 populate 会按空白拆分为 executable + args 前缀。
    std::optional<std::string> terminal_override;

    // 可执行文件路径，由 platform 层 populate 填充。
    // 例：Windows 的 "wt.exe"/"pwsh.exe"/"cmd.exe"，macOS 的 "/usr/bin/osascript"，
    // Linux 的 "gnome-terminal" 等。
    std::filesystem::path executable;

    // argv 参数数组（不含 executable 本身），由 platform 层 populate 填充。
    // 直接传给 posix_spawn/CreateProcessW，不经 shell 解释。
    std::vector<std::string> args;

    // 预计算的危险标志（来自 is_dangerous(command)），供 UI 高亮与确认流程使用。
    bool is_dangerous = false;
};

} // namespace core
