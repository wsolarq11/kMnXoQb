//! Ports (dependency-injection seams) defined in the core, implemented by the
//! imperative shell. Mirrors C# Core/Ports interfaces.

use crate::core::models::LaunchPlan;

/// Win32 process-spawn error codes surfaced by Process.Start (C# Win32ErrorCode).
pub const ERROR_FILE_NOT_FOUND: u32 = 2;
pub const ERROR_PATH_NOT_FOUND: u32 = 3;
pub const ERROR_ACCESS_DENIED: u32 = 5;
pub const ERROR_DIRECTORY: u32 = 267;

#[derive(Debug, Clone, PartialEq)]
pub enum SpawnError {
    Win32 { code: u32 },
    Other(String),
}

impl SpawnError {
    pub fn message(&self) -> String {
        match self {
            SpawnError::Win32 { code } => format!("Win32 error {code}"),
            SpawnError::Other(msg) => msg.clone(),
        }
    }
}

/// Zero-shell spawn: every argv element goes through the args list, never a
/// shell string. The working directory travels via the process start info.
pub trait ProcessSpawner: Send + Sync {
    fn launch(&self, plan: &LaunchPlan) -> Result<(), SpawnError>;
}

/// Terminal availability probe (wt.exe / pwsh.exe on PATH).
/// Named `TerminalAvailability` because Rust shares one namespace for structs
/// and traits, and the infra implementation is called `TerminalDetector`.
pub trait TerminalAvailability: Send + Sync {
    fn terminal_available(&self, name: &str) -> bool;
}
