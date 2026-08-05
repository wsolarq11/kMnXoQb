import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

// WCAG 2.1 AA contrast gates, asserted against the real token pairs the UI
// renders (matrix below). Values are the only source of truth in App.css;
// this test parses the CSS so palette edits cannot drift from the gates.
//   body text >= 4.5:1, component borders >= 3:1 (SC 1.4.11),
//   surface vs app >= 1.15 (M3 light baseline; dark >= 1.10),
//   dark fg-primary >= 14:1 (M3 on-surface baseline — 15.8:1 is the pure
//   white-text ceiling, no shipped theme reaches it).

const css = readFileSync(resolve(process.cwd(), "src/App.css"), "utf8");

type Vars = Record<string, string>;

function extractVars(css: string, selector: string): Vars {
  const start = css.indexOf(`${selector} {`);
  if (start === -1) {
    throw new Error(`CSS block not found: ${selector}`);
  }
  const brace = css.indexOf("{", start);
  const end = css.indexOf("}", brace);
  const vars: Vars = {};
  for (const line of css.slice(brace + 1, end).split("\n")) {
    const m = line.match(/^\s*(--[\w-]+)\s*:\s*(#[0-9a-fA-F]{6})\s*;?\s*$/);
    if (m) {
      vars[m[1]] = m[2];
    }
  }
  return vars;
}

function luminance(hex: string): number {
  const rgb = [0, 2, 4].map((i) => {
    const c = parseInt(hex.slice(1 + i, 3 + i), 16) / 255;
    return c <= 0.03928 ? c / 12.92 : Math.pow((c + 0.055) / 1.055, 2.4);
  });
  return 0.2126 * rgb[0] + 0.7152 * rgb[1] + 0.0722 * rgb[2];
}

function contrast(a: string, b: string): number {
  const [l1, l2] = [luminance(a), luminance(b)].sort((x, y) => y - x);
  return (l1 + 0.05) / (l2 + 0.05);
}

// [fg, bg, min, role]. "border" pairs are SC 1.4.11 non-text (>= 3:1);
// "layer" pairs measure surface separation; everything else is text.
const MATRIX: Array<[string, string, number, string]> = [
  ["--fg-primary", "--bg-surface", 4.5, "text"],
  ["--fg-secondary", "--bg-surface", 4.5, "text"],
  ["--fg-secondary", "--bg-header", 4.5, "text"],
  ["--fg-tertiary", "--bg-surface", 4.5, "text"],
  ["--fg-tertiary", "--bg-app", 4.5, "text"],
  ["--fg-mono", "--bg-code", 4.5, "text"],
  ["--accent", "--bg-surface", 4.5, "text"],
  ["--danger", "--bg-surface", 4.5, "text"],
  ["--on-accent", "--accent", 4.5, "text"],
  ["--on-danger", "--danger", 4.5, "text"],
  ["--border-subtle", "--bg-surface", 3.0, "border"],
  ["--border-strong", "--bg-surface", 3.0, "border"],
  ["--border-strong", "--bg-app", 3.0, "border"],
  // Real rendered separators: header/status bars draw their edge on
  // bg-header, stat-bar on bg-app, keycaps on bg-code.
  ["--border-strong", "--bg-header", 3.0, "border"],
  ["--border-strong", "--bg-code", 3.0, "border"],
  ["--fg-primary", "--bg-elevated", 4.5, "text"],
  ["--fg-secondary", "--bg-elevated", 4.5, "text"],
  ["--bg-surface", "--bg-app", 1.15, "layer"],
  ["--bg-elevated", "--bg-app", 1.15, "layer"],
];

const DARK_MATRIX: Array<[string, string, number, string]> = [
  ["--fg-primary", "--bg-surface", 14.0, "dark-text"],
  ["--fg-secondary", "--bg-surface", 4.5, "text"],
  ["--fg-secondary", "--bg-header", 4.5, "text"],
  ["--fg-tertiary", "--bg-surface", 4.5, "text"],
  ["--fg-tertiary", "--bg-app", 4.5, "text"],
  ["--fg-mono", "--bg-code", 4.5, "text"],
  ["--accent", "--bg-surface", 4.5, "text"],
  ["--danger", "--bg-surface", 4.5, "text"],
  ["--on-accent", "--accent", 4.5, "text"],
  ["--on-danger", "--danger", 4.5, "text"],
  ["--border-subtle", "--bg-surface", 3.0, "border"],
  ["--border-strong", "--bg-surface", 3.0, "border"],
  ["--border-strong", "--bg-app", 3.0, "border"],
  // Real rendered separators: header/status bars draw their edge on
  // bg-header, keycaps on bg-code.
  ["--border-strong", "--bg-header", 3.0, "border"],
  ["--border-strong", "--bg-code", 3.0, "border"],
  ["--fg-primary", "--bg-elevated", 4.5, "text"],
  ["--fg-secondary", "--bg-elevated", 4.5, "text"],
  ["--bg-surface", "--bg-app", 1.1, "layer"],
  ["--bg-elevated", "--bg-app", 1.1, "layer"],
];

function runMatrix(vars: Vars, matrix: Array<[string, string, number, string]>, label: string) {
  for (const [fg, bg, min, role] of matrix) {
    const fgColor = vars[fg];
    const bgColor = vars[bg];
    expect(fgColor, `${label}: ${fg} undefined`).toBeTruthy();
    expect(bgColor, `${label}: ${bg} undefined`).toBeTruthy();
    const actual = contrast(fgColor!, bgColor!);
    expect(actual, `${label}: ${fg} on ${bg} (${role})`).toBeGreaterThanOrEqual(min);
  }
}

describe("theme token contrast (WCAG AA)", () => {
  const light = extractVars(css, ":root");
  const dark = extractVars(css, ':root[data-theme="dark"]');

  it("light palette passes every role gate", () => {
    runMatrix(light, MATRIX, "light");
  });

  it("dark palette passes every role gate", () => {
    runMatrix(dark, DARK_MATRIX, "dark");
  });

  it("palette blocks declare the same token set", () => {
    expect(Object.keys(dark).sort()).toEqual(Object.keys(light).sort());
  });
});
