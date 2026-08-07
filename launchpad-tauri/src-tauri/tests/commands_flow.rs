//! Command-layer end-to-end flow tests: every item/settings/launch command is
//! driven through the real AppState (real ConfigStore on a temp dir, real
//! spawner/detector) via the runtime-free `*_impl` functions. The launch
//! SUCCESS path is covered by the spawner contract tests (it would pop real
//! terminal windows here); this file covers the failure path (missing
//! directory -> structured error, no history update) and the full
//! CRUD/settings/language flows.

use std::sync::Arc;

use launchpad_tauri_lib::app::items::ItemService;
use launchpad_tauri_lib::app::settings::SettingsService;
use launchpad_tauri_lib::commands;
use launchpad_tauri_lib::config::store::ConfigStore;
use launchpad_tauri_lib::core::launch::LaunchService;
use launchpad_tauri_lib::core::models::LaunchItem;
use launchpad_tauri_lib::infra::spawner::SystemProcessSpawner;
use launchpad_tauri_lib::infra::terminal::TerminalDetector;
use launchpad_tauri_lib::state::AppState;

fn scratch() -> std::path::PathBuf {
    let dir = std::env::temp_dir().join(format!(
        "launchpad-cmd-{}",
        std::time::SystemTime::now()
            .duration_since(std::time::UNIX_EPOCH)
            .unwrap()
            .as_nanos()
    ));
    let _ = std::fs::remove_dir_all(&dir);
    std::fs::create_dir_all(&dir).unwrap();
    dir
}

fn test_state() -> (AppState, std::path::PathBuf) {
    let dir = scratch();
    let store = Arc::new(ConfigStore::new(&dir));
    let state = AppState {
        items: ItemService::new(Arc::clone(&store)),
        settings: SettingsService::new(store),
        launch: LaunchService::new(SystemProcessSpawner, TerminalDetector::new()),
    };
    (state, dir)
}

fn input(name: &str, dir: &str, command: &str) -> commands::items::ItemInput {
    commands::items::ItemInput {
        name: name.to_string(),
        directory: dir.to_string(),
        command: command.to_string(),
        confirm: false,
        terminal: None,
    }
}

#[test]
fn full_crud_flow_persists_and_round_trips() {
    let (state, dir) = test_state();

    // Create two items.
    let a = commands::items::create_item_impl(&state, input("my tool", r"D:\x", "snow"))
        .expect("create a");
    assert_eq!("my_tool", a.id);
    let b = commands::items::create_item_impl(&state, input("beta", r"D:\y", "snow run"))
        .expect("create b");
    assert_eq!("beta", b.id);

    // List reflects both.
    let payload = commands::items::list_items_impl(&state);
    assert_eq!(2, payload.items.len());
    assert!(payload.error.is_none());

    // Update b.
    let updated = commands::items::update_item_impl(
        &state,
        b.id.clone(),
        input("beta2", r"D:\z", "opencode"),
    )
    .expect("update b");
    assert_eq!("beta2", updated.name);
    assert_eq!("opencode", updated.command);

    // Move b up (index 1 -> 0); a is already at the top edge.
    commands::items::move_item_impl(&state, b.id.clone(), -1).expect("move b");
    let after_move = commands::items::list_items_impl(&state);
    assert_eq!("beta2", after_move.items[0].name);
    assert_eq!("my tool", after_move.items[1].name);

    // Select via target state, then toggle all.
    commands::items::set_select_impl(&state, a.id.clone(), true).expect("select a");
    let selected = commands::items::list_items_impl(&state);
    assert!(selected.items[1].selected);

    // Delete b.
    commands::items::delete_item_impl(&state, b.id.clone()).expect("delete b");
    let after_delete = commands::items::list_items_impl(&state);
    assert_eq!(1, after_delete.items.len());
    assert_eq!("my_tool", after_delete.items[0].id);

    // Persisted on disk (fresh read).
    let reloaded: Vec<LaunchItem> =
        serde_json::from_str(&std::fs::read_to_string(dir.join("config.json")).unwrap()).unwrap();
    assert_eq!(1, reloaded.len());
    assert_eq!("my_tool", reloaded[0].id);
}

#[test]
fn needs_confirm_flags_dangerous_command() {
    let (state, _) = test_state();
    let item = commands::items::create_item_impl(
        &state,
        input(
            "codex",
            r"D:\x",
            "codex --dangerously-bypass-approvals-and-sandbox",
        ),
    )
    .expect("create");
    commands::items::set_select_impl(&state, item.id.clone(), true).expect("select");

    // confirm_enabled defaults to false -> no confirm needed even when dangerous.
    let info = commands::launch::needs_confirm_impl(&state, item.id.clone()).expect("info");
    assert!(!info.needs_confirm);
    assert_eq!(
        Some(launchpad_tauri_lib::core::i18n::LanguageKey::DangerReasonDangerously),
        info.danger_key
    );

    // Global confirm on -> needs confirm.
    commands::settings::set_confirm_enabled_impl(&state, true).expect("enable confirm");
    let info2 = commands::launch::needs_confirm_impl(&state, item.id).expect("info2");
    assert!(info2.needs_confirm);
}

