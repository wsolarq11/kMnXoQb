using Launchpad.Core.Domain;
using Launchpad.Core.Localization;
using Launchpad.Core.Models;
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
    public void DangerousReason_ReturnsMatchingFlagKey()
    {
        var reason = DangerousFlagDetector.DangerousReason("claude --dangerously-skip-permissions");

        Assert.Equal(LanguageKey.DangerReasonDangerously, reason);
    }

    [Fact]
    public void DangerousReason_ReturnsNullForSafeCommand()
    {
        Assert.Null(DangerousFlagDetector.DangerousReason("snow"));
    }

    [Fact]
    public void LaunchItem_ExposesDangerFlagsWithoutSerialization()
    {
        var item = new LaunchItem
        {
            Name = "codex",
            Directory = @"D:\x",
            Command = "codex --dangerously-bypass-approvals-and-sandbox",
            Confirm = false,
            Id = "codex",
        };

        Assert.True(item.IsDangerous);
        Assert.Equal(LanguageKey.DangerReasonDangerously, item.DangerReason);
    }

    [Fact]
    public void LaunchItem_DangerFlagsAreNotSerialized()
    {
        var item = new LaunchItem
        {
            Name = "codex",
            Directory = @"D:\x",
            Command = "codex --dangerously-bypass-approvals-and-sandbox",
            Confirm = false,
            Id = "codex",
        };

        var json = System.Text.Json.JsonSerializer.Serialize(item, Launchpad.Core.Serialization.LauncherJson.Options);

        Assert.DoesNotContain("\"is_dangerous\"", json);
        Assert.DoesNotContain("\"danger_reason\"", json);
    }
}
