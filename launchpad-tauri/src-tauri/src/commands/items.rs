//! Item CRUD commands: all mutations go through the pure core functions and
//! persist through the store; the frontend never mutates the list locally.
//!
//! Each command is a thin State-unwrapping shell over a testable `*_impl`
//! function that takes `&AppState` directly (no Tauri runtime needed).

use tauri::State;

use crate::app::ItemsPayload;
use crate::core::errors::AppError;
use crate::core::items;
use crate::core::models::LaunchItem;
use crate::state::AppState;

#[derive(serde::Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct ItemInput {
    pub name: String,
    pub directory: String,
    pub command: String,
    pub confirm: bool,
    pub terminal: Option<String>,
}

#[tauri::command]
pub fn list_items(state: State<'_, AppState>) -> ItemsPayload {
    list_items_impl(&state)
}

pub fn list_items_impl(state: &AppState) -> ItemsPayload {
    state.items.load_items()
}

#[tauri::command]
pub fn create_item(state: State<'_, AppState>, input: ItemInput) -> Result<LaunchItem, AppError> {
    create_item_impl(&state, input)
}

pub fn create_item_impl(state: &AppState, input: ItemInput) -> Result<LaunchItem, AppError> {
    let mut all = state.items.all_items();
    let item = items::new_item(
        &input.name,
        &input.directory,
        &input.command,
        input.confirm,
        input.terminal.as_deref(),
        &all,
    );
    all.push(item.clone());
    state.items.save_items(&all)?;
    Ok(item)
}

#[tauri::command]
pub fn update_item(
    state: State<'_, AppState>,
    id: String,
    input: ItemInput,
) -> Result<LaunchItem, AppError> {
    update_item_impl(&state, id, input)
}

pub fn update_item_impl(
    state: &AppState,
    id: String,
    input: ItemInput,
) -> Result<LaunchItem, AppError> {
    let all = state.items.all_items();
    let index = all
        .iter()
        .position(|i| i.id == id)
        .ok_or_else(|| AppError::Unknown(format!("Item not found: {id}")))?;
    let existing = &all[index];
    let item = LaunchItem {
        name: input.name,
        directory: input.directory,
        command: input.command,
        confirm: input.confirm,
        id,
        selected: existing.selected,
        terminal: normalize_optional(input.terminal),
        tag: existing.tag.clone(),
        group: existing.group.clone(),
    };
    let updated = items::upsert(&all, item.clone(), Some(index));
    state.items.save_items(&updated)?;
    Ok(item)
}

#[tauri::command]
pub fn delete_item(state: State<'_, AppState>, id: String) -> Result<(), AppError> {
    delete_item_impl(&state, id)
}

pub fn delete_item_impl(state: &AppState, id: String) -> Result<(), AppError> {
    let all = state.items.all_items();
    let index = all
        .iter()
        .position(|i| i.id == id)
        .ok_or_else(|| AppError::Unknown(format!("Item not found: {id}")))?;
    let updated = items::delete(&all, index);
    state.items.save_items(&updated)
}

#[tauri::command]
pub fn move_item(state: State<'_, AppState>, id: String, delta: i32) -> Result<(), AppError> {
    move_item_impl(&state, id, delta)
}

pub fn move_item_impl(state: &AppState, id: String, delta: i32) -> Result<(), AppError> {
    let all = state.items.all_items();
    let index = all
        .iter()
        .position(|i| i.id == id)
        .ok_or_else(|| AppError::Unknown(format!("Item not found: {id}")))?;
    let updated = items::move_item(&all, index, delta);
    state.items.save_items(&updated)
}

/// Target-state (not flip) semantics; resolved by stable id (idempotent under
/// collection rebuilds).
#[tauri::command]
pub fn set_select(state: State<'_, AppState>, id: String, target: bool) -> Result<(), AppError> {
    set_select_impl(&state, id, target)
}

pub fn set_select_impl(state: &AppState, id: String, target: bool) -> Result<(), AppError> {
    let all = state.items.all_items();
    let updated = items::set_select_by_id(&all, &id, target);
    state.items.save_items(&updated)
}

#[tauri::command]
pub fn toggle_select_all(state: State<'_, AppState>) -> Result<(), AppError> {
    toggle_select_all_impl(&state)
}

pub fn toggle_select_all_impl(state: &AppState) -> Result<(), AppError> {
    let all = state.items.all_items();
    let updated = items::toggle_select_all(&all);
    state.items.save_items(&updated)
}

fn normalize_optional(value: Option<String>) -> Option<String> {
    match value {
        Some(v) if !v.trim().is_empty() => Some(v.trim().to_string()),
        _ => None,
    }
}
