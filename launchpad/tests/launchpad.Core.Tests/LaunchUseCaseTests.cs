using Launchpad.UseCases;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class LaunchUseCaseTests
{
    private static LaunchItem Item(string name = "demo", string command = "snow", bool confirm = false)
        => new()
        {
            Name = name,
            Directory = @"D:\projects\demo",
            Command = command,
            Confirm = confirm,
            Id = name.Replace(' ', '_'),
            Selected = false,
        };

    private static LaunchUseCase UseCase(out FakeSpawner spawner, params string[] available)
    {
        spawner = new FakeSpawner();
        return new LaunchUseCase(spawner, new FakeTerminalDetector(available));
    }

    [Fact]
    public void NeedsConfirm_TrueWhenGlobalOnAndItemOrDangerous()
    {
        var settings = new AppSettings { ConfirmEnabled = true };
        var useCase = UseCase(out _);

        Assert.True(useCase.NeedsConfirm(settings, Item(confirm: true)));
        Assert.True(useCase.NeedsConfirm(settings, Item(command: "claude --yolo")));
    }

    [Fact]
    public void NeedsConfirm_FalseWhenGlobalOff()
    {
        var useCase = UseCase(out _);

        Assert.False(useCase.NeedsConfirm(new AppSettings { ConfirmEnabled = false }, Item(confirm: true)));
        Assert.False(useCase.NeedsConfirm(new AppSettings { ConfirmEnabled = false }, Item(command: "claude --yolo")));
    }

    [Fact]
    public void Plan_DetectsAvailableTerminals()
    {
        var useCase = UseCase(out _, "wt.exe");

        var plan = useCase.Plan(Item());

        Assert.Equal("wt.exe", plan.Executable);
    }

    [Fact]
    public void Plan_FallsBackToCmdWhenNothingDetected()
    {
        var useCase = UseCase(out _);

        var plan = useCase.Plan(Item());

        Assert.Equal("cmd.exe", plan.Executable);
    }

    [Fact]
    public void Launch_SpawnsExactArgv()
    {
        var useCase = UseCase(out var spawner, "wt.exe");
        var item = Item(command: "snow --flag");

        useCase.Launch(item);

        var plan = Assert.Single(spawner.Plans);
        Assert.Equal("wt.exe", plan.Executable);
        Assert.Contains("snow --flag", plan.Args);
        Assert.Equal(@"D:\projects\demo", plan.WorkingDirectory);
    }

    [Fact]
    public void TryLaunch_ReturnsNullOnSuccess()
    {
        var useCase = UseCase(out _, "wt.exe");

        Assert.Null(useCase.TryLaunch(Item()));
    }

    [Fact]
    public void TryLaunch_ReturnsErrorMessageOnSpawnFailure()
    {
        var useCase = new LaunchUseCase(new ThrowingSpawner(), new FakeTerminalDetector("wt.exe"));

        var error = useCase.TryLaunch(Item());

        Assert.Contains("invalid directory", error);
    }

    [Fact]
    public void PushHistory_PrependsAndCapsAtTen()
    {
        var history = new List<string> { "a", "b", "c" };

        var result = LaunchUseCase.PushHistory(history, "new", max: 3);

        Assert.Equal(["new", "a", "b"], result);
    }

    [Fact]
    public void PushHistory_RemovesDuplicateBeforePrepending()
    {
        var history = new List<string> { "a", "b", "a" };

        var result = LaunchUseCase.PushHistory(history, "a", max: 10);

        Assert.Equal(["a", "b"], result);
    }

    [Fact]
    public void PushHistory_NoDuplicateWhenAbsent()
    {
        var result = LaunchUseCase.PushHistory(["x", "y"], "z", max: 10);

        Assert.Equal(["z", "x", "y"], result);
    }

    [Fact]
    public void RequireConfirm_ReturnsOnlyItemsNeedingConfirmation()
    {
        var settings = new AppSettings { ConfirmEnabled = true };
        var useCase = UseCase(out _);
        var items = new[]
        {
            Item(confirm: false),                    // 不需要
            Item(command: "claude --yolo", confirm: false),  // 危险 → 需要
            Item(confirm: true),                     // item.confirm → 需要
        };

        var result = useCase.RequireConfirm(settings, items);

        Assert.Equal([items[1], items[2]], result);
    }

    [Fact]
    public void RequireConfirm_EmptyWhenGlobalOff()
    {
        var useCase = UseCase(out _);
        var items = new[] { Item(command: "claude --yolo") };

        Assert.Empty(useCase.RequireConfirm(new AppSettings { ConfirmEnabled = false }, items));
    }

    [Fact]
    public void LaunchMany_AllSucceed()
    {
        var useCase = new LaunchUseCase(new FakeSpawner(), new FakeTerminalDetector("wt.exe"));
        var items = new[] { Item(command: "a"), Item(command: "b") };

        var (succeeded, failed) = useCase.LaunchMany(items);

        Assert.Equal(2, succeeded);
        Assert.Empty(failed);
    }

    [Fact]
    public void LaunchMany_CollectsFailedIndexes()
    {
        var items = new[] { Item(command: "ok"), Item(command: "fail"), Item(command: "ok2") };
        var useCase = new LaunchUseCase(new PartialFakeSpawner(), new FakeTerminalDetector("wt.exe"));

        var (succeeded, failed) = useCase.LaunchMany(items);

        Assert.Equal(2, succeeded);
        Assert.Equal([1], failed);
    }

    internal sealed class ThrowingSpawner : IProcessSpawner
    {
        public void Launch(LaunchPlan plan) =>
            throw new InvalidOperationException("invalid directory");
    }

    /// <summary>Throws when the plan contains the marker "fail" command.</summary>
    internal sealed class PartialFakeSpawner : IProcessSpawner
    {
        public void Launch(LaunchPlan plan)
        {
            if (plan.Args.Contains("fail"))
            {
                throw new InvalidOperationException("boom");
            }
        }
    }

    internal sealed class FakeSpawner : IProcessSpawner
    {
        public List<LaunchPlan> Plans { get; } = [];

        public void Launch(LaunchPlan plan) => Plans.Add(plan);
    }

    internal sealed class FakeTerminalDetector(params string[] available) : ITerminalDetector
    {
        private readonly HashSet<string> _available = new(available, StringComparer.OrdinalIgnoreCase);

        public bool TerminalAvailable(string name) => _available.Contains(name);
    }
}
