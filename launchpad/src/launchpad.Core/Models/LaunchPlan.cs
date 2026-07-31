namespace Launchpad.Core.Models;

/// <summary>
/// Pure decision produced by <c>LaunchPlanner</c>: what executable to spawn with which argv.
/// In-memory only; never serialized.
/// </summary>
public sealed record LaunchPlan
{
    public required string Executable { get; init; }

    public required IReadOnlyList<string> Args { get; init; }

    public required string WorkingDirectory { get; init; }

    public bool IsDangerous { get; init; }

    public string? TerminalOverride { get; init; }
}
