//! Launchpad Tauri: Rust core + React frontend.

pub mod app;
pub mod commands;
pub mod config;
pub mod core;
pub mod infra;
pub mod state;

use config::paths::detect_install_form;
use state::AppState;
use tauri::Manager;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let exe_dir = std::env::current_exe()
        .ok()
        .and_then(|p| p.parent().map(|d| d.to_path_buf()))
        .unwrap_or_default();
    let install_form = detect_install_form(&exe_dir);

    tauri::Builder::default()
        .plugin(tauri_plugin_dialog::init())
        .plugin(tauri_plugin_single_instance::init(|app, _args, _cwd| {
            // Second instance: focus the existing window and exit.
            if let Some(window) = app.get_webview_window("main") {
                let _ = window.set_focus();
            }
        }))
        .manage(AppState::new(install_form))
        .setup(|app| {
            let window = app
                .get_webview_window("main")
                .ok_or("main window missing")?;
            let app_handle = app.handle().clone();

            // Restore the persisted geometry at startup (already clamped by
            // WindowPosition inside load_window_state_impl).
            let state = app.state::<AppState>();
            if let Ok(Some(ws)) = commands::misc::load_window_state_impl(&state, &window) {
                let _ = window.set_position(tauri::PhysicalPosition::new(ws.x, ws.y));
                let _ = window.set_size(tauri::PhysicalSize::new(ws.width, ws.height));
            }

            // Persist geometry when the window closes (the restore-time clamp
            // guards the -32000 minimized coordinates, so no extra check here).
            let win_for_save = window.clone();
            window.on_window_event(move |event| {
                if let tauri::WindowEvent::CloseRequested { .. } = event {
                    let state = app_handle.state::<AppState>();
                    let _ = commands::misc::save_window_state_impl(&state, &win_for_save);
                }
            });

            Ok(())
        })
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
