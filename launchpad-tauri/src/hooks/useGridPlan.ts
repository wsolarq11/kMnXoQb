import { useLayoutEffect, useRef, useState, type RefObject } from "react";
import { planGrid } from "../lib/grid";

const RESIZE_DEBOUNCE_MS = 100;

// .item-grid content box = border-box minus its padding (App.css).
// planGrid must receive the content box, else the column count shifts by
// the padding (32x28px) and cards come out narrower than planned.
const GRID_PAD_X = 16;

// Observes the grid container and replans columns on every size change
// (window resize, header growth, etc.), debounced to 100ms. useLayoutEffect
// so the first paint never shows a stale single-column layout.
export function useGridPlan(count: number): {
  ref: RefObject<HTMLDivElement | null>;
  columns: number;
} {
  const ref = useRef<HTMLDivElement>(null);
  const [columns, setColumns] = useState(1);

  useLayoutEffect(() => {
    const el = ref.current;
    if (!el) {
      return;
    }
    const recompute = (width: number) => {
      setColumns(planGrid(width, count));
    };
    let timer: number | undefined;
    const observer = new ResizeObserver((entries) => {
      const entry = entries[0];
      if (!entry) {
        return;
      }
      window.clearTimeout(timer);
      timer = window.setTimeout(
        () => recompute(entry.contentRect.width),
        RESIZE_DEBOUNCE_MS,
      );
    });
    observer.observe(el);
    const rect = el.getBoundingClientRect();
    recompute(rect.width - GRID_PAD_X * 2);
    return () => {
      window.clearTimeout(timer);
      observer.disconnect();
    };
  }, [count]);

  return { ref, columns };
}
