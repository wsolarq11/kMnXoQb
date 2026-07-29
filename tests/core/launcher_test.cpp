#include <doctest/doctest.h>

#include "core/launcher.h"
#include "core/selected_store.h"
#include "shell/real_filesystem.h"

// C++23 要求 designated initializer 顺序匹配成员声明顺序
// LaunchItem 顺序: name, directory, command, confirm, id, selected, terminal, tag, group
// 辅助函数避免直接使用 designated initializer 的严格顺序要求
namespace {
    auto make_item(const std::string& name, const std::string& id,
                   const std::string& dir = "C:\\", const std::string& cmd = "") -> core::LaunchItem {
        core::LaunchItem item;
        item.name = name;
        item.id = id;
        item.directory = dir;
        item.command = cmd;
        return item;
    }
}

TEST_CASE("Launcher: validate_item rejects invalid items") {
    shell::RealFilesystem fs;
    core::Launcher launcher("test_dir", fs);
    core::LaunchItem item;

    auto r1 = launcher.validate_item(item);
    CHECK_FALSE(r1.has_value());

    item.name = "test";
    auto r2 = launcher.validate_item(item);
    CHECK_FALSE(r2.has_value());

    item.directory = "C:\\";
    auto r3 = launcher.validate_item(item);
    CHECK_FALSE(r3.has_value());
}

TEST_CASE("Launcher: launch_selected finds by id (Bug fix verification)") {
    shell::RealFilesystem fs;
    core::Launcher launcher("test_dir", fs);
    core::SelectedStore selected;

    std::vector<core::LaunchItem> items;
    items.push_back(make_item("a", "id-a", "C:\\", "snow"));
    items.push_back(make_item("b", "id-b", "C:\\", "codex"));

    selected.set_selected("id-a", true);
    selected.set_selected("id-b", true);

    // Original HTA Bug: btns[id_string] as array index = undefined
    // Fixed: now uses std::find_if to find by .id
    auto result = launcher.launch_selected(items, selected, true);
    CHECK_EQ(result.success, 2);
    CHECK_EQ(result.failed, 0);
}

TEST_CASE("Launcher: launch_selected skips unknown ids gracefully") {
    shell::RealFilesystem fs;
    core::Launcher launcher("test_dir", fs);
    core::SelectedStore selected;

    std::vector<core::LaunchItem> items;
    items.push_back(make_item("a", "id-a", "C:\\", "snow"));

    selected.set_selected("nonexistent", true);

    auto result = launcher.launch_selected(items, selected, true);
    CHECK_EQ(result.success, 0);
    CHECK_EQ(result.failed, 1);
}
