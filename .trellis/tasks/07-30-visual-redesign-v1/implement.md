# Implement: Visual Redesign

## Phase 1: Dependencies + Theme System

- [ ] 1.1 Add `lucide_icons` and `google_fonts` to pubspec.yaml
- [ ] 1.2 Create `lib/theme.dart` — colors, typography, spring curves, glass helper
- [ ] 1.3 Update `main.dart` MaterialApp to use custom theme with Google Fonts
- [ ] 1.4 Verify CJK renders correctly with Inter font
- [ ] 1.5 Remove all Material `Icons.*` references, replace with Lucide

## Phase 2: Source Split

- [ ] 2.1 Extract `_StatCard` → `lib/widgets/stat_card.dart`
- [ ] 2.2 Extract `_LaunchCard` + `_HoverCard` → `lib/widgets/launch_card.dart`
- [ ] 2.3 Extract `EditDialog` + `_DirectoryPickerDialog` → `lib/widgets/edit_dialog.dart`
- [ ] 2.4 Extract search bar + batch bar → `lib/widgets/search_bar.dart`
- [ ] 2.5 Extract `HomeScreen` → `lib/screens/home_screen.dart`
- [ ] 2.6 `main.dart` under 80 lines (app entry + theme only)

## Phase 3: Glassmorphism

- [ ] 3.1 Build `GlassCard` wrapper widget
- [ ] 3.2 Apply to LaunchCard (backdrop blur + border glow)
- [ ] 3.3 Apply to dialog backgrounds
- [ ] 3.4 Neon glow edge on hover

## Phase 4: Spring Physics

- [ ] 4.1 Replace `AnimatedScale` with `spring` animation on card hover
- [ ] 4.2 Dialog open/close spring pop
- [ ] 4.3 Custom `SpringScrollPhysics` for GridView
- [ ] 4.4 Staggered animation on page load (cards fade in with delay)

## Phase 5: Typography

- [ ] 5.1 Apply Inter to all text (with Google Fonts caching)
- [ ] 5.2 Apply JetBrains Mono to commands/directories
- [ ] 5.3 Oversized stat numbers (36px, weight 900)
- [ ] 5.4 Letter-spacing on labels (0.5px uppercase)

## Phase 6: Immersive Empty State

- [ ] 6.1 Build subtle animated background for empty state
- [ ] 6.2 Grid mesh or particle animation using `CustomPainter`
- [ ] 6.3 Pulsing center text with spring breathe animation

## Phase 7: Verification

- [ ] 7.1 `flutter analyze` — zero issues
- [ ] 7.2 `flutter build windows` — success
- [ ] 7.3 All features: launch, edit, delete, search, batch, theme toggle
- [ ] 7.4 CJK text: no tofu on all fonts
- [ ] 7.5 `grep -r "Icons\." lib/` returns zero hits
- [ ] 7.6 Rust CLI unchanged: all 12 tests pass
