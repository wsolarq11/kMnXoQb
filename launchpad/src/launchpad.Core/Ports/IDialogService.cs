using Launchpad.Core.Models;

namespace Launchpad.Core.Ports;

/// <summary>Modal confirmation for launches, shown before spawning when policy demands.</summary>
public interface IDialogService
{
    Task<bool> ConfirmLaunchAsync(LaunchItem item, string? dangerReason);

    /// <summary>Batch confirmation listing every item that needs confirmation.</summary>
    Task<bool> ConfirmBatchAsync(IReadOnlyList<LaunchItem> items);

    /// <summary>Confirmation before deleting an item (legacy behavior: the card
    /// Delete button asks "This cannot be undone" before removing).</summary>
    Task<bool> ConfirmDeleteAsync(LaunchItem item);
}
