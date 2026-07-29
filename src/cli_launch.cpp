#include "shell/launcher_app.h"
#include "core/config.h"
#include "core/launcher.h"
#include "platform/theme_detector.h"
#include "shell/real_filesystem.h"

#include <filesystem>
#include <iostream>

int main_launch(const std::string& config_path, const std::string& id) {
    auto fs = std::make_unique<shell::RealFilesystem>();
    auto theme = pal::ThemeDetector::create();

    std::filesystem::path path(config_path);
    auto config = std::make_unique<core::ConfigIO>(path.parent_path(), *fs);
    auto launcher = std::make_unique<core::Launcher>(path.parent_path().string(), *fs);

    shell::LauncherApp app(std::move(config), std::move(launcher), std::move(theme));
    app.load_config();

    auto result = app.launch(id);
    if (!result) {
        std::cerr << "Launch failed: " << result.error().message() << '\n';
        return 1;
    }

    std::cout << "Launched: " << id << '\n';
    return 0;
}
