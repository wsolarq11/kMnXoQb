#include "app.h"
#include "core/filter_items.h"
#include "core/is_dangerous.h"
#include "shell/logger.h"

#include <glaze/glaze.hpp>
#include <algorithm>
#include <thread>

namespace {

auto parse_theme(const std::string& s) -> core::Theme {
    if (s == "dark") return core::Theme::dark;
    if (s == "light") return core::Theme::light;
    return core::Theme::system;
}

} // namespace

App::App(slint::ComponentHandle<MainWindow> window,
         shell::LauncherApp& app,
         pal::DialogProvider& dialog_provider)
    : window_(std::move(window)), app_(app), dialog_provider_(dialog_provider) {}

int App::run() {
    APP_LOG_INFO("WT Launcher started");
    app_.load_config();
    bind_callbacks();
    refresh_ui();
    window_->run();
    return 0;
}

// ── UI refresh ──

void App::refresh_ui() {
    auto& items = app_.items();
    auto& settings = app_.settings();
    auto& selected = app_.selected();
    auto& theme_detector = app_.theme_detector();

    window_->set_stat_total(static_cast<int>(items.size()));
    window_->set_stat_recent(slint::SharedString(
        !settings.launch_history.empty() ? settings.launch_history.front() : ""));
    window_->set_confirm_enabled(settings.confirm_enabled);
    auto theme = parse_theme(settings.theme);
    window_->set_is_dark(theme == core::Theme::dark
        || (theme == core::Theme::system && theme_detector.is_system_dark()));
    window_->set_selected_count(static_cast<int>(selected.count()));
    window_->set_is_editing(false);
    window_->set_dialog_visible(false);
    window_->set_status_text(slint::SharedString(
        settings.launch_history.empty()
            ? "Ready"
            : "Last launched: " + settings.launch_history.front()));
    update_card_model();
}

void App::update_card_model() {
    if (!card_model_) {
        card_model_ = std::make_shared<slint::VectorModel<LaunchCardData>>();
        window_->set_items(card_model_);
    }
    while (card_model_->row_count() > 0)
        card_model_->erase(card_model_->row_count() - 1);

    auto& items = app_.items();
    auto matched = core::filter_items(items, search_query_);
    for (auto idx : matched) {
        const auto& item = items[idx];
        LaunchCardData data;
        data.name = slint::SharedString(item.name);
        data.dir = slint::SharedString(item.directory);
        data.cmd = slint::SharedString(item.command);
        data.is_selected = app_.selected().is_selected(item.id);
        data.is_dangerous = core::is_dangerous(item.command);
        data.original_index = static_cast<int>(idx);
        card_model_->push_back(std::move(data));
    }
}

// ── Callbacks ──

void App::bind_callbacks() {
    window_->on_toggle_confirm([this](bool e) { app_.settings().confirm_enabled = e; app_.save_config(); });
    window_->on_show_add_dialog([this]() { on_show_add_dialog(); });
    window_->on_launch_selected([this]() { on_launch_selected(); });
    window_->on_toggle_select_all([this](bool a) { on_toggle_select_all(a); });
    window_->on_item_launched([this](int i) { on_launch(i); });
    window_->on_item_edit_clicked([this](int i) { on_edit_item(i); });
    window_->on_item_delete_clicked([this](int i) { on_delete_item(i); });
    window_->on_item_selected_toggled([this](int i, bool s) { on_item_selected_toggled(i, s); });
    window_->on_dialog_save([this](auto n, auto d, auto c, bool cf) {
        on_dialog_save(std::string(n), std::string(d), std::string(c), cf);
    });
    window_->on_dialog_delete([this]() { on_dialog_delete(); });
    window_->on_dialog_cancel([this]() { on_dialog_cancel(); });
    window_->on_dialog_browse([this]() { on_dialog_browse(); });
    window_->on_search_changed([this](auto q) { on_search(std::string(q)); });
    window_->on_confirm_launch([this]() { on_confirm_launch(); });
    window_->on_cancel_launch([this]() { on_cancel_launch(); });
    window_->on_toggle_theme([this]() { on_toggle_theme(); });
}

