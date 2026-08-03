//! Misc commands: language resolution (three-tier priority in the core),
//! native directory picker, window-state persistence.

use tauri::State;

use crate::core::errors::AppError;
use crate::core::i18n::{self, AppLanguage};
use crate::infra::locale;
use crate::state::AppState;

#[derive(serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ResolvedLanguage {
    /// The language actually shown (explicit setting wins, Auto follows system).
    pub effective: AppLanguage,
    /// The raw setting value ("auto" / "zh-CN" / "en-US").
    pub setting: String,
}

#[tauri::command]
pub fn get_language(state: State<'_, AppState>) -> Result<ResolvedLanguage, AppError> {
    get_language_impl(&state)
}

pub fn get_language_impl(state: &AppState) -> Result<ResolvedLanguage, AppError> {
    let settings = state.settings.load()?;
    let setting = i18n::resolve(Some(&settings.language));
    let system = i18n::from_system_language(locale::first_system_language().as_deref());
    let effective = i18n::effective(setting, system);
    Ok(ResolvedLanguage {
        effective,
        setting: settings.language,
    })
}

/// Native folder picker (IFileDialog via the dialog plugin). Runs on the
/// command thread pool; the blocking call never blocks the webview thread.
#[tauri::command]
pub fn pick_directory(app: tauri::AppHandle) -> Result<Option<String>, AppError> {
    use tauri_plugin_dialog::{DialogExt, FilePath};

    let picked = app
        .dialog()
        .file()
        .set_title("Select working directory")
        .blocking_pick_folder();
    match picked {
        Some(FilePath::Path(path)) => Ok(Some(path.display().to_string())),
        Some(FilePath::Url(_)) | None => Ok(None),
    }
}

#[derive(serde::Serialize, serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct WindowStateDto {
    pub x: i32,
    pub y: i32,
    pub width: u32,
    pub height: u32,
}

/// Persists the current window geometry. Wired to the CloseRequested event in
/// lib.rs (the restore-time clamp guards the -32000 minimized coordinates).
#[tauri::command]
pub fn save_window_state(
    state: State<'_, AppState>,
    window: tauri::WebviewWindow,
) -> Result<(), AppError> {
    save_window_state_impl(&state, &window)
}

pub fn save_window_state_impl(
    state: &AppState,
    window: &tauri::WebviewWindow,
) -> Result<(), AppError> {
    let position = window
        .outer_position()
        .map_err(|e| AppError::Unknown(e.to_string()))?;
    let size = window
        .inner_size()
        .map_err(|e| AppError::Unknown(e.to_string()))?;
    let settings = state.settings.load()?;
    let updated = crate::core::settings::set_window_state(
        &settings,
        crate::core::models::WindowState {
            x: position.x,
            y: position.y,
            width: size.width,
            height: size.height,
        },
    );
    state.settings.save(&updated)
}

/// Loads the persisted window state, clamped to the visible virtual desktop
/// (C# WindowPosition.ClampToVisible semantics: -32000 minimized coordinates
/// and degenerate sizes are corrected at restore time).
#[tauri::command]
pub fn load_window_state(
    state: State<'_, AppState>,
    window: tauri::WebviewWindow,
) -> Result<Option<WindowStateDto>, AppError> {
    load_window_state_impl(&state, &window)
}

pub fn load_window_state_impl(
    state: &AppState,
    window: &tauri::WebviewWindow,
) -> Result<Option<WindowStateDto>, AppError> {
    let settings = state.settings.load()?;
    let Some(ws) = settings.window_state else {
        return Ok(None);
    };
    let (left, top, width, height) = virtual_bounds(window)?;
    let clamped = crate::core::window_pos::clamp_to_visible(&ws, left, top, width, height, 100);
    Ok(Some(WindowStateDto {
        x: clamped.x,
        y: clamped.y,
        width: clamped.width,
        height: clamped.height,
    }))
}

/// Union of all monitor bounds (the virtual desktop).
fn virtual_bounds(window: &tauri::WebviewWindow) -> Result<(i32, i32, i32, i32), AppError> {
    let monitors = window
        .available_monitors()
        .map_err(|e| AppError::Unknown(e.to_string()))?;
    let mut left = i32::MAX;
    let mut top = i32::MAX;
    let mut right = i32::MIN;
    let mut bottom = i32::MIN;
    for m in monitors {
        let p = m.position();
        let s = m.size();
        left = left.min(p.x);
        top = top.min(p.y);
        right = right.max(p.x + s.width as i32);
        bottom = bottom.max(p.y + s.height as i32);
    }
    if left == i32::MAX {
        return Err(AppError::Unknown("no monitors available".to_string()));
    }
    Ok((left, top, right - left, bottom - top))
}
