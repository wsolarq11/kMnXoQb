using System.Text.Json.Serialization;

namespace Launchpad.Core.Models;

/// <summary>
/// One launch item in the launcher list.
/// JSON shape mirrors the legacy serde model; keys are snake_case (all single-word,
/// so the naming policy yields lowercase), missing confirm defaults to true.
/// </summary>
public sealed record LaunchItem
{
    public required string Name { get; init; }

    public required string Directory { get; init; }

    public required string Command { get; init; }

    public bool Confirm { get; init; } = true;

    public required string Id { get; init; }

    public bool Selected { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Terminal { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Tag { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Group { get; init; }
}
