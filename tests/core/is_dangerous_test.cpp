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

// --- 新增边界用例 ---

TEST_CASE("is_dangerous: multiple dangerous flags in one command") {
    CHECK(core::is_dangerous("snow --yolo --dangerously-bypass-approvals"));
    CHECK(core::is_dangerous("--dangerously --yolo"));
}

TEST_CASE("is_dangerous: dangerous substring inside normal word") {
    // "dangerously" 是危险关键词，即使嵌入在更长串中也应匹配
    CHECK(core::is_dangerous("notdangerously"));
    CHECK(core::is_dangerous("dangerouslysafe"));
}

TEST_CASE("is_dangerous: flag at different positions") {
    // 开头
    CHECK(core::is_dangerous("--yolo command args"));
    // 中间
    CHECK(core::is_dangerous("snow --yolo --flag"));
    // 结尾
    CHECK(core::is_dangerous("snow --yolo"));
}

TEST_CASE("is_dangerous: bypass variants with dots") {
    // 正则中 . 匹配任意字符，所以通过 . 分隔的变体也能匹配
    CHECK(core::is_dangerous("--bypass approvals"));
    CHECK(core::is_dangerous("--bypass:approvals"));
    CHECK(core::is_dangerous("--bypassXapprovals"));
}

TEST_CASE("is_dangerous: skip permissions variants") {
    CHECK(core::is_dangerous("--skip-permissions"));
    CHECK(core::is_dangerous("--skip_permissions"));
    CHECK(core::is_dangerous("--skip permissions"));
}

TEST_CASE("is_dangerous: yolo in different cases and contexts") {
    CHECK(core::is_dangerous("YOLO"));
    CHECK(core::is_dangerous("yolo"));
    CHECK(core::is_dangerous("Yolo"));
    CHECK(core::is_dangerous("\"yolo\""));
    CHECK(core::is_dangerous("'yolo'"));
}

TEST_CASE("is_dangerous: safe commands with dangerous-looking substrings") {
    // 不含完整危险关键词的安全命令
    CHECK_FALSE(core::is_dangerous("yol"));  // 不是完整 yolo
    CHECK_FALSE(core::is_dangerous("dangerous"));  // 不是完整 dangerously
    CHECK_FALSE(core::is_dangerous("--bypass"));  // 不是完整 bypass.?approvals|sandbox
    CHECK_FALSE(core::is_dangerous("permission"));  // 不是完整 skip.?permissions
}

TEST_CASE("is_dangerous: command with leading and trailing whitespace") {
    CHECK(core::is_dangerous("  --yolo  "));
    CHECK(core::is_dangerous("\t--dangerously\t"));
    CHECK_FALSE(core::is_dangerous("   "));
}

TEST_CASE("is_dangerous: very long safe command") {
    std::string long_cmd(10000, 'a');
    CHECK_FALSE(core::is_dangerous(long_cmd));
}

TEST_CASE("is_dangerous: dangerous keyword in very long command") {
    std::string long_cmd = std::string(5000, 'a') + "--yolo" + std::string(5000, 'a');
    CHECK(core::is_dangerous(long_cmd));
}

TEST_CASE("is_dangerous: pipe and chain operators") {
    CHECK(core::is_dangerous("snow --yolo | echo test"));
    CHECK(core::is_dangerous("echo a && snow --dangerously"));
}
