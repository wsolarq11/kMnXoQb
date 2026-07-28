#include "core/launch_plan_builder.h"

#include <doctest/doctest.h>

TEST_CASE("LaunchPlanBuilder: empty command returns CommandEmpty") {
    core::LaunchItem item;
    item.name = "test";
    item.directory = "/tmp";
    item.command = "";
    item.id = "test";

    auto result = core::LaunchPlanBuilder::build(item);
    CHECK_FALSE(result.has_value());
    CHECK(result.error().code() == core::ErrorCode::kCommandEmpty);
}

TEST_CASE("LaunchPlanBuilder: fills working_dir from directory") {
    core::LaunchItem item;
    item.name = "test";
    item.directory = "D:/projects/demo";
    item.command = "snow";
    item.id = "test";

    auto result = core::LaunchPlanBuilder::build(item);
    REQUIRE(result.has_value());
    CHECK(result->working_dir.string() == "D:/projects/demo");
}

TEST_CASE("LaunchPlanBuilder: forwards terminal_override") {
    core::LaunchItem item;
    item.name = "test";
    item.directory = "/tmp";
    item.command = "snow";
    item.id = "test";
    item.terminal = "wt.exe";

    auto result = core::LaunchPlanBuilder::build(item);
    REQUIRE(result.has_value());
    REQUIRE(result->terminal_override.has_value());
    CHECK(*result->terminal_override == "wt.exe");
}

TEST_CASE("LaunchPlanBuilder: terminal_override empty when not set") {
    core::LaunchItem item;
    item.name = "test";
    item.directory = "/tmp";
    item.command = "snow";
    item.id = "test";

    auto result = core::LaunchPlanBuilder::build(item);
    REQUIRE(result.has_value());
    CHECK_FALSE(result->terminal_override.has_value());
}

TEST_CASE("LaunchPlanBuilder: computes is_dangerous") {
    core::LaunchItem safe_item;
    safe_item.name = "safe";
    safe_item.directory = "/tmp";
    safe_item.command = "snow";
    safe_item.id = "safe";

    auto safe_result = core::LaunchPlanBuilder::build(safe_item);
    REQUIRE(safe_result.has_value());
    CHECK_FALSE(safe_result->is_dangerous);

    core::LaunchItem danger_item;
    danger_item.name = "danger";
    danger_item.directory = "/tmp";
    danger_item.command = "claude --dangerously-skip-permissions";
    danger_item.id = "danger";

    auto danger_result = core::LaunchPlanBuilder::build(danger_item);
    REQUIRE(danger_result.has_value());
    CHECK(danger_result->is_dangerous);
}

TEST_CASE("LaunchPlanBuilder: preserves command") {
    core::LaunchItem item;
    item.name = "test";
    item.directory = "/tmp";
    item.command = "codex --enable goals";
    item.id = "test";

    auto result = core::LaunchPlanBuilder::build(item);
    REQUIRE(result.has_value());
    CHECK(result->command == "codex --enable goals");
}

TEST_CASE("LaunchPlanBuilder: leaves executable and args empty") {
    core::LaunchItem item;
    item.name = "test";
    item.directory = "/tmp";
    item.command = "snow";
    item.id = "test";

    auto result = core::LaunchPlanBuilder::build(item);
    REQUIRE(result.has_value());
    CHECK(result->executable.empty());
    CHECK(result->args.empty());
}
