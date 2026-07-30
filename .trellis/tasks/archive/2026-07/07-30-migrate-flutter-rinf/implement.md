# Implement: Flutter + Rinf Migration

## Phase 0: Scaffold

- [ ] 0.1 Install Flutter SDK (if not present), verify `flutter doctor`
- [ ] 0.2 Install Rinf CLI: `cargo install rinf`
- [ ] 0.3 Create Flutter+Rinf project: `rinf new launchpad` in project root
- [ ] 0.4 Copy existing Rust source into `rust/src/`: config.rs, launch.rs, types.rs
- [ ] 0.5 Verify Rust side compiles: `cd rust && cargo build`
- [ ] 0.6 Define Rinf messages in `rinf.yaml` (read_items, write_items, launch, plan, settings)
- [ ] 0.7 Verify `flutter run` launches empty scaffold window

**Checkpoint:** `flutter run` + `cargo build` both green. Rust core compiles unchanged.

## Phase 1: Data Layer (Rust ↔ Flutter)

- [ ] 1.1 Write `rust/src/api.rs` — FFI message handlers wrapping config.rs/launch.rs
- [ ] 1.2 Generate Dart bindings: `rinf message`
- [ ] 1.3 Write Dart `LaunchItem` model class (mirrors Rust struct)
- [ ] 1.4 Write `lib/services/rust_bridge.dart` — typed send/receive wrappers
- [ ] 1.5 Test: Flutter reads items from config.json via Rust, displays count

**Checkpoint:** Flutter app shows "32 items loaded" from Rust config. CJK text renders correctly.

## Phase 2: Home Screen UI

- [ ] 2.1 Build `HomeScreen`: AppBar + StatsRow + SearchBar + GridView scaffold
- [ ] 2.2 Build `LaunchCard` widget: name, directory, command, tags, danger badge
- [ ] 2.3 Build `BatchBar`: Select All toggle + Launch Selected button
- [ ] 2.4 Implement search filter (client-side, filters visible items)
- [ ] 2.5 Wire theme toggle (dark/light, persisted via Rust)
- [ ] 2.6 Verify: resize window, cards reflow. No tofu anywhere.

**Checkpoint:** Full feature parity with egui GUI. All items visible, search works, theme toggles.

## Phase 3: Edit Dialog

- [ ] 3.1 Build `EditDialog` form: name, directory (with Browse), command, terminal, confirm toggle
- [ ] 3.2 Add form validation (name required, command required, inline errors)
- [ ] 3.3 Add real-time directory existence check
- [ ] 3.4 Implement Save (add/edit) and Delete via Rust FFI
- [ ] 3.5 Implement file picker for directory Browse button

**Checkpoint:** Can add/edit/delete items. Validation works. Config persists.

## Phase 4: Launch & Confirm

- [ ] 4.1 Build `ConfirmDialog`: shows item name + command + directory
- [ ] 4.2 Implement launch via Rust FFI (plan + execute)
- [ ] 4.3 Implement batch launch (iterate selected items, confirm each if enabled)
- [ ] 4.4 Implement launch history (update recent in settings)
- [ ] 4.5 Handle launch errors (show snackbar with error message)

**Checkpoint:** Can launch items. Confirmation dialog works. Status bar updates.

## Phase 5: Polish

- [ ] 5.1 Add card hover animation (scale + shadow)
- [ ] 5.2 Add dialog open/close animation (fade + slide)
- [ ] 5.3 Add reorder buttons (▲▼) via item swap + save
- [ ] 5.4 Verify all 12 existing Rust tests still pass
- [ ] 5.5 `flutter build windows` produces working .exe

**Checkpoint:** Full feature parity + animations. Windows binary works.

## Phase 6: CLI Preservation

- [ ] 6.1 Verify `cargo run -- list <config>` works (CLI is independent of Flutter)
- [ ] 6.2 Verify `cargo run -- launch <config> <id>` works
- [ ] 6.3 Verify `cargo run -- launch --dry-run <config> <id>` works
- [ ] 6.4 Verify `cargo run -- check <config>` works (JSON output + exit codes)

**Checkpoint:** All CLI subcommands unchanged. Backward compatible.

## Phase 7: Documentation

- [ ] 7.1 Update CLAUDE.md with Flutter + Rust architecture
- [ ] 7.2 Write `flutter run` / `flutter build` commands
- [ ] 7.3 Update CI if needed (Flutter SDK in matrix)

## Verification Commands

```bash
# Rust tests
cd rust && cargo test

# Flutter analyze
flutter analyze

# Flutter build
flutter build windows

# CLI (unchanged)
cargo run --release -- list ../config/config.json
cargo run --release -- launch --dry-run ../config/config.json snow_startup
```
