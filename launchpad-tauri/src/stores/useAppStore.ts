// Central store (zustand). The frontend NEVER mutates the item list locally —
// every write goes through a command and the store refreshes from the result
// (design decision D1: the Rust core is the single source of truth).
//
// Status messages carry a language key + args, never pre-rendered text, so a
// language switch re-renders the status bar too. The English diagnostic
// detail (from AppError.description) rides as an arg (C# status-bar strategy).

import { create } from "zustand";
import * as api from "../lib/invoke";
import { commandErrorDetail } from "../lib/invoke";
import type { LanguageKey } from "../i18n/keys";
import type { AppSettings, ConfirmInfo, ItemInput, LaunchItem } from "../types";

export interface StatusMessage {
  key: LanguageKey;
  args?: (string | number)[];
}

export type AppLanguage = "auto" | "zh-CN" | "en-US";

export interface PendingConfirm {
  kind: "single";
  id: string;
  dangerKey?: string | null;
}

export interface PendingBatchConfirm {
  kind: "batch";
  ids: string[];
  confirmIds: string[];
}

interface AppState {
  items: LaunchItem[];
  settings: AppSettings;
  language: AppLanguage;
  searchQuery: string;
  status: StatusMessage | null;
  loading: boolean;
  pendingConfirm: PendingConfirm | PendingBatchConfirm | null;
  editing: LaunchItem | null;
  newDialogOpen: boolean;
  deleteTarget: LaunchItem | null;

  init: () => Promise<void>;
  refreshItems: () => Promise<void>;
  setSearchQuery: (q: string) => void;
  setStatus: (m: StatusMessage | null) => void;
  openNew: () => void;
  openEdit: (item: LaunchItem) => void;
  closeDialogs: () => void;
  askDelete: (item: LaunchItem) => void;

  createItem: (input: ItemInput) => Promise<boolean>;
  updateItem: (id: string, input: ItemInput) => Promise<boolean>;
  deleteItem: (id: string) => Promise<boolean>;
  moveItem: (id: string, delta: number) => Promise<boolean>;
  setSelect: (id: string, target: boolean) => Promise<void>;
  toggleSelectAll: () => Promise<void>;

  launchOne: (id: string) => Promise<void>;
  launchSelected: () => Promise<void>;
  confirmPending: () => Promise<void>;
  cancelPending: () => void;

  toggleTheme: () => Promise<void>;
  toggleLanguage: () => Promise<void>;
  setConfirmEnabled: (enabled: boolean) => Promise<void>;
}

function failStatus(prefix: LanguageKey, e: unknown): StatusMessage {
  return { key: prefix, args: [commandErrorDetail(e)] };
}

