import { beforeEach, describe, expect, it } from "vitest";
import { applyTheme } from "./theme";

describe("applyTheme (three-state wiring)", () => {
  beforeEach(() => {
    delete document.documentElement.dataset.theme;
  });

  it("forces dark via data-theme", () => {
    applyTheme("dark");
    expect(document.documentElement.dataset.theme).toBe("dark");
  });

  it("forces light via data-theme", () => {
    applyTheme("light");
    expect(document.documentElement.dataset.theme).toBe("light");
  });

  it("removes the attribute for system", () => {
    applyTheme("dark");
    applyTheme("system");
    expect(document.documentElement.dataset.theme).toBeUndefined();
  });

  it("falls back to system for unknown values", () => {
    applyTheme("system");
    expect(document.documentElement.dataset.theme).toBeUndefined();
  });
});