#[test]
fn launch_item_missing_directory_returns_structured_error_without_history() {
    let (state, _) = test_state();
    let missing = std::env::temp_dir().join("launchpad-definitely-missing-cmd-flow");
    let item = commands::items::create_item_impl(
        &state,
        input("broken", &missing.display().to_string(), "snow"),
    )
    .expect("create");

    let err = commands::launch::launch_item_impl(&state, item.id).expect_err("should fail");
    assert_eq!("Launch.WorkingDirectoryMissing", err.kind());
    assert!(err
        .description()
        .contains("launchpad-definitely-missing-cmd-flow"));

    // History must NOT contain the failed item (success-only semantics).
    let settings = commands::settings::get_settings_impl(&state).expect("settings");
    assert!(settings.launch_history.is_empty());
}

#[test]
fn launch_many_all_failing_returns_zero_success() {
    let (state, _) = test_state();
    let missing = std::env::temp_dir().join("launchpad-definitely-missing-cmd-many");
    let a = commands::items::create_item_impl(
        &state,
        input("a", &missing.display().to_string(), "snow"),
    )
    .expect("create a");
    let b = commands::items::create_item_impl(
        &state,
        input("b", &missing.display().to_string(), "snow"),
    )
    .expect("create b");

    let result =
        commands::launch::launch_many_impl(&state, vec![a.id.clone(), b.id]).expect("many");
    assert_eq!(0, result.succeeded);
    assert_eq!(vec![0, 1], result.failed_indexes);

    // Selection cleared after batch (legacy behavior).
    let after = commands::items::list_items_impl(&state);
    assert!(after.items.iter().all(|i| !i.selected));
    let _ = a.id;
}

#[test]
fn launch_many_skips_items_with_missing_directory() {
    let (state, _) = test_state();
    // One item with a missing directory, one with an existing one (temp).
    let missing = std::env::temp_dir().join("launchpad-definitely-missing-dir-many");
    let existing = std::env::temp_dir();
    let a = commands::items::create_item_impl(
        &state,
        input("a", &missing.display().to_string(), "snow"),
    )
    .expect("create a (missing dir)");
    // `exit` self-terminates: plan_windows wraps commands as
    // `pwsh -NoExit -Command "cd ...; <cmd>"` — a bare `pwsh.exe` would enter
    // interactive mode and leak a permanent process + window per test run.
    let b = commands::items::create_item_impl(
        &state,
        input("b", &existing.display().to_string(), "exit"),
    )
    .expect("create b (valid dir)");

    let result =
        commands::launch::launch_many_impl(&state, vec![a.id.clone(), b.id.clone()]).expect("many");
    // a is blocked by the directory pre-check; b launches.
    assert_eq!(1, result.succeeded);
    assert!(
        result.failed_indexes.contains(&0),
        "a must be in failed set"
    );

    // Selection cleared after batch.
    let after = commands::items::list_items_impl(&state);
    assert!(after.items.iter().all(|i| !i.selected));
    let _ = (a.id, b.id);
}

#[test]
fn settings_cycles_theme_and_language() {
    let (state, _) = test_state();

    // Theme: system -> dark -> light -> system.
    let s1 = commands::settings::toggle_theme_impl(&state).expect("theme 1");
    assert_eq!("dark", s1.theme);
    let s2 = commands::settings::toggle_theme_impl(&state).expect("theme 2");
    assert_eq!("light", s2.theme);
    let s3 = commands::settings::toggle_theme_impl(&state).expect("theme 3");
    assert_eq!("system", s3.theme);

    // Language: auto -> zh-CN -> en-US -> auto.
    let l1 = commands::settings::toggle_language_impl(&state).expect("lang 1");
    assert_eq!("zh-CN", l1.language);
    let l2 = commands::settings::toggle_language_impl(&state).expect("lang 2");
    assert_eq!("en-US", l2.language);
    let l3 = commands::settings::toggle_language_impl(&state).expect("lang 3");
    assert_eq!("auto", l3.language);

    // get_language resolves the effective language (explicit wins over system).
    let resolved = commands::misc::get_language_impl(&state).expect("resolved");
    assert_eq!("auto", resolved.setting);
    assert!(matches!(
        resolved.effective,
        launchpad_tauri_lib::core::i18n::AppLanguage::ZhCn
            | launchpad_tauri_lib::core::i18n::AppLanguage::EnUs
    ));
}

#[test]
fn toggle_select_all_and_clear_round_trip() {
    let (state, _) = test_state();
    let a = commands::items::create_item_impl(&state, input("a", r"D:\x", "snow")).expect("a");
    let b = commands::items::create_item_impl(&state, input("b", r"D:\x", "snow")).expect("b");

    commands::items::toggle_select_all_impl(&state).expect("select all");
    let all = commands::items::list_items_impl(&state);
    assert!(all.items.iter().all(|i| i.selected));

    commands::items::toggle_select_all_impl(&state).expect("deselect all");
    let none = commands::items::list_items_impl(&state);
    assert!(none.items.iter().all(|i| !i.selected));

    // Per-item target state stays idempotent.
    commands::items::set_select_impl(&state, a.id.clone(), true).expect("select a");
    commands::items::set_select_impl(&state, a.id.clone(), true).expect("select a again");
    let after = commands::items::list_items_impl(&state);
    assert!(after.items[0].selected);
    assert!(!after.items[1].selected);
    let _ = (b, a);
}