export const useAppStore = create<AppState>((set, get) => ({
  items: [],
  settings: {
    confirm_enabled: false,
    theme: "system",
    language: "auto",
    launch_history: [],
  },
  language: "zh-CN",
  searchQuery: "",
  status: null,
  loading: false,
  pendingConfirm: null,
  editing: null,
  newDialogOpen: false,
  deleteTarget: null,

  init: async () => {
    set({ loading: true });
    try {
      const settings = await api.getSettings();
      const language = await api.getLanguage();
      set({ settings, language: language.effective });
      await get().refreshItems();
    } catch (e) {
      set({ status: failStatus("StatusConfigError", e) });
    } finally {
      set({ loading: false });
    }
  },

  refreshItems: async () => {
    try {
      const payload = await api.listItems();
      set({ items: payload.items });
      if (payload.recovery_note) {
        set({ status: { key: "StatusRecovered" } });
      } else if (payload.error) {
        set({ status: failStatus("StatusConfigError", payload.error.detail) });
      }
    } catch (e) {
      set({ status: failStatus("StatusConfigError", e) });
    }
  },

  setSearchQuery: (q) => set({ searchQuery: q }),
  setStatus: (m) => set({ status: m }),
  openNew: () => set({ newDialogOpen: true }),
  openEdit: (item) => set({ editing: item }),
  closeDialogs: () => set({ editing: null, newDialogOpen: false, deleteTarget: null }),
  askDelete: (item) => set({ deleteTarget: item }),

  createItem: async (input) => {
    try {
      await api.createItem(input);
      await get().refreshItems();
      set({ status: { key: "StatusAdded" } });
      return true;
    } catch (e) {
      set({ status: failStatus("StatusSaveFailed", e) });
      return false;
    }
  },

  updateItem: async (id, input) => {
    try {
      await api.updateItem(id, input);
      await get().refreshItems();
      set({ status: { key: "StatusUpdated" } });
      return true;
    } catch (e) {
      set({ status: failStatus("StatusSaveFailed", e) });
      return false;
    }
  },

  deleteItem: async (id) => {
    try {
      await api.deleteItem(id);
      await get().refreshItems();
      set({ status: { key: "StatusDeleted" } });
      return true;
    } catch (e) {
      set({ status: failStatus("StatusSaveFailed", e) });
      return false;
    }
  },

  moveItem: async (id, delta) => {
    try {
      await api.moveItem(id, delta);
      await get().refreshItems();
      return true;
    } catch (e) {
      set({ status: failStatus("StatusSaveFailed", e) });
      return false;
    }
  },

  setSelect: async (id, target) => {
    await api.setSelect(id, target);
    await get().refreshItems();
  },

  toggleSelectAll: async () => {
    await api.toggleSelectAll();
    await get().refreshItems();
  },

  launchOne: async (id) => {
    try {
      const info: ConfirmInfo = await api.needsConfirm(id);
      if (info.needs_confirm) {
        set({ pendingConfirm: { kind: "single", id, dangerKey: info.danger_key } });
        return;
      }
      await api.launchItem(id);
      const settings = await api.getSettings();
      set({ settings, status: { key: "StatusLaunched", args: [nameOf(id, get().items)] } });
    } catch (e) {
      set({ status: failStatus("StatusLaunchFailed", e) });
    }
  },

  launchSelected: async () => {
    const selected = get().items.filter((i) => i.selected).map((i) => i.id);
    if (selected.length === 0) {
      set({ status: null });
      return;
    }
    try {
      const confirmIds: string[] = [];
      for (const id of selected) {
        const info = await api.needsConfirm(id);
        if (info.needs_confirm) confirmIds.push(id);
      }
      if (confirmIds.length > 0) {
        set({ pendingConfirm: { kind: "batch", ids: selected, confirmIds } });
        return;
      }
      await runLaunchMany(selected, set);
    } catch (e) {
      set({ status: failStatus("StatusLaunchFailed", e) });
    }
  },

  confirmPending: async () => {
    const pending = get().pendingConfirm;
    if (!pending) return;
    set({ pendingConfirm: null });
    try {
      if (pending.kind === "single") {
        await api.launchItem(pending.id);
        const settings = await api.getSettings();
        set({ settings, status: { key: "StatusLaunched", args: [nameOf(pending.id, get().items)] } });
      } else {
        await runLaunchMany(pending.ids, set);
      }
    } catch (e) {
      set({ status: failStatus("StatusLaunchFailed", e) });
    }
  },

  cancelPending: () => set({ pendingConfirm: null }),

  toggleTheme: async () => {
    try {
      const settings = await api.toggleTheme();
      set({ settings });
    } catch (e) {
      set({ status: failStatus("StatusSaveFailed", e) });
    }
  },

  toggleLanguage: async () => {
    try {
      const settings = await api.toggleLanguage();
      const language = await api.getLanguage();
      set({ settings, language: language.effective });
    } catch (e) {
      set({ status: failStatus("StatusSaveFailed", e) });
    }
  },

  setConfirmEnabled: async (enabled) => {
    try {
      const settings = await api.setConfirmEnabled(enabled);
      set({ settings });
    } catch (e) {
      set({ status: failStatus("StatusSaveFailed", e) });
    }
  },
}));

async function runLaunchMany(
  ids: string[],
  set: (partial: Partial<AppState>) => void,
): Promise<void> {
  try {
    const result = await api.launchMany(ids);
    const settings = await api.getSettings();
    set({ settings });
    const failed = result.failed_indexes.length;
    set({
      status:
        failed === 0
          ? { key: "StatusLaunchedN", args: [result.succeeded] }
          : {
              key: "StatusLaunchedPartial",
              args: [result.succeeded, ids.length, failed],
            },
    });
  } catch (e) {
    set({ status: failStatus("StatusLaunchFailed", e) });
  }
}

function nameOf(id: string, items: LaunchItem[]): string {
  return items.find((i) => i.id === id)?.name ?? id;
}
