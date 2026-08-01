//! Data model. JSON shape mirrors the C# models byte-for-byte: snake_case keys,
//! legacy defaults (missing confirm -> true), optional fields omitted when None.

use serde::{Deserialize, Serialize};

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub struct LaunchItem {
    pub name: String,
    pub directory: String,
    pub command: String,
    #[serde(default = "default_true")]
    pub confirm: bool,
    pub id: String,
    #[serde(default)]
    pub selected: bool,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub terminal: Option<String>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub tag: Option<String>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub group: Option<String>,
}

fn default_true() -> bool {
    true
}

impl LaunchItem {
    /// Dangerous flags are computed, never serialized (mirrors C# [JsonIgnore]).
    pub fn is_dangerous(&self) -> bool {
        danger::is_dangerous(&self.command)
    }

    pub fn danger_reason(&self) -> Option<LanguageKey> {
        danger::dangerous_reason(&self.command)
    }
}

use crate::core::danger;
use crate::core::i18n::LanguageKey;

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub struct AppSettings {
    pub confirm_enabled: bool,
    #[serde(default = "default_theme")]
    pub theme: String,
    #[serde(default = "default_language")]
    pub language: String,
    #[serde(default)]
    pub launch_history: Vec<String>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub window_state: Option<WindowState>,
    /// Unknown keys are preserved so writes never drop data from future
    /// versions (mirrors C# JsonExtensionData).
    #[serde(flatten)]
    pub unknown: std::collections::HashMap<String, serde_json::Value>,
}

fn default_theme() -> String {
    "system".to_string()
}

fn default_language() -> String {
    "auto".to_string()
}

impl Default for AppSettings {
    fn default() -> Self {
        Self {
            confirm_enabled: false,
            theme: default_theme(),
            language: default_language(),
            launch_history: Vec::new(),
            window_state: None,
            unknown: std::collections::HashMap::new(),
        }
    }
}

#[derive(Debug, Clone, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "snake_case")]
pub struct WindowState {
    #[serde(default)]
    pub x: i32,
    #[serde(default)]
    pub y: i32,
    #[serde(default = "default_width")]
    pub width: u32,
    #[serde(default = "default_height")]
    pub height: u32,
}

fn default_width() -> u32 {
    800
}

fn default_height() -> u32 {
    600
}

/// In-memory launch decision; never serialized.
#[derive(Debug, Clone, PartialEq)]
pub struct LaunchPlan {
    pub executable: String,
    pub args: Vec<String>,
    pub working_directory: String,
    pub is_dangerous: bool,
    pub terminal_override: Option<String>,
}

#[cfg(test)]
mod tests {
    use super::*;

    /// Serialization must match the C# output byte-for-byte: snake_case keys,
    /// declaration-order fields, indented, optional None fields omitted.
    #[test]
    fn launch_item_serializes_snake_case_in_order() {
        let item = LaunchItem {
            name: "demo".to_string(),
            directory: r"D:\projects\demo dir".to_string(),
            command: "snow --dangerously".to_string(),
            confirm: false,
            id: "demo".to_string(),
            selected: false,
            terminal: None,
            tag: None,
            group: None,
        };
        let json = serde_json::to_string_pretty(&item).unwrap();
        assert!(json.contains("\"name\": \"demo\""));
        assert!(json.contains("\"directory\": \"D:\\\\projects\\\\demo dir\""));
        assert!(json.contains("\"command\": \"snow --dangerously\""));
        assert!(json.contains("\"confirm\": false"));
        assert!(json.contains("\"id\": \"demo\""));
        assert!(json.contains("\"selected\": false"));
        assert!(!json.contains("terminal"));
        assert!(!json.contains("tag"));
        assert!(!json.contains("group"));
        // Field order: name must precede directory which precedes command...
        assert!(json.find("\"name\"").unwrap() < json.find("\"directory\"").unwrap());
        assert!(json.find("\"directory\"").unwrap() < json.find("\"command\"").unwrap());
        assert!(json.find("\"command\"").unwrap() < json.find("\"confirm\"").unwrap());
        assert!(json.find("\"confirm\"").unwrap() < json.find("\"id\"").unwrap());
        assert!(json.find("\"id\"").unwrap() < json.find("\"selected\"").unwrap());
    }

    #[test]
    fn launch_item_optional_fields_serialized_when_present() {
        let mut item = LaunchItem {
            name: "legacy".to_string(),
            directory: r"D:\projects\demo dir".to_string(),
            command: "snow".to_string(),
            confirm: true,
            id: "legacy".to_string(),
            selected: false,
            terminal: None,
            tag: None,
            group: None,
        };
        item.terminal = Some("cmd".to_string());
        item.tag = Some("internal".to_string());
        item.group = Some("dev".to_string());
        let json = serde_json::to_string_pretty(&item).unwrap();
        assert!(json.contains("\"terminal\": \"cmd\""));
        assert!(json.contains("\"tag\": \"internal\""));
        assert!(json.contains("\"group\": \"dev\""));
        assert!(json.find("\"selected\"").unwrap() < json.find("\"terminal\"").unwrap());
    }

    #[test]
    fn missing_confirm_defaults_to_true() {
        let json = r#"{"name":"n","directory":"d","command":"c","id":"i"}"#;
        let item: LaunchItem = serde_json::from_str(json).unwrap();
        assert!(item.confirm);
        assert!(!item.selected);
    }

    #[test]
    fn app_settings_serializes_in_order_with_defaults() {
        let settings = AppSettings {
            confirm_enabled: true,
            theme: "dark".to_string(),
            language: "auto".to_string(),
            launch_history: vec!["recent".to_string(), "old".to_string()],
            window_state: Some(WindowState {
                x: 100,
                y: 200,
                width: 900,
                height: 700,
            }),
            unknown: Default::default(),
        };
        let json = serde_json::to_string_pretty(&settings).unwrap();
        assert!(json.contains("\"confirm_enabled\": true"));
        assert!(json.contains("\"theme\": \"dark\""));
        assert!(json.contains("\"language\": \"auto\""));
        assert!(json.contains("\"launch_history\": ["));
        assert!(json.contains("\"window_state\": {"));
        assert!(json.contains("\"x\": 100"));
        assert!(json.find("\"confirm_enabled\"").unwrap() < json.find("\"theme\"").unwrap());
        assert!(json.find("\"theme\"").unwrap() < json.find("\"language\"").unwrap());
        assert!(json.find("\"language\"").unwrap() < json.find("\"launch_history\"").unwrap());
        assert!(json.find("\"launch_history\"").unwrap() < json.find("\"window_state\"").unwrap());
    }

    #[test]
    fn app_settings_missing_fields_use_defaults() {
        let json = r#"{"confirm_enabled":false}"#;
        let settings: AppSettings = serde_json::from_str(json).unwrap();
        assert_eq!("system", settings.theme);
        assert_eq!("auto", settings.language);
        assert!(settings.launch_history.is_empty());
        assert!(settings.window_state.is_none());
    }

    #[test]
    fn window_state_defaults_width_height() {
        let json = r#"{"x":1,"y":2}"#;
        let state: WindowState = serde_json::from_str(json).unwrap();
        assert_eq!(800, state.width);
        assert_eq!(600, state.height);
    }
}
