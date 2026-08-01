//! Structured errors for expected failures. Descriptions stay in English on
//! purpose: they carry diagnostic details (paths, messages) and are never
//! localized — the UI translates the status-bar prefix, not the detail
//! (mirrors C# LaunchErrors / StoreErrors).

use crate::core::ports::SpawnError;

/// Structured error serialized to the frontend as
/// `{ "kind": "<VariantName>", "detail": "<payload>" }`; the frontend maps
/// kind to the status-bar prefix key.
#[derive(Debug, Clone, PartialEq, serde::Serialize)]
#[serde(tag = "kind", content = "detail", rename_all = "camelCase")]
pub enum AppError {
    ProcessNotFound(String),
    WorkingDirectoryMissing(String),
    AccessDenied(String),
    ConfigParse { path: String, detail: String },
    StoreWrite { detail: String },
    StoreRead { detail: String },
    Unknown(String),
}

impl AppError {
    /// Machine-readable kind used by the frontend to pick the status prefix key.
    pub fn kind(&self) -> &'static str {
        match self {
            AppError::ProcessNotFound(_) => "Launch.ProcessNotFound",
            AppError::WorkingDirectoryMissing(_) => "Launch.WorkingDirectoryMissing",
            AppError::AccessDenied(_) => "Launch.AccessDenied",
            AppError::ConfigParse { .. } => "Store.ConfigParse",
            AppError::StoreWrite { .. } => "Store.WriteFailed",
            AppError::StoreRead { .. } => "Store.ReadFailed",
            AppError::Unknown(_) => "Launch.Unknown",
        }
    }

    /// English diagnostic detail for the status bar.
    pub fn description(&self) -> String {
        match self {
            AppError::ProcessNotFound(exe) => format!("Executable not found: {exe}"),
            AppError::WorkingDirectoryMissing(dir) => {
                format!("Working directory does not exist: {dir}")
            }
            AppError::AccessDenied(exe) => format!("Access denied starting: {exe}"),
            AppError::ConfigParse { path, detail } => {
                format!("Failed to read {path}: {detail}")
            }
            AppError::StoreWrite { detail } => format!("Failed to write: {detail}"),
            AppError::StoreRead { detail } => format!("Failed to read: {detail}"),
            AppError::Unknown(msg) => msg.clone(),
        }
    }
}

/// Pure classification of spawn errors (C# TryLaunch's Win32Exception mapping).
/// `dir_exists` is injected so the core stays free of filesystem I/O.
pub fn classify_spawn_error(
    error: &SpawnError,
    executable: &str,
    working_dir: &str,
    dir_exists: bool,
) -> AppError {
    match error {
        SpawnError::Win32 { code } if *code == crate::core::ports::ERROR_FILE_NOT_FOUND => {
            AppError::ProcessNotFound(executable.to_string())
        }
        SpawnError::Win32 { code }
            if *code == crate::core::ports::ERROR_PATH_NOT_FOUND
                || *code == crate::core::ports::ERROR_DIRECTORY =>
        {
            // ERROR_PATH_NOT_FOUND also fires when the executable lookup walks
            // a broken PATH entry; only blame the working directory when it is
            // actually missing.
            if dir_exists {
                AppError::ProcessNotFound(executable.to_string())
            } else {
                AppError::WorkingDirectoryMissing(working_dir.to_string())
            }
        }
        SpawnError::Win32 { code } if *code == crate::core::ports::ERROR_ACCESS_DENIED => {
            AppError::AccessDenied(executable.to_string())
        }
        other => AppError::Unknown(other.message()),
    }
}
