// Types aligned with the Rust core models (snake_case JSON fields).

export interface LaunchItem {
  name: string;
  directory: string;
  command: string;
  confirm: boolean;
  id: string;
  selected: boolean;
  terminal?: string | null;
  tag?: string | null;
  group?: string | null;
}

export interface WindowState {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface AppSettings {
  confirm_enabled: boolean;
  theme: string;
  language: string;
  launch_history: string[];
  window_state?: WindowState | null;
  [key: string]: unknown;
}

export interface ItemsPayload {
  items: LaunchItem[];
  recovery_note?: string | null;
  error?: { kind: string; detail: string } | null;
}

export interface ConfirmInfo {
  needs_confirm: boolean;
  danger_key?: string | null;
}

export interface LaunchManyResult {
  succeeded: number;
  failed_indexes: number[];
}

export interface ResolvedLanguage {
  effective: "auto" | "zh-CN" | "en-US";
  setting: string;
}

export interface ItemInput {
  name: string;
  directory: string;
  command: string;
  confirm: boolean;
  terminal?: string | null;
}
