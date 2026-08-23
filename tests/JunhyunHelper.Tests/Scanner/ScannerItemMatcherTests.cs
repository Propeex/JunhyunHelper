using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerItemMatcherTests
{
    [Fact]
    public void Resolve_ExactCurrentOfficialName_Succeeds()
    {
        var matcher = CreateMatcher(
            Item("water", "물병 Bottle of water (0.6L)"),
            Item("juice", "사과 주스 Apple juice"));

        var result = matcher.Resolve("물병 Bottle of water (0.6L)");

        Assert.True(result.Success);
        Assert.Equal("water", result.ItemId);
        Assert.Equal("EXACT", result.Reason);
    }

    [Fact]
    public void Resolve_SmallOcrError_WithClearMargin_Succeeds()
    {
        var matcher = CreateMatcher(
            Item("m4", "Colt M4A1 5.56x45 돌격소총"),
            Item("ak", "AK-74N 5.45x39 돌격소총"));

        var result = matcher.Resolve("Colt M4A1 5.56x45 들격소총");

        Assert.True(result.Success);
        Assert.Equal("m4", result.ItemId);
    }

    [Fact]
    public void Resolve_OneMissingGlyphAtNinetyPointNinePercent_UsesBoundedEditRecovery()
    {
        var matcher = CreateMatcher(
            Item("thermite", "Thermite 테르밋"),
            Item("thermometer", "Thermometer 온도계"),
            Item("thermal", "Thermal module 열 모듈"));

        var result = matcher.Resolve("Themite 테르밋");

        Assert.True(result.Success);
        Assert.Equal("thermite", result.ItemId);
        Assert.Equal("BOUNDED_EDIT_1", result.Reason);
        Assert.Equal(10.0 / 11.0, result.Confidence, 6);
    }

    [Fact]
    public void Resolve_ShortOneEditTitle_RemainsStrict()
    {
        var matcher = CreateMatcher(
            Item("cpu", "CPU"),
            Item("gpu", "GPU"));

        var result = matcher.Resolve("CPI");

        Assert.False(result.Success);
        Assert.NotEqual("BOUNDED_EDIT_1", result.Reason);
    }

    [Fact]
    public void Resolve_OneEditWithCloseRunnerUp_RemainsFailClosed()
    {
        var matcher = CreateMatcher(
            Item("alpha", "Thermite 테르밋"),
            Item("beta", "Thermita 테르밋"));

        var result = matcher.Resolve("Themite 테르밋");

        Assert.False(result.Success);
        Assert.NotEqual("BOUNDED_EDIT_1", result.Reason);
    }

    [Fact]
    public void Resolve_MultipleEditsAtLowEighties_DoesNotPassOcrAlone()
    {
        var matcher = CreateMatcher(
            Item("eagle", "Gunpowder \"Eagle\" 화약"),
            Item("kite", "Gunpowder \"Kite\" 화약"));

        var result = matcher.Resolve("Gunpowde EagIen 화약");

        Assert.False(result.Success);
        Assert.Equal("LOW_CONFIDENCE", result.Reason);
    }

    [Fact]
    public void Resolve_OldWaterName_DoesNotForceCurrentItem()
    {
        var matcher = CreateMatcher(
            Item("water", "물병 Bottle of water (0.6L)"),
            Item("aquamari", "Aquamari water bottle with filter"));

        var result = matcher.Resolve("Water 0.6L 물병");

        Assert.False(result.Success);
        Assert.NotEqual("EXACT", result.Reason);
    }

    [Fact]
    public void Resolve_DuplicateOfficialName_FailsClosed()
    {
        var matcher = CreateMatcher(
            Item("one", "같은 공식 이름"),
            Item("two", "같은 공식 이름"));

        var result = matcher.Resolve("같은 공식 이름");

        Assert.False(result.Success);
        Assert.Equal("AMBIGUOUS_OFFICIAL_NAME", result.Reason);
    }

    [Fact]
    public void Resolve_ShortNameContainedInLongerNoise_DoesNotUseSubstringShortcut()
    {
        var matcher = CreateMatcher(
            Item("short", "CPU"),
            Item("fan", "CPU fan electronic component"));

        var result = matcher.Resolve("CPU fan electronic component");

        Assert.True(result.Success);
        Assert.Equal("fan", result.ItemId);
    }

    private static ScannerItemMatcher CreateMatcher(params ScannerCatalogItem[] items)
    {
        var matcher = new ScannerItemMatcher();
        matcher.ReplaceCatalog(items);
        return matcher;
    }

    private static ScannerCatalogItem Item(string id, string name) =>
        new(id, name, name, null, null, null, 1, 1);
}
