//! Ported 1:1 from C# JsonRoundTripTests (launchpad.Core.Tests).

use launchpad_tauri_lib::core::models::{AppSettings, LaunchItem, WindowState};

fn serde_pretty<T: serde::Serialize>(v: &T) -> String {
    serde_json::to_string_pretty(v).unwrap()
}

#[test]
fn launch_item_round_trips_all_fields() {
    let item = LaunchItem {
        name: "claude test".to_string(),
        directory: r"D:\projects\demo".to_string(),
        command: "claude --dangerously-skip-permissions".to_string(),
        confirm: true,
        id: "claude_test".to_string(),
        selected: true,
        terminal: Some("pwsh".to_string()),
        tag: Some("ai".to_string()),
        group: Some("dev".to_string()),
    };

    let json = serde_pretty(&item);
    let back: LaunchItem = serde_json::from_str(&json).unwrap();
    assert_eq!(item, back);
}

#[test]
fn launch_item_json_omits_null_optionals() {
    let item = LaunchItem {
        name: "plain".to_string(),
        directory: r"D:\x".to_string(),
        command: "echo hi".to_string(),
        confirm: false,
        id: "plain".to_string(),
        selected: false,
        terminal: None,
        tag: None,
        group: None,
    };

    let json = serde_pretty(&item);
    let lower = json.to_ascii_lowercase();
    assert!(!lower.contains("\"terminal\""));
    assert!(!lower.contains("\"tag\""));
    assert!(!lower.contains("\"group\""));
}

#[test]
fn launch_item_deserialize_missing_confirm_defaults_to_true() {
    let json = r#"{"name":"n","directory":"d","command":"c","id":"i"}"#;
    let item: LaunchItem = serde_json::from_str(json).unwrap();
    assert!(item.confirm);
}

#[test]
fn launch_item_deserialize_missing_required_field_throws() {
    let json = r#"{"name":"n","directory":"d","command":"c"}"#;
    let result: Result<LaunchItem, _> = serde_json::from_str(json);
    assert!(result.is_err());
}

#[test]
fn launch_item_deserialize_matches_legacy_config_file_shape() {
    let json = r#"
        [
          {
            "name": "snow-example",
            "directory": "D:\\projects\\your-project",
            "command": "snow",
            "confirm": false,
            "id": "snow-example",
            "selected": false
          },
          {
            "name": "codex-example",
            "directory": "D:\\projects\\your-project",
            "command": "codex --enable goals --dangerously-bypass-approvals-and-sandbox",
            "confirm": true,
            "id": "codex-example",
            "selected": false
          }
        ]
        "#;

    let items: Vec<LaunchItem> = serde_json::from_str(json).unwrap();
    assert_eq!(2, items.len());
    assert_eq!("snow-example", items[0].id);
    assert!(!items[0].confirm);
    assert!(items[1].confirm);
    assert_eq!(None, items[1].terminal);
}

#[test]
fn app_settings_round_trips_with_snake_case_keys() {
    let settings = AppSettings {
        confirm_enabled: true,
        theme: "dark".to_string(),
        language: "auto".to_string(),
        launch_history: vec!["a".to_string(), "b".to_string()],
        window_state: Some(WindowState {
            x: 10,
            y: 20,
            width: 900,
            height: 700,
        }),
        unknown: Default::default(),
    };

    let json = serde_pretty(&settings);
    assert!(json.contains("\"confirm_enabled\""));
    assert!(json.contains("\"launch_history\""));
    assert!(json.contains("\"window_state\""));
    assert!(json.contains("\"language\""));

    let back: AppSettings = serde_json::from_str(&json).unwrap();
    assert_eq!(settings.confirm_enabled, back.confirm_enabled);
    assert_eq!(settings.theme, back.theme);
    assert_eq!(settings.language, back.language);
    assert_eq!(settings.launch_history, back.launch_history);
    assert_eq!(settings.window_state, back.window_state);
}

#[test]
fn app_settings_deserialize_matches_legacy_settings_file_shape() {
    let json = r#"
        {
          "confirm_enabled": false,
          "theme": "light",
          "launch_history": ["claude_x", "snow_y"]
        }
        "#;

    let settings: AppSettings = serde_json::from_str(json).unwrap();
    assert!(!settings.confirm_enabled);
    assert_eq!("light", settings.theme);
    assert_eq!(vec!["claude_x", "snow_y"], settings.launch_history);
    assert_eq!(None, settings.window_state);
}

#[test]
fn app_settings_round_trip_preserves_unknown_fields() {
    let json = r#"{"confirm_enabled": true, "future_field": 42}"#;
    let settings: AppSettings = serde_json::from_str(json).unwrap();
    assert!(settings.unknown.contains_key("future_field"));
    assert_eq!(42, settings.unknown["future_field"].as_i64().unwrap());

    // Writing back preserves the unknown field.
    let json2 = serde_pretty(&settings);
    let settings2: AppSettings = serde_json::from_str(&json2).unwrap();
    assert!(settings2.unknown.contains_key("future_field"));
}

#[test]
fn app_settings_deserialize_missing_optional_fields_keep_defaults() {
    let json = r#"{"confirm_enabled":true}"#;
    let settings: AppSettings = serde_json::from_str(json).unwrap();
    assert!(settings.confirm_enabled);
    assert_eq!("system", settings.theme);
    assert_eq!("auto", settings.language);
    assert!(settings.launch_history.is_empty());
    assert_eq!(None, settings.window_state);
}
