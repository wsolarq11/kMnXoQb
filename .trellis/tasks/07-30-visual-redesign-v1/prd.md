# PRD: Awwwards-grade Visual Redesign for Flutter Launchpad

## Background

Current Flutter launchpad uses Material Design 3 defaults. Functional but visually generic.

Target: Awwwards / FWA / CSS Design Awards daily-site quality. The launcher is an interactive art canvas, not just a tool UI.

## Requirements

### R1: Lucide Icons — global replacement
- All Material icons replaced with Lucide equivalents
- Zero Material `Icons.*` references remain in lib/
- Use `lucide_icons` Flutter package

### R2: Glassmorphism + Depth
- Card backgrounds: frosted glass with backdrop blur
- Subtle border-edge glow on hover
- Dark base (#0A0A0A) with neon accent palette
- Depth via layered shadows

### R3: Spring Physics Animation
- Card hover: spring-scale with overshoot
- Dialog open/close: spring pop
- Scroll: custom mass-spring physics
- Status changes: staggered spring reveals

### R4: Experimental Typography
- Variable font for headings (Inter Variable or Geist)
- Monospace for commands/directories (JetBrains Mono or Geist Mono)
- Oversized headline numbers (ITEMS count)
- Letter-spacing on labels
- No system CJK tofu

### R5: Immersive Empty State
- Subtle particle or grid animation background
- Atmospheric motion canvas, not plain text

### R6: Functional Parity
- All existing features preserved: launch, edit, delete, search, batch, theme, confirm dialog, directory picker
- CLI unchanged (Rust core untouched)

### R7: Code Structure
- Split main.dart (940 lines) into: theme.dart, screens/, widgets/

## Acceptance Criteria

- [ ] All icons are Lucide; grep `Icons.` in lib/ returns zero hits
- [ ] Glassmorphism on cards (backdrop blur + border glow)
- [ ] Card hover has spring physics
- [ ] Dialog open/close has spring animation
- [ ] Custom scroll physics
- [ ] CJK renders correctly with new fonts
- [ ] All 12 Rust tests pass
- [ ] `flutter build windows` succeeds
- [ ] `flutter analyze` — zero issues
