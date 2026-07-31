//! ConfigIO: read/write config.json and settings.json.
//!
//! Replaces the C++ ConfigIO + Glaze reflection + FilesystemIface.
//! serde handles all serialization; std::fs handles I/O.
//! ~30 lines vs ~70 lines of C++.

use std::fs;
use std::path::PathBuf;

use anyhow::{Context, Result};

use crate::types::{AppSettings, LaunchItem};

/// Reads and writes launcher configuration from a config directory.
pub struct ConfigIO {
    config_dir: PathBuf,
}

impl ConfigIO {
    pub fn new(config_dir: PathBuf) -> Self {
        Self { config_dir }
    }

    pub fn read_items(&self) -> Result<Vec<LaunchItem>> {
        let path = self.config_dir.join("config.json");
        if !path.exists() {
            return Ok(Vec::new());
        }
        let content = fs::read_to_string(&path)
            .with_context(|| format!("Failed to read {}", path.display()))?;
        serde_json::from_str(&content)
            .with_context(|| format!("Failed to parse {}", path.display()))
    }

    pub fn write_items(&self, items: &[LaunchItem]) -> Result<()> {
        let path = self.config_dir.join("config.json");
        // Backup existing
        if path.exists() {
            let _ = fs::copy(&path, self.config_dir.join("config.json.bak"));
        }
        let json = serde_json::to_string_pretty(items)?;
        fs::write(&path, json).with_context(|| format!("Failed to write {}", path.display()))
    }

    pub fn read_settings(&self) -> Result<AppSettings> {
        let path = self.config_dir.join("settings.json");
        if !path.exists() {
            return Ok(AppSettings::default());
        }
        let content = fs::read_to_string(&path)
            .with_context(|| format!("Failed to read {}", path.display()))?;
        serde_json::from_str(&content)
            .with_context(|| format!("Failed to parse {}", path.display()))
    }

    pub fn write_settings(&self, settings: &AppSettings) -> Result<()> {
        let path = self.config_dir.join("settings.json");
        let json = serde_json::to_string_pretty(settings)?;
        fs::write(&path, json).with_context(|| format!("Failed to write {}", path.display()))
    }
}

impl Default for AppSettings {
    fn default() -> Self {
        Self {
            confirm_enabled: false,
            theme: "system".to_string(),
            launch_history: Vec::new(),
            window_state: None,
        }
    }
}
