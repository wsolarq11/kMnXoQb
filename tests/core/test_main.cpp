#define DOCTEST_CONFIG_IMPLEMENT
#include <doctest/doctest.h>

int main() {
    doctest::Context ctx;
    ctx.setOption("no-breaks", true);
    int res = ctx.run();
    ctx.shouldExit();
    return res;
}
