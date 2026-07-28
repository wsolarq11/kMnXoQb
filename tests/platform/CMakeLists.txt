# Platform logic unit tests
# 跨平台编译（子任务 C 移除 if(WIN32) 限制）
# doctest provided by CPM from root CMakeLists.txt

add_executable(platform_tests
    test_main.cpp
)
target_link_libraries(platform_tests PRIVATE
    doctest::doctest
    trompeloeil
    platform_lib
)

target_include_directories(platform_tests PRIVATE ${CMAKE_SOURCE_DIR}/src)

include(CTest)
add_test(NAME platform_tests COMMAND platform_tests)
