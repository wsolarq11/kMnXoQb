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

    /// <summary>Prepend to launch history, capped at <paramref name="max"/> entries
    /// (matches legacy behavior: no deduplication).</summary>
    public static List<string> PushHistory(List<string> history, string name, int max = 10)
    {
        var combined = new List<string>(history);
        combined.Insert(0, name);
        return combined.Take(max).ToList();
    }
}
