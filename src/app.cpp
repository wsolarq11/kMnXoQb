#include "app.h"
#include "core/launch_plan_builder.h"
#include "platform/terminal_launcher.h"
#include "platform/single_instance.h"
#include "platform/theme_detector.h"
#include "core/logger.h"
#include "core/is_dangerous.h"

#include <glaze/glaze.hpp>
#include <algorithm>
#include <expected>
#include <filesystem>
#include <thread>
#include <array>
#include <cstring>
#include <reproc++/reproc.hpp>

#ifdef _WIN32
#include <windows.h>
#include <shlobj.h>  // SHBrowseForFolder
#endif

namespace {

// 缓存 ThemeDetector 实例，避免多次创建。
auto& get_theme_detector() {
    static auto detector = pal::ThemeDetector::create();
    return detector;
}

// 解析主题字符串为 Theme 枚举，替代 magic_enum。
auto parse_theme(const std::string& s) -> core::Theme {
    if (s == "dark") return core::Theme::dark;
    if (s == "light") return core::Theme::light;
    return core::Theme::system;
}

} // anonymous namespace

App::App(slint::ComponentHandle<MainWindow> window)
    : window_(std::move(window)) {}

int App::run() {
    // 单实例检测
    auto instance = pal::SingleInstance::create();
    if (!instance->is_only_instance()) {
        return 0;  // 已有实例运行，静默退出
    }

    resolver_ = pal::PathResolver::create();
    auto config_dir = resolver_->config_directory();
    if (config_dir) {
        core::Logger::init(config_dir->string());
        config_ = std::make_unique<core::ConfigIO>(*config_dir);
        launcher_ = std::make_unique<core::Launcher>(config_dir->string());
    }

    CORE_LOG_INFO("WT Launcher started");
    load_config();
    bind_callbacks();
    refresh_ui();
    window_->run();
    return 0;
}

void App::load_config() {
    if (!config_) return;

    auto items = config_->read_items();
    if (items) {
        items_ = std::move(*items);
        selected_.load_from(items_);
        CORE_LOG_INFO("Loaded {} items from config", items_.size());
    } else {
        CORE_LOG_ERROR("load_config: read_items failed");
    }

    auto settings = config_->read_settings();
    if (settings) {
        settings_ = std::move(*settings);
    }
}

void App::save_config() {
    if (!config_) return;
    selected_.save_to(items_);
    if (auto r = config_->write_items(items_); !r) {
        CORE_LOG_ERROR("save_config: write_items failed: {} (code={})",
            r.error().message(), static_cast<int>(r.error().code()));
    }
    if (auto r = config_->write_settings(settings_); !r) {
        CORE_LOG_ERROR("save_config: write_settings failed: {} (code={})",
            r.error().message(), static_cast<int>(r.error().code()));
    }
}

void App::refresh_ui() {
    window_->set_stat_total(static_cast<int>(items_.size()));
    window_->set_stat_recent(slint::SharedString(
        !settings_.launch_history.empty() ? settings_.launch_history.front() : ""));
    window_->set_confirm_enabled(settings_.confirm_enabled);
    auto theme = parse_theme(settings_.theme);
    window_->set_is_dark(theme == core::Theme::dark
        || (theme == core::Theme::system && get_theme_detector()->is_system_dark()));
    window_->set_selected_count(static_cast<int>(selected_.count()));
    window_->set_is_editing(false);
    window_->set_dialog_visible(false);
    window_->set_status_text(slint::SharedString(
        settings_.launch_history.empty()
            ? "Ready"
            : "Last launched: " + settings_.launch_history.front()));
    update_card_model();
}
void App::update_card_model() {
    if (!card_model_) {
        card_model_ = std::make_shared<slint::VectorModel<LaunchCardData>>();
        window_->set_items(card_model_);
    }

    // 清空现有数据（从后往前逐个删除）
    while (card_model_->row_count() > 0) {
        card_model_->erase(card_model_->row_count() - 1);
    }

    // 搜索词小写化（大小写不敏感匹配）
    std::string query_lower;
    query_lower.resize(search_query_.size());
    std::transform(search_query_.begin(), search_query_.end(),
                   query_lower.begin(), ::tolower);

    for (size_t i = 0; i < items_.size(); ++i) {
        const auto& item = items_[i];

        // 搜索过滤：匹配 name / directory / command
        if (!query_lower.empty()) {
            auto contains = [&](const std::string& field) -> bool {
                std::string field_lower;
                field_lower.resize(field.size());
                std::transform(field.begin(), field.end(),
                               field_lower.begin(), ::tolower);
                return field_lower.find(query_lower) != std::string::npos;
            };

            if (!contains(item.name) && !contains(item.directory) && !contains(item.command)) {
                continue;
            }
        }

        LaunchCardData data;
        data.name = slint::SharedString(item.name);
        data.dir = slint::SharedString(item.directory);
        data.cmd = slint::SharedString(item.command);
        data.is_selected = selected_.is_selected(item.id);
        data.is_dangerous = core::is_dangerous(item.command);
        data.original_index = static_cast<int>(i);
        card_model_->push_back(std::move(data));
    }
}

