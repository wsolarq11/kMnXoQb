//! Launch commands: confirmation policy, single/batch launch with per-item
//! error capture, history updates, selection clearing (all decisions in the
//! core/app layers; this file only wires inputs and outputs).
//!
//! `*_impl` functions take `&AppState` directly for runtime-free testing.

use std::collections::HashSet;

use tauri::State;

use crate::core::errors::AppError;
use crate::core::i18n::LanguageKey;
use crate::core::items;
use crate::core::launch;
use crate::core::models::LaunchItem;
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

fn find(state: &AppState, id: &str) -> Result<LaunchItem, AppError> {
    let all = state.items.all_items();
    all.iter()
        .find(|i| i.id == id)
        .cloned()
        .ok_or_else(|| AppError::Unknown(format!("Item not found: {id}")))
}

#[tauri::command]
pub fn needs_confirm(state: State<'_, AppState>, id: String) -> Result<ConfirmInfo, AppError> {
    needs_confirm_impl(&state, id)
}

pub fn needs_confirm_impl(state: &AppState, id: String) -> Result<ConfirmInfo, AppError> {
    let settings = state.settings.load()?;
    let item = find(state, &id)?;
    Ok(ConfirmInfo {
        needs_confirm: launch::needs_confirm(&settings, &item),
        danger_key: item.danger_reason(),
    })
}

#[tauri::command]
pub fn launch_item(state: State<'_, AppState>, id: String) -> Result<(), AppError> {
    launch_item_impl(&state, id)
}

pub fn launch_item_impl(state: &AppState, id: String) -> Result<(), AppError> {
    let item = find(state, &id)?;

    // Pre-check directory existence before attempting spawn (catches stale
    // configs early, avoids the raw spawn error path).
    if !item.directory.is_empty() && !std::path::Path::new(&item.directory).exists() {
        return Err(AppError::WorkingDirectoryMissing(item.directory.clone()));
    }

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
pub fn launch_many(
    state: State<'_, AppState>,
    ids: Vec<String>,
) -> Result<LaunchManyResult, AppError> {
    launch_many_impl(&state, ids)
}

pub fn launch_many_impl(state: &AppState, ids: Vec<String>) -> Result<LaunchManyResult, AppError> {
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

    // Pre-check directory existence for every item before spawning. Items
    // with a missing directory are counted as failures and never launched.
    let dir_ok_indices: Vec<(usize, &LaunchItem)> = selected
        .iter()
        .enumerate()
        .filter(|(_, i)| i.directory.is_empty() || std::path::Path::new(&i.directory).exists())
        .collect();
    let dir_ok: Vec<LaunchItem> = dir_ok_indices
        .iter()
        .map(|(_, item)| (*item).clone())
        .collect();
    let dir_fail_indices: Vec<usize> = (0..selected.len())
        .filter(|i| {
            let item = &selected[*i];
            !item.directory.is_empty() && !std::path::Path::new(&item.directory).exists()
        })
        .collect();

    // Launch the OK subset; map their result back to the original index.
    let (succeeded, failed_indexes) = state.launch.launch_many(&dir_ok);
    // failed_indexes are indexes into dir_ok — map back to selected indices.
    let launch_fail_original: Vec<usize> = failed_indexes
        .iter()
        .map(|&fi| dir_ok_indices[fi].0)
        .collect();

    // Combine directory-blocked + launch-failed items into one failed set.
    let all_failed: HashSet<usize> = dir_fail_indices
        .iter()
        .chain(launch_fail_original.iter())
        .copied()
        .collect();

    let settings = state.settings.load()?;
    let updated_settings = core_settings::push_history_many(&settings, &selected, &all_failed);
    state.settings.save(&updated_settings)?;

    // Legacy: clear the selection after a batch launch so the same terminals
    // cannot be re-fired by a second click.
    let cleared = items::clear_selection(&all);
    state.items.save_items(&cleared)?;

    // Sort back to index order: the contract is ascending original indices
    // (HashSet iteration order is random, which flaked the integration test).
    let mut failed_indexes: Vec<usize> = all_failed.into_iter().collect();
    failed_indexes.sort_unstable();

    Ok(LaunchManyResult {
        succeeded,
        failed_indexes,
    })
}
