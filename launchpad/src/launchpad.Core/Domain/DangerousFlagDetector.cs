using Launchpad.Core.Localization;

namespace Launchpad.Core.Domain;

/// <summary>
/// Flags a command as dangerous. Behavior mirrors the legacy Rust
/// <c>is_dangerous</c> exactly: case-insensitive substring match over a fixed table.
/// The reason is a language-independent <see cref="LanguageKey"/>; the UI
/// translates it with the current language.
/// </summary>
public static class DangerousFlagDetector
{
    private static readonly (string Flag, LanguageKey Reason)[] Flags =
    [
        ("dangerously", LanguageKey.DangerReasonDangerously),
        ("yolo", LanguageKey.DangerReasonYolo),
        ("skip-permissions", LanguageKey.DangerReasonSkipPermissions),
        ("bypass-approvals", LanguageKey.DangerReasonBypassApprovals),
        ("bypass-sandbox", LanguageKey.DangerReasonBypassSandbox),
        ("bypass.sandbox", LanguageKey.DangerReasonBypassSandbox),
    ];

    public static bool IsDangerous(string command) =>
        Flags.Any(f => command.Contains(f.Flag, StringComparison.OrdinalIgnoreCase));

    public static LanguageKey? DangerousReason(string command)
    {
        // Explicit loop, not FirstOrDefault: the default tuple for a value-type
        // Reason is (null, default(LanguageKey)) — a non-null key that would
        // mis-flag every safe command as dangerous.
        foreach (var (flag, reason) in Flags)
        {
            if (command.Contains(flag, StringComparison.OrdinalIgnoreCase))
            {
                return reason;
            }
        }

        return null;
    }
}
