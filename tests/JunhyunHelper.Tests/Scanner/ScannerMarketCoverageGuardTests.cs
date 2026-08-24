using JunhyunHelper.Core.Scanner;
using JunhyunHelper.Infrastructure.Scanner;
using Xunit;

namespace JunhyunHelper.Tests.Scanner;

public sealed class ScannerMarketCoverageGuardTests
{
    [Fact]
    public void SevereTraderCoverageDropIsRejected()
    {
        var baseline = CreateItems(4000, traderCount: 3200, fleaCount: 2800, slotCount: 3900);
        var candidate = CreateItems(4000, traderCount: 1500, fleaCount: 2800, slotCount: 3900);

        var result = ScannerMarketCoverageGuard.Assess(candidate, baseline);

        Assert.False(result.IsAcceptable);
        Assert.True(result.TraderPriceRegressed);
        Assert.False(result.FleaPriceRegressed);
        Assert.False(result.SlotCoverageRegressed);
    }

    [Fact]
    public void SevereFleaCoverageDropIsRejected()
    {
        var baseline = CreateItems(4000, traderCount: 3200, fleaCount: 2800, slotCount: 3900);
        var candidate = CreateItems(4000, traderCount: 3200, fleaCount: 1200, slotCount: 3900);

        var result = ScannerMarketCoverageGuard.Assess(candidate, baseline);

        Assert.False(result.IsAcceptable);
        Assert.True(result.FleaPriceRegressed);
    }

    [Fact]
    public void SevereSlotCoverageDropIsRejected()
    {
        var baseline = CreateItems(4000, traderCount: 3200, fleaCount: 2800, slotCount: 3900);
        var candidate = CreateItems(4000, traderCount: 3200, fleaCount: 2800, slotCount: 1900);

        var result = ScannerMarketCoverageGuard.Assess(candidate, baseline);

        Assert.False(result.IsAcceptable);
        Assert.True(result.SlotCoverageRegressed);
    }

    [Fact]
    public void OrdinaryMarketChurnIsAccepted()
    {
        var baseline = CreateItems(4000, traderCount: 3200, fleaCount: 2800, slotCount: 3900);
        var candidate = CreateItems(4000, traderCount: 2600, fleaCount: 2300, slotCount: 3600);

        var result = ScannerMarketCoverageGuard.Assess(candidate, baseline);

        Assert.True(result.IsAcceptable);
    }

    [Fact]
    public void SparseBaselineDoesNotCreateFalseRegressionAlarm()
    {
        var baseline = CreateItems(4000, traderCount: 500, fleaCount: 500, slotCount: 500);
        var candidate = CreateItems(4000, traderCount: 0, fleaCount: 0, slotCount: 0);

        var result = ScannerMarketCoverageGuard.Assess(candidate, baseline);

        Assert.True(result.IsAcceptable);
    }

    private static IReadOnlyList<ScannerCatalogItem> CreateItems(
        int count,
        int traderCount,
        int fleaCount,
        int slotCount) =>
        Enumerable.Range(0, count)
            .Select(index => new ScannerCatalogItem(
                $"item-{index}",
                $"Item {index}",
                $"I{index}",
                null,
                index < fleaCount ? 2000 + index : null,
                index < traderCount ? 1000 + index : null,
                index < slotCount ? 2 : 0,
                index < slotCount ? 2 : 0))
            .ToArray();
}