void App::bind_callbacks() {
    window_->on_toggle_confirm([this](bool enabled) { on_toggle_confirm(enabled); });
    window_->on_show_add_dialog([this]() { on_show_add_dialog(); });
    window_->on_launch_selected([this]() { on_launch_selected(); });
    window_->on_toggle_select_all([this](bool all) { on_toggle_select_all(all); });
    window_->on_item_launched([this](int index) { on_launch(index); });
    window_->on_item_edit_clicked([this](int index) { on_edit_item(index); });
    window_->on_item_delete_clicked([this](int index) { on_delete_item(index); });
    window_->on_item_selected_toggled([this](int index, bool selected) {
        on_item_selected_toggled(index, selected);
    });
    window_->on_dialog_save([this](slint::SharedString name, slint::SharedString dir,
                                  slint::SharedString cmd, bool confirm) {
        on_dialog_save(std::string(name), std::string(dir), std::string(cmd), confirm);
    });
    window_->on_dialog_delete([this]() { on_dialog_delete(); });
    window_->on_dialog_cancel([this]() { on_dialog_cancel(); });
    window_->on_dialog_browse([this]() { on_dialog_browse(); });
    window_->on_search_changed([this](slint::SharedString query) {
        on_search(std::string(query));
    });
    window_->on_confirm_launch([this]() { on_confirm_launch(); });
    window_->on_cancel_launch([this]() { on_cancel_launch(); });
    window_->on_toggle_theme([this]() { on_toggle_theme(); });
}

void App::on_toggle_confirm(bool enabled) {
    settings_.confirm_enabled = enabled;
    save_config();
}

void App::on_show_add_dialog() {
    current_edit_index_ = -1;
    window_->set_dialog_title(slint::SharedString("New Item"));
    window_->set_edit_name(slint::SharedString(""));
    window_->set_edit_dir(slint::SharedString(""));
    window_->set_edit_cmd(slint::SharedString(""));
    window_->set_edit_confirm(true);
    window_->set_is_editing(false);
    window_->set_dialog_visible(true);
}

void App::on_launch(int index) {
    if (index < 0 || index >= static_cast<int>(items_.size())) return;
    const auto& item = items_[index];

    // 需要确认时弹窗，不直接启动
    if (settings_.confirm_enabled && (item.confirm || core::is_dangerous(item.command))) {
        pending_launch_index_ = index;
        window_->set_confirm_item_name(slint::SharedString(item.name));
        window_->set_confirm_is_dangerous(core::is_dangerous(item.command));
        window_->set_confirm_dialog_visible(true);
        return;
    }

    // 不需要确认，直接启动
    launch_item(index);
}

void App::launch_item(int index) {
    if (index < 0 || index >= static_cast<int>(items_.size())) return;
    const auto& item = items_[index];

    // 运行时校验目录是否存在
    if (!std::filesystem::exists(item.directory)) {
        CORE_LOG_ERROR("Directory not found: {}", item.directory);
        window_->set_status_text(slint::SharedString(
            "Directory not found: " + item.directory));
        return;
    }

    // 构造 LaunchPlan（纯数据，无字符串拼接）
    auto plan_result = core::LaunchPlanBuilder::build(item);
    if (!plan_result) {
        CORE_LOG_ERROR("Build plan failed: {}", plan_result.error().message());
        window_->set_status_text(slint::SharedString(
            "Build plan failed: " + plan_result.error().message()));
        return;
    }

    // 平台层填充 executable + args（纯逻辑，可在任意线程调用）
    auto terminal = pal::TerminalLauncher::create();
    auto populated = terminal->populate(std::move(*plan_result));
    if (!populated) {
        CORE_LOG_ERROR("Populate failed: {}", populated.error().message());
        window_->set_status_text(slint::SharedString(
            "Populate failed: " + populated.error().message()));
        return;
    }

    // 后台线程执行 launch（posix_spawn/CreateProcessW），避免阻塞 UI 线程。
    // 结果通过 slint::invoke_from_event_loop 回 UI 线程更新状态。
    std::string item_name = item.name;
    auto plan = std::make_shared<core::LaunchPlan>(std::move(*populated));
    auto launcher = std::move(terminal);
    auto weak_self = weak_from_this();
    std::jthread([weak_self, plan, launcher = std::move(launcher), item_name](std::stop_token st) {
        auto result = launcher->launch(*plan);
        if (st.stop_requested()) return;

        auto self = weak_self.lock();
        if (!self || st.stop_requested()) {
            CORE_LOG_WARN("App destroyed before launch completed: {}", item_name);
            return;
        }
        slint::invoke_from_event_loop([weak_self, result = std::move(result), item_name]() {
            auto self = weak_self.lock();
            if (!self) return;
            if (result) {
                CORE_LOG_INFO("Launch succeeded: {}", item_name);
                self->settings_.launch_history.erase(
                    std::remove(self->settings_.launch_history.begin(),
                                self->settings_.launch_history.end(), item_name),
                    self->settings_.launch_history.end());
                self->settings_.launch_history.insert(self->settings_.launch_history.begin(), item_name);
                if (self->settings_.launch_history.size() > 10)
                    self->settings_.launch_history.resize(10);
                self->save_config();
                self->refresh_ui();
            } else {
                CORE_LOG_ERROR("Launch failed: {} - {}", item_name, result.error().message());
                self->window_->set_status_text(slint::SharedString(
                    "Launch failed: " + result.error().message()));
            }
        });
    });
}

