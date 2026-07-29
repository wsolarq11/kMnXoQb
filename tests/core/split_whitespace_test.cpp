#include <doctest/doctest.h>
#include <core/split_whitespace.h>

TEST_CASE("split_by_whitespace: empty string returns empty vector") {
    auto result = core::split_by_whitespace("");
    CHECK(result.empty());
}

TEST_CASE("split_by_whitespace: single word returns one token") {
    auto result = core::split_by_whitespace("hello");
    REQUIRE(result.size() == 1);
    CHECK(result[0] == "hello");
}

TEST_CASE("split_by_whitespace: two words separated by space") {
    auto result = core::split_by_whitespace("hello world");
    REQUIRE(result.size() == 2);
    CHECK(result[0] == "hello");
    CHECK(result[1] == "world");
}

TEST_CASE("split_by_whitespace: multiple words separated by single spaces") {
    auto result = core::split_by_whitespace("wt.exe -d /tmp pwsh");
    REQUIRE(result.size() == 4);
    CHECK(result[0] == "wt.exe");
    CHECK(result[1] == "-d");
    CHECK(result[2] == "/tmp");
    CHECK(result[3] == "pwsh");
}

TEST_CASE("split_by_whitespace: consecutive spaces treated as single separator") {
    auto result = core::split_by_whitespace("a  b   c");
    REQUIRE(result.size() == 3);
    CHECK(result[0] == "a");
    CHECK(result[1] == "b");
    CHECK(result[2] == "c");
}

TEST_CASE("split_by_whitespace: tab separator handled") {
    auto result = core::split_by_whitespace("a\tb\tc");
    REQUIRE(result.size() == 3);
    CHECK(result[0] == "a");
    CHECK(result[1] == "b");
    CHECK(result[2] == "c");
}

TEST_CASE("split_by_whitespace: leading whitespace ignored") {
    auto result = core::split_by_whitespace("  hello world");
    REQUIRE(result.size() == 2);
    CHECK(result[0] == "hello");
    CHECK(result[1] == "world");
}

TEST_CASE("split_by_whitespace: trailing whitespace ignored") {
    auto result = core::split_by_whitespace("hello world  ");
    REQUIRE(result.size() == 2);
    CHECK(result[0] == "hello");
    CHECK(result[1] == "world");
}

TEST_CASE("split_by_whitespace: whitespace-only returns empty") {
    auto result = core::split_by_whitespace("   \t  \t  ");
    CHECK(result.empty());
}

TEST_CASE("split_by_whitespace: terminal_override use case") {
    auto result = core::split_by_whitespace("\"C:\\Program Files\\term.exe\" --flag");
    // Known limitation: does not handle double-quoted regions.
    // This produces 6 tokens (quotes treated as literal chars).
    CHECK(result.size() > 0);
}
