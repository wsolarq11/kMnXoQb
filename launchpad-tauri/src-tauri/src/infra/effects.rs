//! Window material: Mica on Windows 11, Acrylic fallback on Windows 10.
//! Uses Tauri's built-in window effects (tauri::window::Effect), which
//! delegate to the official window-vibrancy crate. Failures degrade
//! silently to the opaque CSS background (body without .material).

use tauri::{utils::config::WindowEffectsConfig, window::Effect, Runtime, WebviewWindow};

/// The material this OS can render, for the frontend's translucent-chrome
/// switch: "mica" | "acrylic" | "none".
pub fn material_state() -> &'static str {
    match windows_build() {
        Some(build) => material_state_for_build(build),
        None => "none",
    }
}

/// Applies the theme-appropriate material. Win11: Mica follows the app
/// theme (MicaDark/MicaLight force it); Win10 1809+: Acrylic. Anything
/// unsupported or failing is ignored so the app stays fully usable.
pub fn apply_window_material<R: Runtime>(window: &WebviewWindow<R>, theme: &str) {
    let Some(build) = windows_build() else { return };
    if let Some(effect) = effect_for_build(build, theme) {
        let _ = window.set_effects(Some(WindowEffectsConfig {
            effects: vec![effect],
            ..Default::default()
        }));
    }
}

fn material_state_for_build(build: u32) -> &'static str {
    if build >= 22000 {
        "mica"
    } else if build >= 17763 {
        "acrylic"
    } else {
        "none"
    }
}

fn effect_for_build(build: u32, theme: &str) -> Option<Effect> {
    if build >= 22000 {
        match theme {
            "dark" => Some(Effect::MicaDark),
            "light" => Some(Effect::MicaLight),
            _ => Some(Effect::Mica),
        }
    } else if build >= 17763 {
        Some(Effect::Acrylic)
    } else {
        None
    }
}

/// Windows build number via RtlGetVersion (works on Win10+; GetVersionEx is
/// manifest-locked). None on non-Windows or when the call fails.
fn windows_build() -> Option<u32> {
    use windows::Wdk::System::SystemServices::RtlGetVersion;
    use windows::Win32::System::SystemInformation::OSVERSIONINFOW;
    unsafe {
        let mut info = OSVERSIONINFOW {
            dwOSVersionInfoSize: std::mem::size_of::<OSVERSIONINFOW>() as u32,
            ..Default::default()
        };
        // NTSTATUS::ok() yields Result<(), Error>; the outer .ok() turns
        // that into Option so `?` works in this Option-returning fn.
        RtlGetVersion(&mut info).ok().ok()?;
        Some(info.dwBuildNumber)
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn win11_reports_mica() {
        assert_eq!(material_state_for_build(22631), "mica");
    }

    #[test]
    fn win10_reports_acrylic() {
        assert_eq!(material_state_for_build(19044), "acrylic");
    }

    #[test]
    fn older_windows_reports_none() {
        assert_eq!(material_state_for_build(17134), "none");
    }

    #[test]
    fn unknown_build_reports_none() {
        assert_eq!(material_state_for_build(0), "none");
    }

    // Theme -> effect mapping (build checks are the OS boundary).
    #[test]
    fn win11_theme_maps_to_mica_variants() {
        assert_eq!(effect_for_build(22631, "system"), Some(Effect::Mica));
        assert_eq!(effect_for_build(22631, "dark"), Some(Effect::MicaDark));
        assert_eq!(effect_for_build(22631, "light"), Some(Effect::MicaLight));
    }

    #[test]
    fn win10_maps_to_acrylic_for_any_theme() {
        assert_eq!(effect_for_build(19044, "system"), Some(Effect::Acrylic));
        assert_eq!(effect_for_build(19044, "dark"), Some(Effect::Acrylic));
    }

    #[test]
    fn too_old_for_material() {
        assert_eq!(effect_for_build(17134, "dark"), None);
    }
}
