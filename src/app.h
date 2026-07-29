#pragma once

#include <memory>
#include <vector>
#include <slint.h>

#include "shell/launcher_app.h"
#include "platform/dialog_provider.h"
#include "main_window.h"

class App : public std::enable_shared_from_this<App> {
public:
    App(slint::ComponentHandle<MainWindow> window,
        shell::LauncherApp& app,
        pal::DialogProvider& dialog_provider);

    int run();

private:
    void refresh_ui();
    void bind_callbacks();
    void update_card_model();

    // Callbacks (delegate to app_)
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

    slint::ComponentHandle<MainWindow> window_;
    shell::LauncherApp& app_;
    pal::DialogProvider& dialog_provider_;

    std::shared_ptr<slint::VectorModel<LaunchCardData>> card_model_;
    int current_edit_index_ = -1;
    int pending_launch_index_ = -1;
    std::string search_query_;
};
