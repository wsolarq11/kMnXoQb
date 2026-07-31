using System.Text.Json;

namespace Launchpad.Core.Serialization;

/// <summary>
/// Shared serializer options for launcher config files.
/// snake_case keys keep byte-compatibility with the legacy Rust/serde files;
/// case-insensitive reads tolerate any casing from hand-edited files.
/// </summary>
public static class LauncherJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}
