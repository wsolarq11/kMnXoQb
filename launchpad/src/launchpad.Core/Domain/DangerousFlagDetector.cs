namespace Launchpad.Core.Domain;

/// <summary>
/// Flags a command as dangerous. Behavior mirrors the legacy Rust
/// <c>is_dangerous</c> exactly: case-insensitive substring match over a fixed table.
/// </summary>
public static class DangerousFlagDetector
{
    private static readonly (string Flag, string Reason)[] Flags =
    [
        ("dangerously", "contains --dangerously flag"),
        ("yolo", "contains --yolo flag"),
        ("skip-permissions", "contains --skip-permissions flag"),
        ("bypass-approvals", "contains --bypass-approvals flag"),
        ("bypass-sandbox", "contains --bypass-sandbox flag"),
        ("bypass.sandbox", "contains --bypass-sandbox flag"),
    ];

    public static bool IsDangerous(string command) =>
        Flags.Any(f => command.Contains(f.Flag, StringComparison.OrdinalIgnoreCase));

    public static string? DangerousReason(string command) =>
        Flags.FirstOrDefault(f => command.Contains(f.Flag, StringComparison.OrdinalIgnoreCase)).Reason;
}
