cmake_minimum_required(VERSION 3.29)
project(WTLauncher VERSION 0.1.0 LANGUAGES CXX)

include(cmake/CompileOptions.cmake)

enable_testing()

find_package(Slint REQUIRED)
find_package(glaze CONFIG REQUIRED)

add_subdirectory(src)
add_subdirectory(tests)
