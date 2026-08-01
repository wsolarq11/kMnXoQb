import { beforeEach, describe, expect, it, vi } from "vitest";

// Mock the Tauri invoke bridge so store logic is testable without a runtime.
const invoke = vi.fn();
vi.mock("@tauri-apps/api/core", () => ({
  invoke: (...args: unknown[]) => invoke(...args),
}));

import { useAppStore } from "./useAppStore";

const item = (id: string, over: Partial<import("../types").LaunchItem> = {}) => ({
  name: id,
  directory: "D:\\x",
  command: "snow",
  confirm: false,
  id,
  selected: false,
  ...over,
});

describe("useAppStore", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useAppStore.setState({
      items: [],
      settings: { confirm_enabled: false, theme: "system", language: "auto", launch_history: [] },
      status: null,
      pendingConfirm: null,
      editing: null,
      newDialogOpen: false,
      deleteTarget: null,
    });
  });

  it("init loads settings, language and items", async () => {
    invoke.mockImplementation(async (cmd: string) => {
      if (cmd === "get_settings") return { confirm_enabled: true, theme: "dark", language: "auto", launch_history: [] };
      if (cmd === "get_language") return { effective: "zh-CN", setting: "auto" };
      if (cmd === "list_items") return { items: [item("a")], recovery_note: null, error: null };
      throw new Error("unexpected " + cmd);
    });

    await useAppStore.getState().init();

    const s = useAppStore.getState();
    expect(s.language).toBe("zh-CN");
    expect(s.settings.theme).toBe("dark");
    expect(s.items).toHaveLength(1);
  });

  it("recovery note surfaces as status", async () => {
    invoke.mockImplementation(async (cmd: string) => {
      if (cmd === "list_items") return { items: [item("a")], recovery_note: "StatusRecovered", error: null };
      throw new Error("unexpected " + cmd);
    });

    await useAppStore.getState().refreshItems();

    expect(useAppStore.getState().status).toEqual({ key: "StatusRecovered" });
  });

  it("createItem persists via command and sets status", async () => {
    invoke.mockImplementation(async (cmd: string) => {
      if (cmd === "create_item") return item("new");
      if (cmd === "list_items") return { items: [item("new")], recovery_note: null, error: null };
      throw new Error("unexpected " + cmd);
    });

    const ok = await useAppStore.getState().createItem({
      name: "new",
      directory: "D:\\x",
      command: "snow",
      confirm: false,
      terminal: null,
    });

    expect(ok).toBe(true);
    expect(useAppStore.getState().status).toEqual({ key: "StatusAdded" });
    expect(useAppStore.getState().items).toHaveLength(1);
  });

  it("failed create surfaces save-failed status", async () => {
    invoke.mockImplementation(async (cmd: string) => {
      if (cmd === "create_item") throw { kind: "StoreWrite", detail: "Failed to write config.json: disk full" };
      throw new Error("unexpected " + cmd);
    });

    const ok = await useAppStore.getState().createItem({
      name: "new",
      directory: "D:\\x",
      command: "snow",
      confirm: false,
      terminal: null,
    });

    expect(ok).toBe(false);
    expect(useAppStore.getState().status?.key).toBe("StatusSaveFailed");
    expect(useAppStore.getState().status?.args?.[0]).toContain("config.json");
  });

  it("launchOne sets pending confirm when needed", async () => {
    invoke.mockImplementation(async (cmd: string) => {
      if (cmd === "needs_confirm") return { needs_confirm: true, danger_key: "DangerReasonDangerously" };
      throw new Error("unexpected " + cmd);
    });

    await useAppStore.getState().launchOne("a");

    expect(useAppStore.getState().pendingConfirm).toEqual({
      kind: "single",
      id: "a",
      dangerKey: "DangerReasonDangerously",
    });
  });

  it("confirmPending launches and updates status", async () => {
    invoke.mockImplementation(async (cmd: string) => {
      if (cmd === "needs_confirm") return { needs_confirm: true, danger_key: null };
      if (cmd === "launch_item") return null;
      if (cmd === "get_settings") return { confirm_enabled: true, theme: "dark", language: "auto", launch_history: ["a"] };
      throw new Error("unexpected " + cmd);
    });
    useAppStore.setState({ items: [item("a")] });

    await useAppStore.getState().launchOne("a");
    await useAppStore.getState().confirmPending();

    expect(useAppStore.getState().pendingConfirm).toBeNull();
    expect(useAppStore.getState().status?.key).toBe("StatusLaunched");
  });
});
