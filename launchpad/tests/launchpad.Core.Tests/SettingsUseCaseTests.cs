using Launchpad.Core.Models;
using Launchpad.UseCases;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class SettingsUseCaseTests
{
    private static LaunchItem Item(string name) => new()
    {
        Name = name,
        Directory = @"D:\x",
        Command = "snow",
        Confirm = false,
        Id = name,
        Selected = false,
    };

    [Fact]
    public void PushHistoryMany_PushesAllInOrder()
    {
        var settings = new AppSettings { LaunchHistory = ["old"] };

        var result = SettingsUseCase.PushHistoryMany(settings, [Item("a"), Item("b")], failedIndexes: new HashSet<int>());

        Assert.Equal(["b", "a", "old"], result.LaunchHistory);
    }

    [Fact]
    public void PushHistoryMany_SkipsFailedIndexes()
    {
        var settings = new AppSettings();

        var result = SettingsUseCase.PushHistoryMany(settings, [Item("a"), Item("b"), Item("c")], failedIndexes: new HashSet<int> { 1 });

        Assert.Equal(["c", "a"], result.LaunchHistory);
    }

    [Fact]
    public void PushHistoryMany_ReordersDuplicatesToFront()
    {
        var settings = new AppSettings { LaunchHistory = ["a", "b"] };

        var result = SettingsUseCase.PushHistoryMany(settings, [Item("b")], failedIndexes: new HashSet<int>());

        Assert.Equal(["b", "a"], result.LaunchHistory);
    }
}
