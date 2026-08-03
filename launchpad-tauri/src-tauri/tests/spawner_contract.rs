//! SystemProcessSpawner contract (ported 1:1 from C# SpawnerContractTests):
//! zero-shell argv spawn succeeds for valid plans and maps missing dirs /
//! missing executables to Win32 error codes (the error source that
//! LaunchService::try_launch classifies into structured errors).

use launchpad_tauri_lib::core::models::LaunchPlan;
use launchpad_tauri_lib::core::ports::{ProcessSpawner, SpawnError};
use launchpad_tauri_lib::infra::spawner::SystemProcessSpawner;

fn plan(executable: &str, args: Vec<&str>, working_directory: &str) -> LaunchPlan {
    LaunchPlan {
        executable: executable.to_string(),
        args: args.into_iter().map(str::to_string).collect(),
        working_directory: working_directory.to_string(),
        is_dangerous: false,
        terminal_override: None,
    }
}

#[test]
fn launch_starts_process_for_valid_plan() {
    let spawner = SystemProcessSpawner;
    let plan = plan(
        "pwsh.exe",
        vec!["-Command", "exit"],
        &std::env::temp_dir().display().to_string(),
    );
    assert!(spawner.launch(&plan).is_ok());
}

#[test]
fn launch_maps_missing_working_directory_to_error_directory() {
    let spawner = SystemProcessSpawner;
    let plan = plan(
        "pwsh.exe",
        vec![],
        r"D:\definitely-not-a-real-launchpad-dir-xyz",
    );
    match spawner.launch(&plan) {
        Err(SpawnError::Win32 { code }) => {
            assert_eq!(launchpad_tauri_lib::core::ports::ERROR_DIRECTORY, code);
        }
        other => panic!("expected Win32 267, got {other:?}"),
    }
}

#[test]
fn launch_maps_missing_executable_to_error_file_not_found() {
    let spawner = SystemProcessSpawner;
    let plan = plan(
        "definitely-not-an-exe-xyz.exe",
        vec![],
        &std::env::temp_dir().display().to_string(),
    );
    match spawner.launch(&plan) {
        Err(SpawnError::Win32 { code }) => {
            assert_eq!(launchpad_tauri_lib::core::ports::ERROR_FILE_NOT_FOUND, code);
        }
        other => panic!("expected Win32 2, got {other:?}"),
    }
}
