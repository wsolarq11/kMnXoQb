//! Item list pure functions: all mutations return new Vecs, never mutate
//! input (ported 1:1 from C# ItemUseCase static functions).

use crate::core::models::LaunchItem;

/// Legacy-compatible id derivation: lowercase, spaces to underscores, and a
/// numeric suffix when the base id collides with an existing item.
pub fn generate_id(items: &[LaunchItem], name: &str) -> String {
    let base_id = name.trim().to_ascii_lowercase().replace(' ', "_");
    if !items.iter().any(|i| i.id == base_id) {
        return base_id;
    }

    let mut n = 2;
    loop {
        let candidate = format!("{base_id}_{n}");
        if !items.iter().any(|i| i.id == candidate) {
            return candidate;
        }
        n += 1;
    }
}

pub fn new_item(
    name: &str,
    directory: &str,
    command: &str,
    confirm: bool,
    terminal: Option<&str>,
    existing: &[LaunchItem],
) -> LaunchItem {
    LaunchItem {
        name: name.to_string(),
        directory: directory.to_string(),
        command: command.to_string(),
        confirm,
        id: generate_id(existing, name),
        selected: false,
        terminal: match terminal {
            Some(t) if !t.trim().is_empty() => Some(t.trim().to_string()),
            _ => None,
        },
        tag: None,
        group: None,
    }
}

pub fn filter(items: &[LaunchItem], query: &str) -> Vec<LaunchItem> {
    if query.trim().is_empty() {
        return items.to_vec();
    }
    let q = query.to_ascii_lowercase();
    items
        .iter()
        .filter(|i| {
            i.name.to_ascii_lowercase().contains(&q)
                || i.directory.to_ascii_lowercase().contains(&q)
                || i.command.to_ascii_lowercase().contains(&q)
        })
        .cloned()
        .collect()
}

pub fn upsert(items: &[LaunchItem], item: LaunchItem, index: Option<usize>) -> Vec<LaunchItem> {
    let mut list = items.to_vec();
    match index {
        None => list.push(item),
        Some(i) if i < list.len() => list[i] = item,
        Some(_) => {}
    }
    list
}

pub fn delete(items: &[LaunchItem], index: usize) -> Vec<LaunchItem> {
    let mut list = items.to_vec();
    if index < list.len() {
        list.remove(index);
    }
    list
}

pub fn move_item(items: &[LaunchItem], index: usize, delta: i32) -> Vec<LaunchItem> {
    let target = index as i64 + delta as i64;
    if index >= items.len() || target < 0 || target >= items.len() as i64 {
        return items.to_vec();
    }
    let mut list = items.to_vec();
    list.swap(index, target as usize);
    list
}

/// Target-state (not flip) semantics: the UI captures the checkbox state at
/// click time and applies it, so binding-driven or stale re-invocations are
/// idempotent.
pub fn set_select(items: &[LaunchItem], index: usize, target: bool) -> Vec<LaunchItem> {
    if index >= items.len() {
        return items.to_vec();
    }
    items
        .iter()
        .enumerate()
        .map(|(i, item)| {
            if i == index {
                item.clone().with_selected(target)
            } else {
                item.clone()
            }
        })
        .collect()
}

/// Resolves by stable id instead of reference: deferred commands may carry an
/// item instance replaced by an earlier collection rebuild (records are
/// immutable, rebuilds create new instances). The id survives.
pub fn set_select_by_id(items: &[LaunchItem], id: &str, target: bool) -> Vec<LaunchItem> {
    for (i, item) in items.iter().enumerate() {
        if item.id == id {
            return set_select(items, i, target);
        }
    }
    items.to_vec()
}

/// Deselect everything: after a batch launch the selection is cleared so a
/// second "Launch Selected" cannot re-fire the same terminals.
pub fn clear_selection(items: &[LaunchItem]) -> Vec<LaunchItem> {
    if items.iter().all(|i| !i.selected) {
        return items.to_vec();
    }
    items
        .iter()
        .map(|i| {
            if i.selected {
                i.clone().with_selected(false)
            } else {
                i.clone()
            }
        })
        .collect()
}

pub fn toggle_select_all(items: &[LaunchItem]) -> Vec<LaunchItem> {
    let select_all = items.iter().any(|i| !i.selected);
    items
        .iter()
        .map(|i| i.clone().with_selected(select_all))
        .collect()
}

trait WithSelected {
    fn with_selected(&self, selected: bool) -> LaunchItem;
}

