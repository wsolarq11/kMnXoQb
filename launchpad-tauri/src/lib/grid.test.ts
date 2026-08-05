import { describe, expect, it } from "vitest";
import { CARD_HEIGHT, CARD_WIDTH, DEFAULT_GAP, planGrid } from "./grid";

describe("planGrid", () => {
  it("empty count yields zero columns (empty state)", () => {
    expect(planGrid(800, 0)).toBe(0);
  });

  it("one card keeps the fixed card width (no stretch)", () => {
    // 800px fits 3 columns; a single card stays 260px wide, not full-width.
    expect(planGrid(800, 1)).toBe(3);
  });

  it("single card in a narrow window gets one column", () => {
    expect(planGrid(280, 1)).toBe(1);
  });

  it("computes columns from fixed card width", () => {
    // (1200 + 10) / 270 = 4.48 -> 4 columns.
    expect(planGrid(1200, 10)).toBe(4);
  });

  it("uses the last column that still fits a full card", () => {
    // (809 + 10) / 270 = 3.03 -> 3; (810 + 10) / 270 = 3.04 -> 3.
    expect(planGrid(810, 8)).toBe(3);
    // (1349 + 10) / 270 = 5.03 -> 5 columns.
    expect(planGrid(1349, 8)).toBe(5);
  });

  it("falls back to one column when the window is narrower than a card", () => {
    expect(planGrid(200, 3)).toBe(1);
  });

  it("card size stays constant regardless of item count", () => {
    // Same width -> same columns, independent of how many items exist.
    expect(planGrid(1200, 2)).toBe(planGrid(1200, 50));
  });

  it("exported card constants match the CSS grid", () => {
    // Grid gap is the CSS 10px gap; CARD_HEIGHT mirrors App.css grid-auto-rows.
    expect(DEFAULT_GAP).toBe(10);
    expect(CARD_WIDTH).toBe(260);
    expect(CARD_HEIGHT).toBe(120);
  });
});
