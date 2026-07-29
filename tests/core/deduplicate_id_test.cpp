#include <doctest/doctest.h>
#include <core/deduplicate_id.h>

#include <vector>

namespace {

auto make_items(std::vector<std::string> ids) -> std::vector<core::LaunchItem> {
    std::vector<core::LaunchItem> items;
    for (auto& id : ids) {
        core::LaunchItem item;
        item.id = std::move(id);
        item.name = item.id;
        item.directory = "/tmp";
        item.command = "echo hello";
        items.push_back(std::move(item));
    }
    return items;
}

} // anonymous namespace

TEST_CASE("deduplicate_id: no collision returns original") {
    auto items = make_items({"VS Code", "Terminal"});
    auto result = core::deduplicate_id("Git Bash", items);
    CHECK(result == "Git Bash");
}

TEST_CASE("deduplicate_id: single collision appends _2") {
    auto items = make_items({"VS Code", "Terminal"});
    auto result = core::deduplicate_id("VS Code", items);
    CHECK(result == "VS Code_2");
}

TEST_CASE("deduplicate_id: multiple collisions increment suffix") {
    auto items = make_items({"test", "test_2", "test_3"});
    auto result = core::deduplicate_id("test", items);
    CHECK(result == "test_4");
}

TEST_CASE("deduplicate_id: empty list returns original") {
    std::vector<core::LaunchItem> items;
    auto result = core::deduplicate_id("anything", items);
    CHECK(result == "anything");
}

TEST_CASE("deduplicate_id: only _2 collides, _3 is free") {
    auto items = make_items({"app", "app_2"});
    auto result = core::deduplicate_id("app", items);
    CHECK(result == "app_3");
}
