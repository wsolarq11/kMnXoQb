// Theme three-state wiring: "dark"/"light" force the CSS variables via the
// data-theme attribute; "system" removes the attribute so
// prefers-color-scheme decides (see App.css).

export function applyTheme(theme: string): void {
  const root = document.documentElement;
  if (theme === "dark" || theme === "light") {
    root.dataset.theme = theme;
  } else {
    delete root.dataset.theme;
  }
}
