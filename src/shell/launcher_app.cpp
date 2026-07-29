#include "shell/launcher_app.h"
#include "core/deduplicate_id.h"
#include "core/is_dangerous.h"
#include "core/launch_plan_builder.h"
#include "core/validate_rules.h"
#include "platform/terminal_launcher.h"
#include <algorithm>

namespace shell {

LauncherApp::LauncherApp(std::unique_ptr<core::ConfigIO> config,
                         std::unique_ptr<core::Launcher> launcher,
                         std::unique_ptr<pal::ThemeDetector> theme_detector,
                         TerminalLauncherFactory terminal_factory)
    : config_(std::move(config))
    , launcher_(std::move(launcher))
    , theme_detector_(std::move(theme_detector))
    , terminal_factory_(std::move(terminal_factory)) {}

void LauncherApp::load_config() {
    if (!config_) return;
    if (auto items = config_->read_items()) {
        items_ = std::move(*items);
        selected_.load_from(items_);
    }
    if (auto settings = config_->read_settings()) {
        settings_ = std::move(*settings);
    }
}

void LauncherApp::save_config() {
    if (!config_) return;
    selected_.save_to(items_);
    config_->write_items(items_);
    config_->write_settings(settings_);
}

auto LauncherApp::find_item_by_id(const std::string& id)
    -> std::pair<size_t, core::LaunchItem*> {
    auto it = std::find_if(items_.begin(), items_.end(),
        [&](const auto& i) { return i.id == id; });
    if (it == items_.end()) return {items_.size(), nullptr};
    return {static_cast<size_t>(std::distance(items_.begin(), it)), &*it};
}

auto LauncherApp::add_item(core::LaunchItem item) -> std::string {
    item.selected = false;
    item.id = core::deduplicate_id(item.name, items_);
    items_.push_back(std::move(item));
    return items_.back().id;
}

auto LauncherApp::edit_item(size_t index, core::LaunchItem item) -> bool {
    if (index >= items_.size()) return false;
    item.id = items_[index].id;
    item.selected = items_[index].selected;
    items_[index] = std::move(item);
    return true;
}

auto LauncherApp::delete_item(size_t index) -> bool {
    if (index >= items_.size()) return false;
    items_.erase(items_.begin() + static_cast<ptrdiff_t>(index));
    return true;
}

auto LauncherApp::launch(const std::string& id) -> std::expected<pal::ProcessHandle, core::Error> {
    auto [idx, item] = find_item_by_id(id);
    if (!item) return std::unexpected(core::Error::InvalidItem("item not found: " + id));

    auto rules = core::validate_rules(*item);
    if (!rules) return std::unexpected(rules.error());

    if (!config_) return std::unexpected(core::Error::Internal("no config"));

    auto plan_result = core::LaunchPlanBuilder::build(*item);
    if (!plan_result) return std::unexpected(plan_result.error());

    auto terminal = terminal_factory_ ? terminal_factory_() : pal::TerminalLauncher::create();
    auto populated = terminal->populate(std::move(*plan_result));
    if (!populated) return std::unexpected(populated.error());

    auto result = terminal->launch(*populated);
    if (!result) return std::unexpected(result.error());

    // Update launch history
    settings_.launch_history.erase(
        std::remove(settings_.launch_history.begin(),
                    settings_.launch_history.end(), item->name),
        settings_.launch_history.end());
    settings_.launch_history.insert(settings_.launch_history.begin(), item->name);
    if (settings_.launch_history.size() > 10) {
        settings_.launch_history.resize(10);
    }

    save_config();
    return std::move(*result);
}

auto LauncherApp::dry_run(const std::string& id)
    -> std::expected<core::LaunchPlan, core::Error> {
    auto [idx, item] = find_item_by_id(id);
    if (!item) return std::unexpected(core::Error::InvalidItem("item not found: " + id));

    auto rules = core::validate_rules(*item);
    if (!rules) return std::unexpected(rules.error());

    auto plan_result = core::LaunchPlanBuilder::build(*item);
    if (!plan_result) return std::unexpected(plan_result.error());

    auto terminal = terminal_factory_ ? terminal_factory_() : pal::TerminalLauncher::create();
    return terminal->populate(std::move(*plan_result));
}

auto LauncherApp::validate_all() -> std::vector<CheckResult> {
    std::vector<CheckResult> results;
    for (const auto& item : items_) {
        CheckResult r;
        r.id = item.id;
        r.name = item.name;
        r.valid = true;

        auto rules = core::validate_rules(item);
        if (!rules) {
            r.valid = false;
            r.errors.push_back(rules.error().message());
        }

        if (r.valid) {
            auto plan = dry_run(item.id);
            if (plan) {
                r.plan = std::move(*plan);
            } else {
                r.valid = false;
                r.errors.push_back(plan.error().message());
            }
        }

        results.push_back(std::move(r));
    }
    return results;
}

} // namespace shell
