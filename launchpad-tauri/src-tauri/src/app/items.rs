//! Item orchestration (ported 1:1 from C# ItemUseCase): load/save through the
//! store port; list mutations are pure core functions (core::items). Persist
//! failures return structured errors so the UI never silently drops a save.

use crate::app::ItemsPayload;
use std::sync::Arc;

use crate::config::store::ConfigStore;
use crate::core::errors::AppError;
use crate::core::models::LaunchItem;

pub struct ItemService {
    store: Arc<ConfigStore>,
}

impl ItemService {
    pub fn new(store: Arc<ConfigStore>) -> Self {
        Self { store }
    }

    /// Loads items; a corrupt config.json (recovery already attempted inside
    /// the store) surfaces as a structured error, and the recovery notice key
    /// rides along for the status bar.
    pub fn load_items(&self) -> ItemsPayload {
        match self.store.read_items() {
            Ok(items) => ItemsPayload {
                items,
                recovery_note: self.store.last_recovery_note_key(),
                error: None,
            },
            Err(error) => ItemsPayload {
                items: Vec::new(),
                recovery_note: None,
                error: Some(error),
            },
        }
    }

    pub fn save_items(&self, items: &[LaunchItem]) -> Result<(), AppError> {
        self.store.write_items(items)
    }

    /// Full list, for id-collision checks when building new items.
    pub fn all_items(&self) -> Vec<LaunchItem> {
        self.store.read_items().unwrap_or_default()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn scratch() -> std::path::PathBuf {
        let dir = std::env::temp_dir().join(format!(
            "launchpad-app-items-{}",
            std::time::SystemTime::now()
                .duration_since(std::time::UNIX_EPOCH)
                .unwrap()
                .as_nanos()
        ));
        let _ = std::fs::remove_dir_all(&dir);
        std::fs::create_dir_all(&dir).unwrap();
        dir
    }

    fn item(name: &str) -> LaunchItem {
        LaunchItem {
            name: name.to_string(),
            directory: "D:\\x".to_string(),
            command: "snow".to_string(),
            confirm: true,
            id: name.to_string(),
            selected: false,
            terminal: None,
            tag: None,
            group: None,
        }
    }

    #[test]
    fn load_items_returns_recovery_note_after_recovery() {
        let dir = scratch();
        let service = ItemService::new(Arc::new(ConfigStore::new(&dir)));

        // Two writes so a backup exists, then corrupt the main file.
        service.save_items(&[item("good")]).unwrap();
        service.save_items(&[item("good"), item("backup")]).unwrap();
        std::fs::write(dir.join("config.json"), "{ not json").unwrap();

        let payload = service.load_items();
        assert!(payload.error.is_none());
        assert_eq!(1, payload.items.len());
        assert_eq!(Some(crate::core::i18n::LanguageKey::StatusRecovered), payload.recovery_note);
    }

    #[test]
    fn load_items_surfaces_config_parse_error() {
        let dir = scratch();
        std::fs::write(dir.join("config.json"), "{ not json").unwrap();
        let service = ItemService::new(Arc::new(ConfigStore::new(&dir)));

        let payload = service.load_items();
        assert!(matches!(payload.error, Some(AppError::ConfigParse { .. })));
    }
}
