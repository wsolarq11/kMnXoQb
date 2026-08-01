import { describe, expect, it } from "vitest";
import { EN_US, LANGUAGE_KEYS, ZH_CN, format, t } from "./keys";

describe("i18n keys", () => {
  it("every key has non-empty text in both languages", () => {
    for (const key of LANGUAGE_KEYS) {
      expect(ZH_CN[key], `zh-CN missing ${key}`).toBeTruthy();
      expect(EN_US[key], `en-US missing ${key}`).toBeTruthy();
    }
  });

  it("key set matches the Rust enum size (62)", () => {
    expect(LANGUAGE_KEYS.length).toBe(63);
  });

  it("first and last keys match the Rust enum order", () => {
    expect(LANGUAGE_KEYS[0]).toBe("ToggleConfirm");
    expect(LANGUAGE_KEYS[LANGUAGE_KEYS.length - 1]).toBe("BootLoading");
  });

  it("t resolves per language", () => {
    expect(t("BtnNew", "zh-CN")).toBe("新建");
    expect(t("BtnNew", "en-US")).toBe("New");
  });

  it("format fills positional placeholders", () => {
    expect(format("DialogDeleteItemMessage", "zh-CN", ["snow"])).toContain("snow");
    expect(format("DialogBatchTitle", "en-US", [3])).toBe("Confirm 3 launches");
    expect(format("StatusLaunchedPartial", "en-US", [2, 5, 1])).toBe(
      "Launched 2 of 5 items (1 failed)",
    );
  });
});
