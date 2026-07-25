#include <doctest/doctest.h>

#include "core/is_dangerous.h"

TEST_CASE("is_dangerous: dangerously flag detected") {
    CHECK(core::is_dangerous("codex --dangerously-bypass-approvals-and-sandbox"));
    CHECK(core::is_dangerous("claude --dangerously-skip-permissions"));
    CHECK(core::is_dangerous("--dangerously"));
}

TEST_CASE("is_dangerous: yolo flag detected") {
    CHECK(core::is_dangerous("snow --yolo"));
    CHECK(core::is_dangerous("command --yolo-mode"));
}

TEST_CASE("is_dangerous: skip-permissions detected") {
    CHECK(core::is_dangerous("claude --dangerously-skip-permissions"));
}

TEST_CASE("is_dangerous: bypass flags detected") {
    CHECK(core::is_dangerous("codex --dangerously-bypass-approvals-and-sandbox"));
    CHECK(core::is_dangerous("--bypass-approvals"));
    CHECK(core::is_dangerous("--bypass-sandbox"));
}

TEST_CASE("is_dangerous: safe command not flagged") {
    CHECK_FALSE(core::is_dangerous("snow"));
    CHECK_FALSE(core::is_dangerous("opencode"));
    CHECK_FALSE(core::is_dangerous(""));
    CHECK_FALSE(core::is_dangerous("git push"));
    CHECK_FALSE(core::is_dangerous("npm install"));
}

TEST_CASE("is_dangerous: case insensitive") {
    CHECK(core::is_dangerous("YOLO"));
    CHECK(core::is_dangerous("Dangerously"));
    CHECK(core::is_dangerous("SKIP-PERMISSIONS"));
}
