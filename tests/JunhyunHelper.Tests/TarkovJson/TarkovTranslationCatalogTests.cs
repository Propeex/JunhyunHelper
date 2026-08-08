using System.Text.Json;
using JunhyunHelper.Infrastructure.TarkovJson;
using Xunit;

namespace JunhyunHelper.Tests.TarkovJson;

public sealed class TarkovTranslationCatalogTests
{
    [Fact]
    public void KoreanFallsBackToEnglishWithoutChangingSourceKey()
    {
        var korean = Catalog("{\"data\":{\"quest-name\":\"퀘스트 이름\"}}");
        var english = Catalog("{\"data\":{\"quest-name\":\"Quest Name\",\"fallback-only\":\"Fallback\"}}");
        var localization = new TarkovLocalization(korean, english);

        var translated = localization.Resolve("quest-name");
        var fallback = localization.Resolve("fallback-only");

        Assert.Equal("퀘스트 이름", translated.Korean);
        Assert.Equal("Quest Name", translated.English);
        Assert.Null(fallback.Korean);
        Assert.Equal("Fallback", fallback.English);
    }

    [Fact]
    public void MissingTranslationKeepsOriginalKeyAsEnglishFallback()
    {
        var localization = new TarkovLocalization();

        var result = localization.Resolve("stable-source-key");

        Assert.Null(result.Korean);
        Assert.Equal("stable-source-key", result.English);
    }

    private static TarkovTranslationCatalog Catalog(string json)
    {
        using var parsed = JsonDocument.Parse(json);
        return TarkovTranslationCatalog.FromDocument(
            TarkovJsonDocument.Parse(parsed.RootElement));
    }
}
