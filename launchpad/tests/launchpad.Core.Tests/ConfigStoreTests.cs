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
    public void Constructor_CreatesConfigDirectory()
    {
        var nested = Path.Combine(_dir, "a", "b");
        var store = new ConfigStore(nested);

        Assert.True(Directory.Exists(nested));
        Assert.NotNull(store);
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
    public void ReadItems_CorruptWithValidBackup_RecoversAndNotes()
    {
        var store = NewStore();
        // Two writes so a backup exists (the first write has nothing to back up).
        store.WriteItems([Item("good")]);
        store.WriteItems([Item("good"), Item("backup")]);
        File.WriteAllText(Path.Combine(_dir, "config.json"), "{ not json");

        var items = store.ReadItems();

        // The backup holds the pre-overwrite state (first write: [good]).
        Assert.Single(items);
        Assert.Equal("good", items[0].Name);
        Assert.NotNull(store.LastRecoveryNote);
        // The good backup must survive recovery (WriteItems would have
        // overwritten it with the corrupt file; the recovery path must not).
        Assert.Contains("good", File.ReadAllText(Path.Combine(_dir, "config.json.bak")));
    }

    [Fact]
    public void ReadItems_CorruptWithCorruptBackup_RaisesWithBothDetails()
    {
        File.WriteAllText(Path.Combine(_dir, "config.json"), "{ not json");
        File.WriteAllText(Path.Combine(_dir, "config.json.bak"), "also not json");

        var ex = Assert.Throws<ConfigParseException>(() => NewStore().ReadItems());

        Assert.Contains("backup also unreadable", ex.Message);
    }

    [Fact]
    public void ReadItems_NoBackup_RaisesMentioningMissingBackup()
    {
        File.WriteAllText(Path.Combine(_dir, "config.json"), "{ not json");

        var ex = Assert.Throws<ConfigParseException>(() => NewStore().ReadItems());

        Assert.Contains("no backup available", ex.Message);
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
