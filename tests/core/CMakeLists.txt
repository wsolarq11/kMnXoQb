# Core logic unit tests

find_package(doctest CONFIG REQUIRED)

add_executable(core_tests
    test_main.cpp
    quote_arg_test.cpp
    is_dangerous_test.cpp
    selected_store_test.cpp
    config_test.cpp
    launcher_test.cpp
)

target_link_libraries(core_tests PRIVATE
    doctest::doctest
    core_lib
)

target_include_directories(core_tests PRIVATE ${CMAKE_SOURCE_DIR}/src)

include(CTest)
include(CTest)
add_test(NAME core_tests COMMAND core_tests)
