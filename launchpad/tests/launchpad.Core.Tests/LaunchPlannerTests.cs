using Launchpad.Core.Domain;
using Launchpad.Core.Models;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class LaunchPlannerTests
{
    private static LaunchItem Item(string command = "snow", string dir = @"D:\projects\demo", string? terminal = null)
        => new()
        {
            Name = "demo",
            Directory = dir,
            Command = command,
            Confirm = true,
            Id = "demo",
            Selected = false,
            Terminal = terminal,
        };

    [Fact]
    public void PlanWindows_PrefersWindowsTerminal()
    {
        var plan = LaunchPlanner.PlanWindows(Item(), wtAvailable: true, pwshAvailable: true);

        Assert.Equal("wt.exe", plan.Executable);
        Assert.Equal(
            ["new-tab", "-d", @"D:\projects\demo", "pwsh", "-NoExit", "-Command", "snow"],
            plan.Args);
        Assert.Equal(@"D:\projects\demo", plan.WorkingDirectory);
    }

    [Fact]
    public void PlanWindows_FallsBackToPwsh()
    {
        var plan = LaunchPlanner.PlanWindows(Item(), wtAvailable: false, pwshAvailable: true);

        Assert.Equal("pwsh.exe", plan.Executable);
        Assert.Equal(["-NoExit", "-Command", "cd 'D:\\projects\\demo'; snow"], plan.Args);
    }

    [Fact]
    public void PlanWindows_FallsBackToCmd()
    {
        var plan = LaunchPlanner.PlanWindows(Item(), wtAvailable: false, pwshAvailable: false);

        Assert.Equal("cmd.exe", plan.Executable);
        Assert.Equal(["/k", "cd /d \"D:\\projects\\demo\" && snow"], plan.Args);
    }

    [Fact]
    public void PlanWindows_UsesTerminalOverride_WhenPresent()
    {
        var plan = LaunchPlanner.PlanWindows(Item(terminal: "pwsh"), wtAvailable: true, pwshAvailable: true);

        Assert.Equal("pwsh", plan.Args[3]);
        Assert.Equal("pwsh", plan.TerminalOverride);
    }

    [Fact]
    public void PlanWindows_DefaultsTerminalToPwsh()
    {
        var plan = LaunchPlanner.PlanWindows(Item(), wtAvailable: true, pwshAvailable: true);

        Assert.Equal("pwsh", plan.Args[3]);
    }

    [Fact]
    public void PlanWindows_MarksDangerousCommands()
    {
        var plan = LaunchPlanner.PlanWindows(Item(command: "claude --dangerously-skip-permissions"), wtAvailable: true, pwshAvailable: true);

        Assert.True(plan.IsDangerous);
    }

    [Fact]
    public void EscapePwshQuotes_DoublesSingleQuotes()
    {
        Assert.Equal(@"D:\a''b", LaunchPlanner.EscapePwshQuotes(@"D:\a'b"));
    }

    [Fact]
    public void EscapeCmdQuotes_DoublesDoubleQuotes()
    {
        Assert.Equal(@"D:\a""""b", LaunchPlanner.EscapeCmdQuotes(@"D:\a""b"));
    }

    [Fact]
    public void PlanWindows_PwshFallback_EscapesDirectoryWithSingleQuote()
    {
        var plan = LaunchPlanner.PlanWindows(Item(dir: @"D:\a'b"), wtAvailable: false, pwshAvailable: true);

        Assert.Equal(["-NoExit", "-Command", "cd 'D:\\a''b'; snow"], plan.Args);
    }

    [Fact]
    public void PlanWindows_CmdFallback_EscapesDirectoryWithDoubleQuote()
    {
        var plan = LaunchPlanner.PlanWindows(Item(dir: @"D:\a""b"), wtAvailable: false, pwshAvailable: false);

        Assert.Equal("/k", plan.Args[0]);
        Assert.Equal("cd /d \"D:\\a\"\"b\" && snow", plan.Args[1]);
    }
}
