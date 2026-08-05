// Command-call wrapper: every invoke goes through here (no scattered calls),
// errors are normalized to a typed shape the status bar can render.

import { invoke as rawInvoke } from "@tauri-apps/api/core";
import type {
  AppSettings,
  ConfirmInfo,
  ItemsPayload,
  ItemInput,
  LaunchItem,
  LaunchManyResult,
  ResolvedLanguage,
} from "../types";

export interface CommandError {
  kind: string;
  detail: string;
}

export function isCommandError(e: unknown): e is CommandError {
  return (
    typeof e === "object" &&
    e !== null &&
    "kind" in e &&
    typeof (e as CommandError).kind === "string"
  );
}

export function commandErrorDetail(e: unknown): string {
  if (isCommandError(e)) return e.detail;
  return String(e);
}

// ---- items ----
export const listItems = () => rawInvoke<ItemsPayload>("list_items");
export const createItem = (input: ItemInput) => rawInvoke<LaunchItem>("create_item", { input });
export const updateItem = (id: string, input: ItemInput) =>
  rawInvoke<LaunchItem>("update_item", { id, input });
export const deleteItem = (id: string) => rawInvoke<void>("delete_item", { id });
export const moveItem = (id: string, delta: number) =>
  rawInvoke<void>("move_item", { id, delta });
export const setSelect = (id: string, target: boolean) =>
  rawInvoke<void>("set_select", { id, target });
export const toggleSelectAll = () => rawInvoke<void>("toggle_select_all");

// ---- launch ----
export const needsConfirm = (id: string) => rawInvoke<ConfirmInfo>("needs_confirm", { id });
export const launchItem = (id: string) => rawInvoke<void>("launch_item", { id });
export const launchMany = (ids: string[]) => rawInvoke<LaunchManyResult>("launch_many", { ids });

// ---- settings ----
export const getSettings = () => rawInvoke<AppSettings>("get_settings");
export const toggleTheme = () => rawInvoke<AppSettings>("toggle_theme");
export const toggleLanguage = () => rawInvoke<AppSettings>("toggle_language");
export const setConfirmEnabled = (enabled: boolean) =>
  rawInvoke<AppSettings>("set_confirm_enabled", { enabled });

// ---- misc ----
export const getLanguage = () => rawInvoke<ResolvedLanguage>("get_language");
export const windowMaterial = () => rawInvoke<"mica" | "acrylic" | "none">("window_material");
export const pickDirectory = () => rawInvoke<string | null>("pick_directory");
export const saveWindowState = () => rawInvoke<void>("save_window_state");
export const loadWindowState = () =>
  rawInvoke<{ x: number; y: number; width: number; height: number } | null>(
    "load_window_state",
  );
