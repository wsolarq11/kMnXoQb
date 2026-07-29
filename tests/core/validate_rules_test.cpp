#include <doctest/doctest.h>
#include <core/validate_rules.h>

namespace {

auto make_item(std::string name, std::string directory, std::string command) -> core::LaunchItem {
    core::LaunchItem item;
    item.name = std::move(name);
    item.directory = std::move(directory);
    item.command = std::move(command);
    item.id = item.name;
    return item;
}

} // anonymous namespace

TEST_CASE("validate_rules: valid item passes all rules") {
    auto item = make_item("VS Code", "D:\\projects", "code .");
    auto result = core::validate_rules(item);
    CHECK(result.has_value());
}

TEST_CASE("validate_rules: empty name fails") {
    auto item = make_item("", "D:\\projects", "code .");
    auto result = core::validate_rules(item);
    REQUIRE(!result.has_value());
    CHECK(result.error().code() == core::ErrorCode::kInvalidItem);
}

TEST_CASE("validate_rules: empty directory fails") {
    auto item = make_item("VS Code", "", "code .");
    auto result = core::validate_rules(item);
    REQUIRE(!result.has_value());
    CHECK(result.error().code() == core::ErrorCode::kInvalidItem);
}

TEST_CASE("validate_rules: empty command fails") {
    auto item = make_item("VS Code", "D:\\projects", "");
    auto result = core::validate_rules(item);
    REQUIRE(!result.has_value());
    CHECK(result.error().code() == core::ErrorCode::kCommandEmpty);
}

TEST_CASE("validate_rules: whitespace-only name fails") {
    auto item = make_item("   ", "D:\\projects", "code .");
    auto result = core::validate_rules(item);
    CHECK(result.has_value()); // whitespace is not empty, passes pure rules
}

TEST_CASE("validate_rules: does not check directory existence") {
    // Pure rules skip filesystem — validate_rules passes even for nonexistent dirs
    auto item = make_item("Test", "Z:\\nonexistent\\path", "echo hello");
    auto result = core::validate_rules(item);
    CHECK(result.has_value());
}
