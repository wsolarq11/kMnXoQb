using Launchpad.Core.Localization;

namespace Launchpad.Core.Domain;

/// <summary>Validation outcome for the edit dialog form. Errors are
/// language-independent keys; the UI translates them.</summary>
public sealed record ItemValidationErrors(LanguageKey? NameError, LanguageKey? CommandError)
{
    public bool IsValid => NameError is null && CommandError is null;
}

/// <summary>Pure form validation; mirrors the Flutter edit dialog rules.</summary>
public static class ItemValidator
{
    public static ItemValidationErrors Validate(string? name, string? command)
    {
        var nameError = string.IsNullOrWhiteSpace(name) ? (LanguageKey?)LanguageKey.ValidationNameRequired : null;
        var commandError = string.IsNullOrWhiteSpace(command) ? (LanguageKey?)LanguageKey.ValidationCommandRequired : null;
        return new ItemValidationErrors(nameError, commandError);
    }
}
