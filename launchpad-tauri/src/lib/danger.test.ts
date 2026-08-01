import { describe, expect, it } from "vitest";
import { dangerKey, dangerKeyFromRust, isDangerous } from "./danger";

describe("danger flags (mirrors Rust DangerousFlagDetector)", () => {
  it("flags all six known patterns", () => {
    const cases = [
      "codex --dangerously-bypass-approvals-and-sandbox",
      "npm i --yolo",
      "claude --dangerously-skip-permissions",
      "tool --bypass-approvals run",
      "tool --bypass-sandbox run",
      "tool --bypass.sandbox run",
    ];
    for (const c of cases) expect(isDangerous(c), c).toBe(true);
  });

  it("is case insensitive", () => {
    expect(isDangerous("claude --DANGEROUSLY-skip-permissions")).toBe(true);
  });

  it("does not flag safe commands", () => {
    for (const c of ["snow", "opencode", "echo safe", "git status"]) {
      expect(isDangerous(c), c).toBe(false);
    }
  });

  it("dangerKey returns the first matching flag key", () => {
    expect(dangerKey("claude --dangerously-skip-permissions")).toBe("DangerReasonDangerously");
    expect(dangerKey("snow")).toBeNull();
  });

  it("dangerKeyFromRust guards unknown variant names", () => {
    expect(dangerKeyFromRust("DangerReasonYolo")).toBe("DangerReasonYolo");
    expect(dangerKeyFromRust("NotARealKey")).toBeNull();
    expect(dangerKeyFromRust(null)).toBeNull();
  });
});