void App::on_toggle_confirm(bool enabled) {
    app_.settings().confirm_enabled = enabled;
    app_.save_config();
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
    auto& items = app_.items();
    if (index < 0 || index >= static_cast<int>(items.size())) return;
    const auto& item = items[static_cast<size_t>(index)];
    if (app_.settings().confirm_enabled && (item.confirm || core::is_dangerous(item.command))) {
        pending_launch_index_ = index;
        window_->set_confirm_item_name(slint::SharedString(item.name));
        window_->set_confirm_is_dangerous(core::is_dangerous(item.command));
        window_->set_confirm_dialog_visible(true);
        return;
    }
    auto weak_self = weak_from_this();
    std::jthread([weak_self, id = item.id](std::stop_token st) {
        if (st.stop_requested()) return;
        auto self = weak_self.lock();
        if (!self) return;
        auto result = self->app_.launch(id);
        slint::invoke_from_event_loop([weak_self, result = std::move(result), id]() {
            auto self = weak_self.lock();
            if (!self) return;
            if (!result)
                self->window_->set_status_text(slint::SharedString(
                    "Launch failed: " + result.error().message()));
            else self->refresh_ui();
        });
    });
}

void App::on_confirm_launch() {
    window_->set_confirm_dialog_visible(false);
    if (pending_launch_index_ >= 0) {
        int idx = pending_launch_index_;
        pending_launch_index_ = -1;
        on_launch(idx);
    }
}

void App::on_cancel_launch() {
    window_->set_confirm_dialog_visible(false);
    pending_launch_index_ = -1;
}

void App::on_toggle_theme() {
    auto& s = app_.settings();
    auto theme = parse_theme(s.theme);
    switch (theme) {
        case core::Theme::light: s.theme = "dark"; break;
        case core::Theme::dark:  s.theme = "system"; break;
        case core::Theme::system: s.theme = "light"; break;
    }
    theme = parse_theme(s.theme);
    window_->set_is_dark(theme == core::Theme::dark
        || (theme == core::Theme::system && app_.theme_detector().is_system_dark()));
    app_.save_config();
}

void App::on_edit_item(int index) {
    auto& items = app_.items();
    if (index < 0 || index >= static_cast<int>(items.size())) return;
    const auto& item = items[static_cast<size_t>(index)];
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
    if (index < 0 || index >= static_cast<int>(app_.items().size())) return;
    app_.delete_item(static_cast<size_t>(index));
    app_.save_config();
    refresh_ui();
}

void App::on_launch_selected() {
    auto ids = app_.selected().selected_ids();
    for (const auto& id : ids) {
        auto& items = app_.items();
        auto it = std::find_if(items.begin(), items.end(),
            [&](const auto& i) { return i.id == id; });
        if (it == items.end()) continue;
        int idx = static_cast<int>(std::distance(items.begin(), it));
        const auto& item = *it;
        if (app_.settings().confirm_enabled && (item.confirm || core::is_dangerous(item.command))) {
            pending_launch_index_ = idx;
            window_->set_confirm_item_name(slint::SharedString(item.name));
            window_->set_confirm_is_dangerous(core::is_dangerous(item.command));
            window_->set_confirm_dialog_visible(true);
            return;
        }
        std::jthread([weak_self = weak_from_this(), id = item.id](std::stop_token st) {
            if (st.stop_requested()) return;
            if (auto self = weak_self.lock()) (void)self->app_.launch(id);
        });
    }
    app_.selected().deselect_all();
    app_.save_config();
    refresh_ui();
}

void App::on_toggle_select_all(bool all) {
    if (all) app_.selected().select_all(app_.items());
    else app_.selected().deselect_all();
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

    if (current_edit_index_ >= 0 && current_edit_index_ < static_cast<int>(app_.items().size())) {
        app_.edit_item(static_cast<size_t>(current_edit_index_), std::move(item));
    } else {
        app_.add_item(std::move(item));
    }
    app_.save_config();
    on_dialog_cancel();
    refresh_ui();
}

void App::on_dialog_delete() {
    if (current_edit_index_ >= 0) {
        app_.delete_item(static_cast<size_t>(current_edit_index_));
        app_.save_config();
    }
    on_dialog_cancel();
    refresh_ui();
}

void App::on_dialog_cancel() {
    window_->set_dialog_visible(false);
}

void App::on_dialog_browse() {
    auto dir = dialog_provider_.browse_directory();
    if (dir) window_->set_edit_dir(slint::SharedString(dir->string()));
}

void App::on_item_selected_toggled(int index, bool selected) {
    if (index < 0 || index >= static_cast<int>(app_.items().size())) return;
    app_.selected().set_selected(app_.items()[static_cast<size_t>(index)].id, selected);
    refresh_ui();
}
