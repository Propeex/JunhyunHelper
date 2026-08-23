using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerOcrCatalogAlphabetTests
{
    [Fact]
    public void Assess_CatalogImpossibleUnicodeLetter_IsNotTrustedAsIdentityCharacter()
    {
        var policy = CreatePolicy("Rotor 필터", "Motor 모터");

        var result = policy.Assess("RØtor 필터");

        Assert.True(result.HasPlausibleVariant);
        Assert.Equal("Rtor 필터", result.FilteredText);
        Assert.Equal("R?tor 필터", result.UnknownGlyphPatternText);
        Assert.Equal(1, result.InvalidCharacterCount);
        Assert.Equal(1, result.UnknownGlyphCount);
        Assert.True(result.HasUnknownGlyphPattern);
    }

    [Fact]
    public void Assess_TwoCatalogImpossibleEmbeddedGlyphs_PreservesBoundedUnknownPattern()
    {
        var policy = CreatePolicy(
            "Grizzly 응급 치료 키트",
            "Salewa 응급 처치 키트");

        var result = policy.Assess("G「izz※y 응급 치료 키트");

        Assert.True(result.HasPlausibleVariant);
        Assert.Equal("Gizzy 응급 치료 키트", result.FilteredText);
        Assert.Equal("G?izz?y 응급 치료 키트", result.UnknownGlyphPatternText);
        Assert.Equal(2, result.InvalidCharacterCount);
        Assert.Equal(2, result.UnknownGlyphCount);
    }

    [Fact]
    public void UnknownGlyphRecovery_ShortUniquePattern_RecoversWithoutGuessingCharacter()
    {
        var catalog = new[]
        {
            Item("1", "Sugar"),
            Item("2", "Super"),
            Item("3", "Spear"),
        };

        var result = ScannerUnknownGlyphCatalogRecovery.TryRecover(
            "Suga?",
            catalog,
            ScannerRecognition.Failed("UNKNOWN_GLYPH_NO_CANDIDATE"));

        Assert.True(result.Success);
        Assert.Equal("UNKNOWN_GLYPH_1_CATALOG", result.Reason);
        Assert.Equal("1", result.ItemId);
        Assert.Equal("Sugar", result.OfficialName);
    }

    [Fact]
    public void UnknownGlyphRecovery_TwoUnknownsWithLongContext_RecoversUniqueCatalogName()
    {
        var catalog = new[]
        {
            Item("1", "Grizzly 응급 치료 키트"),
            Item("2", "Salewa 응급 처치 키트"),
            Item("3", "AI-2 휴대용 응급 키트"),
        };

        var result = ScannerUnknownGlyphCatalogRecovery.TryRecover(
            "G?izz?y 응급 치료 키트",
            catalog,
            ScannerRecognition.Failed("NO_UNKNOWN_GLYPH_PATTERN"));

        Assert.True(result.Success);
        Assert.Equal("UNKNOWN_GLYPH_2_CATALOG", result.Reason);
        Assert.Equal("1", result.ItemId);
    }

    [Fact]
    public void UnknownGlyphRecovery_AmbiguousExactPattern_FailsClosed()
    {
        var catalog = new[]
        {
            Item("1", "Sugar"),
            Item("2", "Sugax"),
        };
        var ordinary = ScannerRecognition.Failed("UNKNOWN_GLYPH_AMBIGUOUS");

        var result = ScannerUnknownGlyphCatalogRecovery.TryRecover(
            "Suga?",
            catalog,
            ordinary);

        Assert.False(result.Success);
        Assert.Same(ordinary, result);
    }

    private static ScannerOcrCharacterPolicy CreatePolicy(params string[] names)
    {
        var policy = new ScannerOcrCharacterPolicy();
        policy.ReplaceCatalog(names.Select((name, index) => Item(index.ToString(), name)));
        return policy;
    }

    private static ScannerCatalogItem Item(string id, string name) =>
        new(id, name, name, null, null, null, 1, 1);
}
