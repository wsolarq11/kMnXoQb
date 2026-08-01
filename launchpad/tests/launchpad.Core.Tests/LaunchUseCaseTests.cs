using System.ComponentModel;
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
    public void TryLaunch_ReturnsSuccessOnOk()
    {
        var useCase = UseCase(out _, "wt.exe");

        Assert.False(useCase.TryLaunch(Item()).IsError);
    }

    [Fact]
    public void TryLaunch_ReturnsStructuredErrorOnSpawnFailure()
    {
        var useCase = new LaunchUseCase(new ThrowingSpawner(), new FakeTerminalDetector("wt.exe"));

        var error = useCase.TryLaunch(Item());

        Assert.True(error.IsError);
        Assert.Contains("invalid directory", error.FirstError.Description);
        Assert.Equal("Launch.Unknown", error.FirstError.Code);
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

    /// <summary>Throws the given Win32 error code for every spawn.</summary>
    internal sealed class Win32ThrowingSpawner(int nativeErrorCode) : IProcessSpawner
    {
        public void Launch(LaunchPlan plan) =>
            throw new Win32Exception(nativeErrorCode);
    }

    [Fact]
    public void TryLaunch_PathNotFoundWithExistingWorkingDir_ReportsExecutable()
    {
        var useCase = new LaunchUseCase(new Win32ThrowingSpawner(3), new FakeTerminalDetector("wt.exe"));
        var item = Item() with { Directory = Directory.GetCurrentDirectory() };

        var error = useCase.TryLaunch(item);

        Assert.Equal("Launch.ProcessNotFound", error.FirstError.Code);
    }

    [Fact]
    public void TryLaunch_PathNotFoundWithMissingWorkingDir_ReportsWorkingDirectory()
    {
        var useCase = new LaunchUseCase(new Win32ThrowingSpawner(3), new FakeTerminalDetector("wt.exe"));
        var missing = Path.Combine(Path.GetTempPath(), "launchpad-definitely-missing-" + Guid.NewGuid().ToString("N"));
        var item = Item() with { Directory = missing };

        var error = useCase.TryLaunch(item);

        Assert.Equal("Launch.WorkingDirectoryMissing", error.FirstError.Code);
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
