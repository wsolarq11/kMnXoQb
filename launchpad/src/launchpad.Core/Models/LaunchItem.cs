using System.Text.Json.Serialization;

namespace Launchpad.Core.Models;

/// <summary>
/// One launch item in the launcher list.
/// JSON shape mirrors the legacy serde model; keys are snake_case (all single-word,
/// so the naming policy yields lowercase), missing confirm defaults to true.
/// </summary>
public sealed record LaunchItem
{
    /// <summary>Object-initializer path (with-expressions, test builders).
    /// Required members are enforced by the compiler at construction sites.</summary>
    public LaunchItem()
    {
    }

    /// <summary>Source-generated constructor binding: JSON keys map to
    /// parameters (case-insensitive, snake_case). Parameter defaults carry the
    /// legacy semantics — a missing "confirm" keeps the default true — which
    /// the generated parameterless path would drop (init initializers do not
    /// run under constructor binding).</summary>
    [JsonConstructor]
    public LaunchItem(string name, string directory, string command, string id, bool confirm = true)
    {
        Name = name;
        Directory = directory;
        Command = command;
        Id = id;
        Confirm = confirm;
    }

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

    [JsonIgnore]
    public bool HasTag => Tag is not null;

    [JsonIgnore]
    public bool HasGroup => Group is not null;

    [JsonIgnore]
    public bool IsDangerous => Domain.DangerousFlagDetector.IsDangerous(Command);

    [JsonIgnore]
    public Localization.LanguageKey? DangerReason => Domain.DangerousFlagDetector.DangerousReason(Command);
}
