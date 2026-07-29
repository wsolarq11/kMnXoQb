#include "shell/launcher_app.h"
#include "core/config.h"
#include "core/launcher.h"
#include "platform/theme_detector.h"
#include "shell/real_filesystem.h"

#include <filesystem>
#include <iostream>
#include <string>

namespace {

auto escape_json(const std::string& s) -> std::string {
    std::string out;
    out.reserve(s.size() + 8);
    for (char c : s) {
        switch (c) {
            case '\\': out += "\\\\"; break;
            case '"':  out += "\\\""; break;
            case '\n': out += "\\n"; break;
            case '\r': out += "\\r"; break;
            case '\t': out += "\\t"; break;
            default:   out += c; break;
        }
    }
    return out;
}

void print_plan(const core::LaunchPlan& plan) {
    std::cout << "      \"executable\": \"" << escape_json(plan.executable.string()) << "\",\n";
    std::cout << "      \"working_dir\": \"" << escape_json(plan.working_dir.string()) << "\",\n";
    std::cout << "      \"is_dangerous\": " << (plan.is_dangerous ? "true" : "false") << ",\n";
    std::cout << "      \"args\": [";
    for (size_t i = 0; i < plan.args.size(); ++i) {
        if (i > 0) std::cout << ", ";
        std::cout << '"' << escape_json(plan.args[i]) << '"';
    }
    std::cout << "]\n";
}

} // namespace

int main_check(const std::string& config_path) {
    auto fs = std::make_unique<shell::RealFilesystem>();
    auto theme = pal::ThemeDetector::create();

    std::filesystem::path path(config_path);
    auto config = std::make_unique<core::ConfigIO>(path.parent_path(), *fs);
    auto launcher = std::make_unique<core::Launcher>(path.parent_path().string(), *fs);

    shell::LauncherApp app(std::move(config), std::move(launcher), std::move(theme));
    app.load_config();

    auto results = app.validate_all();

    std::cout << "{\n  \"items\": [\n";
    for (size_t i = 0; i < results.size(); ++i) {
        const auto& r = results[i];
        std::cout << "    {\n";
        std::cout << "      \"id\": \"" << escape_json(r.id) << "\",\n";
        std::cout << "      \"name\": \"" << escape_json(r.name) << "\",\n";
        std::cout << "      \"valid\": " << (r.valid ? "true" : "false");
        if (!r.errors.empty()) {
            std::cout << ",\n      \"errors\": [";
            for (size_t j = 0; j < r.errors.size(); ++j) {
                if (j > 0) std::cout << ", ";
                std::cout << '"' << escape_json(r.errors[j]) << '"';
            }
            std::cout << "]";
        }
        if (r.plan.has_value()) {
            std::cout << ",\n      \"plan\": {\n";
            print_plan(*r.plan);
            std::cout << "      }";
        }
        std::cout << "\n    }";
        if (i + 1 < results.size()) std::cout << ",";
        std::cout << "\n";
    }
    std::cout << "  ]\n}\n";

    for (const auto& r : results) {
        if (!r.valid) return 1;
    }
    return 0;
}
