#define DOCTEST_CONFIG_IMPLEMENT
#include <doctest/doctest.h>
#include <trompeloeil.hpp>
#include "platform/theme_detector.h"
#include "platform/single_instance.h"

namespace pal {

class MockThemeDetector final : public ThemeDetector {
public:
    MAKE_MOCK0(is_system_dark, bool(), override);
};

class MockSingleInstance final : public SingleInstance {
public:
    MAKE_CONST_MOCK0(is_only_instance, bool(), override);
};

} // namespace pal

TEST_CASE("ThemeDetector returns bool") {
    pal::MockThemeDetector mock;
    REQUIRE_CALL(mock, is_system_dark())
        .RETURN(true);
    CHECK(mock.is_system_dark() == true);
}

TEST_CASE("ThemeDetector default factory creates valid instance") {
    auto detector = pal::ThemeDetector::create();
    CHECK(detector != nullptr);
    // is_system_dark 返回某个 bool 值，不会崩溃
    CHECK_NOTHROW(detector->is_system_dark());
}

TEST_CASE("SingleInstance default factory creates valid instance") {
    auto instance = pal::SingleInstance::create();
    CHECK(instance != nullptr);
    // is_only_instance 返回某个 bool 值，不会崩溃
    CHECK_NOTHROW(instance->is_only_instance());
}

TEST_CASE("SingleInstance with mock returns expected values") {
    pal::MockSingleInstance mock;
    REQUIRE_CALL(mock, is_only_instance())
        .RETURN(false);
    CHECK(mock.is_only_instance() == false);

    REQUIRE_CALL(mock, is_only_instance())
        .RETURN(true);
    CHECK(mock.is_only_instance() == true);
}

int main() {
    doctest::Context ctx;
    ctx.setOption("no-breaks", true);
    ctx.setOption("order-by", "name");

    // Trompeloeil 异常处理：将 mock 验证失败映射为 doctest 断言
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
