# Core library (zero Slint dependency)
add_library(core_lib STATIC
    core/config.cpp
    core/quote_arg.cpp
    core/is_dangerous.cpp
    core/selected_store.cpp
    core/launcher.cpp
)

target_include_directories(core_lib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(core_lib PUBLIC glaze::glaze)
# Platform library
add_library(platform_lib STATIC
    platform/path_resolver.cpp
    platform/terminal_launcher_win.cpp
    platform/single_instance.cpp
)

target_include_directories(platform_lib PUBLIC ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(platform_lib PRIVATE core_lib)

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
)

target_include_directories(wt-launcher PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})
