# Design: Flutter + Rinf + Rust Architecture

## Layer Diagram

```
┌─────────────────────────────────────┐
│  Flutter UI (Dart)                  │
│  lib/screens/   lib/widgets/        │
│  Material Design, Navigator 2.0     │
├─────────────────────────────────────┤
│  Generated FFI Bindings             │
│  (flutter_rust_bridge or Rinf)      │
├─────────────────────────────────────┤
│  Rust Business Logic (unchanged)    │
│  config.rs  launch.rs  types.rs     │
│  + api.rs (FFI export surface)      │
└─────────────────────────────────────┘
```

## FFI Bridge: Rinf over flutter_rust_bridge

**Decision: Rinf** (2.7k stars, MIT, 2.7k stars)

Reasoning:
- Lighter than flutter_rust_bridge: no codegen daemon, fewer build steps
- Uses Dart FFI directly, no protobuf/message serialization overhead
- Messages are JSON over Dart `SendPort`/`ReceivePort`
- Works with `flutter run` hot reload (Dart side only; Rust rebuilds on `flutter run` restart)

### Data Flow

```
User taps "Launch" on card
  → Flutter sends message { "type": "launch", "id": "snow_startup" }
  → Rinf routes to Rust handler
  → Rust launch.rs: launch(&item) → spawns process
  → Rust sends response { "type": "launched", "success": true }
  → Flutter updates status bar
```

### API Surface (Rust → Flutter)

```rust
// api.rs — functions exposed to Flutter
pub fn read_items(config_dir: String) -> Vec<LaunchItem> { ... }
pub fn write_items(config_dir: String, items: Vec<LaunchItem>) -> Result<()> { ... }
pub fn read_settings(config_dir: String) -> AppSettings { ... }
pub fn write_settings(config_dir: String, settings: AppSettings) -> Result<()> { ... }

// These are NOT called via FFI — CLI uses them directly:
pub fn launch(item: LaunchItem) -> Result<Child> { ... }  // wrapped in api.rs for Flutter
pub fn plan(item: LaunchItem) -> LaunchPlan { ... }
pub fn is_dangerous(command: &str) -> bool { ... }
pub fn dangerous_reason(command: &str) -> Option<&str> { ... }
```

### State Ownership

- **Rust owns the data**: config.json, settings.json, process spawning
- **Flutter owns the UI**: widget tree, animations, navigation, theme
- **No shared mutable state**: Flutter sends events, Rust responds with results
- Each Flutter action is a request-response cycle through Rinf messages

## Project Structure

```
launchpad/
├── rust/
│   ├── Cargo.toml
│   └── src/
│       ├── lib.rs         ← re-exports + Rinf message handler registration
│       ├── config.rs      ← from current project (unchanged)
│       ├── launch.rs      ← from current project (unchanged)
│       ├── types.rs       ← from current project (minor: remove JsonSchema derive)
│       └── api.rs         ← NEW: FFI entry points for Flutter messages
├── lib/
│   ├── main.dart          ← MaterialApp + GoRouter + theme
│   ├── screens/
│   │   └── home_screen.dart   ← main launcher UI
│   ├── widgets/
│   │   ├── launch_card.dart   ← adaptive card widget
│   │   ├── search_bar.dart    ← search input
│   │   ├── edit_dialog.dart   ← add/edit item form
│   │   └── batch_bar.dart     ← select all + launch selected
│   ├── models/
│   │   └── launch_item.dart   ← Dart mirror of Rust LaunchItem
│   └── services/
│       └── rust_bridge.dart   ← Rinf message send/receive helpers
├── pubspec.yaml
├── rinf.yaml                ← Rinf config (message definitions)
└── CLAUDE.md                ← updated with Dart conventions
```

## Widget Tree (Home Screen)

```
MaterialApp
└── Scaffold
    ├── AppBar
    │   ├── Title: "WT Launcher"
    │   ├── ThemeSwitch (dark/light toggle)
    │   ├── ConfirmToggle (global confirm switch)
    │   └── AddButton ("+ New")
    ├── Body: Column
    │   ├── StatsRow (ITEMS count + RECENT)
    │   ├── SearchBar (TextField)
    │   ├── BatchBar (Select All + Launch Selected)
    │   └── Expanded
    │       └── GridView.builder (adaptive columns)
    │           └── LaunchCard (per item)
    │               ├── Row: Name + Edit/Del/Reorder/Select
    │               ├── Directory (monospace, grey)
    │               ├── Command (monospace, dark grey)
    │               └── Row: Tags + DangerBadge
    └── BottomBar: StatusText
```

## Card Layout Strategy

Flutter's `GridView.builder` with `SliverGridDelegateWithMaxCrossAxisExtent`:
```dart
GridView.builder(
  gridDelegate: SliverGridDelegateWithMaxCrossAxisExtent(
    maxCrossAxisExtent: 320,  // max card width before wrapping
    childAspectRatio: 2.8,    // height = width / 2.8
    crossAxisSpacing: 12,
    mainAxisSpacing: 12,
  ),
  itemCount: items.length,
  itemBuilder: (context, index) => LaunchCard(item: items[index]),
)
```

No pixel math. Resize → Flutter recalculates columns automatically.

## Theme

```dart
ThemeData(
  brightness: isDark ? Brightness.dark : Brightness.light,
  colorScheme: ColorScheme.fromSeed(seedColor: Colors.blueGrey),
  useMaterial3: true,
)
```

One toggle, Material 3 handles all widget colors.

## Route Design

```
/                   → HomeScreen (launcher grid)
/edit/:id           → EditDialog (add/edit item, pushed as full-screen dialog)
/confirm/:id        → ConfirmDialog (launch confirmation)
```

## Risk & Rollback

| Risk | Mitigation |
|------|-----------|
| Rinf build breaks on flutter upgrade | Pin Rinf version, CI validates build |
| FFI latency for large item lists | Batch load: read all items once at startup into Dart state |
| CLI regression | `launchpad-rs` CLI binary built separately from `rust/` crate |
| Config backward compat | Same `serde` types, same JSON schema; no migration needed |

### Rollback Plan
- Old egui code preserved in git history (tag `v0.1.0-egui` before migration)
- `rust/` core logic unchanged — if Flutter UI fails, switch back to egui shell
- CLI binary independent of Flutter — always works
