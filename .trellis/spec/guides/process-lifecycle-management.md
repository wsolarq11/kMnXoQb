# Process Lifecycle Management

## Principle

**Do not spawn real OS processes in automated tests.** Terminal processes cannot be reliably terminated across platforms (Windows Terminal delegates tabs to a persistent `WindowsTerminal.exe`; Unix pseudo-terminals may detach). Automated test coverage of the launch path stops at `dry_run()` (build → populate).

## Rationale

`TerminalLauncher::launch()` creates real processes. Attempting to test this path automatically:

- **Windows**: `wt.exe` acts as a client to `WindowsTerminal.exe`. `TerminateProcess(wt_handle)` does not close the tab — the window persists.
- **macOS/Linux**: `posix_spawnp` may create process groups that resist `SIGTERM`.
- **CI**: Headless environments may lack terminal emulators entirely, causing false failures.
- **User desktop**: Orphaned terminal windows accumulate after test runs.

The populate path (`LaunchPlanBuilder::build` → `TerminalLauncher::populate`) covers the entire business logic of launch preparation without creating a process. The final `CreateProcessW` / `posix_spawnp` call is a thin OS wrapper that can only be validated manually or via integration smoke tests.

## Rules

### R1: Automated tests stop at `dry_run()`

```cpp
// COVERED by tests: build → populate path
TEST_CASE("dry_run: returns valid LaunchPlan") {
    auto plan = app.dry_run(item_id);
    CHECK(plan->executable == "wt.exe");
    CHECK(!plan->args.empty());
}

// NOT covered by automated tests: actual process spawn
// Validated manually: wt-launcher launch "id"
```

### R2: `launch()` returns `ProcessHandle`

The `launch()` method returns `std::expected<ProcessHandle, Error>` — the caller owns the handle. In CLI/GUI contexts, the handle is discarded and the process runs independently. This is correct: `CloseHandle` (Windows) or scope exit (Unix) detaches without killing.

### R3: `kill()` is available but not used in tests

`ProcessHandle::kill()` exists for programmatic use (e.g., CLI could implement a `--timeout` flag). Automated tests do not call it because it cannot guarantee cleanup on all platforms.

### R4: Manual verification covers the `launch()` path

The real launch path is verified by:

```
wt-launcher launch "item-id"              # CLI: launches real terminal
wt-launcher --check config.json           # covers all dry-run paths
```

## Scope

- `tests/core/` — pure functions only, zero process spawning
- `tests/shell/` — stops at `dry_run()`; validates launch rejection paths (empty command, unknown id) without spawning
- `tests/platform/` — platform abstractions only; no real terminal spawning
- Manual / smoke — actual process spawning verified interactively

## Enforcement

- Code review: no `app.launch()` call in test code except those that verify rejection (empty command, unknown id) — which never reach the spawn call
- These rejection tests are safe because `validate_rules()` fails before `TerminalLauncher::create()` is reached
