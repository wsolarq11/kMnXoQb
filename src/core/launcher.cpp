#include "core/launcher.h"
#include "core/fs_iface.h"
#include "core/validate_rules.h"
#include <algorithm>

namespace core {

Launcher::Launcher(std::string config_dir, FilesystemIface& fs)
    : config_dir_(std::move(config_dir)), fs_(fs) {}

auto Launcher::validate_item(const LaunchItem& item) -> std::expected<void, Error> {
    auto rules = validate_rules(item);
    if (!rules) {
        return rules;
    }
    if (!fs_.directory_exists(item.directory)) {
        return std::unexpected(Error::DirectoryNotFound(item.directory));
    }
    return {};
}

auto Launcher::launch_selected(const std::vector<LaunchItem>& items, const SelectedStore& selected, bool skip_confirm) -> LaunchResult {
    (void)skip_confirm;
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

        auto launch_result = validate_item(*it);
        if (launch_result) {
            result.success++;
        } else {
            result.failed++;
        }
    }

    return result;
}

} // namespace core
