# Process Lifecycle Management

## Principle

**Do not spawn real OS processes in automated tests.** Terminal processes cannot be reliably terminated (Windows Terminal delegates tabs to a persistent `WindowsTerminal.exe`; the launcher spawns `wt.exe` as a client). Automated test coverage of the launch path stops at plan construction and argv assertion; the real spawn is verified manually.

## Rationale

`ProcessSpawner.Start()` creates real processes. Attempting to test this path automatically:

- **Windows**: `wt.exe` acts as a client to `WindowsTerminal.exe`. Killing the `wt.exe` handle does not close the tab — the window persists.
- **CI**: Headless environments may lack terminal emulators entirely, causing false failures.
- **User desktop**: Orphaned terminal windows accumulate after test runs.

The pure decision path (`LaunchPlanner.PlanWindows` → `LaunchPlan`) covers the entire business logic of launch preparation without creating a process. The final `Process.Start` call is a thin OS wrapper (`ProcessSpawner`, infrastructure layer) that can only be validated manually or via smoke tests.

## Rules

### R1: Automated tests stop at `PlanWindows`

```csharp
// COVERED by tests: pure plan construction + argv assertion
[Fact]
public void PlanWindows_WtAvailable_UsesWtExe()
{
    var plan = LaunchPlanner.PlanWindows(item, wtAvailable: true, pwshAvailable: true);
    Assert.Equal("wt.exe", plan.Executable);
    Assert.Equal(["new-tab", "-d", dir, terminal, "-NoExit", "-Command", item.Command], plan.Args);
}

// NOT covered by automated tests: actual process spawn
// Validated manually: launch app, click a launch item card
```

### R2: Use cases go through ports with fakes

`LaunchUseCase` depends on `IProcessSpawner`; tests inject a fake that records argv and returns success — the spawn boundary is mocked, launch rejection paths (empty command, unknown item, dangerous confirm) are asserted without spawning.

### R3: Spawner is infrastructure-only

`ProcessSpawner` (infrastructure layer) is the only place `System.Diagnostics.Process` is constructed. It is not covered by automated tests; correctness of argv is asserted at the planner/use-case level.

### R4: Manual verification covers the real launch path

The real launch path is verified by:

```
# run the app, click a launch item card -> real terminal opens
# or invoke the built exe with a configured item
dotnet run --project src/launchpad
```

## Scope

- `tests/launchpad.Core.Tests/` — pure functions + use cases with fakes, zero process spawning
- Manual / smoke — actual process spawning verified interactively (release build + window + second-instance mutex probe)

## Enforcement

- Code review: no `IProcessSpawner.Start` call in test code except via fakes; spawn assertions check argv recorded by the fake.
- Rejection tests are safe because validation happens before the spawn call is reached.
