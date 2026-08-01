// Grid layout planning: pick the column count whose card aspect ratio is
// closest to the golden ratio (1.618), so the whole grid fills the window
// with all cards visible. When no column count can keep every card at least
// minWidth wide AND minHeight tall, fall back to a scrollable grid at the
// minimum readable size.

export const PHI = 1.618;
export const DEFAULT_GAP = 10;
export const DEFAULT_MIN_WIDTH = 240;
export const DEFAULT_MIN_HEIGHT = 96;

export interface GridPlanOptions {
  /** Gap between cards, px. */
  gap: number;
  /** Minimum readable card width, px. */
  minWidth: number;
  /** Minimum readable card height, px (3 text lines + padding). */
  minHeight: number;
  /** Target aspect ratio (width / height). Defaults to PHI. */
  phi?: number;
}

export interface GridPlan {
  columns: number;
  /** true = scroll fallback (columns sized at minWidth, vertical scroll). */
  scroll: boolean;
}

// Example: planGrid(1200, 700, 10, { gap: 10, minWidth: 240, minHeight: 96 })
//   -> { columns: 4, scroll: false }  (card ratio ~1.29, closest to PHI)
export function planGrid(
  width: number,
  height: number,
  count: number,
  options: GridPlanOptions,
): GridPlan {
  if (count <= 0) {
    return { columns: 0, scroll: false };
  }

  const phi = options.phi ?? PHI;
  let best: { columns: number; score: number } | null = null;

  for (let c = 1; c <= count; c++) {
    const rows = Math.ceil(count / c);
    const cardW = (width - options.gap * (c - 1)) / c;
    const cardH = (height - options.gap * (rows - 1)) / rows;
    if (cardW < options.minWidth || cardH < options.minHeight) {
      continue;
    }
    const score = Math.abs(cardW / cardH - phi);
    if (best === null || score < best.score) {
      best = { columns: c, score };
    }
  }

  if (best !== null) {
    return { columns: best.columns, scroll: false };
  }

  const columns = Math.max(1, Math.floor((width + options.gap) / (options.minWidth + options.gap)));
  return { columns, scroll: true };
}
