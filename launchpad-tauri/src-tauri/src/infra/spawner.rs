//! Zero-shell spawn: every argv element goes through the args list, never a
//! shell string. The working directory travels via current_dir (the cmd /k
//! quoting trap is avoided by never prefixing a cd command).

use crate::core::models::LaunchPlan;
use crate::core::ports::{ProcessSpawner, SpawnError};

pub struct SystemProcessSpawner;

impl Default for SystemProcessSpawner {
    fn default() -> Self {
        Self
    }
}

impl ProcessSpawner for SystemProcessSpawner {
    fn launch(&self, plan: &LaunchPlan) -> Result<(), SpawnError> {
        let mut cmd = std::process::Command::new(&plan.executable);
        cmd.args(&plan.args);
        cmd.current_dir(&plan.working_directory);
        // A GUI host has no console to inherit, so Windows allocates a fresh
        // console window for console children automatically (the legacy Rust
        // CREATE_NEW_CONSOLE flag is unnecessary here, same as the C# port).
        match cmd.spawn() {
            Ok(_) => Ok(()),
            Err(e) => Err(match e.raw_os_error() {
                // raw_os_error is i32 on all platforms; Win32 codes fit u32.
                Some(code) => SpawnError::Win32 {
                    code: code.max(0) as u32,
                },
                None if e.kind() == std::io::ErrorKind::NotFound => {
                    // Rust std drops the Win32 code for missing executables
                    // ("program not found"); the C# contract expects
                    // ERROR_FILE_NOT_FOUND (2) so classification stays intact.
                    SpawnError::Win32 {
                        code: crate::core::ports::ERROR_FILE_NOT_FOUND,
                    }
                }
                None => SpawnError::Other(e.to_string()),
            }),
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn missing_working_directory_maps_to_win32_error_directory() {
        let spawner = SystemProcessSpawner;
        let plan = LaunchPlan {
            executable: "pwsh.exe".to_string(),
            args: vec![],
            working_directory: r"D:\definitely-not-a-real-launchpad-dir-xyz".to_string(),
            is_dangerous: false,
            terminal_override: None,
        };
        match spawner.launch(&plan) {
            Err(SpawnError::Win32 { code }) => {
                assert_eq!(crate::core::ports::ERROR_DIRECTORY, code);
            }
            other => panic!("expected Win32 267, got {other:?}"),
        }
    }

    #[test]
    fn missing_executable_maps_to_win32_error_file_not_found() {
        let spawner = SystemProcessSpawner;
        let plan = LaunchPlan {
            executable: "definitely-not-an-exe-xyz.exe".to_string(),
            args: vec![],
            working_directory: std::env::temp_dir().display().to_string(),
            is_dangerous: false,
            terminal_override: None,
        };
        match spawner.launch(&plan) {
            Err(SpawnError::Win32 { code }) => {
                assert_eq!(crate::core::ports::ERROR_FILE_NOT_FOUND, code);
            }
            other => panic!("expected Win32 2, got {other:?}"),
        }
    }
}
