#include <doctest/doctest.h>

#ifdef _WIN32

#include "platform/terminal_command.h"
#include <string>

TEST_CASE("build_wt_command_string: basic path and command") {
    auto result = pal::build_wt_command_string("C:\\projects\\myapp", "snow");
    // wt -d "<dir>" pwsh -NoExit -Command "<command>"
    CHECK(result.find("wt -d ") == 0);
    CHECK(result.find(" pwsh -NoExit -Command ") != std::string::npos);
    // 验证目录和命令都被引号包裹
    CHECK(result.find("\"C:\\projects\\myapp\"") != std::string::npos);
    CHECK(result.find("\"snow\"") != std::string::npos);
}

TEST_CASE("build_wt_command_string: path with spaces") {
    auto result = pal::build_wt_command_string("C:\\Program Files\\App", "codex");
    CHECK(result.find("wt -d ") == 0);
    // 路径包含空格，应被正确引号化
    CHECK(result.find("\"C:\\Program Files\\App\"") != std::string::npos);
}

TEST_CASE("build_wt_command_string: command with special chars") {
    auto result = pal::build_wt_command_string("C:\\test", "snow --yolo --dangerously");
    CHECK(result.find("\"snow --yolo --dangerously\"") != std::string::npos);
}

TEST_CASE("build_wt_command_string: empty directory") {
    auto result = pal::build_wt_command_string("", "snow");
    CHECK(result.find("wt -d \"\"") != std::string::npos);
}

TEST_CASE("build_wt_command_string: empty command") {
    auto result = pal::build_wt_command_string("C:\\test", "");
    CHECK(result.find("\"\"") != std::string::npos);
}

TEST_CASE("build_pwsh_command_string: basic path and command") {
    auto result = pal::build_pwsh_command_string("C:\\projects\\myapp", "snow");
    // pwsh -NoExit -Command "cd <dir>; <command>"
    CHECK(result.find("pwsh -NoExit -Command ") == 0);
    // 复合命令应包含 "cd <dir>; <command>"
    CHECK(result.find("cd C:\\projects\\myapp; snow") != std::string::npos);
}

TEST_CASE("build_pwsh_command_string: path with spaces") {
    auto result = pal::build_pwsh_command_string("C:\\Program Files\\App", "codex");
    CHECK(result.find("pwsh -NoExit -Command ") == 0);
    // 验证整个复合命令被引号包裹（含空格路径）
    CHECK(result.find("\"cd C:\\Program Files\\App; codex\"") != std::string::npos);
}

TEST_CASE("build_pwsh_command_string: special chars in command") {
    auto result = pal::build_pwsh_command_string("C:\\test", "snow --dangerously");
    CHECK(result.find("cd C:\\test; snow --dangerously") != std::string::npos);
}

TEST_CASE("build_pwsh_command_string: empty directory") {
    auto result = pal::build_pwsh_command_string("", "snow");
    CHECK(result.find("\"cd ; snow\"") != std::string::npos);
}

TEST_CASE("build_pwsh_command_string: empty command") {
    auto result = pal::build_pwsh_command_string("C:\\test", "");
    CHECK(result.find("\"cd C:\\test; \"") != std::string::npos);
}

TEST_CASE("build_wt_and_pwsh: produce different command prefixes") {
    auto wt_cmd = pal::build_wt_command_string("C:\\test", "snow");
    auto pwsh_cmd = pal::build_pwsh_command_string("C:\\test", "snow");
    CHECK(wt_cmd.find("wt ") == 0);
    CHECK(pwsh_cmd.find("pwsh ") == 0);
    CHECK_NE(wt_cmd, pwsh_cmd);
}

#else
// 非 Windows 平台：空测试桩确保 test 文件编译通过
TEST_CASE("terminal_command: not available on this platform") {
    // Windows-only tests, skipped on other platforms
    CHECK(true);
}
#endif
