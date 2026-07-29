# Core library (zero Slint dependency)
add_library(core_lib STATIC
    core/config.cpp
    core/quote_arg.cpp
    core/is_dangerous.cpp
    core/selected_store.cpp
    core/launcher.cpp
    core/launch_plan_builder.cpp
    core/split_whitespace.cpp
    core/validate_rules.cpp
    core/deduplicate_id.cpp
    core/filter_items.cpp
)

target_include_directories(core_lib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(core_lib PUBLIC glaze::glaze)
# Platform library
add_library(platform_lib STATIC
    platform/path_resolver.cpp
    platform/terminal_launcher_win.cpp
    platform/terminal_launcher_macos.cpp
    platform/terminal_launcher_linux.cpp
    platform/terminal_launcher_factory.cpp
    platform/single_instance.cpp
    platform/theme_detector.cpp
    platform/dialog_provider_win.cpp
    platform/dialog_provider_macos.cpp
    platform/dialog_provider_linux.cpp
    platform/dialog_provider_factory.cpp
    platform/terminal_launcher_conpty.cpp
)

target_include_directories(platform_lib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(platform_lib PRIVATE core_lib reproc reproc++)

# Shell library (LauncherApp, logger, real FS — shared between GUI and tests)
add_library(shell_lib STATIC
    shell/launcher_app.cpp
)

target_include_directories(shell_lib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(shell_lib PUBLIC core_lib platform_lib spdlog::spdlog)

# Main executable
add_executable(wt-launcher
    main.cpp
    app.cpp
    cli_check.cpp
    cli_launch.cpp
)

slint_target_sources(wt-launcher
    ${CMAKE_CURRENT_SOURCE_DIR}/ui/main_window.slint
)

target_link_libraries(wt-launcher PRIVATE
    Slint::Slint
    shell_lib
    reproc
    reproc++
)

target_include_directories(wt-launcher PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

# 自动复制所有运行时 DLL 到输出目录
# $<TARGET_RUNTIME_DLLS:target> 是 CMake 3.21+ 的标准 genex，
# 运行时展开为 target 所有传递依赖中 SHARED IMPORTED 目标的 DLL 路径列表。
# COMMAND_EXPAND_LISTS 将分号分隔的列表拆为多个参数传给 copy_if_different。
add_custom_command(TARGET wt-launcher POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy_if_different
        $<TARGET_RUNTIME_DLLS:wt-launcher>
        $<TARGET_FILE_DIR:wt-launcher>
    COMMAND_EXPAND_LISTS
    COMMENT "Copying runtime DLLs to output directory"
)
