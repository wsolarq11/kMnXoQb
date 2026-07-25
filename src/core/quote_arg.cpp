#include "core/quote_arg.h"

namespace core {

auto quote_arg(const std::string& s) -> std::string {
    std::string out = "\"";
    int bs = 0;
    for (auto ch : s) {
        if (ch == '\\') {
            bs++;
            continue;
        }
        if (ch == '"') {
            // 尾部反斜杠翻倍（2*bs 个），再输出引号
            while (bs-- > 0) out += "\\\\";
            out += "\\\"";
            bs = 0;
            continue;
        }
        // 中间反斜杠不翻倍（bs 个）
        while (bs-- > 0) out += "\\";
        out += ch;
        bs = 0;
    }
    // 字符串末尾的反斜杠翻倍（2*bs 个）
    while (bs-- > 0) out += "\\\\";
    out += "\"";
    return out;
}

} // namespace core
