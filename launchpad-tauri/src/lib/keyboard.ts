// Global keyboard shortcuts:
// - Escape closes any open dialog / pending confirmation
// - Ctrl+F focuses the search box
// Item cards handle Enter themselves (tabIndex + onKeyDown, see ItemCard).

export function installGlobalKeys(
  onEscape: () => void,
  focusSearch: () => void,
): () => void {
  const handler = (e: KeyboardEvent) => {
    if (e.key === "Escape") {
      onEscape();
    } else if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "f") {
      e.preventDefault();
      focusSearch();
    }
  };
  window.addEventListener("keydown", handler);
  return () => window.removeEventListener("keydown", handler);
}