void App::on_confirm_launch() {
    window_->set_confirm_dialog_visible(false);
    if (pending_launch_index_ >= 0) {
        int idx = pending_launch_index_;
        pending_launch_index_ = -1;
        launch_item(idx);
    }
}
void App::on_cancel_launch() {
    window_->set_confirm_dialog_visible(false);
    pending_launch_index_ = -1;
}

void App::on_toggle_theme() {
    // 循环切换：light -> dark -> system
    auto theme = parse_theme(settings_.theme);
    switch (theme) {
        case core::Theme::light: settings_.theme = "dark"; break;
        case core::Theme::dark:  settings_.theme = "system"; break;
        case core::Theme::system: settings_.theme = "light"; break;
    }
    theme = parse_theme(settings_.theme);
    bool is_dark = (theme == core::Theme::dark)
        || (theme == core::Theme::system && get_theme_detector()->is_system_dark());
    window_->set_is_dark(is_dark);
    save_config();
}

void App::on_edit_item(int index) {
    if (index < 0 || index >= static_cast<int>(items_.size())) return;
    const auto& item = items_[index];

    current_edit_index_ = index;
    window_->set_dialog_title(slint::SharedString("Edit Item"));
    window_->set_edit_name(slint::SharedString(item.name));
    window_->set_edit_dir(slint::SharedString(item.directory));
    window_->set_edit_cmd(slint::SharedString(item.command));
    window_->set_edit_confirm(item.confirm);
    window_->set_is_editing(true);
    window_->set_dialog_visible(true);
}

void App::on_delete_item(int index) {
    if (index < 0 || index >= static_cast<int>(items_.size())) return;
    items_.erase(items_.begin() + index);
    save_config();
    refresh_ui();
}

void App::on_launch_selected() {
    auto ids = selected_.selected_ids();
    int launched = 0;
    for (const auto& id : ids) {
        auto it = std::find_if(items_.begin(), items_.end(),
            [&id](const auto& item) { return item.id == id; });
        if (it == items_.end()) continue;
        int idx = static_cast<int>(std::distance(items_.begin(), it));
        const auto& item = items_[idx];

        // 逐项检查确认（通过 on_launch 的统一确认逻辑）
        if (settings_.confirm_enabled && (item.confirm || core::is_dangerous(item.command))) {
            pending_launch_index_ = idx;
            window_->set_confirm_item_name(slint::SharedString(item.name));
            window_->set_confirm_is_dangerous(core::is_dangerous(item.command));
            window_->set_confirm_dialog_visible(true);
            // 等待用户确认，不继续后续启动
            return;
        }

        launch_item(idx);
        launched++;
    }

    if (launched > 0) {
        selected_.deselect_all();
        save_config();
        refresh_ui();
    }
}

void App::on_toggle_select_all(bool all) {
    if (all) selected_.select_all(items_);
    else selected_.deselect_all();
    refresh_ui();
}
void App::on_search(const std::string& query) {
    search_query_ = query;
    update_card_model();
}

