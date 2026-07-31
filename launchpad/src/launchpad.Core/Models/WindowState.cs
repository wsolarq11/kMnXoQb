using System.Text.Json.Serialization;

namespace Launchpad.Core.Models;

/// <summary>Last window position/size, restored on launch. Defaults match the legacy model.</summary>
public sealed record WindowState
{
    public int X { get; init; }

    public int Y { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public uint Width { get; init; } = 800;

    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public uint Height { get; init; } = 600;
}
