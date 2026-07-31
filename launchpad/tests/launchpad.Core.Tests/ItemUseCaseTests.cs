using Launchpad.UseCases;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class ItemUseCaseTests
{
    private static LaunchItem Item(string name, string command = "snow")
        => new()
        {
            Name = name,
            Directory = @"D:\projects\demo",
            Command = command,
            Confirm = true,
            Id = name.Replace(' ', '_'),
            Selected = false,
        };

    [Fact]
    public void NewItem_GeneratesIdFromName()
    {
        var item = ItemUseCase.NewItem("my tool", @"D:\x", "snow", confirm: true, terminal: "pwsh", existing: []);

        Assert.Equal("my_tool", item.Id);
        Assert.Equal("pwsh", item.Terminal);
    }

    [Fact]
    public void NewItem_BlankTerminalBecomesNull()
    {
        var item = ItemUseCase.NewItem("t", @"D:\x", "snow", confirm: true, terminal: "  ", existing: []);

        Assert.Null(item.Terminal);
    }

    [Fact]
    public void GenerateId_LowercasesAndSwapsSpaces()
    {
        Assert.Equal("my tool", "my tool".ToLowerInvariant());
        Assert.Equal("my_tool", ItemUseCase.GenerateId([], "My Tool"));
    }

    [Fact]
    public void GenerateId_AppendsSuffixOnCollision()
    {
        var existing = new[] { Item("a_b") };

        Assert.Equal("a_b_2", ItemUseCase.GenerateId(existing, "a b"));
    }

    [Fact]
    public void GenerateId_SkipsTakenSuffixes()
    {
        var existing = new[] { Item("a_b"), Item("a_b_2") };

        Assert.Equal("a_b_3", ItemUseCase.GenerateId(existing, "A B"));
    }

    [Fact]
    public void GenerateId_TrimsName()
    {
        Assert.Equal("t", ItemUseCase.GenerateId([], "  t  "));
    }

    [Fact]
    public void SaveItems_ReturnsSuccessOnOk()
    {
        var store = new FakeStore();
        var useCase = new ItemUseCase(store);

        var result = useCase.SaveItems([Item("a")]);

        Assert.False(result.IsError);
        Assert.Single(store.Saved);
    }

    [Fact]
    public void SaveItems_ReturnsStructuredErrorOnWriteFailure()
    {
        var useCase = new ItemUseCase(new ThrowingStore());

        var result = useCase.SaveItems([Item("a")]);

        Assert.True(result.IsError);
        Assert.Equal("Store.WriteFailed", result.FirstError.Code);
        Assert.Contains("config.json", result.FirstError.Description);
    }

    [Fact]
    public void Filter_MatchesNameDirectoryCommand_CaseInsensitive()
    {
        var items = new[] { Item("Alpha"), Item("Beta", "npm run dev") };

        Assert.Single(ItemUseCase.Filter(items, "alpha"));
        Assert.Single(ItemUseCase.Filter(items, "NPM"));
        Assert.Empty(ItemUseCase.Filter(items, "zzz"));
        Assert.Equal(2, ItemUseCase.Filter(items, "").Count);
    }

    [Fact]
    public void Upsert_AddsWhenIndexNull()
    {
        var result = ItemUseCase.Upsert([Item("a")], Item("b"), index: null);

        Assert.Equal(2, result.Count);
        Assert.Equal("b", result[1].Name);
    }

    [Fact]
    public void Upsert_ReplacesAtIndex()
    {
        var result = ItemUseCase.Upsert([Item("a"), Item("b")], Item("c"), index: 1);

        Assert.Equal("c", result[1].Name);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Delete_RemovesAtIndex()
    {
        var result = ItemUseCase.Delete([Item("a"), Item("b"), Item("c")], 1);

        Assert.Equal(["a", "c"], result.Select(i => i.Name));
    }

    [Fact]
    public void Move_SwapsAdjacentItems()
    {
        var result = ItemUseCase.Move([Item("a"), Item("b"), Item("c")], 1, -1);

        Assert.Equal(["b", "a", "c"], result.Select(i => i.Name));
    }

    [Fact]
    public void Move_ClampsAtEdges()
    {
        var items = new[] { Item("a"), Item("b") };

        Assert.Same(items, ItemUseCase.Move(items, 0, -1));
        Assert.Same(items, ItemUseCase.Move(items, 1, 1));
    }

    [Fact]
    public void ToggleSelect_FlipsOnlyTarget()
    {
        var result = ItemUseCase.ToggleSelect([Item("a"), Item("b")], 0);

        Assert.True(result[0].Selected);
        Assert.False(result[1].Selected);
    }

    [Fact]
    public void ToggleSelectAll_SelectsAllWhenNoneSelected()
    {
        var result = ItemUseCase.ToggleSelectAll([Item("a"), Item("b")]);

        Assert.All(result, i => Assert.True(i.Selected));
    }

    [Fact]
    public void ToggleSelectAll_DeselectsAllWhenAllSelected()
    {
        var items = new[] { Item("a") with { Selected = true }, Item("b") with { Selected = true } };

        var result = ItemUseCase.ToggleSelectAll(items);

        Assert.All(result, i => Assert.False(i.Selected));
    }

    internal sealed class FakeStore : IConfigStore
    {
        public List<IReadOnlyList<LaunchItem>> Saved { get; } = [];

        public IReadOnlyList<LaunchItem> ReadItems() => [];

        public AppSettings ReadSettings() => new();

        public void WriteItems(IReadOnlyList<LaunchItem> items) => Saved.Add(items);

        public void WriteSettings(AppSettings settings)
        {
        }
    }

    internal sealed class ThrowingStore : IConfigStore
    {
        public IReadOnlyList<LaunchItem> ReadItems() => [];

        public AppSettings ReadSettings() => new();

        public void WriteItems(IReadOnlyList<LaunchItem> items) =>
            throw new IOException("disk full");

        public void WriteSettings(AppSettings settings) =>
            throw new IOException("disk full");
    }
}
