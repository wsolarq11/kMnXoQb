#include <doctest/doctest.h>

#include <algorithm>
#include "core/selected_store.h"

namespace {
    auto make_item(const std::string& name, const std::string& id, bool selected = false) -> core::LaunchItem {
        core::LaunchItem item;
        item.name = name;
        item.id = id;
        item.selected = selected;
        return item;
    }
}

TEST_CASE("SelectedStore: set and check selection") {
    core::SelectedStore store;
    store.set_selected("id-1", true);
    CHECK(store.is_selected("id-1"));
    CHECK_FALSE(store.is_selected("id-2"));
    CHECK_EQ(store.count(), 1);
}

TEST_CASE("SelectedStore: deselect removes entry") {
    core::SelectedStore store;
    store.set_selected("id-1", true);
    store.set_selected("id-1", false);
    CHECK_FALSE(store.is_selected("id-1"));
    CHECK_EQ(store.count(), 0);
}

TEST_CASE("SelectedStore: select_all and deselect_all") {
    core::SelectedStore store;
    std::vector<core::LaunchItem> items;
    items.push_back(make_item("a", "a"));
    items.push_back(make_item("b", "b"));
    store.select_all(items);
    CHECK_EQ(store.count(), 2);
    store.deselect_all();
    CHECK_EQ(store.count(), 0);
}

TEST_CASE("SelectedStore: load_from and save_to roundtrip") {
    core::SelectedStore store;
    std::vector<core::LaunchItem> items;
    items.push_back(make_item("a", "a", true));
    items.push_back(make_item("b", "b", false));
    store.load_from(items);
    CHECK(store.is_selected("a"));
    CHECK_FALSE(store.is_selected("b"));

    std::vector<core::LaunchItem> output;
    output.push_back(make_item("a", "a"));
    output.push_back(make_item("b", "b"));
    store.save_to(output);
    CHECK(output[0].selected);
    CHECK_FALSE(output[1].selected);
}

TEST_CASE("SelectedStore: selected_ids returns all selected ids") {
    core::SelectedStore store;
    store.set_selected("x", true);
    store.set_selected("y", true);
    auto ids = store.selected_ids();
    CHECK_EQ(ids.size(), 2);
}

// --- 新增边界用例 ---

TEST_CASE("SelectedStore: duplicate id set_selected only counts once") {
    core::SelectedStore store;
    store.set_selected("dup-id", true);
    store.set_selected("dup-id", true);
    store.set_selected("dup-id", true);
    CHECK_EQ(store.count(), 1);
    CHECK(store.is_selected("dup-id"));
}

TEST_CASE("SelectedStore: set_selected with empty string id") {
    core::SelectedStore store;
    store.set_selected("", true);
    CHECK(store.is_selected(""));
    CHECK_EQ(store.count(), 1);
    store.set_selected("", false);
    CHECK_FALSE(store.is_selected(""));
    CHECK_EQ(store.count(), 0);
}

TEST_CASE("SelectedStore: set_selected deselect reselect cycle") {
    core::SelectedStore store;
    store.set_selected("id", true);
    CHECK(store.is_selected("id"));
    store.set_selected("id", false);
    CHECK_FALSE(store.is_selected("id"));
    store.set_selected("id", true);
    CHECK(store.is_selected("id"));
    CHECK_EQ(store.count(), 1);
}

TEST_CASE("SelectedStore: save_to with empty store clears all selections") {
    core::SelectedStore store;
    std::vector<core::LaunchItem> items;
    items.push_back(make_item("a", "a"));
    items.push_back(make_item("b", "b"));

    store.save_to(items);
    CHECK_FALSE(items[0].selected);
    CHECK_FALSE(items[1].selected);
}

TEST_CASE("SelectedStore: large number of items") {
    core::SelectedStore store;
    std::vector<core::LaunchItem> items;
    for (int i = 0; i < 1000; ++i) {
        auto id = "id-" + std::to_string(i);
        items.push_back(make_item("item-" + std::to_string(i), id, i % 2 == 0));
    }
    store.load_from(items);
    CHECK_EQ(store.count(), 500);
    CHECK(store.is_selected("id-0"));
    CHECK_FALSE(store.is_selected("id-1"));
    CHECK(store.is_selected("id-998"));
    CHECK_FALSE(store.is_selected("id-999"));
}

TEST_CASE("SelectedStore: select_all then deselect_all then select_all") {
    core::SelectedStore store;
    std::vector<core::LaunchItem> items;
    items.push_back(make_item("a", "a"));
    items.push_back(make_item("b", "b"));

    store.select_all(items);
    CHECK_EQ(store.count(), 2);

    store.deselect_all();
    CHECK_EQ(store.count(), 0);

    store.select_all(items);
    CHECK_EQ(store.count(), 2);
}

TEST_CASE("SelectedStore: load_from with no selected items") {
    core::SelectedStore store;
    std::vector<core::LaunchItem> items;
    items.push_back(make_item("a", "a", false));
    items.push_back(make_item("b", "b", false));

    store.load_from(items);
    CHECK_EQ(store.count(), 0);
    CHECK_FALSE(store.is_selected("a"));
    CHECK_FALSE(store.is_selected("b"));
}

TEST_CASE("SelectedStore: selected_ids order stability") {
    // selected_ids 返回顺序与 unordered_map 迭代顺序一致，
    // 但应保证所有选中 ID 都被包含
    core::SelectedStore store;
    store.set_selected("first", true);
    store.set_selected("second", true);
    store.set_selected("third", true);

    auto ids = store.selected_ids();
    CHECK_EQ(ids.size(), 3);

    // 验证所有 ID 都在结果中
    auto contains = [&](const std::string& id) {
        return std::find(ids.begin(), ids.end(), id) != ids.end();
    };
    CHECK(contains("first"));
    CHECK(contains("second"));
    CHECK(contains("third"));
}

TEST_CASE("SelectedStore: load_from preserves existing selections with new items") {
    core::SelectedStore store;
    store.set_selected("existing", true);

    std::vector<core::LaunchItem> new_items;
    new_items.push_back(make_item("new-a", "new-a", true));
    new_items.push_back(make_item("new-b", "new-b", false));

    // load_from 应清空并重新加载
    store.load_from(new_items);
    CHECK_EQ(store.count(), 1);
    CHECK_FALSE(store.is_selected("existing"));
    CHECK(store.is_selected("new-a"));
}
