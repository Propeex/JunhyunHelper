using JunhyunHelper.Core.Scanner;

namespace JunhyunHelper.Infrastructure.Scanner;

/// <summary>
/// Protects a previously healthy Scanner market cache from a structurally incomplete
/// upstream payload. Identity health remains independent: on a first install, missing
/// market fields still fail closed per item. The regression guard only applies when a
/// sufficiently populated last-known-good baseline exists for the same game mode.
/// </summary>
public static class ScannerMarketCoverageGuard
{
    private const int MinimumComparableCoverage = 1000;

    public static ScannerMarketCoverageAssessment Assess(
        IReadOnlyCollection<ScannerCatalogItem> candidate,
        IReadOnlyCollection<ScannerCatalogItem> baseline)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(baseline);

        var candidateTrader = candidate.Count(item => item.BestTraderSellPrice is > 0);
        var baselineTrader = baseline.Count(item => item.BestTraderSellPrice is > 0);
        var candidateFlea = candidate.Count(item => item.FleaAveragePrice is > 0);
        var baselineFlea = baseline.Count(item => item.FleaAveragePrice is > 0);
        var candidateSlots = candidate.Count(item => item.Slots > 0);
        var baselineSlots = baseline.Count(item => item.Slots > 0);

        var traderRegressed = IsSevereDrop(candidateTrader, baselineTrader);
        var fleaRegressed = IsSevereDrop(candidateFlea, baselineFlea);
        var slotRegressed = IsSevereDrop(candidateSlots, baselineSlots);

        return new ScannerMarketCoverageAssessment(
            !(traderRegressed || fleaRegressed || slotRegressed),
            candidateTrader,
            baselineTrader,
            candidateFlea,
            baselineFlea,
            candidateSlots,
            baselineSlots,
            traderRegressed,
            fleaRegressed,
            slotRegressed);
    }

    private static bool IsSevereDrop(int candidateCount, int baselineCount) =>
        baselineCount >= MinimumComparableCoverage &&
        candidateCount * 2 <= baselineCount;
}

public sealed record ScannerMarketCoverageAssessment(
    bool IsAcceptable,
    int CandidateTraderPriceCount,
    int BaselineTraderPriceCount,
    int CandidateFleaPriceCount,
    int BaselineFleaPriceCount,
    int CandidateSlotCount,
    int BaselineSlotCount,
    bool TraderPriceRegressed,
    bool FleaPriceRegressed,
    bool SlotCoverageRegressed);
