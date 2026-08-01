//! Launch commands: confirmation policy, single/batch launch with per-item
//! error capture, history updates, selection clearing (all decisions in the
//! core/app layers; this file only wires inputs and outputs).

use std::collections::HashSet;

use tauri::State;

use crate::core::errors::AppError;
use crate::core::i18n::LanguageKey;
use crate::core::items;
use crate::core::launch;
use crate::core::settings as core_settings;
use crate::state::AppState;

#[derive(serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct ConfirmInfo {
    pub needs_confirm: bool,
    pub danger_key: Option<LanguageKey>,
}

#[derive(serde::Serialize)]
#[serde(rename_all = "camelCase")]
pub struct LaunchManyResult {
    pub succeeded: usize,
    pub failed_indexes: Vec<usize>,
}

fn find(state: &AppState, id: &str) -> Result<crate::core::models::LaunchItem, AppError> {
    let all = state.items.all_items();
    all.iter()
        .find(|i| i.id == id)
        .cloned()
        .ok_or_else(|| AppError::Unknown(format!("Item not found: {id}")))
}

#[tauri::command]
pub fn needs_confirm(state: State<'_, AppState>, id: String) -> Result<ConfirmInfo, AppError> {
    let settings = state.settings.load()?;
    let item = find(&state, &id)?;
    Ok(ConfirmInfo {
        needs_confirm: launch::needs_confirm(&settings, &item),
        danger_key: item.danger_reason(),
    })
}

#[tauri::command]
pub fn launch_item(state: State<'_, AppState>, id: String) -> Result<(), AppError> {
    let item = find(&state, &id)?;
    state
        .launch
        .try_launch(&item, |dir| std::path::Path::new(dir).exists())?;

    // History update happens only on a successful launch (C# semantics).
    let settings = state.settings.load()?;
    let updated = core_settings::push_history_name(&settings, &item.name);
    state.settings.save(&updated)?;
    Ok(())
}

#[tauri::command]
pub fn launch_many(state: State<'_, AppState>, ids: Vec<String>) -> Result<LaunchManyResult, AppError> {
    let all = state.items.all_items();
    let selected: Vec<_> = ids
        .iter()
        .filter_map(|id| all.iter().find(|i| i.id == *id).cloned())
        .collect();
    if selected.is_empty() {
        return Ok(LaunchManyResult {
            succeeded: 0,
            failed_indexes: vec![],
        });
    }

    let (succeeded, failed_indexes) = state.launch.launch_many(&selected);
    let failed_set: HashSet<usize> = failed_indexes.iter().copied().collect();

    let settings = state.settings.load()?;
    let updated_settings = core_settings::push_history_many(&settings, &selected, &failed_set);
    state.settings.save(&updated_settings)?;

    // Legacy: clear the selection after a batch launch so the same terminals
    // cannot be re-fired by a second click.
    let cleared = items::clear_selection(&all);
    state.items.save_items(&cleared)?;

    Ok(LaunchManyResult {
        succeeded,
        failed_indexes,
    })
}
