#include <doctest/doctest.h>

#ifdef _WIN32
#define NOMINMAX
#include <windows.h>
#include <tlhelp32.h>
#endif

#include "core/config.h"
#include "core/launch_plan_builder.h"
#include "core/launcher.h"
#include "platform/theme_detector.h"
#include "shell/launcher_app.h"
#include "shell/real_filesystem.h"

#include <algorithm>
#include <filesystem>

namespace pal {
extern auto create_conpty_launcher() -> std::unique_ptr<TerminalLauncher>;
}

namespace {

struct TempDir {
    std::filesystem::path path;
    TempDir() {
        auto base = std::filesystem::temp_directory_path();
        for (int i = 0; i < 100; ++i) {
            auto candidate = base / ("launcher_app_test_" + std::to_string(i));
            if (!std::filesystem::exists(candidate)) {
                path = candidate;
                std::filesystem::create_directories(path);
                return;
            }
        }
    }
    ~TempDir() {
        std::error_code ec;
        std::filesystem::remove_all(path, ec);
    }
};

struct TestHarness {
    TempDir tmp;
    shell::RealFilesystem fs;
    std::unique_ptr<pal::ThemeDetector> theme;
    std::unique_ptr<core::ConfigIO> config;
    std::unique_ptr<core::Launcher> launcher;
    std::unique_ptr<shell::LauncherApp> app;

    TestHarness() {
        theme = pal::ThemeDetector::create();
        config = std::make_unique<core::ConfigIO>(tmp.path, fs);
        launcher = std::make_unique<core::Launcher>(tmp.path.string(), fs);
        app = std::make_unique<shell::LauncherApp>(
            std::move(config), std::move(launcher), std::move(theme));
    }

    auto make_item(std::string name, std::string dir, std::string cmd) -> core::LaunchItem {
        core::LaunchItem item;
        item.name = std::move(name);
        item.directory = std::move(dir);
        item.command = std::move(cmd);
        return item;
    }
};

} // anonymous namespace

// ═══ add_item ═══

TEST_CASE("add_item: unique id assigned") {
    TestHarness h;
    CHECK(h.app->add_item(h.make_item("test", h.tmp.path.string(), "echo")) == "test");
    CHECK(h.app->add_item(h.make_item("test", h.tmp.path.string(), "echo")) == "test_2");
    CHECK(h.app->add_item(h.make_item("test", h.tmp.path.string(), "echo")) == "test_3");
}

TEST_CASE("add_item: persists to config round-trip") {
    TestHarness h;
    h.app->add_item(h.make_item("app", h.tmp.path.string(), "echo"));
    h.app->save_config();

    shell::RealFilesystem fs2;
    auto c2 = std::make_unique<core::ConfigIO>(h.tmp.path, fs2);
    auto l2 = std::make_unique<core::Launcher>(h.tmp.path.string(), fs2);
    shell::LauncherApp app2(std::move(c2), std::move(l2), pal::ThemeDetector::create());
    app2.load_config();

    REQUIRE(app2.items().size() == 1);
    CHECK(app2.items()[0].name == "app");
}

// ═══ edit_item ═══

TEST_CASE("edit_item: updates fields, preserves id") {
    TestHarness h;
    h.app->add_item(h.make_item("old", h.tmp.path.string(), "cmd"));
    auto orig_id = h.app->items()[0].id;

    auto edited = h.make_item("new", h.tmp.path.string(), "new_cmd");
    CHECK(h.app->edit_item(0, std::move(edited)));
    CHECK(h.app->items()[0].name == "new");
    CHECK(h.app->items()[0].command == "new_cmd");
    CHECK(h.app->items()[0].id == orig_id);
}

TEST_CASE("edit_item: out of range") {
    TestHarness h;
    CHECK(!h.app->edit_item(0, h.make_item("x", h.tmp.path.string(), "x")));
}

// ═══ delete_item ═══

TEST_CASE("delete_item: removes and persists") {
    TestHarness h;
    h.app->add_item(h.make_item("a", h.tmp.path.string(), "cmd"));
    h.app->add_item(h.make_item("b", h.tmp.path.string(), "cmd"));

    CHECK(h.app->delete_item(0));
    h.app->save_config();

    shell::RealFilesystem fs2;
    auto c2 = std::make_unique<core::ConfigIO>(h.tmp.path, fs2);
    auto l2 = std::make_unique<core::Launcher>(h.tmp.path.string(), fs2);
    shell::LauncherApp app2(std::move(c2), std::move(l2), pal::ThemeDetector::create());
    app2.load_config();

    REQUIRE(app2.items().size() == 1);
    CHECK(app2.items()[0].name == "b");
}

