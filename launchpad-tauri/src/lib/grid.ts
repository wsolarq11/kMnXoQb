// Grid layout planning: fixed card size. Columns are computed from the
// container width alone (cardWidth + gap), so card dimensions never change
// with the item count or window size — only the column count does, and the
// grid scrolls when rows exceed the container height.

export const CARD_WIDTH = 260;
export const CARD_HEIGHT = 120;
export const DEFAULT_GAP = 10;

// Example: planGrid(1200, 12) -> 4 columns  ((1200 + 10) / 270 = 4.48 -> 4)
export function planGrid(width: number, count: number): number {
  if (count <= 0) {
    return 0;
  }
  return Math.max(1, Math.floor((width + DEFAULT_GAP) / (CARD_WIDTH + DEFAULT_GAP)));
}
