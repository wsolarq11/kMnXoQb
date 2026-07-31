using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Launchpad.Infrastructure;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "launchpad-tests-" + Guid.NewGuid().ToString("N"));

    public ConfigStoreTests()
    {
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
    }

    private ConfigStore NewStore() => new(_dir);

    private static LaunchItem Item(string name)
        => new()
        {
            Name = name,
            Directory = @"D:\x",
            Command = "snow",
            Confirm = true,
            Id = name,
            Selected = false,
        };

    [Fact]
    public void ReadItems_ReturnsEmptyWhenFileMissing()
    {
        Assert.Empty(NewStore().ReadItems());
    }

    [Fact]
    public void WriteThenReadItems_RoundTrips()
    {
        var store = NewStore();
        store.WriteItems([Item("a"), Item("b")]);

        var items = store.ReadItems();

        Assert.Equal(2, items.Count);
        Assert.Equal("a", items[0].Name);
    }

    [Fact]
    public void WriteItems_CreatesBackupBeforeOverwrite()
    {
        var store = NewStore();
        store.WriteItems([Item("v1")]);
        store.WriteItems([Item("v2")]);

        Assert.True(File.Exists(Path.Combine(_dir, "config.json.bak")));
        var backup = File.ReadAllText(Path.Combine(_dir, "config.json.bak"));
        Assert.Contains("v1", backup);
    }

    [Fact]
    public void ReadItems_CorruptFile_RaisesConfigParseException()
    {
        File.WriteAllText(Path.Combine(_dir, "config.json"), "{ not json");

        var ex = Assert.Throws<ConfigParseException>(() => NewStore().ReadItems());

        Assert.Contains("config.json", ex.FilePath);
    }

    [Fact]
    public void ReadSettings_ReturnsDefaultsWhenFileMissing()
    {
        var settings = NewStore().ReadSettings();

        Assert.False(settings.ConfirmEnabled);
        Assert.Equal("system", settings.Theme);
        Assert.Empty(settings.LaunchHistory);
    }

    [Fact]
    public void WriteThenReadSettings_RoundTrips()
    {
        var store = NewStore();
        store.WriteSettings(new AppSettings
        {
            ConfirmEnabled = true,
            Theme = "dark",
            LaunchHistory = ["a"],
            WindowState = new WindowState { X = 1, Y = 2, Width = 900, Height = 700 },
        });

        var settings = store.ReadSettings();

        Assert.True(settings.ConfirmEnabled);
        Assert.Equal("dark", settings.Theme);
        Assert.Equal(["a"], settings.LaunchHistory);
        Assert.Equal(new WindowState { X = 1, Y = 2, Width = 900, Height = 700 }, settings.WindowState);
    }
}
