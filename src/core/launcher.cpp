#include "core/launcher.h"
#include "core/quote_arg.h"
#include "core/is_dangerous.h"
#include <filesystem>
#include <algorithm>

namespace core {

Launcher::Launcher(std::string config_dir)
    : config_dir_(std::move(config_dir)) {}

auto Launcher::validate_item(const LaunchItem& item) -> std::expected<void, Error> {
    if (item.name.empty()) {
        return std::unexpected(Error::InvalidItem("name is empty"));
    }
    if (item.directory.empty()) {
        return std::unexpected(Error::InvalidItem("directory is empty"));
    }
    if (!std::filesystem::exists(item.directory)) {
        return std::unexpected(Error::DirectoryNotFound(item.directory));
    }
    if (item.command.empty()) {
        return std::unexpected(Error::CommandEmpty());
    }
    return {};
}

auto Launcher::build_command(const LaunchItem& item) -> std::expected<std::string, Error> {
    // 构建 wt -d "<directory>" pwsh -NoExit -Command "<command>"
    std::string cmd = "wt -d " + quote_arg(item.directory)
        + " pwsh -NoExit -Command " + quote_arg(item.command);
    return cmd;
}

auto Launcher::launch_single(const LaunchItem& item, bool skip_confirm) -> std::expected<void, Error> {
    auto validation = validate_item(item);
    if (!validation) {
        return std::unexpected(validation.error());
    }

    // 实际进程启动由 App 层通过 pal::TerminalLauncher 统一调度
    // 此处仅进行校验并返回成功
    (void)skip_confirm;
    return {};
}

auto Launcher::launch_selected(const std::vector<LaunchItem>& items, const SelectedStore& selected, bool skip_confirm) -> LaunchResult {
    LaunchResult result;
    auto ids = selected.selected_ids();

    for (const auto& id : ids) {
        // 修复原 HTA Bug：用 id 查找而非数组下标
        // 原 HTA 第 797 行 btns[keys[k]] 用 id 字符串作数组下标恒为 undefined
        auto it = std::find_if(items.begin(), items.end(),
            [&id](const LaunchItem& item) { return item.id == id; });

        if (it == items.end()) {
            result.failed++;
            continue;
        }

        auto launch_result = launch_single(*it, skip_confirm);
        if (launch_result) {
            result.success++;
        } else {
            result.failed++;
        }
    }

    return result;
}

auto Launcher::is_dangerous(const std::string& command) -> bool {
    return core::is_dangerous(command);
}

auto Launcher::quote_arg(const std::string& arg) -> std::string {
    return core::quote_arg(arg);
}

} // namespace core
