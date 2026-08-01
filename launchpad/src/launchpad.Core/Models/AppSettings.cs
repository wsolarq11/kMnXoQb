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
    /// <summary>Object-initializer path (code-constructed settings).</summary>
    public AppSettings()
    {
    }

    /// <summary>Source-generated constructor binding; parameter defaults carry
    /// the legacy semantics (missing theme -> "system", missing history -> [])
    /// because init initializers do not run under constructor binding.</summary>
    [JsonConstructor]
    public AppSettings(
        bool confirmEnabled,
        string theme = "system",
        List<string>? launchHistory = null,
        WindowState? windowState = null)
    {
        ConfirmEnabled = confirmEnabled;
        Theme = theme;
        LaunchHistory = launchHistory ?? [];
        WindowState = windowState;
    }

    public bool ConfirmEnabled { get; init; }

    public string Theme { get; init; } = "system";

    public List<string> LaunchHistory { get; init; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public WindowState? WindowState { get; init; }

    // set (not init) because System.Text.Json source generation binds extension
    // data through the property setter; init-only extension data fails with
    // "cannot bind with a parameter in the deserialization constructor".
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? UnknownFields { get; set; }
}
