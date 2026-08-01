//! Settings orchestration (ported 1:1 from C# SettingsUseCase); mutations are
//! pure core functions (core::settings). Persist failures return structured
//! errors so the UI never silently drops a save.

use std::sync::Arc;

use crate::config::store::ConfigStore;
use crate::core::errors::AppError;
use crate::core::models::AppSettings;

pub struct SettingsService {
    store: Arc<ConfigStore>,
}

impl SettingsService {
    pub fn new(store: Arc<ConfigStore>) -> Self {
        Self { store }
    }

    pub fn load(&self) -> Result<AppSettings, AppError> {
        self.store.read_settings()
    }

    pub fn save(&self, settings: &AppSettings) -> Result<(), AppError> {
        self.store.write_settings(settings)
    }
}
