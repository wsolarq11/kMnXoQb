using System.Text.Json;
using Launchpad.Core.Models;
using Launchpad.Core.Serialization;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class JsonRoundTripTests
{
    private static readonly JsonSerializerOptions Options = LauncherJson.Options;

    [Fact]
    public void LaunchItem_RoundTrips_AllFields()
    {
        var item = new LaunchItem
        {
            Name = "claude test",
            Directory = @"D:\projects\demo",
            Command = "claude --dangerously-skip-permissions",
            Confirm = true,
            Id = "claude_test",
            Selected = true,
            Terminal = "pwsh",
            Tag = "ai",
            Group = "dev",
        };

        var json = JsonSerializer.Serialize(item, Options);
        var back = JsonSerializer.Deserialize<LaunchItem>(json, Options);

        Assert.Equal(item, back);
    }

    [Fact]
    public void LaunchItem_Json_OmitsNullOptionals()
    {
        var item = new LaunchItem
        {
            Name = "plain",
            Directory = @"D:\x",
            Command = "echo hi",
            Confirm = false,
            Id = "plain",
            Selected = false,
        };

        var json = JsonSerializer.Serialize(item);
        Assert.DoesNotContain("\"terminal\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"tag\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"group\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LaunchItem_Deserialize_MissingConfirmDefaultsToTrue()
    {
        const string json = """{"name":"n","directory":"d","command":"c","id":"i"}""";

        var item = JsonSerializer.Deserialize<LaunchItem>(json, Options);

        Assert.True(item!.Confirm);
    }

    [Fact]
    public void LaunchItem_Deserialize_MissingRequiredField_Throws()
    {
        const string json = """{"name":"n","directory":"d","command":"c"}""";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<LaunchItem>(json, Options));
    }

    [Fact]
    public void LaunchItem_Deserialize_MatchesLegacyConfigFileShape()
    {
        const string json = """
            [
              {
                "name": "snow-example",
                "directory": "D:\\projects\\your-project",
                "command": "snow",
                "confirm": false,
                "id": "snow-example",
                "selected": false
              },
              {
                "name": "codex-example",
                "directory": "D:\\projects\\your-project",
                "command": "codex --enable goals --dangerously-bypass-approvals-and-sandbox",
                "confirm": true,
                "id": "codex-example",
                "selected": false
              }
            ]
            """;

        var items = JsonSerializer.Deserialize<List<LaunchItem>>(json, Options);

        Assert.Equal(2, items!.Count);
        Assert.Equal("snow-example", items[0].Id);
        Assert.False(items[0].Confirm);
        Assert.True(items[1].Confirm);
        Assert.Null(items[1].Terminal);
    }

    [Fact]
    public void AppSettings_RoundTrips_WithSnakeCaseKeys()
    {
        var settings = new AppSettings
        {
            ConfirmEnabled = true,
            Theme = "dark",
            LaunchHistory = ["a", "b"],
            WindowState = new WindowState { X = 10, Y = 20, Width = 900, Height = 700 },
        };

        var json = JsonSerializer.Serialize(settings, Options);
        Assert.Contains("\"confirm_enabled\"", json);
        Assert.Contains("\"launch_history\"", json);
        Assert.Contains("\"window_state\"", json);

        var back = JsonSerializer.Deserialize<AppSettings>(json, Options);
        Assert.Equal(settings.ConfirmEnabled, back!.ConfirmEnabled);
        Assert.Equal(settings.Theme, back.Theme);
        Assert.Equal(settings.LaunchHistory, back.LaunchHistory);
        Assert.Equal(settings.WindowState, back.WindowState);
    }

    [Fact]
    public void AppSettings_Deserialize_MatchesLegacySettingsFileShape()
    {
        const string json = """
            {
              "confirm_enabled": false,
              "theme": "light",
              "launch_history": ["claude_x", "snow_y"]
            }
            """;

        var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);

        Assert.False(settings!.ConfirmEnabled);
        Assert.Equal("light", settings.Theme);
        Assert.Equal(["claude_x", "snow_y"], settings.LaunchHistory);
        Assert.Null(settings.WindowState);
    }

    [Fact]
    public void AppSettings_RoundTrip_PreservesUnknownFields()
    {
        const string json = """{"confirm_enabled": true, "future_field": 42}""";

        var settings = JsonSerializer.Deserialize<AppSettings>(json, Options);
        var json2 = JsonSerializer.Serialize(settings);

        Assert.Contains("\"future_field\":42", json2);
    }
}
