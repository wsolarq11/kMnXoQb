#include <doctest/doctest.h>
#include <core/filter_items.h>

#include <vector>

namespace {

auto make_items() -> std::vector<core::LaunchItem> {
    std::vector<core::LaunchItem> items;

    core::LaunchItem a;
    a.name = "VS Code";
    a.directory = "D:\\projects";
    a.command = "code .";
    a.id = "vs-code";
    items.push_back(a);

    core::LaunchItem b;
    b.name = "Terminal";
    b.directory = "/home/user";
    b.command = "bash";
    b.id = "terminal";
    items.push_back(b);

    core::LaunchItem c;
    c.name = "Notes";
    c.directory = "D:\\docs";
    c.command = "notepad.exe";
    c.id = "notes";
    items.push_back(c);

    return items;
}

} // anonymous namespace

TEST_CASE("filter_items: empty query returns all indices") {
    auto items = make_items();
    auto result = core::filter_items(items, "");
    REQUIRE(result.size() == 3);
    CHECK(result[0] == 0);
    CHECK(result[1] == 1);
    CHECK(result[2] == 2);
}

TEST_CASE("filter_items: match by name") {
    auto items = make_items();
    auto result = core::filter_items(items, "code");
    REQUIRE(result.size() == 1);
    CHECK(result[0] == 0); // VS Code
}

TEST_CASE("filter_items: match by directory") {
    auto items = make_items();
    auto result = core::filter_items(items, "docs");
    REQUIRE(result.size() == 1);
    CHECK(result[0] == 2); // Notes in D:\docs
}

TEST_CASE("filter_items: match by command") {
    auto items = make_items();
    auto result = core::filter_items(items, "bash");
    REQUIRE(result.size() == 1);
    CHECK(result[0] == 1); // Terminal with bash command
}

TEST_CASE("filter_items: case insensitive") {
    auto items = make_items();
    auto result = core::filter_items(items, "VS CODE");
    REQUIRE(result.size() == 1);
    CHECK(result[0] == 0);
}

TEST_CASE("filter_items: no match returns empty") {
    auto items = make_items();
    auto result = core::filter_items(items, "xyz_nonexistent");
    CHECK(result.empty());
}

TEST_CASE("filter_items: partial match works") {
    auto items = make_items();
    auto result = core::filter_items(items, "note");
    REQUIRE(result.size() == 1);
    CHECK(result[0] == 2); // Notes + notepad.exe
}
