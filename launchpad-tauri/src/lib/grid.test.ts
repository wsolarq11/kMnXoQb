import { describe, expect, it } from "vitest";
import { planGrid } from "./grid";

const opts = { gap: 10, minWidth: 240, minHeight: 96 };

describe("planGrid", () => {
  it("single card fills the grid in one column", () => {
    expect(planGrid(800, 600, 1, opts)).toEqual({ columns: 1, scroll: false });
  });

  it("empty count yields zero columns (empty state)", () => {
    expect(planGrid(800, 600, 0, opts)).toEqual({ columns: 0, scroll: false });
  });

  it("picks the column count whose card ratio is closest to PHI", () => {
    // W=1200 H=700 count=10: c=4 (ratio 1.29, score 0.33) beats c=3 (2.35),
    // c=2 (4.5), c=1 (19.7); c>=5 drops below minWidth.
    expect(planGrid(1200, 700, 10, opts)).toEqual({ columns: 4, scroll: false });
  });

  it("prefers more columns on wider windows", () => {
    // W=1600 H=700 count=12: c=4 (ratio 1.73, score 0.11) beats c=5 (1.38) and c=3.
    expect(planGrid(1600, 700, 12, opts)).toEqual({ columns: 4, scroll: false });
  });

  it("respects a custom phi target", () => {
    // phi=1.4: c=5 (ratio 1.38, score 0.02) beats c=4 (1.73, score 0.33).
    expect(planGrid(1600, 700, 12, { ...opts, phi: 1.4 })).toEqual({ columns: 5, scroll: false });
  });

  it("falls back to scrolling when cards get too short for the row count", () => {
    // W=520 H=200 count=6: every c fails minHeight (c=1 h=25, c=2 h=60) or minWidth.
    expect(planGrid(520, 200, 6, opts)).toEqual({ columns: 2, scroll: true });
  });

  it("falls back to one column when the window is narrower than minWidth", () => {
    expect(planGrid(200, 600, 3, opts)).toEqual({ columns: 1, scroll: true });
  });

  it("stays non-scrolling when a column count just meets minWidth", () => {
    // c=1 gives cardW 250 >= 240, ratio 1.5625 (score 0.0555).
    expect(planGrid(250, 500, 3, opts)).toEqual({ columns: 1, scroll: false });
  });

  it("scroll fallback keeps as many minWidth columns as fit", () => {
    // W=520 H=500 count=10: c=1 h=41 and c=2 h=92 both fail minHeight,
    // c>=3 fails minWidth; scroll kicks in with floor((520+10)/250) = 2 columns.
    expect(planGrid(520, 500, 10, opts)).toEqual({ columns: 2, scroll: true });
  });
});
