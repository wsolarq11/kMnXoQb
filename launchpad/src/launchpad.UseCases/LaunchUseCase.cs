using Launchpad.Core.Domain;
using Launchpad.Core.Models;
using Launchpad.Core.Ports;

namespace Launchpad.UseCases;

/// <summary>
/// Launch orchestration: confirmation policy, plan construction (via pure
/// <see cref="LaunchPlanner"/>), and actual spawn through ports.
/// </summary>
public sealed class LaunchUseCase(IProcessSpawner spawner, ITerminalDetector detector)
{
    public bool NeedsConfirm(AppSettings settings, LaunchItem item) =>
        settings.ConfirmEnabled && (item.Confirm || DangerousFlagDetector.IsDangerous(item.Command));

    public LaunchPlan Plan(LaunchItem item)
    {
        var wtAvailable = detector.TerminalAvailable("wt.exe");
        var pwshAvailable = detector.TerminalAvailable("pwsh.exe");
        return LaunchPlanner.PlanWindows(item, wtAvailable, pwshAvailable);
    }

    public void Launch(LaunchItem item) => spawner.Launch(Plan(item));

    /// <summary>Items that must be confirmed before launching (confirm on, or dangerous).</summary>
    public IReadOnlyList<LaunchItem> RequireConfirm(AppSettings settings, IReadOnlyList<LaunchItem> items) =>
        items.Where(i => NeedsConfirm(settings, i)).ToList();

    /// <summary>
    /// Batch launch with per-item error capture. Returns the count of successful
    /// launches and the indexes (into <paramref name="items"/>) that failed.
    /// </summary>
    public (int Succeeded, List<int> FailedIndexes) LaunchMany(IReadOnlyList<LaunchItem> items)
    {
        var succeeded = 0;
        var failedIndexes = new List<int>();
        for (var i = 0; i < items.Count; i++)
        {
            if (TryLaunch(items[i]) is null)
            {
                succeeded++;
            }
            else
            {
                failedIndexes.Add(i);
            }
        }

        return (succeeded, failedIndexes);
    }

    /// <summary>
    /// Spawn with error capture: returns a message on failure (invalid directory,
    /// missing terminal), null on success. The UI surfaces the message in the
    /// status bar instead of crashing the app.
    /// </summary>
    public string? TryLaunch(LaunchItem item)
    {
        try
        {
            spawner.Launch(Plan(item));
            return null;
        }
        catch (Exception ex)
        {
            return ex.Message;
        }
    }

    /// <summary>Prepend to launch history, capped at <paramref name="max"/> entries.
    /// Duplicates of the name are removed first (matches the legacy Rust behavior).</summary>
    public static List<string> PushHistory(List<string> history, string name, int max = 10)
    {
        var deduped = history.Where(h => h != name).ToList();
        deduped.Insert(0, name);
        return deduped.Take(max).ToList();
    }
}
