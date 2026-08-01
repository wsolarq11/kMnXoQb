//! Ported 1:1 from C# ConfigStoreTests (launchpad.Core.Tests).

use std::path::PathBuf;

use launchpad_tauri_lib::config::store::ConfigStore;
use launchpad_tauri_lib::core::errors::AppError;
use launchpad_tauri_lib::core::models::{AppSettings, LaunchItem, WindowState};

fn scratch() -> PathBuf {
    let dir = std::env::temp_dir().join(format!(
        "launchpad-tests-{}",
        std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos()
    ));
    std::fs::create_dir_all(&dir).unwrap();
    dir
}

fn cleanup(dir: &PathBuf) {
    let _ = std::fs::remove_dir_all(dir);
}

fn item(name: &str) -> LaunchItem {
    LaunchItem {
        name: name.to_string(),
        directory: r"D:\x".to_string(),
        command: "snow".to_string(),
        confirm: true,
        id: name.to_string(),
        selected: false,
        terminal: None,
        tag: None,
        group: None,
    }
}

fn config_parse_err(err: &AppError) -> (&str, &str) {
    match err {
        AppError::ConfigParse { path, detail } => (path.as_str(), detail.as_str()),
        other => panic!("expected ConfigParse, got {other:?}"),
    }
}

#[test]
fn read_items_returns_empty_when_file_missing() {
    let dir = scratch();
    let store = ConfigStore::new(&dir);
    assert!(store.read_items().unwrap().is_empty());
    cleanup(&dir);
}

#[test]
fn constructor_creates_config_directory() {
    let dir = scratch();
    let nested = dir.join("a").join("b");
    let store = ConfigStore::new(&nested);
    assert!(nested.is_dir());
    assert!(store.read_items().is_ok());
    cleanup(&dir);
}

#[test]
fn write_then_read_items_round_trips() {
    let dir = scratch();
    let store = ConfigStore::new(&dir);
    store.write_items(&[item("a"), item("b")]).unwrap();

    let items = store.read_items().unwrap();
    assert_eq!(2, items.len());
    assert_eq!("a", items[0].name);
    cleanup(&dir);
}

#[test]
fn write_items_creates_backup_before_overwrite() {
    let dir = scratch();
    let store = ConfigStore::new(&dir);
    store.write_items(&[item("v1")]).unwrap();
    store.write_items(&[item("v2")]).unwrap();

    let backup = std::fs::read_to_string(dir.join("config.json.bak")).unwrap();
    assert!(backup.contains("v1"));
    cleanup(&dir);
}

#[test]
fn read_items_corrupt_file_raises_config_parse() {
    let dir = scratch();
    std::fs::write(dir.join("config.json"), "{ not json").unwrap();
    let store = ConfigStore::new(&dir);

    let err = store.read_items().unwrap_err();
    let (path, _) = config_parse_err(&err);
    assert!(path.contains("config.json"));
    cleanup(&dir);
}

#[test]
fn read_items_corrupt_with_valid_backup_recovers_and_notes() {
    let dir = scratch();
    let store = ConfigStore::new(&dir);
    // Two writes so a backup exists (the first write has nothing to back up).
    store.write_items(&[item("good")]).unwrap();
    store.write_items(&[item("good"), item("backup")]).unwrap();
    std::fs::write(dir.join("config.json"), "{ not json").unwrap();

    let items = store.read_items().unwrap();

    // The backup holds the pre-overwrite state (first write: [good]).
    assert_eq!(1, items.len());
    assert_eq!("good", items[0].name);
    assert!(store.last_recovery_note_key().is_some());
    // The good backup must survive recovery (the recovery path must not
    // overwrite it with the corrupt file).
    let backup = std::fs::read_to_string(dir.join("config.json.bak")).unwrap();
    assert!(backup.contains("good"));
    cleanup(&dir);
}

#[test]
fn read_items_corrupt_with_corrupt_backup_raises_with_both_details() {
    let dir = scratch();
    std::fs::write(dir.join("config.json"), "{ not json").unwrap();
    std::fs::write(dir.join("config.json.bak"), "also not json").unwrap();
    let store = ConfigStore::new(&dir);

    let err = store.read_items().unwrap_err();
    let (_, detail) = config_parse_err(&err);
    assert!(detail.contains("backup also unreadable"));
    cleanup(&dir);
}

#[test]
fn read_items_no_backup_raises_mentioning_missing_backup() {
    let dir = scratch();
    std::fs::write(dir.join("config.json"), "{ not json").unwrap();
    let store = ConfigStore::new(&dir);

    let err = store.read_items().unwrap_err();
    let (_, detail) = config_parse_err(&err);
    assert!(detail.contains("no backup available"));
    cleanup(&dir);
}

#[test]
fn read_settings_returns_defaults_when_file_missing() {
    let dir = scratch();
    let store = ConfigStore::new(&dir);
    let settings = store.read_settings().unwrap();

    assert!(!settings.confirm_enabled);
    assert_eq!("system", settings.theme);
    assert!(settings.launch_history.is_empty());
    cleanup(&dir);
}

#[test]
fn write_then_read_settings_round_trips() {
    let dir = scratch();
    let store = ConfigStore::new(&dir);
    store
        .write_settings(&AppSettings {
            confirm_enabled: true,
            theme: "dark".to_string(),
            language: "auto".to_string(),
            launch_history: vec!["a".to_string()],
            window_state: Some(WindowState {
                x: 1,
                y: 2,
                width: 900,
                height: 700,
            }),
            unknown: Default::default(),
        })
        .unwrap();

    let settings = store.read_settings().unwrap();
    assert!(settings.confirm_enabled);
    assert_eq!("dark", settings.theme);
    assert_eq!(vec!["a"], settings.launch_history);
    assert_eq!(
        Some(WindowState {
            x: 1,
            y: 2,
            width: 900,
            height: 700,
        }),
        settings.window_state
    );
    cleanup(&dir);
}

#[test]
fn write_items_is_atomic_no_partial_file_on_failure() {
    // The atomic-write defense: after a successful write there is no .tmp
    // leftover, and the rename replaced the previous file wholesale.
    let dir = scratch();
    let store = ConfigStore::new(&dir);
    store.write_items(&[item("a")]).unwrap();
    assert!(!dir.join("config.json.tmp").exists());
    let content = std::fs::read_to_string(dir.join("config.json")).unwrap();
    assert!(content.contains("\"name\": \"a\""));
    cleanup(&dir);
}
