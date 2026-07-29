add_executable(shell_tests
    test_main.cpp
    launcher_app_test.cpp
)
target_link_libraries(shell_tests PRIVATE
    doctest::doctest
    trompeloeil
    shell_lib
)

target_include_directories(shell_tests PRIVATE ${CMAKE_SOURCE_DIR}/src)

# Copy runtime DLLs (spdlog, etc.) to test output directory
add_custom_command(TARGET shell_tests POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        $<TARGET_RUNTIME_DLLS:shell_tests>
        $<TARGET_FILE_DIR:shell_tests>
    COMMAND_EXPAND_LISTS
    COMMENT "Copying runtime DLLs to shell_tests output directory"
)

include(CTest)
add_test(NAME shell_tests COMMAND shell_tests)
