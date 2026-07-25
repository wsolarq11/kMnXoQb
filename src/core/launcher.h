#pragma once

#include <string>
#include <vector>
#include <expected>

#include "core/error.h"
#include "core/launch_item.h"
#include "core/selected_store.h"

namespace core {

struct LaunchResult {
    int success = 0;
    int failed = 0;
};

class Launcher {
public:
    explicit Launcher(std::string config_dir);

    // 校验启动项（目录存在、命令非空）
    auto validate_item(const LaunchItem& item) -> std::expected<void, Error>;

    // 构建启动命令字符串
    auto build_command(const LaunchItem& item) -> std::expected<std::string, Error>;

    // 单独启动
    auto launch_single(const LaunchItem& item, bool skip_confirm = false) -> std::expected<void, Error>;

    // 批量启动选中项（修复 launchSelected Bug：用 id 查找而非数组下标）
    auto launch_selected(const std::vector<LaunchItem>& items, const SelectedStore& selected, bool skip_confirm = false) -> LaunchResult;

    // 以下委托给独立模块
    auto is_dangerous(const std::string& command) -> bool;
    auto quote_arg(const std::string& arg) -> std::string;

    auto config_directory() const -> const std::string& { return config_dir_; }

private:
    std::string config_dir_;
};

} // namespace core
