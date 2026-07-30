//! Types: single source of truth for data model, serialization, and schema.
//!
//! `#[derive(JsonSchema)]` on these types generates JSON Schema automatically.
//! `#[derive(Serialize, Deserialize)]` handles config.json + settings.json.
//! Changing a field here updates the schema, the GUI form, and the CLI parser
//! — no manual synchronization needed.

use schemars::JsonSchema;
use serde::{Deserialize, Serialize};

/// A launch item — one entry in the launcher list.
#[derive(Debug, Clone, Serialize, Deserialize, JsonSchema)]
pub struct LaunchItem {
    /// Display name shown in the launcher UI.
    #[schemars(length(min = 1), description = "Display name")]
    pub name: String,

    /// Working directory in which the command runs.
    #[schemars(description = "Working directory")]
    pub directory: String,

    /// Shell command to execute (argv, zero-shell).
    #[schemars(length(min = 1), description = "Command to execute")]
    pub command: String,

    /// Per-item confirmation before launch.
    #[serde(default = "default_bool_true")]
    #[schemars(description = "Show confirmation dialog before launch")]
    pub confirm: bool,

    /// Unique identifier — auto-generated from name if not set.
    #[schemars(description = "Unique item identifier")]
    pub id: String,

    /// Batch selection state (transient, persisted to config).
    #[serde(default)]
    pub selected: bool,

    /// Override the default terminal (e.g. "pwsh", "gnome-terminal").
    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[schemars(description = "Optional terminal override")]
    pub terminal: Option<String>,

    /// Tag for filtering / categorization.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[schemars(description = "Tag for filtering")]
    pub tag: Option<String>,

    /// Group identifier for organizational grouping.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    #[schemars(description = "Group identifier")]
    pub group: Option<String>,
}

fn default_bool_true() -> bool {
    true
}

/// App settings persisted to settings.json.
#[derive(Debug, Clone, Serialize, Deserialize, JsonSchema)]
pub struct AppSettings {
    /// Global confirm toggle (overrides per-item confirm when false).
    #[serde(default, alias = "confirmEnabled")]
    #[schemars(description = "Enable confirmation dialogs")]
    pub confirm_enabled: bool,

    /// Theme: "light", "dark", or "system".
    #[serde(default = "default_theme")]
    #[schemars(description = "Theme: light, dark, or system")]
    pub theme: String,

    /// Recently launched item names (max 10).
    #[serde(default)]
    #[schemars(description = "Recent launch history (max 10)")]
    pub launch_history: Vec<String>,

    /// Persisted window position and size.
    #[serde(default, skip_serializing_if = "Option::is_none")]
    pub window_state: Option<WindowState>,
}

/// Window position and size for persistence.
#[derive(Debug, Clone, Serialize, Deserialize, JsonSchema)]
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

fn default_theme() -> String {
    "system".to_string()
}

/// Result of a launch plan (used for dry-run display).
#[derive(Debug)]
pub struct LaunchPlan {
    pub executable: String,
    pub args: Vec<String>,
    pub working_dir: String,
    pub is_dangerous: bool,
    pub terminal_override: Option<String>,
}
