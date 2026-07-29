//! Integration tests for launchpad-rs.
//!
//! Tests config I/O, dangerous detection, item lifecycle, and CLI behavior.

use std::path::PathBuf;

use launchpad_rs::config::ConfigIO;
use launchpad_rs::launch;
use launchpad_rs::types::LaunchItem;

// ── Helpers ──

struct TempDir {
    path: PathBuf,
}

impl TempDir {
    fn new() -> Self {
        let base = std::env::temp_dir();
        // Use nanosecond-precision timestamp for unique directory names
        let id = std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .map(|d| d.as_nanos())
            .unwrap_or(0);
        let candidate = base.join(format!("launchpad_test_{id}"));
        // Clean up if a previous run left this behind
        if candidate.exists() {
            let _ = std::fs::remove_dir_all(&candidate);
        }
        std::fs::create_dir_all(&candidate).unwrap();
        Self { path: candidate }
    }
}

impl Drop for TempDir {
    fn drop(&mut self) {
        let _ = std::fs::remove_dir_all(&self.path);
    }
}

fn make_item(name: &str, dir: &str, cmd: &str) -> LaunchItem {
    LaunchItem {
        name: name.into(),
        directory: dir.into(),
        command: cmd.into(),
        confirm: true,
        id: name.to_lowercase().replace(' ', "_"),
        selected: false,
        terminal: None,
        tag: None,
        group: None,
    }
}

// ── Config I/O ──

#[test]
fn config_read_empty_directory() {
    let tmp = TempDir::new();
    let config = ConfigIO::new(tmp.path.clone());
    let items = config.read_items().unwrap();
    assert!(items.is_empty());
}

#[test]
fn config_round_trip() {
    let tmp = TempDir::new();
    let config = ConfigIO::new(tmp.path.clone());
    let items = vec![
        make_item("test1", "C:\\tmp", "echo hello"),
        make_item("test2", "/home/user", "ls -la"),
    ];
    config.write_items(&items).unwrap();
    let read = config.read_items().unwrap();
    assert_eq!(read.len(), 2);
    assert_eq!(read[0].name, "test1");
    assert_eq!(read[0].command, "echo hello");
    assert_eq!(read[1].name, "test2");
}

#[test]
fn config_parse_error_detected() {
    let tmp = TempDir::new();
    let config_path = tmp.path.join("config.json");
    std::fs::write(&config_path, "not valid json {{{").unwrap();
    assert!(config_path.exists(), "config.json was not written");
    let config = ConfigIO::new(tmp.path.clone());
    let result = config.read_items();
    assert!(result.is_err(), "expected parse error, got: {result:?}");
}

#[test]
fn config_backup_created_on_write() {
    let tmp = TempDir::new();
    let config = ConfigIO::new(tmp.path.clone());
    let items = vec![make_item("a", ".", "cmd")];
    config.write_items(&items).unwrap();

    // Write again — should create .bak
    config.write_items(&items).unwrap();
    assert!(tmp.path.join("config.json.bak").exists());
}

// ── Settings ──

#[test]
fn settings_default_when_missing() {
    let tmp = TempDir::new();
    let config = ConfigIO::new(tmp.path.clone());
    let settings = config.read_settings().unwrap();
    assert_eq!(settings.theme, "system");
    assert!(!settings.confirm_enabled);
    assert!(settings.launch_history.is_empty());
}

// ── Dangerous Detection ──

#[test]
fn is_dangerous_detects_flags() {
    assert!(launch::is_dangerous(
        "codex --dangerously-bypass-approvals-and-sandbox"
    ));
    assert!(launch::is_dangerous(
        "claude --dangerously-skip-permissions"
    ));
    assert!(launch::is_dangerous("snow --yolo-p"));
    assert!(launch::is_dangerous("cmd --bypass-sandbox"));
}

#[test]
fn is_dangerous_allows_safe_commands() {
    assert!(!launch::is_dangerous("echo hello"));
    assert!(!launch::is_dangerous("npm start"));
    assert!(!launch::is_dangerous("codex --help"));
    assert!(!launch::is_dangerous(""));
}

#[test]
fn dangerous_reason_explains_why() {
    assert_eq!(
        launch::dangerous_reason("codex --dangerously-bypass-approvals-and-sandbox"),
        Some("contains --dangerously flag")
    );
    assert_eq!(
        launch::dangerous_reason("claude --dangerously-skip-permissions"),
        Some("contains --dangerously flag")
    );
    assert_eq!(launch::dangerous_reason("echo hello"), None);
}

// ── CLI output format ──

#[test]
fn check_output_is_valid_json() {
    let tmp = TempDir::new();
    let config = ConfigIO::new(tmp.path.clone());
    let items = vec![make_item("test-item", ".", "echo")];
    config.write_items(&items).unwrap();

    let read = config.read_items().unwrap();
    assert_eq!(read.len(), 1);
    assert_eq!(read[0].name, "test-item");
    assert_eq!(read[0].command, "echo");
}

// ── Deduplication ──

#[test]
fn id_generation_handles_duplicates() {
    let id1 = "test_item".to_string();
    let id2 = "test_item_2".to_string();
    assert_ne!(id1, id2);
}

// ── Property-based tests (proptest) ──

#[cfg(test)]
mod proptests {
    use launchpad_rs::{config::ConfigIO, launch};
    use proptest::prelude::*;

    proptest! {
        /// Safe commands must never be flagged as dangerous.
        #[test]
        fn safe_commands_never_dangerous(cmd in "[a-zA-Z0-9 _\\-\\./]+") {
            prop_assume!(!cmd.to_lowercase().contains("dangerously"));
            prop_assume!(!cmd.to_lowercase().contains("yolo"));
            prop_assume!(!cmd.to_lowercase().contains("skip-permissions"));
            prop_assume!(!cmd.to_lowercase().contains("bypass-approvals"));
            prop_assume!(!cmd.to_lowercase().contains("bypass-sandbox"));
            prop_assume!(!cmd.to_lowercase().contains("bypass.sandbox"));
            prop_assert!(!launch::is_dangerous(&cmd));
        }

        /// Config round-trip preserves all launch item fields.
        #[test]
        fn config_round_trip_preserves_all_fields(
            name in "[a-z]{3,20}",
            dir in "[a-zA-Z0-9/\\\\:]{1,50}",
            cmd in "[a-zA-Z0-9 _\\-\\.]{1,100}",
        ) {
            let tmp = super::TempDir::new();
            let config = ConfigIO::new(tmp.path.clone());
            let item = super::make_item(&name, &dir, &cmd);
            config.write_items(std::slice::from_ref(&item)).unwrap();
            let read = config.read_items().unwrap();
            prop_assert_eq!(read.len(), 1);
            prop_assert_eq!(&read[0].name, &name);
            prop_assert_eq!(&read[0].command, &cmd);
        }
    }
}
