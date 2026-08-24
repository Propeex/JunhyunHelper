using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerOcrSubstitutionTests
{
    [Fact]
    public void ReplacesObservedGlyphWithoutChangingUnrelatedText()
    {
        var rules = new[]
        {
            new ScannerOcrSubstitutionRule { Source = "「", Replacement = "r" },
        };

        var result = ScannerOcrSubstitutionEngine.Apply("T「iton M43-A", rules);

        Assert.Equal("Triton M43-A", result.Text);
        Assert.Equal(1, result.ReplacementCount);
        Assert.Equal(["「"], result.AppliedSources);
    }

    [Fact]
    public void ReplacementOutputIsNotProcessedAgain()
    {
        var rules = new[]
        {
            new ScannerOcrSubstitutionRule { Source = "A", Replacement = "B" },
            new ScannerOcrSubstitutionRule { Source = "B", Replacement = "C" },
        };

        var result = ScannerOcrSubstitutionEngine.Apply("AB", rules);

        Assert.Equal("BC", result.Text);
        Assert.Equal(2, result.ReplacementCount);
    }

    [Fact]
    public void CyclicRulesRemainSinglePassAndFinite()
    {
        var rules = new[]
        {
            new ScannerOcrSubstitutionRule { Source = "A", Replacement = "B" },
            new ScannerOcrSubstitutionRule { Source = "B", Replacement = "A" },
        };

        var result = ScannerOcrSubstitutionEngine.Apply("ABBA", rules);

        Assert.Equal("BAAB", result.Text);
        Assert.Equal(4, result.ReplacementCount);
    }

    [Fact]
    public void LongestSourceWinsAtSameOriginalPosition()
    {
        var rules = new[]
        {
            new ScannerOcrSubstitutionRule { Source = "[", Replacement = "r" },
            new ScannerOcrSubstitutionRule { Source = "[[", Replacement = "n" },
        };

        var result = ScannerOcrSubstitutionEngine.Apply("A[[B", rules);

        Assert.Equal("AnB", result.Text);
        Assert.Equal(1, result.ReplacementCount);
    }

    [Fact]
    public void DisabledRuleDoesNotChangeEvidence()
    {
        var rules = new[]
        {
            new ScannerOcrSubstitutionRule { Enabled = false, Source = "「", Replacement = "r" },
        };

        var result = ScannerOcrSubstitutionEngine.Apply("T「iton", rules);

        Assert.Equal("T「iton", result.Text);
        Assert.False(result.Changed);
    }

    [Fact]
    public void EmptyReplacementCanRemoveKnownGarbageGlyph()
    {
        var rules = new[]
        {
            new ScannerOcrSubstitutionRule { Source = "¤", Replacement = string.Empty },
        };

        var result = ScannerOcrSubstitutionEngine.Apply("M¤4", rules);

        Assert.Equal("M4", result.Text);
        Assert.Equal(1, result.ReplacementCount);
    }
}