impl WithSelected for LaunchItem {
    fn with_selected(&self, selected: bool) -> LaunchItem {
        let mut clone = self.clone();
        clone.selected = selected;
        clone
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    fn item(name: &str, command: &str) -> LaunchItem {
        LaunchItem {
            name: name.to_string(),
            directory: r"D:\projects\demo".to_string(),
            command: command.to_string(),
            confirm: true,
            id: name.replace(' ', "_"),
            selected: false,
            terminal: None,
            tag: None,
            group: None,
        }
    }

    #[test]
    fn new_item_generates_id_from_name() {
        let i = new_item("my tool", r"D:\x", "snow", true, Some("pwsh"), &[]);
        assert_eq!("my_tool", i.id);
        assert_eq!(Some("pwsh".to_string()), i.terminal);
    }

    #[test]
    fn new_item_blank_terminal_becomes_none() {
        let i = new_item("t", r"D:\x", "snow", true, Some("  "), &[]);
        assert_eq!(None, i.terminal);
    }

    #[test]
    fn generate_id_lowercases_and_swaps_spaces() {
        assert_eq!("my_tool", generate_id(&[], "My Tool"));
    }

    #[test]
    fn generate_id_appends_suffix_on_collision() {
        assert_eq!("a_b_2", generate_id(&[item("a b", "snow")], "a b"));
    }

    #[test]
    fn generate_id_skips_taken_suffixes() {
        let existing = [item("a b", "snow"), item("a_b_2", "snow")];
        assert_eq!("a_b_3", generate_id(&existing, "A B"));
    }

    #[test]
    fn generate_id_trims_name() {
        assert_eq!("t", generate_id(&[], "  t  "));
    }

    #[test]
    fn filter_matches_name_directory_command_case_insensitive() {
        let items = [item("Alpha", "snow"), item("Beta", "npm run dev")];
        assert_eq!(1, filter(&items, "alpha").len());
        assert_eq!(1, filter(&items, "NPM").len());
        assert_eq!(0, filter(&items, "zzz").len());
        assert_eq!(2, filter(&items, "").len());
    }

    #[test]
    fn upsert_adds_when_index_none() {
        let r = upsert(&[item("a", "snow")], item("b", "snow"), None);
        assert_eq!(2, r.len());
        assert_eq!("b", r[1].name);
    }

    #[test]
    fn upsert_replaces_at_index() {
        let r = upsert(&[item("a", "snow"), item("b", "snow")], item("c", "snow"), Some(1));
        assert_eq!("c", r[1].name);
        assert_eq!(2, r.len());
    }

    #[test]
    fn delete_removes_at_index() {
        let r = delete(&[item("a", "s"), item("b", "s"), item("c", "s")], 1);
        let names: Vec<_> = r.iter().map(|i| i.name.as_str()).collect();
        assert_eq!(vec!["a", "c"], names);
    }

    #[test]
    fn move_swaps_adjacent_items() {
        let r = move_item(&[item("a", "s"), item("b", "s"), item("c", "s")], 1, -1);
        let names: Vec<_> = r.iter().map(|i| i.name.as_str()).collect();
        assert_eq!(vec!["b", "a", "c"], names);
    }

    #[test]
    fn move_clamps_at_edges() {
        let items = [item("a", "s"), item("b", "s")];
        assert_eq!(items.to_vec(), move_item(&items, 0, -1));
        assert_eq!(items.to_vec(), move_item(&items, 1, 1));
    }

    #[test]
    fn set_select_sets_target_state_not_flip() {
        let mut a = item("a", "s");
        a.selected = true;
        let items = [a, item("b", "s")];

        let r = set_select(&items, 0, false);
        assert!(!r[0].selected);
        assert!(!r[1].selected);
    }

    #[test]
    fn set_select_reapplying_same_target_is_idempotent() {
        let r = set_select(&[item("a", "s"), item("b", "s")], 0, true);
        assert!(r[0].selected);
        assert!(!r[1].selected);
    }

    #[test]
    fn set_select_out_of_range_returns_same_list() {
        let items = [item("a", "s"), item("b", "s")];
        assert_eq!(items.to_vec(), set_select(&items, 5, true));
    }

    #[test]
    fn set_select_by_id_resolves_after_instance_replacement() {
        // Simulates the double-click race: the item instance was replaced by a
        // collection rebuild, but the id survives.
        let mut a = item("a", "s");
        a.selected = true;
        let rebuilt = [a, item("b", "s")];
        let stale_reference = item("a", "s");

        let r = set_select_by_id(&rebuilt, &stale_reference.id, false);
        assert!(!r[0].selected);
        assert!(!r[1].selected);
    }

    #[test]
    fn set_select_by_id_unknown_id_returns_same_list() {
        let items = [item("a", "s"), item("b", "s")];
        assert_eq!(items.to_vec(), set_select_by_id(&items, "missing", true));
    }

    #[test]
    fn toggle_select_all_selects_all_when_none_selected() {
        let r = toggle_select_all(&[item("a", "s"), item("b", "s")]);
        assert!(r.iter().all(|i| i.selected));
    }

    #[test]
    fn toggle_select_all_deselects_all_when_all_selected() {
        let mut a = item("a", "s");
        a.selected = true;
        let mut b = item("b", "s");
        b.selected = true;
        let r = toggle_select_all(&[a, b]);
        assert!(r.iter().all(|i| !i.selected));
    }
}
