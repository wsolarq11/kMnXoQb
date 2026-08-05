//! Settings commands: three-state theme/language cycles decided in the core
//! (C# had these cycles in the ViewModel; they moved to pure functions).
//!
//! `*_impl` functions take `&AppState` directly for runtime-free testing.

use tauri::State;

use crate::core::errors::AppError;
use crate::core::models::AppSettings;
use crate::core::settings as core_settings;
use crate::state::AppState;

#[tauri::command]
pub fn get_settings(state: State<'_, AppState>) -> Result<AppSettings, AppError> {
    get_settings_impl(&state)
}

pub fn get_settings_impl(state: &AppState) -> Result<AppSettings, AppError> {
    state.settings.load()
}

/// Three-state cycle: system → dark → light → system.
#[tauri::command]
pub fn toggle_theme(
    state: State<'_, AppState>,
    window: tauri::WebviewWindow,
) -> Result<AppSettings, AppError> {
    let updated = toggle_theme_impl(&state)?;
    // Keep the window material (MicaDark/MicaLight/Acrylic) in sync with the
    // theme the UI just switched to.
    crate::infra::effects::apply_window_material(&window, &updated.theme);
    Ok(updated)
}

pub fn toggle_theme_impl(state: &AppState) -> Result<AppSettings, AppError> {
    let settings = state.settings.load()?;
    let next = next_theme(&settings.theme);
    let updated = core_settings::set_theme(&settings, next);
    state.settings.save(&updated)?;
    Ok(updated)
}

/// Language setting cycle: auto → zh-CN → en-US → auto (C# LanguageService).
#[tauri::command]
pub fn toggle_language(state: State<'_, AppState>) -> Result<AppSettings, AppError> {
    toggle_language_impl(&state)
}

pub fn toggle_language_impl(state: &AppState) -> Result<AppSettings, AppError> {
    let settings = state.settings.load()?;
    let next = next_language(&settings.language);
    let updated = core_settings::set_language(&settings, next);
    state.settings.save(&updated)?;
    Ok(updated)
}

#[tauri::command]
pub fn set_confirm_enabled(
    state: State<'_, AppState>,
    enabled: bool,
) -> Result<AppSettings, AppError> {
    set_confirm_enabled_impl(&state, enabled)
}

pub fn set_confirm_enabled_impl(state: &AppState, enabled: bool) -> Result<AppSettings, AppError> {
    let settings = state.settings.load()?;
    let updated = core_settings::set_confirm_enabled(&settings, enabled);
    state.settings.save(&updated)?;
    Ok(updated)
}

fn next_theme(current: &str) -> &'static str {
    match current {
        "system" => "dark",
        "dark" => "light",
        _ => "system",
    }
}

fn next_language(current: &str) -> &'static str {
    match current {
        "auto" => "zh-CN",
        "zh-CN" => "en-US",
        _ => "auto",
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn theme_cycles_system_dark_light() {
        assert_eq!("dark", next_theme("system"));
        assert_eq!("light", next_theme("dark"));
        assert_eq!("system", next_theme("light"));
        assert_eq!("system", next_theme("unknown"));
    }

    #[test]
    fn language_cycles_auto_zh_en() {
        assert_eq!("zh-CN", next_language("auto"));
        assert_eq!("en-US", next_language("zh-CN"));
        assert_eq!("auto", next_language("en-US"));
        assert_eq!("auto", next_language("unknown"));
    }
}
