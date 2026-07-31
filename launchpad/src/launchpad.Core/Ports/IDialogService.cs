using Launchpad.Core.Models;

namespace Launchpad.Core.Ports;

/// <summary>Modal confirmation for launches, shown before spawning when policy demands.</summary>
public interface IDialogService
{
    Task<bool> ConfirmLaunchAsync(LaunchItem item, string? dangerReason);

    /// <summary>Batch confirmation listing every item that needs confirmation.</summary>
    Task<bool> ConfirmBatchAsync(IReadOnlyList<LaunchItem> items);
}
