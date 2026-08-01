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
    /// <summary>Loads items; a corrupt config.json (recovery attempt already
    /// happened inside the store) surfaces as a structured error instead of a
    /// startup crash, and the recovery note rides along for the status bar.
    /// ErrorOr's implicit conversion requires a concrete type, hence List.</summary>
    public (ErrorOr<List<LaunchItem>> Result, string? RecoveryNote) LoadItems()
    {
        try
        {
            ErrorOr<List<LaunchItem>> result = store.ReadItems().ToList();
            return (result, store.LastRecoveryNote);
        }
        catch (Launchpad.Core.Ports.ConfigParseException e)
        {
            return (StoreErrors.ReadFailed("config.json", e.Message), null);
        }
    }

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

    /// <summary>
    /// Sets the selection target state at <paramref name="index"/>. Target-state
    /// (not flip) semantics: the UI captures the checkbox state at click time and
    /// applies it, so binding-driven or stale re-invocations are idempotent.
    /// </summary>
    public static IReadOnlyList<LaunchItem> SetSelect(IReadOnlyList<LaunchItem> items, int index, bool target)
    {
        if (index < 0 || index >= items.Count)
        {
            return items;
        }

        return items.Select((item, i) =>
            i == index ? item with { Selected = target } : item).ToList();
    }

    /// <summary>
    /// Resolves by stable <paramref name="id"/> instead of reference: deferred
    /// commands may carry an item instance that was replaced by an earlier
    /// collection rebuild (records are immutable, so rebuilds create new
    /// instances). The id survives, so rapid double-toggles still apply.
    /// </summary>
    public static IReadOnlyList<LaunchItem> SetSelectById(IReadOnlyList<LaunchItem> items, string id, bool target)
    {
        for (var i = 0; i < items.Count; i++)
        {
            if (items[i].Id == id)
            {
                return SetSelect(items, i, target);
            }
        }

        return items;
    }

    /// <summary>Deselect everything. Legacy behavior: after a batch launch the
    /// selection is cleared so a second "Launch Selected" cannot re-fire the
    /// same terminals (archive/launchpad-rs app.rs batch_launch).</summary>
    public static IReadOnlyList<LaunchItem> ClearSelection(IReadOnlyList<LaunchItem> items)
    {
        if (items.All(i => !i.Selected))
        {
            return items;
        }

        return items.Select(i => i with { Selected = false }).ToList();
    }

    public static IReadOnlyList<LaunchItem> ToggleSelectAll(IReadOnlyList<LaunchItem> items)
    {
        var selectAll = items.Any(i => !i.Selected);
        return items.Select(i => i with { Selected = selectAll }).ToList();
    }
}
