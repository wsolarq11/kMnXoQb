using Launchpad.Core.Localization;
using Xunit;

namespace Launchpad.Core.Tests;

public sealed class TranslationsTests
{
    [Fact]
    public void EveryKey_ExistsInBothLanguages()
    {
        foreach (var key in Enum.GetValues<LanguageKey>())
        {
            Assert.False(string.IsNullOrWhiteSpace(Translations.T(key, AppLanguage.ZhCn)), $"zh-CN missing {key}");
            Assert.False(string.IsNullOrWhiteSpace(Translations.T(key, AppLanguage.EnUs)), $"en-US missing {key}");
        }
    }

    [Fact]
    public void Resolve_MapSettingsValues()
    {
        Assert.Equal(AppLanguage.Auto, Translations.Resolve(null));
        Assert.Equal(AppLanguage.Auto, Translations.Resolve("auto"));
        Assert.Equal(AppLanguage.Auto, Translations.Resolve("unknown-value"));
        Assert.Equal(AppLanguage.ZhCn, Translations.Resolve("zh-CN"));
        Assert.Equal(AppLanguage.EnUs, Translations.Resolve("en-US"));
    }

    [Theory]
    [InlineData("zh-CN", AppLanguage.ZhCn)]
    [InlineData("zh-Hans-CN", AppLanguage.ZhCn)]
    [InlineData("en-US", AppLanguage.EnUs)]
    [InlineData("fr-FR", AppLanguage.EnUs)]
    [InlineData(null, AppLanguage.EnUs)]
    public void FromSystemLanguage_MapsFirstPreferredLanguage(string? system, AppLanguage expected)
    {
        Assert.Equal(expected, Translations.FromSystemLanguage(system));
    }

    [Fact]
    public void Effective_AutoFollowsSystem()
    {
        Assert.Equal(AppLanguage.ZhCn, Translations.Effective(AppLanguage.Auto, AppLanguage.ZhCn));
        Assert.Equal(AppLanguage.EnUs, Translations.Effective(AppLanguage.Auto, AppLanguage.EnUs));
        Assert.Equal(AppLanguage.ZhCn, Translations.Effective(AppLanguage.ZhCn, AppLanguage.EnUs));
        Assert.Equal(AppLanguage.EnUs, Translations.Effective(AppLanguage.EnUs, AppLanguage.ZhCn));
    }

    [Fact]
    public void T_UnknownKeyResolution_Throws()
    {
        // A key missing from one table would surface at runtime; the
        // completeness test above is the guard, this pins the failure mode.
        Assert.Throws<KeyNotFoundException>(() => Translations.T((LanguageKey)999, AppLanguage.ZhCn));
    }

    [Fact]
    public void Format_FillsPlaceholders()
    {
        var zh = Translations.Format(LanguageKey.DialogDeleteItemMessage, AppLanguage.ZhCn, "snow");
        Assert.Contains("snow", zh);

        var en = Translations.Format(LanguageKey.DialogBatchTitle, AppLanguage.EnUs, 3);
        Assert.Equal("Confirm 3 launches", en);
    }
}
