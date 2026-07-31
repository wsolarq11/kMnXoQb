namespace Launchpad.Core.Domain;

/// <summary>Validation outcome for the edit dialog form.</summary>
public sealed record ItemValidationErrors(string? NameError, string? CommandError)
{
    public bool IsValid => NameError is null && CommandError is null;
}

/// <summary>Pure form validation; mirrors the Flutter edit dialog rules.</summary>
public static class ItemValidator
{
    public static ItemValidationErrors Validate(string? name, string? command)
    {
        var nameError = string.IsNullOrWhiteSpace(name) ? "Name is required" : null;
        var commandError = string.IsNullOrWhiteSpace(command) ? "Command is required" : null;
        return new ItemValidationErrors(nameError, commandError);
    }
}
