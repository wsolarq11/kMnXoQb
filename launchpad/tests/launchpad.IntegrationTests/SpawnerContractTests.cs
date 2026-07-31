using System.ComponentModel;
using Launchpad.Core.Models;
using Launchpad.Infrastructure;
using Xunit;

namespace Launchpad.IntegrationTests;

/// <summary>
/// ProcessSpawner contract: zero-shell argv spawn succeeds for valid plans and
/// raises Win32Exception for invalid directories (the error source that
/// LaunchUseCase.TryLaunch maps to structured errors).
/// </summary>
public sealed class SpawnerContractTests
{
    [Fact]
    public void Launch_StartsProcessForValidPlan()
    {
        var spawner = new ProcessSpawner();
        var plan = new LaunchPlan
        {
            Executable = "pwsh.exe",
            Args = ["-Command", "exit"], // exits on its own; no orphan process
            WorkingDirectory = Path.GetTempPath(),
        };

        var ex = Record.Exception(() => spawner.Launch(plan));

        Assert.Null(ex);
    }

    [Fact]
    public void Launch_ThrowsWin32PathNotFound_ForMissingWorkingDirectory()
    {
        var spawner = new ProcessSpawner();
        var plan = new LaunchPlan
        {
            Executable = "pwsh.exe",
            Args = [],
            WorkingDirectory = @"D:\definitely-not-a-real-launchpad-dir-xyz",
        };

        var ex = Assert.Throws<Win32Exception>(() => spawner.Launch(plan));

        Assert.Equal(267, ex.NativeErrorCode); // ERROR_DIRECTORY — Process.Start reports an invalid dir name
    }

    [Fact]
    public void Launch_ThrowsWin32FileNotFound_ForMissingExecutable()
    {
        var spawner = new ProcessSpawner();
        var plan = new LaunchPlan
        {
            Executable = "definitely-not-an-exe-xyz.exe",
            Args = [],
            WorkingDirectory = Path.GetTempPath(),
        };

        var ex = Assert.Throws<Win32Exception>(() => spawner.Launch(plan));

        Assert.Equal(2, ex.NativeErrorCode); // ERROR_FILE_NOT_FOUND
    }
}
