//! Settings mutations are pure record updates (ported 1:1 from C#
//! SettingsUseCase). Persistence lives in the config layer.

use crate::core::launch::push_history;
use crate::core::models::{AppSettings, LaunchItem, WindowState};

pub fn set_theme(settings: &AppSettings, theme: &str) -> AppSettings {
    let mut s = settings.clone();
    s.theme = theme.to_string();
    s
}

pub fn set_confirm_enabled(settings: &AppSettings, enabled: bool) -> AppSettings {
    let mut s = settings.clone();
    s.confirm_enabled = enabled;
    s
}

/// Language setting value: "auto" (follow system), "zh-CN", or "en-US".
pub fn set_language(settings: &AppSettings, language: &str) -> AppSettings {
    let mut s = settings.clone();
    s.language = language.to_string();
    s
}

pub fn push_history_name(settings: &AppSettings, name: &str) -> AppSettings {
    let mut s = settings.clone();
    s.launch_history = push_history(&settings.launch_history, name, 10);
    s
}

pub fn set_window_state(settings: &AppSettings, window_state: WindowState) -> AppSettings {
    let mut s = settings.clone();
    s.window_state = Some(window_state);
    s
}

/// Push multiple names in order, skipping failed indexes.
pub fn push_history_many(
    settings: &AppSettings,
    launched: &[LaunchItem],
    failed_indexes: &std::collections::HashSet<usize>,
) -> AppSettings {
    let mut current = settings.clone();
    for (i, item) in launched.iter().enumerate() {
        if !failed_indexes.contains(&i) {
            current = push_history_name(&current, &item.name);
        }
    }
    current
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn set_theme_replaces_value() {
        let s = set_theme(&AppSettings::default(), "dark");
        assert_eq!("dark", s.theme);
        assert!(!s.confirm_enabled);
    }

    #[test]
    fn set_confirm_enabled_replaces_value() {
        let s = set_confirm_enabled(&AppSettings::default(), true);
        assert!(s.confirm_enabled);
    }

    #[test]
    fn set_language_replaces_value() {
        let s = set_language(&AppSettings::default(), "zh-CN");
        assert_eq!("zh-CN", s.language);
    }

    #[test]
    fn push_history_name_prepends() {
        let s = push_history_name(&AppSettings::default(), "snow");
        assert_eq!(vec!["snow"], s.launch_history);
    }

    #[test]
    fn push_history_many_skips_failed_indexes() {
        let launched = [
            LaunchItem {
                name: "a".to_string(),
                directory: "D:\\x".to_string(),
                command: "snow".to_string(),
                confirm: false,
                id: "a".to_string(),
                selected: false,
                terminal: None,
                tag: None,
                group: None,
            },
            LaunchItem {
                name: "b".to_string(),
                directory: "D:\\x".to_string(),
                command: "snow".to_string(),
                confirm: false,
                id: "b".to_string(),
                selected: false,
                terminal: None,
                tag: None,
                group: None,
            },
        ];
        let failed: std::collections::HashSet<usize> = [1].into();
        let s = push_history_many(&AppSettings::default(), &launched, &failed);
        assert_eq!(vec!["a"], s.launch_history);
    }
}
