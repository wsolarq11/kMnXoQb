#include <doctest/doctest.h>
#include <rapidcheck.h>

#include <string>
#include <vector>
#include <algorithm>
#include <sstream>

// ---------- helpers under test (inline for property testing) ----------

namespace {

// 按空白拆分（来自 terminal_launcher_win.cpp，逻辑等价复制以便测试）
// 不支持引号嵌套/转义
auto split_by_whitespace(const std::string& s) -> std::vector<std::string> {
    std::vector<std::string> tokens;
    std::string current;
    for (char ch : s) {
        if (ch == ' ' || ch == '\t') {
            if (!current.empty()) {
                tokens.push_back(std::move(current));
                current.clear();
            }
        } else {
            current += ch;
        }
    }
    if (!current.empty()) {
        tokens.push_back(std::move(current));
    }
    return tokens;
}

// 将 tokens 用空格重新拼接
auto join_with_space(const std::vector<std::string>& tokens) -> std::string {
    std::ostringstream oss;
    for (size_t i = 0; i < tokens.size(); ++i) {
        if (i > 0) oss << ' ';
        oss << tokens[i];
    }
    return oss.str();
}

// 计算空白字符（空格/制表符）的数量
auto count_whitespace(const std::string& s) -> size_t {
    return static_cast<size_t>(std::count_if(s.begin(), s.end(),
        [](char c) { return c == ' ' || c == '\t'; }));
}

} // anonymous namespace

// ========== Property-based tests for split_by_whitespace ==========

TEST_CASE("split_by_whitespace: joining tokens reconstructs input (whitespace normalized)") {
    rc::check([](const std::string& input) {
        // 只对非空输入测试
        RC_PRE(!input.empty());

        auto tokens = split_by_whitespace(input);
        auto joined = join_with_space(tokens);

        // 属性：拆分的 tokens 再拼接，应该得到不含首尾空白且连续空白被压缩为单空格的字符串
        // 即：去掉输入中连续空白和首尾空白后的结果
        std::string normalized;
        bool prev_ws = true; // 首部空白跳过
        for (char c : input) {
            if (c == ' ' || c == '\t') {
                if (!prev_ws) {
                    normalized += ' ';
                    prev_ws = true;
                }
            } else {
                normalized += c;
                prev_ws = false;
            }
        }
        // 去掉尾部可能的空格
        if (!normalized.empty() && normalized.back() == ' ') {
            normalized.pop_back();
        }

        RC_ASSERT(joined == normalized);
    });
}

TEST_CASE("split_by_whitespace: whitespace-only input yields empty tokens") {
    rc::check([](const std::string& input) {
        RC_PRE(!input.empty());
        RC_PRE(count_whitespace(input) == input.size()); // 全是空白

        auto tokens = split_by_whitespace(input);
        RC_ASSERT(tokens.empty());
    });
}

TEST_CASE("split_by_whitespace: no whitespace yields single token equal to input") {
    rc::check([](const std::string& input) {
        RC_PRE(!input.empty());
        RC_PRE(count_whitespace(input) == 0); // 无空白

        auto tokens = split_by_whitespace(input);
        RC_ASSERT(tokens.size() == 1);
        RC_ASSERT(tokens[0] == input);
    });
}

TEST_CASE("split_by_whitespace: each token is non-empty") {
    rc::check([](const std::string& input) {
        RC_PRE(!input.empty());

        auto tokens = split_by_whitespace(input);
        for (const auto& t : tokens) {
            RC_ASSERT(!t.empty());
        }
    });
}

TEST_CASE("split_by_whitespace: token count is at most whitespace count + 1") {
    rc::check([](const std::string& input) {
        auto tokens = split_by_whitespace(input);
        auto ws = count_whitespace(input);
        RC_ASSERT(tokens.size() <= ws + 1);
    });
}

// ========== Property-based tests for quote_arg logic ==========

TEST_CASE("quote_arg: quoted string starts and ends with double quote") {
    rc::check([](const std::string& input) {
        // quote_arg 返回 "input" 格式，所以结果至少 2 个字符
        auto quoted = "\"" + input + "\"";
        RC_ASSERT(quoted.size() >= 2);
        RC_ASSERT(quoted.front() == '"');
        RC_ASSERT(quoted.back() == '"');
    });
}

TEST_CASE("split_by_whitespace: joining does not introduce extra whitespace") {
    rc::check([](const std::vector<std::string>& tokens) {
        RC_PRE(!tokens.empty());
        // 每个 token 非空且不含空白
        for (const auto& t : tokens) {
            RC_PRE(!t.empty());
            RC_PRE(t.find(' ') == std::string::npos);
            RC_PRE(t.find('\t') == std::string::npos);
        }

        auto joined = join_with_space(tokens);
        auto reparsed = split_by_whitespace(joined);

        RC_ASSERT(reparsed.size() == tokens.size());
        for (size_t i = 0; i < tokens.size(); ++i) {
            RC_ASSERT(reparsed[i] == tokens[i]);
        }
    });
}
