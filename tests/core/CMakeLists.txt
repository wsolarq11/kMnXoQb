# Core logic unit tests

# doctest provided by CPM from root CMakeLists.txt

add_executable(core_tests
    test_main.cpp
    quote_arg_test.cpp
    is_dangerous_test.cpp
    selected_store_test.cpp
    config_test.cpp
    launcher_test.cpp
    launch_plan_builder_test.cpp
    rapidcheck_test.cpp
    split_whitespace_test.cpp
    validate_rules_test.cpp
    deduplicate_id_test.cpp
    filter_items_test.cpp
)
target_link_libraries(core_tests PRIVATE
    doctest::doctest
    rapidcheck
    core_lib
)

target_include_directories(core_tests PRIVATE ${CMAKE_SOURCE_DIR}/src)

include(CTest)
add_test(NAME core_tests COMMAND core_tests)
# RapidCheck builds as DLL; prepend its directory to PATH so ctest can find librapidcheck.dll
set_tests_properties(core_tests PROPERTIES
    ENVIRONMENT_MODIFICATION "PATH=path_list_prepend:${CMAKE_BINARY_DIR}/_deps/rapidcheck-build")
