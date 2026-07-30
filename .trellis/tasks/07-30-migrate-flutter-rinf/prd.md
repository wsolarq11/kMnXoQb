# PRD: Migrate launchpad-rs from egui to Flutter + Rinf + Rust

## Background

Current launchpad-rs uses egui (immediate-mode GUI). Limitations:
- CJK font loading requires manual setup (tofu/口字形乱码)
- No built-in adaptive layout (cards hardcoded to 280px)
- No animation system, no built-in form validation, no routing
- Every new UI feature requires hand-writing render math

Target: Flutter (declarative UI) + Rinf (Rust FFI bridge) — same Rust business logic, Flutter handles all rendering.

## Requirements

### R1: Architecture — Rust core, Flutter shell
- `rust/`: config.rs, launch.rs, types.rs — kept from current project, minimized changes
- `lib/`: Flutter UI layer — all widgets, screens, navigation
- `flutter_rust_bridge` or `rinf` for type-safe FFI
- Rust never calls Flutter directly; Flutter calls Rust via generated bindings

### R2: CJK rendering — zero-config
- All CJK text renders correctly without any font loading code in app code
- System fonts used automatically

### R3: Adaptive card layout
- Cards fill available width, wrapping into multiple columns based on window size
- No hardcoded pixel widths
- Long directory paths elide gracefully when space is insufficient

### R4: Feature parity with egui version
- All current launch items load and display
- Search/filter by name, directory, command
- Add/Edit/Delete launch items
- Reorder items
- Batch select + launch
- Launch single item with confirmation dialog
- Dangerous command detection + warning display
- Dark/Light theme toggle
- Config file persistence (backward compatible with existing config.json)

### R5: Animations
- Card hover effects
- Dialog open/close transitions

### R6: Native platform look
- Material Design widgets
- Native window frame
- Platform-standard keyboard shortcuts

### R7: CLI preserved
- All 4 subcommands (list, check, launch, dry-run) remain functional
- CLI is pure Rust, no Flutter involved
- Same config.json backward compatibility

## Acceptance Criteria

- [ ] 1. `cargo run` (CLI) — all 4 subcommands work identically to current
- [ ] 2. `flutter run` (GUI) — all items render with zero CJK tofu
- [ ] 3. Resize window: cards reflow automatically
- [ ] 4. Edit dialog: form validates (empty name/cmd rejected with inline error)
- [ ] 5. Theme toggle: dark/light, persists to settings.json
- [ ] 6. Launch: terminal opens with correct command in correct directory
- [ ] 7. `flutter build windows` produces standalone .exe
- [ ] 8. All 12 existing Rust tests pass unchanged
- [ ] 9. Config backward compatible: existing config.json loads without migration

## Out of Scope (v1)

- Mobile (iOS/Android) — architecture supports but no testing
- Web target
- Hot reload of Rust code (Dart UI hot reload works)
- Settings migration (AppSettings regenerated with defaults on first run)
