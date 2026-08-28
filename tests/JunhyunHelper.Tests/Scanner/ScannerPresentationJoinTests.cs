using JunhyunHelper.Core.Items;
using JunhyunHelper.Core.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerPresentationJoinTests
{
    [Fact]
    public void ResolveJoinsEverySecondaryValueByConfirmedItemId()
    {
        var target = new ScannerCatalogItem(
            "item-a",
            "공식 A",
            "A",
            "https://scanner.test/a.png",
            FleaAveragePrice: 123_456,
            BestTraderSellPrice: 80_000,
            Width: 2,
            Height: 3)
        {
            BestTraderId = "trader-a",
            BestTraderName = "상인 A",
            FleaMinimumPrice = 111_000,
        };
        var canonicalItems = new[]
        {
            Item("item-b", "https://canonical.test/b.png", "https://wiki.test/b"),
            Item("item-a", "https://canonical.test/a.png", "https://wiki.test/a"),
        };
        var neededItems = new[]
        {
            (ItemId: "item-b", RemainingTotal: 99),
            (ItemId: "item-a", RemainingTotal: 7),
        };

        var result = ScannerPresentationJoin.Resolve(target, canonicalItems, neededItems);

        Assert.Equal("item-a", result.ItemId);
        Assert.Equal("공식 A", result.OfficialName);
        Assert.Equal("https://canonical.test/a.png", result.IconUrl);
        Assert.Equal("https://wiki.test/a", result.WikiUrl);
        Assert.Equal(80_000, result.TraderSellPrice);
        Assert.Equal(123_456, result.FleaAveragePrice);
        Assert.Equal(111_000, result.FleaMinimumPrice);
        Assert.Equal(13_333, result.TraderPricePerSlot);
        Assert.Equal(20_576, result.FleaPricePerSlot);
        Assert.Equal(6, result.Slots);
        Assert.Equal(7, result.CurrentNeeded);
        Assert.Equal("상인 A", result.BestTraderName);
    }

    [Fact]
    public void ResolveUsesRemainingNeedRatherThanOriginalRequirement()
    {
        var target = new ScannerCatalogItem(
            "item-a",
            "공식 A",
            "A",
            "https://scanner.test/a.png",
            FleaAveragePrice: null,
            BestTraderSellPrice: null,
            Width: 1,
            Height: 1);

        // The presentation join receives the inventory-adjusted remaining amount from
        // NeededItem. A total requirement of 10 with 6 already owned must therefore be
        // represented by the remaining value 4, never by the original requirement 10.
        var result = ScannerPresentationJoin.Resolve(
            target,
            [Item("item-a", "https://canonical.test/a.png", "https://wiki.test/a")],
            [(ItemId: "item-a", RemainingTotal: 4)]);

        Assert.Equal(4, result.CurrentNeeded);
    }

    [Fact]
    public void ResolveUsesCatalogIconAndZeroNeededWhenCanonicalOrPlanEntryIsAbsent()
    {
        var target = new ScannerCatalogItem(
            "item-a",
            "공식 A",
            "A",
            "https://scanner.test/a.png",
            FleaAveragePrice: null,
            BestTraderSellPrice: null,
            Width: 1,
            Height: 1);

        var result = ScannerPresentationJoin.Resolve(
            target,
            [Item("item-b", "https://canonical.test/b.png", "https://wiki.test/b")],
            [(ItemId: "item-b", RemainingTotal: 5)]);

        Assert.Equal("item-a", result.ItemId);
        Assert.Equal("https://scanner.test/a.png", result.IconUrl);
        Assert.Null(result.WikiUrl);
        Assert.Equal(0, result.CurrentNeeded);
        Assert.Null(result.TraderSellPrice);
        Assert.Null(result.FleaAveragePrice);
        Assert.Null(result.FleaMinimumPrice);
    }

    private static GameItem Item(string id, string iconUrl, string wikiUrl) =>
        new(
            id,
            $"이름 {id}",
            $"Name {id}",
            null,
            null,
            iconUrl,
            wikiUrl,
            Array.Empty<string>());
}
