#pragma once

#include <memory>
#include <vector>
#include <slint.h>

#include "core/config.h"
#include "core/launcher.h"
#include "core/selected_store.h"
#include "platform/path_resolver.h"
#include "main_window.h"

class App : public std::enable_shared_from_this<App> {
public:
    App(slint::ComponentHandle<MainWindow> window);
    int run();

private:
    void load_config();
    void save_config();
    void refresh_ui();
    void bind_callbacks();

    void on_toggle_confirm(bool enabled);
    void on_show_add_dialog();
    void on_launch(int index);
    void on_edit_item(int index);
    void on_delete_item(int index);
    void on_launch_selected();
    void on_toggle_select_all(bool all);
    void on_search(const std::string& query);
    void on_dialog_save(const std::string& name, const std::string& dir,
                        const std::string& cmd, bool confirm);
    void on_dialog_delete();
    void on_dialog_cancel();
    void on_dialog_browse();
    void on_item_selected_toggled(int index, bool selected);
    void on_confirm_launch();
    void on_cancel_launch();
    void on_toggle_theme();
    void launch_item(int index);
    void update_card_model();

    slint::ComponentHandle<MainWindow> window_;
    std::unique_ptr<core::ConfigIO> config_;
    std::unique_ptr<core::Launcher> launcher_;
    core::SelectedStore selected_;
    std::unique_ptr<pal::PathResolver> resolver_;
    std::vector<core::LaunchItem> items_;
    core::AppSettings settings_;
    int current_edit_index_ = -1;
    int pending_launch_index_ = -1;
    std::shared_ptr<slint::VectorModel<LaunchCardData>> card_model_;
    std::string search_query_;
};
