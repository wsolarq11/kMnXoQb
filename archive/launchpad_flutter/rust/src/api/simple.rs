//! API bridge: synchronous Rust functions callable from Flutter.
//! Add #[flutter_rust_bridge::frb(sync)] to expose to Dart codegen.

use crate::config::ConfigIO;
use crate::types::{LaunchItem, LaunchPlan, AppSettings};

#[flutter_rust_bridge::frb(sync)]
pub fn read_items(config_dir: String) -> Vec<LaunchItem> {
    let io = ConfigIO::new(config_dir.into());
    io.read_items().unwrap_or_default()
}

#[flutter_rust_bridge::frb(sync)]
pub fn write_items(config_dir: String, items: Vec<LaunchItem>) -> Result<(), String> {
    let io = ConfigIO::new(config_dir.into());
    io.write_items(&items).map_err(|e| e.to_string())
}

#[flutter_rust_bridge::frb(sync)]
pub fn read_settings(config_dir: String) -> AppSettings {
    let io = ConfigIO::new(config_dir.into());
    io.read_settings().unwrap_or_default()
}

#[flutter_rust_bridge::frb(sync)]
pub fn write_settings(config_dir: String, settings: AppSettings) -> Result<(), String> {
    let io = ConfigIO::new(config_dir.into());
    io.write_settings(&settings).map_err(|e| e.to_string())
}

#[flutter_rust_bridge::frb(sync)]
pub fn launch_item(item: LaunchItem) -> Result<bool, String> {
    crate::launch::launch(&item)
        .map(|_| true)
        .map_err(|e| e.to_string())
}

#[flutter_rust_bridge::frb(sync)]
pub fn is_dangerous_cmd(command: String) -> bool {
    crate::launch::is_dangerous(&command)
}

#[flutter_rust_bridge::frb(sync)]
pub fn dangerous_reason_str(command: String) -> Option<String> {
    crate::launch::dangerous_reason(&command).map(|s| s.to_string())
}

#[flutter_rust_bridge::frb(sync)]
pub fn plan_launch(item: LaunchItem) -> LaunchPlan {
    crate::launch::plan(&item)
}

#[flutter_rust_bridge::frb(init)]
pub fn init_app() {
    flutter_rust_bridge::setup_default_user_utils();
}
