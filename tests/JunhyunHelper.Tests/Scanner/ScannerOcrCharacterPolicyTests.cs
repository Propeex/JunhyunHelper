using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerOcrCharacterPolicyTests
{
    [Fact]
    public void Assess_MixedKoreanLatinAndCatalogPunctuation_IsAccepted()
    {
        var policy = CreatePolicy(
            "Water 0.6L 물병",
            "AK-74N 5.45x39 돌격소총");

        var result = policy.Assess("Water 0.6L 물병");

        Assert.True(result.HasPlausibleVariant);
        Assert.False(result.IsCorrupted);
        Assert.Equal("Water 0.6L 물병", result.FilteredText);
        Assert.Equal(string.Empty, result.UnknownGlyphPatternText);
        Assert.Equal(0, result.HanCharacterCount);
    }

    [Fact]
    public void Assess_HanIdeographVariant_IsRejected()
    {
        var policy = CreatePolicy("Water 0.6L 물병");

        var result = policy.Assess("Wa水er 0.6L 물병");

        Assert.False(result.HasPlausibleVariant);
        Assert.True(result.IsCorrupted);
        Assert.Equal(1, result.HanCharacterCount);
        Assert.Equal(string.Empty, result.FilteredText);
    }

    [Fact]
    public void Assess_BadVariantPlusExactVariant_PreservesOnlyExactVariant()
    {
        var policy = CreatePolicy("Water 0.6L 물병");

        var result = policy.Assess("Wa水er 0.6L 물병 | Water 0.6L 물병");

        Assert.True(result.HasPlausibleVariant);
        Assert.Equal("Water 0.6L 물병", result.FilteredText);
        Assert.Equal(1, result.HanCharacterCount);
    }

    [Fact]
    public void Assess_UnknownSymbolInShortTitle_FailsClosed()
    {
        var policy = CreatePolicy("CPU", "GPU");

        var result = policy.Assess("C※U");

        Assert.False(result.HasPlausibleVariant);
        Assert.False(result.HasSingleUnknownGlyphPattern);
        Assert.True(result.IsCorrupted);
    }

    [Fact]
    public void Assess_CharactersBecomeAllowedWhenPresentInCurrentCatalog()
    {
        var policy = CreatePolicy("M4A1 (FDE) / 5.56x45mm");

        var result = policy.Assess("M4A1 (FDE) / 5.56x45mm");

        Assert.True(result.HasPlausibleVariant);
        Assert.Equal("M4A1 (FDE) / 5.56x45mm", result.FilteredText);
        Assert.Equal(1.0, result.ValidCharacterRatio, 6);
    }

    [Fact]
    public void Assess_ImpossibleJapaneseBracket_IsRemovedButPreservedAsUnknownEmbeddedGlyph()
    {
        var policy = CreatePolicy("Thermite 테르밋", "Esmarch 에스마르호 지혈대");

        var result = policy.Assess("Esma「ch 에스마르호 지혈대");

        Assert.True(result.HasPlausibleVariant);
        Assert.True(result.HasSingleUnknownGlyphPattern);
        Assert.Equal("Esmach 에스마르호 지혈대", result.FilteredText);
        Assert.Equal("Esma?ch 에스마르호 지혈대", result.UnknownGlyphPatternText);
        Assert.Equal(1, result.InvalidCharacterCount);
        Assert.Equal(1, result.UnknownGlyphCount);
        Assert.Equal(0, result.HanCharacterCount);
    }

    [Fact]
    public void Assess_LeadingGarbageNeverBecomesUnknownGlyph()
    {
        var policy = CreatePolicy("Thermite 테르밋");

        var result = policy.Assess("` The「mite 테르밋");

        Assert.True(result.HasPlausibleVariant);
        Assert.Equal("Themite 테르밋", result.FilteredText);
        Assert.Equal("The?mite 테르밋", result.UnknownGlyphPatternText);
        Assert.Equal(2, result.InvalidCharacterCount);
        Assert.Equal(1, result.UnknownGlyphCount);
    }

    [Fact]
    public void Assess_CatalogQuoteSurvivesWhileImpossibleBracketIsRemoved()
    {
        var policy = CreatePolicy("Gunpowder \"Eagle\" 화약", "AK-74N 5.45x39 돌격소총");

        var result = policy.Assess("` Gunpowde「 \"EagIen\" 화약");

        Assert.True(result.HasPlausibleVariant);
        Assert.Equal("Gunpowde \"EagIen\" 화약", result.FilteredText);
        Assert.DoesNotContain('「', result.FilteredText);
        Assert.DoesNotContain('`', result.FilteredText);
        Assert.Contains('"', result.FilteredText);
        // The bracket is followed by whitespace, so it is not evidence for one missing
        // embedded Latin glyph and cannot enter the wildcard recovery path.
        Assert.False(result.HasSingleUnknownGlyphPattern);
    }

    [Fact]
    public void Assess_SymbolWhitelistFollowsCurrentCatalogReplacement()
    {
        var policy = CreatePolicy("M4A1 (FDE)");
        Assert.Contains('(', policy.Assess("M4A1 (FDE)").FilteredText);

        policy.ReplaceCatalog([
            new ScannerCatalogItem("1", "Thermite 테르밋", "Thermite", null, null, null, 1, 1),
        ]);

        var result = policy.Assess("Thermite (테르밋)");
        Assert.Equal("Thermite 테르밋", result.FilteredText);
    }

    private static ScannerOcrCharacterPolicy CreatePolicy(params string[] names)
    {
        var policy = new ScannerOcrCharacterPolicy();
        policy.ReplaceCatalog(names.Select((name, index) =>
            new ScannerCatalogItem(index.ToString(), name, name, null, null, null, 1, 1)));
        return policy;
    }
}
