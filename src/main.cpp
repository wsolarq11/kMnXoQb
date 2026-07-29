#include "app.h"
#include "cli.h"
#include "core/config.h"
#include "core/launcher.h"
#include "main_window.h"
#include "platform/dialog_provider.h"
#include "platform/path_resolver.h"
#include "platform/single_instance.h"
#include "platform/theme_detector.h"
#include "shell/launcher_app.h"
#include "shell/logger.h"
#include "shell/real_filesystem.h"

#include <cstdio>
#include <cstring>
#include <string>

// ── CLI dispatch ──

static bool is_cli(int argc, char* argv[]) {
    return argc >= 3 && std::strcmp(argv[1], "--check") == 0;
}

static bool is_launch(int argc, char* argv[]) {
    return argc >= 2 && std::strcmp(argv[1], "launch") == 0;
}

static bool print_usage(const char* prog) {
    std::fprintf(stderr,
        "Usage:\n"
        "  %s                     Launch GUI\n"
        "  %s --check <config>    Validate all items (JSON to stdout)\n"
        "  %s launch <config> <id> Launch item by id\n",
        prog, prog, prog);
    return false;
}

// ── GUI entry ──

static int run_gui() {
    auto window = MainWindow::create();

    auto fs = std::make_unique<shell::RealFilesystem>();
    auto resolver = pal::PathResolver::create();
    auto instance = pal::SingleInstance::create();
    auto theme_detector = pal::ThemeDetector::create();
    auto dialog_provider = pal::DialogProvider::create();

    if (!instance->is_only_instance()) return 0;

    auto config_dir = resolver->config_directory();
    if (!config_dir) return 1;

    shell::Logger::init(config_dir->string());

    auto config = std::make_unique<core::ConfigIO>(*config_dir, *fs);
    auto launcher = std::make_unique<core::Launcher>(config_dir->string(), *fs);

    auto app = std::make_shared<shell::LauncherApp>(
        std::move(config), std::move(launcher), std::move(theme_detector));

    auto gui = std::make_shared<App>(window, *app, *dialog_provider);
    return gui->run();
}

// ── main ──

int main(int argc, char* argv[]) {
    if (is_cli(argc, argv)) return main_check(argv[2]);
    if (is_launch(argc, argv)) {
        if (argc < 4) { print_usage(argv[0]); return 1; }
        return main_launch(argv[2], argv[3]);
    }
    return run_gui();
}
