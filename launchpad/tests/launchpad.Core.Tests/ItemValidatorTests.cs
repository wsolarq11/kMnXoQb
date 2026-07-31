using Launchpad.Core.Domain;
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
        Assert.Equal("Name is required", result.NameError);
    }

    [Fact]
    public void Validate_RejectsBlankCommand()
    {
        var result = ItemValidator.Validate("snow", "");

        Assert.False(result.IsValid);
        Assert.Equal("Command is required", result.CommandError);
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
