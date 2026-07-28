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

// --- 新增边界用例 ---

TEST_CASE("quote_arg: multiple consecutive quotes") {
    // 三个连续引号: a + '"' + '"' + '"' + b
    auto result = core::quote_arg(std::string("a\"\"\"b"));
    REQUIRE(result == "\"a\\\"\\\"\\\"b\"");
}

TEST_CASE("quote_arg: only backslashes no other chars") {
    // 三个中间反斜杠（不翻倍）
    auto result = core::quote_arg(std::string("\\\\\\"));
    // 字符串: "\\\\\\" 即 3 个反斜杠，尾部最后一个翻倍 => 2+1=5个? 实际上输入是 \\\ (三个)
    // 遍历: \\\ -> 前两个是中间不翻倍，第三个是尾部翻倍 = 1+1+2 = 4个反斜杠 + 两端引号
    // 先确认: 输入为 "\\\\\\" 在 C++ literal 中是 \\\ (三个反斜杠)
    // 遍历: bs=1, bs=2, bs=3(结束) -> 尾部翻倍 => 6个 \\
    // 实际上 "\\\\\\" C++ string = 3 个反斜杠
    // 期望: \" + (中间不翻倍: \\\\) + (尾部翻倍: \\\\\\\\) + \" = 4+6=10? 不对
    // 3个反斜杠全都在尾部，所以 3*2=6个反斜杠 + 引号
    REQUIRE(result == "\"\\\\\\\\\\\\\"");
}

TEST_CASE("quote_arg: mixed quote backslash sequences") {
    // 引号+反斜杠+引号
    auto result = core::quote_arg(std::string("\"\\\""));
    // '"' + '\\' + '"' -> 引号前无反斜杠，输出 \"，中间反斜杠不翻倍输出 \，再输出 \"
    // 结果: \" + \\ + \" = "\\\"\\\""
    REQUIRE(result == "\"\\\"\\\\\\\"\"");
}

TEST_CASE("quote_arg: string with spaces") {
    // 带空格字符串
    auto result = core::quote_arg("hello world");
    REQUIRE(result == "\"hello world\"");
}

TEST_CASE("quote_arg: path-like string") {
    // 输入: C:\Program Files\App\  (C++: "C:\\Program Files\\App\\")
    auto input = std::string("C:\\Program Files\\App\\");
    auto result = core::quote_arg(input);
    // 验证两端有引号
    REQUIRE(result.front() == '\"');
    REQUIRE(result.back() == '\"');
    // 验证引号都被正确转义
    for (size_t i = 1; i < result.size() - 1; ++i) {
        if (result[i] == '\"') {
            REQUIRE(i > 1);
            REQUIRE(result[i - 1] == '\\');
        }
    }
}

TEST_CASE("quote_arg: unicode characters") {
    // Unicode 字符应原样保留
    auto result = core::quote_arg("启动器");
    REQUIRE(result == "\"启动器\"");
}

TEST_CASE("quote_arg: mixed unicode with special chars") {
    // Unicode + 空格 + 引号
    auto result = core::quote_arg(std::string("项目\"路径\""));
    REQUIRE(result == "\"项目\\\"路径\\\"\"");
}

TEST_CASE("quote_arg: very long string") {
    // 超长输入测试（1000 个字符）
    std::string long_input(1000, 'a');
    auto result = core::quote_arg(long_input);
    REQUIRE(result.size() == 1002);  // 两端各加一个引号
    REQUIRE(result.front() == '\"');
    REQUIRE(result.back() == '\"');
    // 中间内容应与输入一致
    REQUIRE(result.substr(1, 1000) == long_input);
}

TEST_CASE("quote_arg: all special characters combined") {
    // 包含反斜杠、引号、空格、Unicode
    auto input = std::string("a\\b\"c d\\e\"f\\g\\");
    auto result = core::quote_arg(input);
    // 仅验证不崩溃且两端有引号
    REQUIRE(result.front() == '\"');
    REQUIRE(result.back() == '\"');
    // 验证引号都被转义
    for (size_t i = 1; i < result.size() - 1; ++i) {
        if (result[i] == '\"') {
            // 引号前必须有反斜杠
            REQUIRE(i > 1);
            REQUIRE(result[i - 1] == '\\');
        }
    }
}
