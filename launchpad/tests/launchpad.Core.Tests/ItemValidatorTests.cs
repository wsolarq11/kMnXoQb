using Launchpad.Core.Domain;
using Launchpad.Core.Localization;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class ItemValidatorTests
{
    [Fact]
    public void Validate_AcceptsFilledForm()
    {
        var result = ItemValidator.Validate("snow", "snow");

        Assert.True(result.IsValid);
        Assert.Null(result.NameError);
        Assert.Null(result.CommandError);
    }

    [Fact]
    public void Validate_RejectsBlankName()
    {
        var result = ItemValidator.Validate("   ", "snow");

        Assert.False(result.IsValid);
        Assert.Equal(LanguageKey.ValidationNameRequired, result.NameError);
    }

    [Fact]
    public void Validate_RejectsBlankCommand()
    {
        var result = ItemValidator.Validate("snow", "");

        Assert.False(result.IsValid);
        Assert.Equal(LanguageKey.ValidationCommandRequired, result.CommandError);
    }

    [Fact]
    public void Validate_RejectsBothBlank()
    {
        var result = ItemValidator.Validate(null, null);

        Assert.False(result.IsValid);
        Assert.NotNull(result.NameError);
        Assert.NotNull(result.CommandError);
    }
}
