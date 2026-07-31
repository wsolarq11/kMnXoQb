using System.Text.Json;
using System.Text.Json.Serialization;

namespace Launchpad.Core.Models;

/// <summary>
/// App-wide settings, stored in settings.json.
/// Keys are snake_case via the shared naming policy; unknown keys are preserved
/// via <see cref="UnknownFields"/> so writes never drop data from future versions.
/// </summary>
public sealed record AppSettings
{
    public bool ConfirmEnabled { get; init; }

    public string Theme { get; init; } = "system";

    public List<string> LaunchHistory { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WindowState? WindowState { get; init; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? UnknownFields { get; init; }
}
