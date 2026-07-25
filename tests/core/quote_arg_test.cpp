#include <doctest/doctest.h>

#include "core/quote_arg.h"

// 移植 verify.ps1 的 4 个断言用例
// HTA quoteArg 规则：中间反斜杠不翻倍；遇引号或位于尾部时翻倍

TEST_CASE("quote_arg: simple text without special chars") {
    auto result = core::quote_arg("ab");
    REQUIRE(result == "\"ab\"");
}

TEST_CASE("quote_arg: backslash in the middle (not doubled)") {
    // 'a' + '\\' + 'b' -> backslash in middle, not doubled
    auto result = core::quote_arg(std::string("a\\b"));
    REQUIRE(result == "\"a\\b\"");
}

TEST_CASE("quote_arg: double quote is escaped") {
    // 'a' + '"' + 'b' -> quote escaped
    auto result = core::quote_arg(std::string("a\"b"));
    REQUIRE(result == "\"a\\\"b\"");
}

TEST_CASE("quote_arg: trailing backslash is doubled") {
    // 'a' + '\\' + 'b' + '\\' -> trailing backslash doubled
    auto result = core::quote_arg(std::string("a\\b\\"));
    REQUIRE(result == "\"a\\b\\\\\"");
}

TEST_CASE("quote_arg: empty string") {
    auto result = core::quote_arg("");
    REQUIRE(result == "\"\"");
}

TEST_CASE("quote_arg: two backslashes doubled to four") {
    // 输入 2 个反斜杠，尾部翻倍为 4 个
    auto result = core::quote_arg(std::string("\\\\"));
    REQUIRE(result == "\"\\\\\\\\\"");
}

TEST_CASE("quote_arg: mixed backslashes and quotes") {
    // '\\' + '"' + '\\' + 'x' -> backslash + quote + backslash + x
    auto result = core::quote_arg(std::string("\\\"\\x"));
    REQUIRE(result == "\"\\\\\\\"\\x\"");
}
