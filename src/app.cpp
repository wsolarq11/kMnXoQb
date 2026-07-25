#include "app.h"
#include "platform/terminal_launcher.h"
#include "platform/single_instance.h"

#include <glaze/glaze.hpp>
#include <algorithm>

#ifdef _WIN32
#include <windows.h>
#include <shlobj.h>  // SHBrowseForFolder
#endif

App::App(slint::ComponentHandle<MainWindow> window)
    : window_(std::move(window)) {}

int App::run() {
    // 单实例检测
    pal::SingleInstance instance;
    if (!instance.is_only_instance()) {
        return 0;  // 已有实例运行，静默退出
    }

    resolver_ = std::make_unique<pal::PathResolver>();
    auto config_dir = resolver_->config_directory();
    if (config_dir) {
        config_ = std::make_unique<core::ConfigIO>(*config_dir);
        launcher_ = std::make_unique<core::Launcher>(config_dir->string());
    }

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
    }

    auto settings = config_->read_settings();
    if (settings) {
        settings_ = std::move(*settings);
    }
}

void App::save_config() {
    if (!config_) return;
    selected_.save_to(items_);
    config_->write_items(items_);
    config_->write_settings(settings_);
}

void App::refresh_ui() {
    window_->set_stat_total(static_cast<int>(items_.size()));
    window_->set_stat_recent(slint::SharedString(
        !settings_.launch_history.empty() ? settings_.launch_history.front() : ""));
    window_->set_confirm_enabled(settings_.confirm_enabled);
    window_->set_selected_count(static_cast<int>(selected_.count()));
    window_->set_is_editing(false);
    window_->set_dialog_visible(false);
    update_card_model();
}

void App::update_card_model() {
    // ponytail: Phase 4 实现完整 VectorModel 绑定
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

    if (settings_.confirm_enabled && (item.confirm || launcher_->is_dangerous(item.command))) {
        // TODO: Phase 4 集成 Slint 确认对话框
    }

    // 使用 TerminalLauncher 真正启动进程
    auto terminal = pal::TerminalLauncher::create();
    auto result = terminal->launch(item.directory, item.command);
    if (result) {
        settings_.launch_history.erase(
            std::remove(settings_.launch_history.begin(),
                        settings_.launch_history.end(), item.name),
            settings_.launch_history.end());
        settings_.launch_history.insert(settings_.launch_history.begin(), item.name);
        if (settings_.launch_history.size() > 10)
            settings_.launch_history.resize(10);
        save_config();
        refresh_ui();
    }
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
    auto terminal = pal::TerminalLauncher::create();
    // 逐个启动选中项
    auto ids = selected_.selected_ids();
    int ok = 0, fail = 0;
    for (const auto& id : ids) {
        auto it = std::find_if(items_.begin(), items_.end(),
            [&id](const auto& item) { return item.id == id; });
        if (it == items_.end()) { fail++; continue; }
        auto result = terminal->launch(it->directory, it->command);
        if (result) ok++; else fail++;
    }
    selected_.deselect_all();
    save_config();
    refresh_ui();
}

void App::on_toggle_select_all(bool all) {
    if (all) selected_.select_all(items_);
    else selected_.deselect_all();
    refresh_ui();
}

void App::on_search(const std::string& query) {
    (void)query;
}

void App::on_dialog_save(const std::string& name, const std::string& dir,
                          const std::string& cmd, bool confirm) {
    if (name.empty()) return;

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
#else
    // TODO: Phase 5 实现 macOS/Linux 目录对话框
#endif
}

void App::on_item_selected_toggled(int index, bool selected) {
    if (index < 0 || index >= static_cast<int>(items_.size())) return;
    selected_.set_selected(items_[index].id, selected);
    refresh_ui();
}
