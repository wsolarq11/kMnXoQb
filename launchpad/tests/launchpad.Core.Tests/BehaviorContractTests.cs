using Launchpad.Core.Domain;
using Launchpad.Core.Models;
using Launchpad.Core.Serialization;
using Launchpad.UseCases;
using VerifyXunit;
using Xunit;

namespace Launchpad.Core.Tests;

/// <summary>
/// Snapshot contracts pin the behavior surface that was aligned 1:1 with the
/// legacy Rust app. Any intended behavior change now requires a reviewed
/// snapshot diff instead of a silent drift (Verify fails on change).
/// </summary>
public sealed class BehaviorContractTests
{
    private static LaunchItem Item(string name, string command = "snow", bool confirm = true, bool selected = false)
        => new()
        {
            Name = name,
            Directory = @"D:\projects\demo dir",
            Command = command,
            Confirm = confirm,
            Id = name.Replace(' ', '_'),
            Selected = selected,
        };

    [Fact]
    public Task LaunchPlanner_AllTerminalPaths_Snapshot()
    {
        var item = Item("demo", "echo 'hi'; pwd", confirm: false);
        var plans = new[]
        {
            LaunchPlanner.PlanWindows(item, wtAvailable: true, pwshAvailable: true),
            LaunchPlanner.PlanWindows(item, wtAvailable: false, pwshAvailable: true),
            LaunchPlanner.PlanWindows(item, wtAvailable: false, pwshAvailable: false),
        };
        return Verify(plans.Select(p => new
        {
            p.Executable,
            p.Args,
            p.WorkingDirectory,
            p.IsDangerous,
        }));
    }

    [Fact]
    public Task LaunchPlanner_QuoteTrapDirectory_Snapshot()
    {
        var item = Item("quoted", "snow") with { Directory = @"D:\weird dir's\" };
        var plans = new[]
        {
            LaunchPlanner.PlanWindows(item, wtAvailable: false, pwshAvailable: true),
            LaunchPlanner.PlanWindows(item, wtAvailable: false, pwshAvailable: false),
        };
        return Verify(plans.Select(p => new { p.Executable, p.Args }));
    }

    [Fact]
    public Task LaunchHistory_Mutations_Snapshot()
    {
        var history = new List<string> { "old", "mid", "recent" };
        var pushed = LaunchUseCase.PushHistory(history, "mid");
        var pushedAgain = LaunchUseCase.PushHistory(pushed, "new");
        var capped = LaunchUseCase.PushHistory(Enumerable.Range(0, 12).Select(i => $"item{i}").ToList(), "fresh");

        var items = new[]
        {
            Item("a", selected: true),
            Item("b", selected: true),
            Item("c"),
        };
        var settings = new AppSettings { LaunchHistory = history };
        var many = SettingsUseCase.PushHistoryMany(settings, items, new HashSet<int> { 1 });

        return Verify(new
        {
            pushed,
            pushedAgain,
            capped,
            many.LaunchHistory,
        });
    }

    [Fact]
    public Task SelectionMutations_Snapshot()
    {
        var items = new[]
        {
            Item("a", selected: true),
            Item("b", selected: true),
            Item("c"),
        };

        var cleared = ItemUseCase.ClearSelection(items);
        var setById = ItemUseCase.SetSelectById(items, "b", true);
        var selectAll = ItemUseCase.ToggleSelectAll(items);

        return Verify(new
        {
            cleared = cleared.Select(i => (i.Name, i.Selected)),
            setById = setById.Select(i => (i.Name, i.Selected)),
            selectAll = selectAll.Select(i => (i.Name, i.Selected)),
        });
    }

    [Fact]
    public Task SerializedConfig_Snapshot()
    {
        var items = new[]
        {
            Item("demo", "snow --dangerously", confirm: false),
            Item("legacy") with { Terminal = "cmd", Tag = "internal", Group = "dev" },
        };
        var settings = new AppSettings
        {
            ConfirmEnabled = true,
            Theme = "dark",
            LaunchHistory = ["recent", "old"],
            WindowState = new WindowState { X = 100, Y = 200, Width = 900, Height = 700 },
        };

        return Verify(new
        {
            Items = System.Text.Json.JsonSerializer.Serialize(items, LauncherJson.Options),
            Settings = System.Text.Json.JsonSerializer.Serialize(settings, LauncherJson.Options),
        });
    }
}
