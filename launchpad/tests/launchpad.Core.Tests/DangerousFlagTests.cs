using Launchpad.Core.Domain;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class DangerousFlagTests
{
    [Theory]
    [InlineData("codex --dangerously-bypass-approvals-and-sandbox")]
    [InlineData("npm i --yolo")]
    [InlineData("claude --dangerously-skip-permissions")]
    [InlineData("tool --bypass-approvals run")]
    [InlineData("tool --bypass-sandbox run")]
    [InlineData("tool --bypass.sandbox run")]
    public void IsDangerous_FlagsKnownDangerousCommands(string command)
    {
        Assert.True(DangerousFlagDetector.IsDangerous(command));
    }

    [Fact]
    public void IsDangerous_IsCaseInsensitive()
    {
        Assert.True(DangerousFlagDetector.IsDangerous("claude --DANGEROUSLY-skip-permissions"));
    }

    [Theory]
    [InlineData("snow")]
    [InlineData("opencode")]
    [InlineData("echo safe")]
    [InlineData("git status")]
    public void IsDangerous_DoesNotFlagSafeCommands(string command)
    {
        Assert.False(DangerousFlagDetector.IsDangerous(command));
    }

    [Fact]
    public void DangerousReason_ReturnsMatchingFlagReason()
    {
        var reason = DangerousFlagDetector.DangerousReason("claude --dangerously-skip-permissions");

        Assert.Equal("contains --dangerously flag", reason);
    }

    [Fact]
    public void DangerousReason_ReturnsNullForSafeCommand()
    {
        Assert.Null(DangerousFlagDetector.DangerousReason("snow"));
    }
}
