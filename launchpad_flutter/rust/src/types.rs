//! Types: data model for serde serialization and Flutter bridge (flutter_rust_bridge).

use serde::{Deserialize, Serialize};

/// A launch item — one entry in the launcher list.
#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct LaunchItem {
    pub name: String,
    pub directory: String,
    pub command: String,
    #[serde(default = "default_bool_true")]
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

fn default_bool_true() -> bool {
    true
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AppSettings {
    #[serde(default, alias = "confirmEnabled")]
    pub confirm_enabled: bool,
    #[serde(default = "default_theme")]
    pub theme: String,
    #[serde(default)]
    pub launch_history: Vec<String>,
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub window_state: Option<WindowState>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
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

fn default_width() -> u32 { 800 }
fn default_height() -> u32 { 600 }
fn default_theme() -> String { "system".to_string() }

#[derive(Debug, Clone)]
pub struct LaunchPlan {
    pub executable: String,
    pub args: Vec<String>,
    pub working_dir: String,
    pub is_dangerous: bool,
    pub terminal_override: Option<String>,
}
