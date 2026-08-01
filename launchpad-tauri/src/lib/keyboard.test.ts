import { describe, expect, it, vi } from "vitest";
import { installGlobalKeys } from "./keyboard";

describe("installGlobalKeys", () => {
  it("Escape triggers the escape handler", () => {
    const onEscape = vi.fn();
    const focusSearch = vi.fn();
    const uninstall = installGlobalKeys(onEscape, focusSearch);
    window.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    expect(onEscape).toHaveBeenCalledTimes(1);
    uninstall();
  });

  it("Ctrl+F focuses search and prevents default", () => {
    const focusSearch = vi.fn();
    const uninstall = installGlobalKeys(() => {}, focusSearch);
    const e = new KeyboardEvent("keydown", { key: "f", ctrlKey: true, cancelable: true });
    window.dispatchEvent(e);
    expect(focusSearch).toHaveBeenCalledTimes(1);
    expect(e.defaultPrevented).toBe(true);
    uninstall();
  });

  it("uninstall removes the listener", () => {
    const onEscape = vi.fn();
    const uninstall = installGlobalKeys(onEscape, () => {});
    uninstall();
    window.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape" }));
    expect(onEscape).not.toHaveBeenCalled();
  });
});
