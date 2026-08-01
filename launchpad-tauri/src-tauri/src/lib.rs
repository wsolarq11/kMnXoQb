//! Launchpad Tauri: Rust core + React frontend.
//!
//! Phase 0 gate code (probe commands) was retired at the end of phase 3;
//! the functional surface is now the real item/launch/settings commands.

pub mod app;
pub mod commands;
pub mod config;
pub mod core;
pub mod infra;
pub mod state;

use config::paths::detect_install_form;
use state::AppState;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let exe_dir = std::env::current_exe()
        .ok()
        .and_then(|p| p.parent().map(|d| d.to_path_buf()))
        .unwrap_or_default();
    let install_form = detect_install_form(&exe_dir);

    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .manage(AppState::new(install_form))
        .invoke_handler(tauri::generate_handler![
            commands::items::list_items,
            commands::items::create_item,
            commands::items::update_item,
            commands::items::delete_item,
            commands::items::move_item,
            commands::items::set_select,
            commands::items::toggle_select_all,
            commands::launch::needs_confirm,
            commands::launch::launch_item,
            commands::launch::launch_many,
            commands::settings::get_settings,
            commands::settings::toggle_theme,
            commands::settings::toggle_language,
            commands::settings::set_confirm_enabled,
            commands::misc::get_language,
            commands::misc::pick_directory,
            commands::misc::save_window_state,
            commands::misc::load_window_state,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
