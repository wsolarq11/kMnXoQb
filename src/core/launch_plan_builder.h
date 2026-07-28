#pragma once

#include "core/error.h"       // brings in <expected>
#include "core/launch_item.h"
#include "core/launch_plan.h"

namespace core {

// LaunchPlanBuilder 从 LaunchItem 构造 LaunchPlan（纯逻辑，零平台依赖）。
// 仅填充 command / working_dir / terminal_override / is_dangerous 四个字段，
// executable 与 args 留空，交由 platform::TerminalLauncher::populate 填充。
//
// 这是 core 层与平台无关的启动契约构造点：core 不关心用哪个终端、怎么 exec，
// 只负责把用户配置项归一化为 LaunchPlan 纯数据。
class LaunchPlanBuilder {
public:
    // 从 LaunchItem 构造 LaunchPlan。
    // 失败条件：command 为空（Error::CommandEmpty）。
    // 不校验 directory 存在性（运行时校验在 App 层与 platform 层）。
    static auto build(const LaunchItem& item) -> std::expected<LaunchPlan, Error>;
};

} // namespace core
