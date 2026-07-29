#include <doctest/doctest.h>

#include "core/config.h"
#include "shell/real_filesystem.h"
#include <filesystem>
#include <fstream>

namespace fs = std::filesystem;

// 临时目录 RAII 封装
struct TempDir {
    fs::path path;
    TempDir() {
        path = fs::temp_directory_path() / "wt_launcher_test_XXXXXX";
        // 使用随机后缀避免冲突
        for (int i = 0; i < 100; ++i) {
            auto candidate = fs::temp_directory_path() / ("wt_launcher_test_" + std::to_string(i));
            if (!fs::exists(candidate)) {
                path = candidate;
                break;
            }
        }
        fs::create_directories(path);
    }
    ~TempDir() {
        std::error_code ec;
        fs::remove_all(path, ec);
    }
};

TEST_CASE("ConfigIO: read_items from non-existent dir returns ConfigNotFound") {
    TempDir tmp;
    shell::RealFilesystem fs;
    core::ConfigIO config(tmp.path / "nonexistent", fs);
    auto result = config.read_items();
    REQUIRE_FALSE(result.has_value());
    CHECK_EQ(result.error().code(), core::ErrorCode::kConfigNotFound);
}

TEST_CASE("ConfigIO: write_items then read_items roundtrip") {
    TempDir tmp;
    shell::RealFilesystem fs;
    core::ConfigIO config(tmp.path, fs);

    // 准备数据
    std::vector<core::LaunchItem> items;
    core::LaunchItem item1;
    item1.name = "test-app";
    item1.directory = "C:\\projects\\test";
    item1.command = "snow";
    item1.confirm = false;
    item1.id = "test-app";
    item1.selected = true;
    items.push_back(item1);

    core::LaunchItem item2;
    item2.name = "another-app";
    item2.directory = "/home/user/project";
    item2.command = "codex --dangerously-bypass";
    item2.confirm = true;
    item2.id = "another-app";
    item2.terminal = "wt";
    item2.tag = "dev";
    item2.group = "ai-tools";
    items.push_back(item2);

    // 写入
    auto write_result = config.write_items(items);
    REQUIRE(write_result.has_value());

    // 验证文件存在
    REQUIRE(fs::exists(tmp.path / "config.json"));

    // 读取
    auto read_result = config.read_items();
    REQUIRE(read_result.has_value());
    CHECK_EQ(read_result->size(), 2);

    // 验证 item1
    CHECK_EQ((*read_result)[0].name, "test-app");
    CHECK_EQ((*read_result)[0].id, "test-app");
    CHECK_EQ((*read_result)[0].selected, true);
    CHECK_EQ((*read_result)[0].confirm, false);

    // 验证 item2
    CHECK_EQ((*read_result)[1].name, "another-app");
    CHECK_EQ((*read_result)[1].id, "another-app");
    CHECK_EQ((*read_result)[1].tag.value_or(""), "dev");
    CHECK_EQ((*read_result)[1].group.value_or(""), "ai-tools");
    CHECK_EQ((*read_result)[1].terminal.value_or(""), "wt");
}

TEST_CASE("ConfigIO: write_items creates backup of existing file") {
    TempDir tmp;
    shell::RealFilesystem fs;
    core::ConfigIO config(tmp.path, fs);

    // 先写入一份数据
    std::vector<core::LaunchItem> items;
    core::LaunchItem item;
    item.name = "original";
    item.id = "orig";
    items.push_back(item);
    auto r1 = config.write_items(items);
    REQUIRE(r1.has_value());

    // 修改后再次写入
    items[0].name = "modified";
    auto r2 = config.write_items(items);
    REQUIRE(r2.has_value());

    // 验证备份文件存在
    REQUIRE(fs::exists(tmp.path / "config.json.bak"));

    // 验证备份内容是原始数据（重读备份文件验证）
    std::ifstream bak(tmp.path / "config.json.bak");
    std::string bak_content((std::istreambuf_iterator<char>(bak)), std::istreambuf_iterator<char>());
    CHECK(bak_content.find("original") != std::string::npos);
    CHECK(bak_content.find("modified") == std::string::npos);
}

TEST_CASE("ConfigIO: read_settings returns defaults when file missing") {
    TempDir tmp;
    shell::RealFilesystem fs;
    core::ConfigIO config(tmp.path, fs);
    auto result = config.read_settings();
    REQUIRE(result.has_value());
    CHECK_EQ(result->confirm_enabled, false);
    CHECK_EQ(result->theme, "system");
    CHECK(result->launch_history.empty());
}

TEST_CASE("ConfigIO: write_settings then read_settings roundtrip") {
    TempDir tmp;
    shell::RealFilesystem fs;
    core::ConfigIO config(tmp.path, fs);

    core::AppSettings settings;
    settings.confirm_enabled = true;
    settings.theme = "dark";
    settings.launch_history = {"snow", "codex"};

    auto write_result = config.write_settings(settings);
    REQUIRE(write_result.has_value());

    auto read_result = config.read_settings();
    REQUIRE(read_result.has_value());
    CHECK_EQ(read_result->confirm_enabled, true);
    CHECK_EQ(read_result->theme, "dark");
    REQUIRE_EQ(read_result->launch_history.size(), 2);
    CHECK_EQ(read_result->launch_history[0], "snow");
    CHECK_EQ(read_result->launch_history[1], "codex");
}

TEST_CASE("ConfigIO: read_items with invalid JSON returns ConfigParseError") {
    TempDir tmp;
    shell::RealFilesystem fs;
    core::ConfigIO config(tmp.path, fs);

    // 写入无效 JSON
    std::ofstream file(tmp.path / "config.json");
    file << "this is not valid json";
    file.close();

    auto result = config.read_items();
    REQUIRE_FALSE(result.has_value());
    CHECK_EQ(result.error().code(), core::ErrorCode::kConfigParseError);
}

TEST_CASE("ConfigIO: write_items with empty list") {
    TempDir tmp;
    shell::RealFilesystem fs;
    core::ConfigIO config(tmp.path, fs);

    std::vector<core::LaunchItem> empty;
    auto result = config.write_items(empty);
    REQUIRE(result.has_value());

    auto read_result = config.read_items();
    REQUIRE(read_result.has_value());
    CHECK(read_result->empty());
}