void App::on_dialog_save(const std::string& name, const std::string& dir,
                          const std::string& cmd, bool confirm) {
    if (name.empty() || cmd.empty()) return;

    core::LaunchItem item;
    item.name = name;
    item.directory = dir;
    item.command = cmd;
    item.confirm = confirm;

    if (current_edit_index_ >= 0 && current_edit_index_ < static_cast<int>(items_.size())) {
        item.id = items_[current_edit_index_].id;
        item.selected = items_[current_edit_index_].selected;
        items_[current_edit_index_] = std::move(item);
    } else {
        item.selected = false;
        item.id = item.name;
        int n = 2;
        while (std::any_of(items_.begin(), items_.end(),
            [&](const auto& i) { return i.id == item.id; })) {
            item.id = item.name + "_" + std::to_string(n++);
        }
        items_.push_back(std::move(item));
    }

    save_config();
    on_dialog_cancel();
    refresh_ui();
}

void App::on_dialog_delete() {
    if (current_edit_index_ >= 0 && current_edit_index_ < static_cast<int>(items_.size())) {
        items_.erase(items_.begin() + current_edit_index_);
        save_config();
    }
    on_dialog_cancel();
    refresh_ui();
}

void App::on_dialog_cancel() {
    window_->set_dialog_visible(false);
}

void App::on_dialog_browse() {
#ifdef _WIN32
    // 使用 Windows IFileDialog 选择目录（现代 API）
    IFileDialog* pfd = nullptr;
    HRESULT hr = CoCreateInstance(CLSID_FileOpenDialog, nullptr,
        CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&pfd));
    if (SUCCEEDED(hr)) {
        DWORD options;
        pfd->GetOptions(&options);
        pfd->SetOptions(options | FOS_PICKFOLDERS);  // 目录选择模式

        if (SUCCEEDED(pfd->Show(nullptr))) {
            IShellItem* psi = nullptr;
            if (SUCCEEDED(pfd->GetResult(&psi))) {
                PWSTR path = nullptr;
                if (SUCCEEDED(psi->GetDisplayName(SIGDN_FILESYSPATH, &path))) {
                    int len = WideCharToMultiByte(CP_UTF8, 0, path, -1, nullptr, 0, nullptr, nullptr);
                    std::string dir(len, '\0');
                    WideCharToMultiByte(CP_UTF8, 0, path, -1, &dir[0], len, nullptr, nullptr);
                    window_->set_edit_dir(slint::SharedString(dir));
                    CoTaskMemFree(path);
                }
                psi->Release();
            }
        }
        pfd->Release();
    }
#elif defined(__APPLE__)
    // macOS: 使用 NSOpenPanel (通过 osascript, reproc)
    {
        reproc::process process;
        std::error_code ec = process.start({"/usr/bin/osascript", "-e",
            "tell app \"Finder\" to POSIX path of (choose folder with prompt \"Select directory\")"});
        if (!ec) {
            std::array<char, 4096> buf{};
            auto [bytes_read, read_ec] = process.read(reproc::stream::out, buf.data(), buf.size());
            process.wait(reproc::infinite);
            process.stop();
            if (!read_ec && bytes_read > 0) {
                std::string dir(buf.data(), static_cast<size_t>(bytes_read));
                while (!dir.empty() && (dir.back() == '\n' || dir.back() == '\r'))
                    dir.pop_back();
                if (!dir.empty())
                    window_->set_edit_dir(slint::SharedString(dir));
            }
        }
    }
#else
    // Linux: 尝试 zenity / kdialog / Xdialog (使用 reproc)
    {
        const char* const dialog_candidates[] = {"zenity", "kdialog", "Xdialog"};
        for (const char* bin : dialog_candidates) {
            reproc::process process;
            std::error_code ec;
            if (std::strcmp(bin, "zenity") == 0)
                ec = process.start({bin, "--file-selection", "--directory"});
            else if (std::strcmp(bin, "kdialog") == 0)
                ec = process.start({bin, "--getexistingdirectory", "."});
            else
                ec = process.start({bin, "--dirstdout", "."});
            if (ec) continue;

            std::array<char, 4096> buf{};
            auto [bytes_read, read_ec] = process.read(reproc::stream::out, buf.data(), buf.size());
            process.wait(reproc::infinite);
            process.stop();
            if (!read_ec && bytes_read > 0) {
                std::string dir(buf.data(), static_cast<size_t>(bytes_read));
                while (!dir.empty() && (dir.back() == '\n' || dir.back() == '\r'))
                    dir.pop_back();
                if (!dir.empty()) {
                    window_->set_edit_dir(slint::SharedString(dir));
                    break;
                }
            }
        }
    }
#endif
}

void App::on_item_selected_toggled(int index, bool selected) {
    if (index < 0 || index >= static_cast<int>(items_.size())) return;
    selected_.set_selected(items_[index].id, selected);
    refresh_ui();
}
