//! Application layer (mirrors C# UseCases): orchestration over the pure core
//! and the injected ports. Commands stay thin — all decisions live here.

pub mod items;
pub mod settings;

use crate::core::errors::AppError;
use crate::core::i18n::LanguageKey;
use crate::core::models::LaunchItem;

/// Loaded items plus the language-independent recovery note key (set when a
/// corrupt config.json was recovered from the backup; None = no recovery) and
/// a structured load error (None = loaded fine).
#[derive(serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ItemsPayload {
    pub items: Vec<LaunchItem>,
    pub recovery_note: Option<LanguageKey>,
    pub error: Option<AppError>,
}
