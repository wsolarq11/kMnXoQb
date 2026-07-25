#include <doctest/doctest.h>

#include "core/config.h"

// ConfigIO requires filesystem access.
// These tests verify the interface compiles and handles basic errors.
// Full integration tests with temp directories will be added in Phase 2+3.

TEST_CASE("ConfigIO: interface compiles") {
    // Just verify the header is includable and types are accessible
    auto check_types = [](const core::LaunchItem& item) {
        return item.id;
    };
    core::LaunchItem item;
    item.id = "test";
    CHECK_EQ(check_types(item), "test");
}
