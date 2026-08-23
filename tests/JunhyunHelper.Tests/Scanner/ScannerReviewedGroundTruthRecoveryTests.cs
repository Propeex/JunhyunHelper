using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerReviewedGroundTruthRecoveryTests
{
    [Fact]
    public void TryRecover_EmelyaReviewedTwoEdit_RecoversUniqueTopCandidate()
    {
        var catalog = new[]
        {
            Item("emelya", "Emelya 에멜야 호밀 크루통"),
            Item("rye", "Rye 크루통"),
            Item("iskra", "Iskra 식량 팩"),
        };
        var matcher = Matcher(catalog);
        const string observed = "Emelya 에일야 호일 크루통";
        var ordinary = matcher.Resolve(observed);

        Assert.False(ordinary.Success);
        Assert.Equal("LOW_CONFIDENCE", ordinary.Reason);

        var recovered = ScannerReviewedGroundTruthRecovery.TryRecover(observed, catalog, ordinary);

        Assert.True(recovered.Success);
        Assert.Equal("emelya", recovered.ItemId);
        Assert.Equal("BOUNDED_EDIT_2_UNIQUE", recovered.Reason);
        Assert.Equal(12.0 / 14.0, recovered.Confidence, 6);
    }

    [Fact]
    public void TryRecover_GrizzlyReviewedThreeEdit_WithLongExactSuffix_RecoversUniqueTopCandidate()
    {
        var catalog = new[]
        {
            Item("grizzly", "Grizzly 응급 치료 키트"),
            Item("ai2", "AI-2 응급 치료 키트"),
            Item("salewa", "Salewa 응급 치료 키트"),
        };
        var matcher = Matcher(catalog);
        const string observed = "Giz기y 응급 치료 키트";
        var ordinary = matcher.Resolve(observed);

        Assert.False(ordinary.Success);
        Assert.Equal("LOW_CONFIDENCE", ordinary.Reason);
        Assert.Equal("grizzly", ordinary.ItemId);

        var recovered = ScannerReviewedGroundTruthRecovery.TryRecover(observed, catalog, ordinary);

        Assert.True(recovered.Success);
        Assert.Equal("grizzly", recovered.ItemId);
        Assert.Equal("SUFFIX_ANCHORED_EDIT_2_3", recovered.Reason);
        Assert.Equal(10.0 / 13.0, recovered.Confidence, 6);
    }

    [Fact]
    public void TryRecover_MultipleEditsWithoutLongSuffix_RemainsFailClosed()
    {
        var catalog = new[]
        {
            Item("eagle", "Gunpowder \"Eagle\" 화약"),
            Item("kite", "Gunpowder \"Kite\" 화약"),
        };
        var matcher = Matcher(catalog);
        const string observed = "Gunpowde EagIen 화약";
        var ordinary = matcher.Resolve(observed);

        Assert.False(ordinary.Success);
        var recovered = ScannerReviewedGroundTruthRecovery.TryRecover(observed, catalog, ordinary);

        Assert.False(recovered.Success);
        Assert.Equal("LOW_CONFIDENCE", recovered.Reason);
    }

    [Fact]
    public void TryRecover_TwoEditPatternSharedByTwoCatalogItems_RemainsFailClosed()
    {
        var catalog = new[]
        {
            Item("one", "Emelya 에멜야 호밀 크루통"),
            Item("two", "Emelya 에셀야 호밀 크루통"),
            Item("other", "Iskra 식량 팩"),
        };
        var matcher = Matcher(catalog);
        const string observed = "Emelya 에일야 호일 크루통";
        var ordinary = matcher.Resolve(observed);

        Assert.False(ordinary.Success);
        var recovered = ScannerReviewedGroundTruthRecovery.TryRecover(observed, catalog, ordinary);

        Assert.False(recovered.Success);
    }

    private static ScannerItemMatcher Matcher(IEnumerable<ScannerCatalogItem> items)
    {
        var matcher = new ScannerItemMatcher();
        matcher.ReplaceCatalog(items);
        return matcher;
    }

    private static ScannerCatalogItem Item(string id, string name) =>
        new(id, name, name, null, null, null, 1, 1);
}
