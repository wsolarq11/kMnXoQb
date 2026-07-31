using Launchpad.UseCases;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class LaunchUseCaseTests
{
    private static LaunchItem Item(string command = "snow", bool confirm = false)
        => new()
        {
            Name = "demo",
            Directory = @"D:\projects\demo",
            Command = command,
            Confirm = confirm,
            Id = "demo",
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
    public void PushHistory_PrependsAndCapsAtTen()
    {
        var history = new List<string> { "a", "b", "c" };

        var result = LaunchUseCase.PushHistory(history, "new", max: 3);

        Assert.Equal(["new", "a", "b"], result);
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
