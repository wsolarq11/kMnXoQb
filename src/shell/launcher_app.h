#pragma once

#include "core/config.h"
#include "core/launch_plan.h"
#include "core/launcher.h"
#include "core/selected_store.h"
#include "platform/terminal_launcher.h"
#include "platform/theme_detector.h"

#include <expected>
#include <functional>
#include <memory>
#include <optional>
#include <string>
#include <vector>

namespace shell {

struct CheckResult {
    std::string id;
    std::string name;
    bool valid = false;
    std::vector<std::string> errors;
    std::optional<core::LaunchPlan> plan;
};

using TerminalLauncherFactory = std::function<std::unique_ptr<pal::TerminalLauncher>()>;

class LauncherApp {
public:
    LauncherApp(std::unique_ptr<core::ConfigIO> config,
                std::unique_ptr<core::Launcher> launcher,
                std::unique_ptr<pal::ThemeDetector> theme_detector,
                TerminalLauncherFactory terminal_factory = nullptr);

    auto items() -> std::vector<core::LaunchItem>& { return items_; }
    auto items() const -> const std::vector<core::LaunchItem>& { return items_; }
    auto settings() -> core::AppSettings& { return settings_; }
    auto settings() const -> const core::AppSettings& { return settings_; }
    auto selected() -> core::SelectedStore& { return selected_; }
    auto selected() const -> const core::SelectedStore& { return selected_; }
    auto theme_detector() -> pal::ThemeDetector& { return *theme_detector_; }

    void load_config();
    void save_config();
    auto add_item(core::LaunchItem item) -> std::string;
    auto edit_item(size_t index, core::LaunchItem item) -> bool;
    auto delete_item(size_t index) -> bool;
    auto launch(const std::string& id) -> std::expected<pal::ProcessHandle, core::Error>;
    auto dry_run(const std::string& id) -> std::expected<core::LaunchPlan, core::Error>;
    auto validate_all() -> std::vector<CheckResult>;

private:
    auto find_item_by_id(const std::string& id)
        -> std::pair<size_t, core::LaunchItem*>;

    std::unique_ptr<core::ConfigIO> config_;
    std::unique_ptr<core::Launcher> launcher_;
    std::unique_ptr<pal::ThemeDetector> theme_detector_;
    TerminalLauncherFactory terminal_factory_;
    core::SelectedStore selected_;
    std::vector<core::LaunchItem> items_;
    core::AppSettings settings_;
};

} // namespace shell
