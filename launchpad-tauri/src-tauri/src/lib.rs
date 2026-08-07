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

/// WebView2 Runtime presence check (the app cannot start without it; Win11
/// ships it, Win10 — including LTSC — does not). Falls back to a native
/// message box with install guidance instead of failing silently.
fn webview2_available() -> bool {
    let root = std::path::Path::new(r"C:\Program Files (x86)\Microsoft\EdgeWebView\Application");
    if !root.is_dir() {
        return false;
    }
    // A version subdirectory (e.g. 150.0.4078.105) proves a real install.
    std::fs::read_dir(root)
        .map(|entries| {
            entries
                .flatten()
                .any(|e| e.file_type().map(|t| t.is_dir()).unwrap_or(false))
        })
        .unwrap_or(false)
}

fn warn_webview2_missing() {
    use windows::Win32::UI::WindowsAndMessaging::{
        MessageBoxW, MB_ICONWARNING, MB_OK, MB_SETFOREGROUND,
    };
    let title = windows::core::w!("Launchpad");
    let msg = windows::core::w!(
        "Launchpad 需要 Microsoft Edge WebView2 Runtime 才能运行。\n\n\
         Windows 11 已内置；Windows 10 请访问\n\
         https://developer.microsoft.com/microsoft-edge/webview2 安装后重试。"
    );
    // SAFETY: static wide strings; the call blocks until the user dismisses.
    unsafe {
        let _ = MessageBoxW(None, msg, title, MB_OK | MB_ICONWARNING | MB_SETFOREGROUND);
    }
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    if !webview2_available() {
        warn_webview2_missing();
        return;
    }

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

            // Apply the window material (Mica/Acrylic) for the persisted
            // theme; failures degrade silently to the opaque background.
            let state = app.state::<AppState>();
            if let Ok(settings) = state.settings.load() {
                infra::effects::apply_window_material(&window, &settings.theme);
            }

            // Restore the persisted geometry at startup (already clamped by
            // WindowPosition inside load_window_state_impl).
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
            commands::misc::window_material,
            commands::misc::check_directory,
            commands::misc::check_directories,
        ])
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
