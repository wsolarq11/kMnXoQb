#define DOCTEST_CONFIG_IMPLEMENT
#include <doctest/doctest.h>
#include <trompeloeil.hpp>

int main() {
    doctest::Context ctx;
    ctx.setOption("no-breaks", true);
    ctx.setOption("order-by", "name");

    trompeloeil::set_reporter([](trompeloeil::severity s,
                                  const char* file,
                                  unsigned long line,
                                  const std::string& msg) {
        if (s == trompeloeil::severity::fatal) {
            FAIL(msg.c_str());
        } else {
            ADD_FAIL_CHECK_AT(file, static_cast<int>(line), msg);
        }
    });

    int res = ctx.run();
    ctx.shouldExit();
    return res;
}