TEST_CASE("delete_item: out of range") {
    TestHarness h;
    CHECK(!h.app->delete_item(0));
}

// ═══ dry_run ═══

TEST_CASE("dry_run: returns valid LaunchPlan") {
    TestHarness h;
    h.app->add_item(h.make_item("test", h.tmp.path.string(), "echo hello"));
    auto result = h.app->dry_run(h.app->items()[0].id);
    CHECK(result.has_value());
    CHECK(!result->executable.empty());
    CHECK(!result->args.empty());
}

TEST_CASE("dry_run: rejects unknown id") {
    TestHarness h;
    CHECK(!h.app->dry_run("nonexistent").has_value());
}

// ═══ launch ═══

TEST_CASE("launch: rejects empty command") {
    TestHarness h;
    h.app->add_item(h.make_item("bad", h.tmp.path.string(), ""));
    CHECK(!h.app->launch(h.app->items()[0].id).has_value());
}

TEST_CASE("launch: rejects unknown id") {
    TestHarness h;
    CHECK(!h.app->launch("nonexistent").has_value());
}

// ═══ ConPTY: automated lifecycle, no windows ═══

TEST_CASE("launch: full path via ConPTY — same LauncherApp::launch() as production") {
    TempDir tmp;
    shell::RealFilesystem fs;
    auto theme = pal::ThemeDetector::create();
    auto config = std::make_unique<core::ConfigIO>(tmp.path, fs);
    auto launcher = std::make_unique<core::Launcher>(tmp.path.string(), fs);

    // Inject ConPTY factory — same LauncherApp::launch() code path as production
    auto app = std::make_unique<shell::LauncherApp>(
        std::move(config), std::move(launcher), std::move(theme),
        []() { return pal::create_conpty_launcher(); });

    auto item = core::LaunchItem{};
    item.name = "test"; item.directory = tmp.path.string(); item.command = "echo hello";
    item.id = "test";
    auto id = app->add_item(std::move(item));

    auto result = app->launch(id);
    REQUIRE(result.has_value());
    CHECK_NE(result->pid_, 0u);
    CHECK_NE(result->conpty_, nullptr);
    CHECK(!app->settings().launch_history.empty());
    CHECK(app->settings().launch_history[0] == "test");

    result->kill();
    CHECK(result->conpty_ == nullptr);
}

// ═══ validate_all ═══

TEST_CASE("validate_all: all valid items passed") {
    TestHarness h;
    h.app->add_item(h.make_item("a", h.tmp.path.string(), "echo"));
    h.app->add_item(h.make_item("b", h.tmp.path.string(), "ls"));
    auto results = h.app->validate_all();
    REQUIRE(results.size() == 2);
    CHECK(results[0].valid);
    CHECK(results[1].valid);
}

TEST_CASE("validate_all: empty command marked invalid") {
    TestHarness h;
    h.app->add_item(h.make_item("bad", "Z:\\missing", ""));
    auto results = h.app->validate_all();
    REQUIRE(results.size() == 1);
    CHECK(!results[0].valid);
    CHECK(!results[0].errors.empty());
}

// ═══ settings round-trip ═══

TEST_CASE("settings: round-trip through save/load") {
    TestHarness h;
    h.app->settings().confirm_enabled = true;
    h.app->settings().theme = "dark";
    h.app->save_config();

    shell::RealFilesystem fs2;
    auto c2 = std::make_unique<core::ConfigIO>(h.tmp.path, fs2);
    auto l2 = std::make_unique<core::Launcher>(h.tmp.path.string(), fs2);
    shell::LauncherApp app2(std::move(c2), std::move(l2), pal::ThemeDetector::create());
    app2.load_config();

    CHECK(app2.settings().confirm_enabled);
    CHECK(app2.settings().theme == "dark");
}

// ═══ empty load ═══

TEST_CASE("load_config: handles empty config directory") {
    TempDir tmp;
    shell::RealFilesystem fs;
    auto config = std::make_unique<core::ConfigIO>(tmp.path, fs);
    auto launcher = std::make_unique<core::Launcher>(tmp.path.string(), fs);
    shell::LauncherApp app(std::move(config), std::move(launcher), pal::ThemeDetector::create());

    app.load_config();
    CHECK(app.items().empty());
    CHECK(app.settings().theme == "system");
}
