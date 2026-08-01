import { useLayoutEffect, useRef, useState, type RefObject } from "react";
import {
  DEFAULT_GAP,
  DEFAULT_MIN_HEIGHT,
  DEFAULT_MIN_WIDTH,
  planGrid,
  type GridPlan,
} from "../lib/grid";

const RESIZE_DEBOUNCE_MS = 100;

// .item-grid content box = border-box minus its padding (App.css).
// planGrid must receive the content box, else minWidth/minHeight thresholds
// shift by the padding (32x28px) and cards come out narrower than planned.
const GRID_PAD_X = 16;
const GRID_PAD_Y = 14;

const OPTIONS = {
  gap: DEFAULT_GAP,
  minWidth: DEFAULT_MIN_WIDTH,
  minHeight: DEFAULT_MIN_HEIGHT,
};

// Observes the grid container and replans columns on every size change
// (window resize, header growth, etc.), debounced to 100ms. useLayoutEffect
// so the first paint never shows a stale single-column layout.
export function useGridPlan(count: number): { ref: RefObject<HTMLDivElement | null>; plan: GridPlan } {
  const ref = useRef<HTMLDivElement>(null);
  const [plan, setPlan] = useState<GridPlan>({ columns: 1, scroll: false });

  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) {
      return;
    }
    const recompute = (width: number, height: number) => {
      setPlan(planGrid(width, height, count, OPTIONS));
    };
    let timer: number | undefined;
    const observer = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (!entry) {
        return;
      }
      window.clearTimeout(timer);
      timer = window.setTimeout(
        () => recompute(entry.contentRect.width, entry.contentRect.height),
        RESIZE_DEBOUNCE_MS,
      );
    });
    observer.observe(el);
    const rect = el.getBoundingClientRect();
    recompute(rect.width - GRID_PAD_X * 2, rect.height - GRID_PAD_Y * 2);
    return () => {
      window.clearTimeout(timer);
      observer.disconnect();
    };
  }, [count]);

  return { ref, plan };
}
