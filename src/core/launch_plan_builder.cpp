#include "core/launch_plan_builder.h"

#include "core/is_dangerous.h"

namespace core {

auto LaunchPlanBuilder::build(const LaunchItem& item) -> std::expected<LaunchPlan, Error> {
    if (item.command.empty()) {
        return std::unexpected(Error::CommandEmpty());
    }

    LaunchPlan plan;
    plan.command = item.command;
    plan.working_dir = std::filesystem::path(item.directory);
    plan.terminal_override = item.terminal;
    plan.is_dangerous = core::is_dangerous(item.command);
    // executable 与 args 留空，由 platform::TerminalLauncher::populate 填充。
    return plan;
}

} // namespace core
