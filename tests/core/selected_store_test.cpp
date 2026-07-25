#include <doctest/doctest.h>

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
