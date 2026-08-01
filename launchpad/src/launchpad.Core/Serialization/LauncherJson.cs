using System.Text.Json;
using System.Text.Json.Serialization;
using Launchpad.Core.Models;

namespace Launchpad.Core.Serialization;

/// <summary>
/// Shared serializer options for launcher config files.
/// snake_case keys keep byte-compatibility with the legacy Rust/serde files;
/// case-insensitive reads tolerate any casing from hand-edited files.
/// Metadata comes from the source-generated <see cref="LaunchpadJsonContext"/>
/// (no runtime reflection): faster cold start, AOT/trim-friendly.
/// </summary>
public static class LauncherJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        TypeInfoResolver = LaunchpadJsonContext.Default,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };
}

/// <summary>
/// Source-generated (de)serialization metadata for the config model.
/// JsonExtensionData (UnknownFields round-trip) is supported by source gen.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
[JsonSerializable(typeof(LaunchItem))]
[JsonSerializable(typeof(LaunchItem[]))]
[JsonSerializable(typeof(List<LaunchItem>))]
[JsonSerializable(typeof(AppSettings))]
public sealed partial class LaunchpadJsonContext : JsonSerializerContext;
