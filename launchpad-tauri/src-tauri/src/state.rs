//! App-wide state assembled in the Tauri setup (the composition root,
//! mirroring C# App.xaml.cs ConfigureServices). Every dependency is injected
//! through the constructor; DI-by-concrete-type resolution is not used.

use std::sync::Arc;

use crate::app::items::ItemService;
use crate::app::settings::SettingsService;
use crate::config::paths::{resolve, InstallForm};
use crate::config::store::ConfigStore;
use crate::core::launch::LaunchService;
use crate::infra::spawner::SystemProcessSpawner;
use crate::infra::terminal::TerminalDetector;

pub struct AppState {
    pub items: ItemService,
    pub settings: SettingsService,
    pub launch: LaunchService<SystemProcessSpawner, TerminalDetector>,
}

impl AppState {
    pub fn new(install_form: InstallForm) -> Self {
        let exe_dir = std::env::current_exe()
            .ok()
            .and_then(|p| p.parent().map(|d| d.to_path_buf()))
            .unwrap_or_default();
        let appdata = std::env::var_os("APPDATA").map(std::path::PathBuf::from);
        let config_dir = resolve(install_form, &exe_dir, appdata.as_deref()).dir;

        // One ConfigStore shared by both services (C# DI singleton semantics):
        // the recovery-note state must be observable from both read paths.
        let store = Arc::new(ConfigStore::new(config_dir));
        let items = ItemService::new(Arc::clone(&store));
        let settings = SettingsService::new(store);
        let launch = LaunchService::new(SystemProcessSpawner, TerminalDetector::new());
        Self {
            items,
            settings,
            launch,
        }
    }
}
