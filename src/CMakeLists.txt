# Core library (zero Slint dependency)
add_library(core_lib STATIC
    core/config.cpp
    core/quote_arg.cpp
    core/is_dangerous.cpp
    core/selected_store.cpp
    core/launcher.cpp
    core/launch_plan_builder.cpp
)

target_include_directories(core_lib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(core_lib PUBLIC glaze::glaze spdlog::spdlog)
# Platform library
add_library(platform_lib STATIC
    platform/path_resolver.cpp
    platform/terminal_launcher_win.cpp
    platform/terminal_launcher_macos.cpp
    platform/terminal_launcher_linux.cpp
    platform/terminal_launcher_factory.cpp
    platform/single_instance.cpp
    platform/theme_detector.cpp
)

target_include_directories(platform_lib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(platform_lib PRIVATE core_lib reproc reproc++)

# Main executable
add_executable(wt-launcher
    main.cpp
    app.cpp
)

slint_target_sources(wt-launcher
    ${CMAKE_CURRENT_SOURCE_DIR}/ui/main_window.slint
)

target_link_libraries(wt-launcher PRIVATE
    Slint::Slint
    core_lib
    platform_lib
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
