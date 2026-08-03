//! File-backed config store (ported 1:1 from C# ConfigStore, plus the
//! atomic-write defense from the cc-switch reference):
//! - config.json is backed up to config.json.bak before every write;
//! - writes go through a temp file + rename so a crash mid-write can never
//!   leave a half-written config.json;
//! - a corrupt config.json is recovered from the backup by a file-level copy
//!   (NOT via the write path, which would overwrite the backup with the
//!   corrupt file); the recovery note key rides along for the status bar;
//! - the config directory is created on construction.

use std::path::{Path, PathBuf};
use std::sync::Mutex;

use crate::core::errors::AppError;
use crate::core::i18n::LanguageKey;
use crate::core::models::{AppSettings, LaunchItem};

const ITEMS_FILE: &str = "config.json";
const SETTINGS_FILE: &str = "settings.json";
const BACKUP_FILE: &str = "config.json.bak";

pub struct ConfigStore {
    dir: PathBuf,
    /// Language-independent key set when the last ReadItems recovered a
    /// corrupt config.json from the backup (null = no recovery).
    last_recovery_note: Mutex<Option<LanguageKey>>,
}

impl ConfigStore {
    pub fn new(config_dir: impl Into<PathBuf>) -> Self {
        let dir = config_dir.into();
        std::fs::create_dir_all(&dir).expect("failed to create config directory");
        Self {
            dir,
            last_recovery_note: Mutex::new(None),
        }
    }

    pub fn last_recovery_note_key(&self) -> Option<LanguageKey> {
        *self.last_recovery_note.lock().unwrap()
    }

    pub fn read_items(&self) -> Result<Vec<LaunchItem>, AppError> {
        *self.last_recovery_note.lock().unwrap() = None;
        let path = self.dir.join(ITEMS_FILE);
        if !path.exists() {
            return Ok(Vec::new());
        }

        match parse_file(&path) {
            Ok(items) => Ok(items),
            Err(corrupt) => self.recover_from_backup(&path, &corrupt),
        }
    }

    fn recover_from_backup(
        &self,
        path: &Path,
        corrupt: &AppError,
    ) -> Result<Vec<LaunchItem>, AppError> {
        let backup_path = self.dir.join(BACKUP_FILE);
        if !backup_path.exists() {
            return Err(with_detail(
                corrupt,
                "; no backup available (config.json.bak missing)",
            ));
        }

        match parse_file(&backup_path) {
            Ok(recovered) => {
                // File-level copy: recovery must NOT go through the write path
                // (it would overwrite the backup with the corrupt file).
                std::fs::copy(&backup_path, path).map_err(|e| AppError::StoreWrite {
                    detail: format!("recovery copy failed: {e}"),
                })?;
                *self.last_recovery_note.lock().unwrap() = Some(LanguageKey::StatusRecovered);
                Ok(recovered)
            }
            Err(backup_corrupt) => Err(with_detail(
                corrupt,
                &format!("; backup also unreadable: {}", backup_corrupt.description()),
            )),
        }
    }

    pub fn write_items(&self, items: &[LaunchItem]) -> Result<(), AppError> {
        let path = self.dir.join(ITEMS_FILE);
        if path.exists() {
            std::fs::copy(&path, self.dir.join(BACKUP_FILE)).map_err(|e| AppError::StoreWrite {
                detail: format!("backup failed: {e}"),
            })?;
        }

        let json = serde_json::to_string_pretty(items).map_err(|e| AppError::StoreWrite {
            detail: e.to_string(),
        })?;
        atomic_write(&path, json.as_bytes())
    }

    pub fn read_settings(&self) -> Result<AppSettings, AppError> {
        let path = self.dir.join(SETTINGS_FILE);
        if !path.exists() {
            return Ok(AppSettings::default());
        }
        parse_file(&path)
    }

    pub fn write_settings(&self, settings: &AppSettings) -> Result<(), AppError> {
        let path = self.dir.join(SETTINGS_FILE);
        let json = serde_json::to_string_pretty(settings).map_err(|e| AppError::StoreWrite {
            detail: e.to_string(),
        })?;
        atomic_write(&path, json.as_bytes())
    }
}

fn parse_file<T: serde::de::DeserializeOwned>(path: &Path) -> Result<T, AppError> {
    let content = std::fs::read_to_string(path).map_err(|e| AppError::StoreRead {
        detail: format!("{}: {e}", path.display()),
    })?;
    serde_json::from_str(&content).map_err(|e| AppError::ConfigParse {
        path: path.display().to_string(),
        detail: e.to_string(),
    })
}

/// Temp file + rename: a crash mid-write leaves the old file intact.
fn atomic_write(path: &Path, bytes: &[u8]) -> Result<(), AppError> {
    let tmp_path = tmp_file_for(path);
    std::fs::write(&tmp_path, bytes).map_err(|e| AppError::StoreWrite {
        detail: format!("{}: {e}", tmp_path.display()),
    })?;
    std::fs::rename(&tmp_path, path).map_err(|e| AppError::StoreWrite {
        detail: format!("rename to {}: {e}", path.display()),
    })
}

fn tmp_file_for(path: &Path) -> PathBuf {
    let name = path.file_name().unwrap_or_default().to_string_lossy();
    path.with_file_name(format!("{name}.tmp"))
}

fn with_detail(err: &AppError, suffix: &str) -> AppError {
    match err {
        AppError::ConfigParse { path, detail } => AppError::ConfigParse {
            path: path.clone(),
            detail: format!("{detail}{suffix}"),
        },
        other => other.clone(),
    }
}
