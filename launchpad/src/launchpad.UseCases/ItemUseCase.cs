using ErrorOr;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;

namespace Launchpad.UseCases;

/// <summary>
/// Item list orchestration: load/save through the store port; all list
/// mutations are pure (return new lists, never mutate input).
/// Persist failures return structured <see cref="ErrorOr{T}"/> errors so the UI
/// never silently drops a save.
/// </summary>
public sealed class ItemUseCase(IConfigStore store)
{
    public IReadOnlyList<LaunchItem> LoadItems() => store.ReadItems();

    public ErrorOr<Success> SaveItems(IReadOnlyList<LaunchItem> items)
    {
        try
        {
            store.WriteItems(items);
            return Result.Success;
        }
        catch (Exception e)
        {
            return StoreErrors.WriteFailed("config.json", e.Message);
        }
    }

    public static LaunchItem NewItem(string name, string directory, string command, bool confirm, string? terminal, IReadOnlyList<LaunchItem> existing)
    {
        return new LaunchItem
        {
            Name = name,
            Directory = directory,
            Command = command,
            Confirm = confirm,
            Id = GenerateId(existing, name),
            Selected = false,
            Terminal = string.IsNullOrWhiteSpace(terminal) ? null : terminal.Trim(),
        };
    }

    /// <summary>
    /// Legacy-compatible id derivation: lowercase, spaces to underscores, and a
    /// numeric suffix when the base id collides with an existing item.
    /// </summary>
    public static string GenerateId(IReadOnlyList<LaunchItem> items, string name)
    {
        var baseId = name.Trim().ToLowerInvariant().Replace(' ', '_');
        if (!items.Any(i => i.Id == baseId))
        {
            return baseId;
        }

        for (var n = 2; ; n++)
        {
            var candidate = $"{baseId}_{n}";
            if (!items.Any(i => i.Id == candidate))
            {
                return candidate;
            }
        }
    }

    public static IReadOnlyList<LaunchItem> Filter(IReadOnlyList<LaunchItem> items, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return items;
        }

        return items.Where(i =>
            i.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            i.Directory.Contains(query, StringComparison.OrdinalIgnoreCase) ||
            i.Command.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();
    }

    public static IReadOnlyList<LaunchItem> Upsert(IReadOnlyList<LaunchItem> items, LaunchItem item, int? index)
    {
        var list = items.ToList();
        if (index is null)
        {
            list.Add(item);
        }
        else if (index >= 0 && index < list.Count)
        {
            list[index.Value] = item;
        }

        return list;
    }

    public static IReadOnlyList<LaunchItem> Delete(IReadOnlyList<LaunchItem> items, int index)
    {
        var list = items.ToList();
        if (index >= 0 && index < list.Count)
        {
            list.RemoveAt(index);
        }

        return list;
    }

    public static IReadOnlyList<LaunchItem> Move(IReadOnlyList<LaunchItem> items, int index, int delta)
    {
        var target = index + delta;
        if (index < 0 || index >= items.Count || target < 0 || target >= items.Count)
        {
            return items;
        }

        var list = items.ToList();
        (list[index], list[target]) = (list[target], list[index]);
        return list;
    }

    public static IReadOnlyList<LaunchItem> ToggleSelect(IReadOnlyList<LaunchItem> items, int index)
    {
        if (index < 0 || index >= items.Count)
        {
            return items;
        }

        return items.Select((item, i) =>
            i == index ? item with { Selected = !item.Selected } : item).ToList();
    }

    public static IReadOnlyList<LaunchItem> ToggleSelectAll(IReadOnlyList<LaunchItem> items)
    {
        var selectAll = items.Any(i => !i.Selected);
        return items.Select(i => i with { Selected = selectAll }).ToList();
    }
}
